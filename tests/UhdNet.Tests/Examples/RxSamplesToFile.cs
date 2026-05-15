using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using UhdNet;
using UhdNet.Native;

namespace UhdNet.Tests.Examples;

/// <summary>
/// High-performance port of UHD's <c>rx_samples_to_file</c> example.
///
/// Architecture:
///   - NativeMemory ring buffer: sample buffers live entirely outside the GC heap
///     (64-byte aligned, no pinning needed in the recv path).
///   - GC.TryStartNoGCRegion: suspends GC for the duration of the stream so that
///     ReceiveFast ([SuppressGCTransition]) is always safe to call.
///   - Producer/consumer: recv thread (Highest priority) fills slots; a dedicated
///     writer thread (AboveNormal) drains them to disk — decoupling USB latency from I/O.
///   - Thread priority + uhd_set_thread_priority: elevates the recv thread in both
///     the .NET scheduler and the UHD driver thread pool.
///   - Linux thread affinity: recv thread is pinned to core 1 via sched_setaffinity
///     (core 0 is avoided as it handles hardware IRQs on many systems).
/// </summary>
internal static unsafe class RxSamplesToFile
{
    private const int    DefaultSpbMultiple = 8;          // SPB = MaxSamplesPerPacket × this
    private const int    DefaultRingSlots   = 8;
    private const long   NoGcHeapBudget     = 256L * 1024 * 1024;
    private const int    MaxTimeouts        = 10;

    // ─── Configuration ──────────────────────────────────────────────────────────

    private sealed class Config
    {
        public string DeviceArgs  = string.Empty;
        public string FilePath    = "samples.bin";
        public string Type        = "short";      // short|sc16 or float|fc32
        public string WireFmt     = "sc16";       // sc16, sc12, sc8
        public double Rate        = 1e6;
        public double Freq        = 0.0;
        public double Gain        = 0.0;
        public string Ant         = string.Empty;
        public double Bw          = 0.0;
        public long   NSamps      = 0;            // 0 = unlimited
        public double Duration    = 0.0;          // seconds; 0 = unlimited
        public int    Spb         = 0;            // 0 = auto
        public int    RingSlots   = DefaultRingSlots;
        public double SetupTime   = 1.0;
        public bool   Progress    = true;
        public bool   Stats       = true;
    }

    // ─── Ring slot ──────────────────────────────────────────────────────────────

    private struct Slot
    {
        public void* Ptr;       // NativeMemory pointer (null = not allocated)
        public nuint Bytes;     // allocated size
        public int   Samples;   // samples filled by producer
    }

    // ─── Linux thread affinity (sched_setaffinity with tid=0 = current thread) ──

    [DllImport("libc", EntryPoint = "sched_setaffinity", SetLastError = true)]
    private static extern int sched_setaffinity(int pid, nuint cpusetsize, byte* mask);

    // ─── Entry point ────────────────────────────────────────────────────────────

    public static int Run(string[] args)
    {
        Config cfg;
        try
        {
            cfg = ParseArgs(args);
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Argument error: {ex.Message}");
            PrintUsage();
            return 1;
        }

        try
        {
            Console.WriteLine($"Opening USRP: \"{cfg.DeviceArgs}\"");
            using var usrp = Usrp.Make(cfg.DeviceArgs);
            ConfigureUsrp(usrp, cfg);
            return RunStream(usrp, cfg);
        }
        catch (UhdException ex)
        {
            Console.Error.WriteLine($"UHD error [{ex.Code}]: {ex.Message}");
            return 3;
        }
    }

    // ─── Device configuration ────────────────────────────────────────────────────

    private static void ConfigureUsrp(Usrp usrp, Config cfg)
    {
        usrp.SetRxRate(cfg.Rate);
        Console.WriteLine($"Actual RX rate : {usrp.GetRxRate() / 1e6:F6} Msps");

        usrp.SetRxFrequency(new TuneRequest(cfg.Freq), 0, out var tune);
        Console.WriteLine($"Actual RX freq : {tune.ActualRfFrequency / 1e6:F6} MHz");

        usrp.SetRxGain(cfg.Gain);
        Console.WriteLine($"Actual RX gain : {usrp.GetRxGain():F1} dB");

        if (!string.IsNullOrEmpty(cfg.Ant))
            usrp.SetRxAntenna(cfg.Ant);
        Console.WriteLine($"RX antenna     : {usrp.GetRxAntenna()}");

        if (cfg.Bw > 0)
        {
            usrp.SetRxBandwidth(cfg.Bw);
            Console.WriteLine($"Actual RX BW   : {usrp.GetRxBandwidth() / 1e6:F3} MHz");
        }

        if (cfg.SetupTime > 0)
        {
            Console.WriteLine($"Settling for {cfg.SetupTime:F1} s...");
            Thread.Sleep(TimeSpan.FromSeconds(cfg.SetupTime));
        }
    }

