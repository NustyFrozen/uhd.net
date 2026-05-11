using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UhdNet;
using UhdNet.Native;

namespace UhdNet.Tests.Examples;

/// <summary>
/// Port of UHD's <c>rx_samples_to_file</c>: stream samples from a single RX channel into a file.
///
/// Equivalent to:
/// <code>
/// rx_samples_to_file --args="" --file=samples.bin --type=short --duration=10
///                    --rate=1e6 --freq=2.4e9 --gain=40 --ant=RX2 --bw=1e6
/// </code>
/// </summary>
internal static unsafe class RxSamplesToFile
{
    public static int Run(string[] args)
    {
        Options opts;
        try { opts = Options.Parse(args); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Options.PrintUsage();
            return 1;
        }

        Console.WriteLine($"UHD version: {Uhd.VersionString}");
        Console.WriteLine($"Creating USRP with args: \"{opts.DeviceArgs}\"...");

        using var usrp = Usrp.Make(opts.DeviceArgs);
        Console.WriteLine($"Using device:\n{usrp.GetPrettyString()}");

        if (!string.IsNullOrEmpty(opts.ReferenceSource))
            usrp.SetClockSource(opts.ReferenceSource);

        Console.WriteLine($"Setting RX rate: {opts.SampleRate / 1e6:F3} Msps...");
        usrp.SetRxRate(opts.SampleRate, opts.Channel);
        Console.WriteLine($"Actual RX rate:  {usrp.GetRxRate(opts.Channel) / 1e6:F3} Msps");

        Console.WriteLine($"Setting RX freq: {opts.Frequency / 1e6:F3} MHz...");
        usrp.SetRxFrequency(new TuneRequest(opts.Frequency), opts.Channel, out var tune);
        Console.WriteLine(
            $"Actual RX freq:  {usrp.GetRxFrequency(opts.Channel) / 1e6:F3} MHz " +
            $"(RF tuned to {tune.ActualRfFrequency / 1e6:F3} MHz, " +
            $"DSP {tune.ActualDspFrequency / 1e3:+0.000;-0.000} kHz)");

        Console.WriteLine($"Setting RX gain: {opts.Gain:F1} dB...");
        usrp.SetRxGain(opts.Gain, opts.Channel);
        Console.WriteLine($"Actual RX gain:  {usrp.GetRxGain(opts.Channel):F1} dB");

        if (opts.Bandwidth > 0)
        {
            Console.WriteLine($"Setting RX bw:   {opts.Bandwidth / 1e6:F3} MHz...");
            usrp.SetRxBandwidth(opts.Bandwidth, opts.Channel);
            Console.WriteLine($"Actual RX bw:    {usrp.GetRxBandwidth(opts.Channel) / 1e6:F3} MHz");
        }

        if (!string.IsNullOrEmpty(opts.Antenna))
            usrp.SetRxAntenna(opts.Antenna, opts.Channel);
        Console.WriteLine($"Antenna: {usrp.GetRxAntenna(opts.Channel)}");

        Thread.Sleep(opts.SetupDelay);

        // Elevate priorities; ask UHD to pin its transport threads as realtime.
        NativeMethods.uhd_set_thread_priority(1.0f, true);
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        // sc16 CPU / sc12 OTW: 12-bit compressed on the wire saves ~25% USB/PCIe bandwidth.
        var streamArgs = new StreamArgs
        {
            Channels = new[] { (nuint)opts.Channel },
            Args = "num_recv_frames=16384",
            CpuFormat = "sc8",
            OtwFormat = "sc8",
        };
        using var streamer = usrp.GetRxStream(streamArgs);

        nuint maxSpp = streamer.MaxSamplesPerPacket;
        // Default to 32× max packet size so each recv/write round-trip amortises overhead well.
        nuint bufferSamples = opts.BufferSamples == 0
            ? maxSpp * 32
            : (nuint)opts.BufferSamples;
        if (bufferSamples < maxSpp) bufferSamples = maxSpp;

        //int bytesPerSample = opts.SampleType == SampleType.Float ? sizeof(float) * 2 : sizeof(short) * 2;
        int bytesPerSample = sizeof(byte) * 2;
        nuint slotBytes = bufferSamples * (nuint)bytesPerSample;

        Console.WriteLine($"Stream: cpu={streamArgs.CpuFormat}, otw={streamArgs.OtwFormat}, " +
                          $"max_spp={maxSpp}, buffer={bufferSamples} samples");

        // Two native-memory slots for double-buffering: recv fills one while the writer drains
        // the other. Both are 64-byte aligned for any downstream SIMD processing.
        const int NumSlots = 2;
        var slotPtrs = new void*[NumSlots];
        var slotWriteBytes = new int[NumSlots]; // bytes queued per slot; negative = stop sentinel

        for (int i = 0; i < NumSlots; i++)
        {
            slotPtrs[i] = NativeMemory.AlignedAlloc(slotBytes, 64);
            if (slotPtrs[i] == null) throw new OutOfMemoryException("AlignedAlloc returned null.");
        }

        try
        {
            using var metadata = new RxMetadata();
            using var output = new FileStream(
                opts.OutputFile, FileMode.Create, FileAccess.Write, FileShare.None,
                100 << 20, FileOptions.SequentialScan);

            long targetSamples = opts.NumberOfSamples > 0
                ? opts.NumberOfSamples
                : opts.Duration > 0
                    ? (long)(opts.Duration * usrp.GetRxRate(opts.Channel))
                    : long.MaxValue;

            var streamCmd = StreamCommand.StartContinuousNow;
            Console.WriteLine(
                $"Issuing stream command (mode={streamCmd.Mode}, target={targetSamples}). Press Ctrl+C to stop.");

            using var stop = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };

            streamer.IssueStreamCommand(streamCmd);

            long total = 0;
            long overflows = 0;
            var sw = Stopwatch.StartNew();
            var lastReport = TimeSpan.Zero;

            // freeSlots: slots the recv thread may write into (starts full).
            // readySlots: slots filled and waiting for the writer (starts empty).
            var freeSlots = new SemaphoreSlim(NumSlots, NumSlots);
            var readySlots = new SemaphoreSlim(0, NumSlots);
            Exception? writerEx = null;

            // Dedicated writer thread so disk I/O never blocks the recv loop.
            var writerThread = new Thread(() =>
            {
                int idx = 0;
                try
                {
                    while (true)
                    {
                        readySlots.Wait();
                        int si = idx++ & 1;
                        int n = slotWriteBytes[si];
                        if (n < 0) return; // stop sentinel
                        try { output.Write(new ReadOnlySpan<byte>(slotPtrs[si], n)); }
                        finally { freeSlots.Release(); } // always release even on write exception
                    }
                }
                catch (Exception ex) { writerEx = ex; }
            });
            writerThread.Priority = ThreadPriority.AboveNormal;
            writerThread.IsBackground = true;
            writerThread.Name = "rx-writer";
            writerThread.Start();

            // Hoist per-recv allocations outside the loop.
            // stackalloc once: the JIT would otherwise repeat the stack adjustment every inlined call.
            // Cache metadata.Handle: UHD never replaces the handle, so one managed-heap read suffices.
            void** buffs = stackalloc void*[1];
            UhdRxMetadataHandle mdHandle = metadata.Handle;

            // Enter a no-GC region: prevents any collection on any thread for the duration of
            // the recv loop. 64 MiB covers ~18 hours of once-per-second report strings without
            // exhausting the budget. disallowFullBlockingGC:false — if the budget overflows GC
            // runs normally instead of throwing; EndNoGCRegion then throws, which we swallow.
            bool noGcActive = GC.TryStartNoGCRegion(64L << 20, disallowFullBlockingGC: false);

            // Prevent the OS scheduler from migrating this thread to a different core mid-loop,
            // which would invalidate L1/L2 cache lines holding the hot variables.
            Thread.BeginThreadAffinity();

            int pIdx = 0; // only incremented when a slot is successfully handed to the writer
            try
            {
                while (!stop.IsCancellationRequested && total < targetSamples)
                {
                    if (writerEx != null)
                    {
                        Console.Error.WriteLine($"Writer thread failed: {writerEx.Message}");
                        break;
                    }

                    // No CancellationToken on Wait: registering a token per call can allocate
                    // and would blow the no-GC budget. Cancellation is handled by the
                    // stop.IsCancellationRequested check at the top of the loop instead.
                    freeSlots.Wait();

                    int si = pIdx & 1;
                    buffs[0] = slotPtrs[si];
                    nuint want = (nuint)Math.Min((long)bufferSamples, targetSamples - total);
                    nuint got = streamer.ReceiveFast(buffs, want, &mdHandle, opts.Timeout, onePacket: false);

                    // Direct native call — bypasses ThrowIfDisposed and Interop.Check on the metadata query.
                    UhdRxMetadataErrorCode errCode;
                    NativeMethods.uhd_rx_metadata_error_code(mdHandle, &errCode);
                    if (errCode != UhdRxMetadataErrorCode.None)
                    {
                        if ((errCode & UhdRxMetadataErrorCode.Overflow) != 0)
                            overflows++;
                        freeSlots.Release(); // return the unused slot
                        if (!opts.ContinueOnError) break;
                        continue;
                    }

                    if (got == 0) { freeSlots.Release(); continue; }

                    slotWriteBytes[si] = checked((int)got * bytesPerSample);
                    total += (long)got;
                    pIdx++;
                    readySlots.Release(); // hand slot to writer

                    if (sw.Elapsed - lastReport > TimeSpan.FromSeconds(1))
                    {
                        lastReport = sw.Elapsed;
                        Console.WriteLine(
                            $"  rx {total:N0} samples ({(double)(total * bytesPerSample) / (1 << 20):F1} MiB) " +
                            $"@ {total / sw.Elapsed.TotalSeconds / 1e6:F3} Msps, overflows={overflows}");
                    }
                }
            }
            finally
            {
                // Exit the no-GC and thread-affinity regions before any managed teardown.
                Thread.EndThreadAffinity();
                if (noGcActive)
                {
                    try { GC.EndNoGCRegion(); }
                    catch (InvalidOperationException) { } // budget was exhausted; region already ended
                }

                // Send the stop sentinel to the writer and wait for it to drain.
                freeSlots.Wait(); // always succeeds — writer's finally always releases
                slotWriteBytes[pIdx & 1] = -1;
                readySlots.Release();
                writerThread.Join();
            }

            if (writerEx != null)
                throw new Exception("Writer thread failed.", writerEx);

            if (streamCmd.Mode == UhdStreamMode.StartContinuous)
            {
                streamer.IssueStreamCommand(StreamCommand.StopContinuous);
                DrainRemaining(streamer, slotPtrs[0], bufferSamples, metadata, opts.Timeout);
            }

            output.Flush();
            sw.Stop();

            Console.WriteLine();
            Console.WriteLine(
                $"Done. Wrote {total:N0} samples ({(long)total * bytesPerSample:N0} bytes) " +
                $"in {sw.Elapsed.TotalSeconds:F2}s ({total / sw.Elapsed.TotalSeconds / 1e6:F3} Msps).");
            if (overflows > 0)
                Console.WriteLine($"WARN: {overflows} overflow event(s); host could not keep up with the device.");
            Console.WriteLine($"Output: {Path.GetFullPath(opts.OutputFile)}");
            return 0;
        }
        catch (UhdException ex)
        {
            Console.Error.WriteLine($"UHD error: {ex.Code} - {ex.Message}");
            return 3;
        }
        finally
        {
            for (int i = 0; i < NumSlots; i++)
                NativeMemory.AlignedFree(slotPtrs[i]);
        }
    }

    private static void DrainRemaining(RxStreamer streamer, void* buffer, nuint bufferSamples,
                                       RxMetadata metadata, double timeout)
    {
        for (int i = 0; i < 16; i++)
        {
            nuint got = streamer.Receive(buffer, bufferSamples, metadata, timeout, onePacket: true);
            if (got == 0 || (metadata.ErrorCode & UhdRxMetadataErrorCode.Timeout) != 0) break;
        }
    }

    private enum SampleType { Float, Short }

    private sealed class Options
    {
        public string DeviceArgs = string.Empty;
        public string OutputFile = "samples.bin";
        public SampleType SampleType = SampleType.Short;
        public double SampleRate = 30e6;
        public double Frequency = 2.4e9;
        public double Gain = 0;
        public double Bandwidth = 0;
        public string Antenna = string.Empty;
        public string ReferenceSource = string.Empty;
        public double Duration = 10;
        public long NumberOfSamples = 0;
        public double Timeout = 10.0;
        public TimeSpan SetupDelay = TimeSpan.FromSeconds(1);
        public nuint Channel = 0;
        public int BufferSamples = 0;
        public bool ContinueOnError = false;

        public static Options Parse(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {a}");
                switch (a)
                {
                    case "--args":     o.DeviceArgs = Next(); break;
                    case "--file":     o.OutputFile = Next(); break;
                    case "--type":     o.SampleType = Next() switch
                    {
                        "float" or "fc32" => SampleType.Float,
                        "short" or "sc16" => SampleType.Short,
                        var s => throw new ArgumentException($"Unknown --type {s}; use float|short."),
                    }; break;
                    case "--rate":     o.SampleRate    = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--freq":     o.Frequency     = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--gain":     o.Gain          = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--bw":       o.Bandwidth     = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--ant":      o.Antenna       = Next(); break;
                    case "--ref":      o.ReferenceSource = Next(); break;
                    case "--duration": o.Duration      = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--nsamps":   o.NumberOfSamples = long.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--timeout":  o.Timeout       = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--channel":  o.Channel       = (nuint)uint.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--buffer":   o.BufferSamples = int.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--continue": o.ContinueOnError = true; break;
                    case "-h" or "--help": PrintUsage(); Environment.Exit(0); break;
                    default: throw new ArgumentException($"Unknown option: {a}");
                }
            }
            return o;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: rx_samples_to_file [options]");
            Console.WriteLine("  --args <s>     Device address args (e.g. \"type=b200\", \"addr=192.168.10.2\")");
            Console.WriteLine("  --file <p>     Output file path (default: samples.bin)");
            Console.WriteLine("  --type <t>     Sample type: float (fc32) or short (sc16, default)");
            Console.WriteLine("  --rate <Hz>    Sample rate in samples/sec (default: 1e6)");
            Console.WriteLine("  --freq <Hz>    Center frequency (default: 2.4e9)");
            Console.WriteLine("  --gain <dB>    RX gain (default: 40)");
            Console.WriteLine("  --bw <Hz>      Analog bandwidth (default: device default)");
            Console.WriteLine("  --ant <name>   RX antenna (e.g. RX2)");
            Console.WriteLine("  --ref <src>    Clock reference (e.g. internal, external)");
            Console.WriteLine("  --duration <s> Capture duration in seconds (default: 10)");
            Console.WriteLine("  --nsamps <n>   Capture exact number of samples (overrides --duration)");
            Console.WriteLine("  --timeout <s>  Recv timeout (default: 3.0)");
            Console.WriteLine("  --channel <n>  Channel index (default: 0)");
            Console.WriteLine("  --buffer <n>   Buffer size in samples (default: 32 × max packet size)");
            Console.WriteLine("  --continue     Continue on non-fatal errors (including overflows)");
        }
    }
}