    // ─── Stream setup ────────────────────────────────────────────────────────────

    private static int RunStream(Usrp usrp, Config cfg)
    {
        string cpuFmt        = cfg.Type is "short" or "sc16" ? "sc16" : "fc32";
        int    bytesPerSample = cpuFmt == "sc16" ? 4 : 8;

        using var rxStream = usrp.GetRxStream(new StreamArgs
        {
            CpuFormat = cpuFmt,
            OtwFormat = cfg.WireFmt,
            Channels  = new nuint[] { 0 },
        });

        nuint maxSpp  = rxStream.MaxSamplesPerPacket;
        int   spb     = cfg.Spb > 0 ? cfg.Spb : Math.Max(8192, (int)maxSpp * DefaultSpbMultiple);
        nuint slotBytes = (nuint)(spb * bytesPerSample);

        Console.WriteLine(
            $"MaxSamplesPerPacket={maxSpp}  SPB={spb} ({slotBytes / 1024.0:F1} KiB/slot)  ring={cfg.RingSlots}");

        // Allocate ring buffer outside the GC heap; 64-byte alignment = cache line / AVX-512
        var ring = new Slot[cfg.RingSlots];
        try
        {
            for (int i = 0; i < cfg.RingSlots; i++)
            {
                ring[i].Ptr   = NativeMemory.AlignedAlloc(slotBytes, 64);
                ring[i].Bytes = slotBytes;
            }

            // Suspend GC for the duration of streaming so ReceiveFast ([SuppressGCTransition])
            // is always safe to call.  disallowFullBlockingGC=false lets the runtime do a full
            // GC if the budget is exceeded rather than throwing.
            bool noGc = GC.TryStartNoGCRegion(NoGcHeapBudget, disallowFullBlockingGC: false);
            if (!noGc)
                Console.WriteLine("Warning: GC.TryStartNoGCRegion failed — GC may cause latency spikes.");

            try
            {
                return DoStream(rxStream, ring, (nuint)spb, bytesPerSample, cfg);
            }
            finally
            {
                if (noGc)
                    try { GC.EndNoGCRegion(); } catch { /* already ended */ }
            }
        }
        finally
        {
            for (int i = 0; i < cfg.RingSlots; i++)
                if (ring[i].Ptr != null)
                    NativeMemory.AlignedFree(ring[i].Ptr);
        }
    }

    // ─── Pipeline ────────────────────────────────────────────────────────────────

    private static int DoStream(
        RxStreamer rxStream,
        Slot[]     ring,
        nuint      spb,
        int        bytesPerSample,
        Config     cfg)
    {
        int n = cfg.RingSlots;

        // freeSlots:  slot indices available for the recv thread to fill
        // readySlots: slot indices filled and waiting to be written
        //
        // Invariant: |free| + |ready| + |in-flight| == n  at all times.
        // Consequence: TryWrite to either channel always succeeds when the slot was
        // just taken from the other channel — capacity n is never exceeded.
        var freeSlots = Channel.CreateBounded<int>(new BoundedChannelOptions(n)
        {
            FullMode    = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });
        var readySlots = Channel.CreateBounded<int>(new BoundedChannelOptions(n)
        {
            FullMode    = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });

        for (int i = 0; i < n; i++)
            freeSlots.Writer.TryWrite(i);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nInterrupted.");
        };

        using var outFile = new FileStream(
            cfg.FilePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 65536, FileOptions.None);

        var sw = Stopwatch.StartNew();
        Exception? writerErr = null;

        // Writer thread: drains readySlots to disk, returns indices to freeSlots
        var writerThread = new Thread(() =>
        {
            try   { WriteWorker(outFile, ring, readySlots.Reader, freeSlots.Writer, bytesPerSample, cts.Token); }
            catch (OperationCanceledException) { }
            catch (Exception ex)               { Volatile.Write(ref writerErr, ex); cts.Cancel(); }
        })
        {
            Name       = "uhd-file-writer",
            Priority   = ThreadPriority.AboveNormal,
            IsBackground = true,
        };
        writerThread.Start();

        // Recv thread (current thread): elevate priority, pin to core, ask UHD driver to do the same
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        _ = NativeMethods.uhd_set_thread_priority(1.0f, realtime: true);
        TryPinCurrentThreadToCore(1); // core 1: avoids core 0 which owns most hardware IRQs

        long totalSamples = 0;
        long overflows    = 0;

        try
        {
            RecvWorker(rxStream, cfg, ring, freeSlots.Reader, freeSlots.Writer, readySlots.Writer,
                       spb, cts.Token, ref totalSamples, ref overflows, sw);
        }
        finally
        {
            readySlots.Writer.Complete(); // signal writer to drain and exit
            writerThread.Join(TimeSpan.FromSeconds(30));
        }

        sw.Stop();

        if (writerErr != null)
        {
            Console.Error.WriteLine($"Writer error: {writerErr.Message}");
            return 4;
        }

        if (cfg.Stats)
            PrintStats(totalSamples, overflows, bytesPerSample, sw.Elapsed);

        return 0;
    }

    // ─── Recv worker ─────────────────────────────────────────────────────────────
    //
    // Hot-path constraints:
    //   - All sample buffers are NativeMemory: no managed heap, no GC pressure.
    //   - ReceiveFast uses [SuppressGCTransition]: safe inside GC.TryStartNoGCRegion.
    //   - Error code is read via NativeMethods directly: no managed property dispatch.
    //   - buffs[] is stackalloc'd once before the loop; only buffs[0] is updated.
    //   - mdHandle is a value copy of the opaque metadata pointer; UHD fills the
    //     pointed-to native struct in place — the handle value itself never changes.

    private static void RecvWorker(
        RxStreamer         streamer,
        Config             cfg,
        Slot[]             ring,
        ChannelReader<int>  freeSlotReader,
        ChannelWriter<int>  freeSlotWriter,
        ChannelWriter<int>  readySlots,
        nuint              spb,
        CancellationToken  ct,
        ref long           totalSamples,
        ref long           overflows,
        Stopwatch          sw)
    {
        using var metadata = new RxMetadata();
        UhdRxMetadataHandle mdHandle = metadata.Handle;

        // Stack-allocate the buffer-pointer array once; only [0] changes per iteration
        void** buffs = stackalloc void*[1];

        long   maxSamples  = cfg.NSamps   > 0 ? cfg.NSamps   : long.MaxValue;
        double maxSecs     = cfg.Duration > 0 ? cfg.Duration : double.MaxValue;
        double nextPrint   = 1.0;
        int    timeouts    = 0;
        bool   abort       = false;

        streamer.IssueStreamCommand(StreamCommand.StartContinuousNow);

        while (!abort && !ct.IsCancellationRequested &&
               totalSamples < maxSamples &&
               sw.Elapsed.TotalSeconds < maxSecs)
        {
            // Block until the writer returns a free slot
            int idx;
            try
            {
                var vt = freeSlotReader.ReadAsync(ct);
                idx = vt.IsCompletedSuccessfully ? vt.Result : vt.AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { break; }
            catch (ChannelClosedException)     { break; }

            buffs[0] = ring[idx].Ptr;

            nuint want = spb;
            if (cfg.NSamps > 0)
            {
                nuint rem = (nuint)(maxSamples - totalSamples);
                if (rem < want) want = rem;
            }

            // === Innermost call: zero GC-transition cost, raw native pointer ===
            nuint got = streamer.ReceiveFast(buffs, want, &mdHandle, 0.5, onePacket: false);

            // Read error code directly via NativeMethods (avoids managed property ThrowIfDisposed guard)
            UhdRxMetadataErrorCode err;
            _ = NativeMethods.uhd_rx_metadata_error_code(mdHandle, &err);

            if (err == UhdRxMetadataErrorCode.Timeout)
            {
                freeSlotWriter.TryWrite(idx);     // return slot without writing
                if (++timeouts >= MaxTimeouts)
                {
                    Console.Error.WriteLine($"ERROR: {MaxTimeouts} consecutive timeouts — aborting.");
                    abort = true;
                }
                continue;
            }

            if (err != UhdRxMetadataErrorCode.None    &&
                err != UhdRxMetadataErrorCode.BrokenChain &&
                err != UhdRxMetadataErrorCode.Overflow)
            {
                Console.Error.WriteLine($"ERROR: recv returned {err} — aborting.");
                freeSlotWriter.TryWrite(idx);
                abort = true;
                continue;
            }

            timeouts = 0;

            if (err == UhdRxMetadataErrorCode.Overflow)
                overflows++;
            // Samples in `got` are valid even on overflow (data was dropped before us, not in this call)

            ring[idx].Samples = (int)got;
            totalSamples += (long)got;

            // Hand the filled slot to the writer (always succeeds: see ring invariant comment above)
            readySlots.TryWrite(idx);

            if (cfg.Progress)
            {
                double t = sw.Elapsed.TotalSeconds;
                if (t >= nextPrint)
                {
                    double msps = totalSamples / t / 1e6;
                    Console.Write($"\r  {totalSamples / 1e6:F2} MSa   {msps:F3} Msps   overflows={overflows}  ");
                    nextPrint = t + 1.0;
                }
            }
        }

        streamer.IssueStreamCommand(StreamCommand.StopContinuous);
    }

    // ─── Write worker ────────────────────────────────────────────────────────────
    //
    // Reads ready slots in order, writes raw bytes to outFile, returns slots to freeSlots.
    // FileStream.Write(ReadOnlySpan<byte>) over a NativeMemory pointer is a zero-copy path
    // down to the OS write() syscall — no internal .NET buffer is used because our slot size
    // (≥ 8192 samples) exceeds FileStream's internal buffer (64 KiB).

    private static void WriteWorker(
        FileStream         file,
        Slot[]             ring,
        ChannelReader<int>  readySlots,
        ChannelWriter<int>  freeSlots,
        int                bytesPerSample,
        CancellationToken  ct)
    {
        while (true)
        {
            int idx;
            try
            {
                var vt = readySlots.ReadAsync(ct);
                idx = vt.IsCompletedSuccessfully ? vt.Result : vt.AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { return; }
            catch (ChannelClosedException)     { return; }

            ref Slot s = ref ring[idx];
            if (s.Samples > 0)
                file.Write(new ReadOnlySpan<byte>((byte*)s.Ptr, s.Samples * bytesPerSample));

            freeSlots.TryWrite(idx);
        }
    }

    // ─── Stats ───────────────────────────────────────────────────────────────────

    private static void PrintStats(long samples, long overflows, int bytesPerSample, TimeSpan elapsed)
    {
        double secs = elapsed.TotalSeconds;
        double msps = samples / secs / 1e6;
        double mibs = samples * (double)bytesPerSample / secs / (1024.0 * 1024.0);

        Console.WriteLine();
        Console.WriteLine("─── Benchmark results ─────────────────────────────────────");
        Console.WriteLine($"  Elapsed     : {elapsed:c}");
        Console.WriteLine($"  Samples     : {samples:N0}");
        Console.WriteLine($"  Bytes       : {samples * (double)bytesPerSample / (1024.0 * 1024.0):F2} MiB");
        Console.WriteLine($"  Throughput  : {msps:F4} Msps  /  {mibs:F2} MiB/s");
        Console.WriteLine($"  Overflows   : {overflows}");
        if (overflows > 0)
            Console.WriteLine("  NOTE: Overflows mean the USRP FPGA FIFO overflowed — samples were dropped");
        Console.WriteLine("────────────────────────────────────────────────────────────");
    }

    // ─── Argument parsing ────────────────────────────────────────────────────────

    private static Config ParseArgs(string[] argv)
    {
        var cfg = new Config();
        for (int i = 0; i < argv.Length; i++)
        {
            string a = argv[i];
            string V() => i + 1 < argv.Length
                ? argv[++i]
                : throw new ArgumentException($"Missing value after '{a}'");
            double D() => double.Parse(V(), CultureInfo.InvariantCulture);

            switch (a)
            {
                case "--args":       case "-a": cfg.DeviceArgs = V();         break;
                case "--file":       case "-o": cfg.FilePath   = V();         break;
                case "--type":       case "-t": cfg.Type       = V();         break;
                case "--wirefmt":               cfg.WireFmt    = V();         break;
                case "--rate":       case "-r": cfg.Rate       = D();         break;
                case "--freq":       case "-f": cfg.Freq       = D();         break;
                case "--gain":       case "-g": cfg.Gain       = D();         break;
                case "--ant":                   cfg.Ant        = V();         break;
                case "--bw":                    cfg.Bw         = D();         break;
                case "--nsamps":     case "-n": cfg.NSamps     = long.Parse(V()); break;
                case "--duration":   case "-d": cfg.Duration   = D();         break;
                case "--spb":                   cfg.Spb        = int.Parse(V()); break;
                case "--ring":                  cfg.RingSlots  = int.Parse(V()); break;
                case "--setup-time":            cfg.SetupTime  = D();         break;
                case "--no-progress":           cfg.Progress   = false;       break;
                case "--no-stats":              cfg.Stats      = false;       break;
                case "--help":       case "-h": PrintUsage(); throw new OperationCanceledException();
                default:
                    if (a.StartsWith('-'))
                        throw new ArgumentException($"Unknown option '{a}'");
                    break;
            }
        }

        if (cfg.Type is not ("short" or "sc16" or "float" or "fc32"))
            throw new ArgumentException($"--type must be 'short'/'sc16' or 'float'/'fc32'; got '{cfg.Type}'");
        if (cfg.RingSlots < 2)
            throw new ArgumentException("--ring must be >= 2");
        if (cfg.Spb < 0)
            throw new ArgumentException("--spb must be >= 0");

        return cfg;
    }

    private static void PrintUsage() => Console.WriteLine("""
        rx_samples_to_file — high-performance USRP RX benchmark (UHD.NET / C#)

        Usage:
          UhdNet.Tests rx_samples_to_file [options]

        Core options:
          -a, --args <str>      Device args e.g. "type=b200" (default: "")
          -o, --file <path>     Output file path (default: samples.bin)
          -t, --type <str>      CPU sample format: short|sc16 or float|fc32 (default: short)
              --wirefmt <str>   Over-the-wire format: sc16, sc12, sc8 (default: sc16)
          -r, --rate <Hz>       Sample rate; scientific notation ok: 1e6, 2.5e6 (default: 1e6)
          -f, --freq <Hz>       Center frequency (default: 0)
          -g, --gain <dB>       RX gain dB (default: 0)
              --ant <name>      Antenna name (default: device default)
              --bw <Hz>         Analog bandwidth — 0 = no override (default: 0)

        Capture control:
          -n, --nsamps <N>      Samples to capture; 0 = unlimited (default: 0)
          -d, --duration <s>    Duration in seconds; 0 = unlimited (default: 0)

        Performance tuning:
              --spb <N>         Samples per buffer; 0 = auto (MaxSPP×8) (default: 0)
              --ring <N>        NativeMemory ring slot count (default: 8)
              --setup-time <s>  Post-tune settle time in seconds (default: 1.0)

        Output:
              --no-progress     Suppress live throughput display
              --no-stats        Suppress final benchmark summary
          -h, --help            Show this help and exit

        Examples:
          rx_samples_to_file --freq 2.4e9 --rate 1e6 --gain 40 --duration 10
          rx_samples_to_file -a "type=b200" -f 433e6 -r 2e6 -g 30 -n 20000000 -t float
          rx_samples_to_file -f 915e6 -r 10e6 -d 5 --spb 32768 --ring 16 --no-progress
        """);

    // ─── Linux thread affinity ───────────────────────────────────────────────────

    private static void TryPinCurrentThreadToCore(int core)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        if ((uint)core >= 1024) return;
        try
        {
            // cpu_set_t on Linux is 128 bytes (1024 CPUs × 1 bit).
            // pid=0 means "current thread" in sched_setaffinity.
            byte* mask = stackalloc byte[128];
            new Span<byte>(mask, 128).Clear();
            mask[core / 8] |= (byte)(1 << (core % 8));
            if (sched_setaffinity(0, 128, mask) != 0)
                Console.WriteLine($"Note: thread affinity not set (errno={Marshal.GetLastPInvokeError()}).");
        }
        catch { }
    }
}
