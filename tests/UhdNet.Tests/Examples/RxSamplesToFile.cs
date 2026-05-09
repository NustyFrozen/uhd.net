using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        {
            usrp.SetClockSource(opts.ReferenceSource);
        }

        // Configure RX chain.
        Console.WriteLine($"Setting RX rate: {opts.SampleRate / 1e6:F3} Msps...");
        usrp.SetRxRate(opts.SampleRate, opts.Channel);
        Console.WriteLine($"Actual RX rate:  {usrp.GetRxRate(opts.Channel) / 1e6:F3} Msps");

        Console.WriteLine($"Setting RX freq: {opts.Frequency / 1e6:F3} MHz...");
        usrp.SetRxFrequency(new TuneRequest(opts.Frequency), opts.Channel, out var tune);
        Console.WriteLine(
            $"Actual RX freq:  {usrp.GetRxFrequency(opts.Channel) / 1e6:F3} MHz " +
            $"(RF tuned to {tune.ActualRfFrequency / 1e6:F3} MHz, DSP {tune.ActualDspFrequency / 1e3:+0.000;-0.000} kHz)");

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
        {
            usrp.SetRxAntenna(opts.Antenna, opts.Channel);
        }
        Console.WriteLine($"Antenna: {usrp.GetRxAntenna(opts.Channel)}");

        // Allow LO to settle.
        System.Threading.Thread.Sleep(opts.SetupDelay);

        // Build streamer.
        var streamArgs = opts.SampleType == SampleType.Float
            ? StreamArgs.SingleChannelFc32(opts.Channel)
            : StreamArgs.SingleChannelSc16(opts.Channel);
        using var streamer = usrp.GetRxStream(streamArgs);

        nuint maxSpp = streamer.MaxSamplesPerPacket;
        nuint bufferSamples = opts.BufferSamples == 0 ? maxSpp : (nuint)opts.BufferSamples;
        if (bufferSamples < maxSpp) bufferSamples = maxSpp;

        Console.WriteLine($"Stream: cpu={streamArgs.CpuFormat}, otw={streamArgs.OtwFormat}, " +
                          $"max_spp={maxSpp}, buffer={bufferSamples} samples");

        // Allocate the sample buffer in native memory so it stays pinned across recv() calls
        // and is naturally aligned for SIMD post-processing.
        int bytesPerSample = opts.SampleType == SampleType.Float ? sizeof(float) * 2 : sizeof(short) * 2;
        nuint totalBufferBytes = bufferSamples * (nuint)bytesPerSample;
        void* sampleBuffer = NativeMemory.AlignedAlloc(totalBufferBytes, 64);
        if (sampleBuffer == null) throw new OutOfMemoryException("AlignedAlloc returned null.");

        try
        {
            using var metadata = new RxMetadata();
            using var output = new FileStream(
                opts.OutputFile, FileMode.Create, FileAccess.Write, FileShare.Read, 100 << 20);

            // Determine target.
            long targetSamples = opts.NumberOfSamples > 0
                ? opts.NumberOfSamples
                : opts.Duration > 0
                    ? (long)(opts.Duration * usrp.GetRxRate(opts.Channel))
                    : long.MaxValue;

            var streamCmd = targetSamples == long.MaxValue
                ? StreamCommand.StartContinuousNow
                : StreamCommand.FiniteNow((nuint)targetSamples);

            Console.WriteLine($"Issuing stream command (mode={streamCmd.Mode}, target={targetSamples}). Press Ctrl+C to stop.");

            using var stop = new System.Threading.CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };

            streamer.IssueStreamCommand(streamCmd);

            long total = 0;
            long overflows = 0;
            var stopwatch = Stopwatch.StartNew();
            var lastReport = TimeSpan.Zero;

            while (!stop.IsCancellationRequested && total < targetSamples)
            {
                nuint want = (nuint)Math.Min((long)bufferSamples, targetSamples - total);
                nuint got = streamer.Receive(sampleBuffer, want, metadata, opts.Timeout, onePacket: false);

                var err = metadata.ErrorCode;
                if ((err & UhdRxMetadataErrorCode.Timeout) != 0)
                {
                    Console.Error.WriteLine($"[timeout after {opts.Timeout:F1}s]");
                    if (total == 0) continue;
                    break;
                }
                if ((err & UhdRxMetadataErrorCode.Overflow) != 0)
                {
                    overflows++;
                    Console.Error.Write('O');
                }
                else if (err != UhdRxMetadataErrorCode.None)
                {
                    Console.Error.WriteLine($"[rx error: {err} - {metadata.GetErrorString()}]");
                    if (opts.ContinueOnError) continue;
                    break;
                }

                if (got == 0) continue;

                int writeBytes = checked((int)got * bytesPerSample);
                var span = new ReadOnlySpan<byte>(sampleBuffer, writeBytes);
                output.Write(span);
                total += (long)got;

                if (stopwatch.Elapsed - lastReport > TimeSpan.FromSeconds(1))
                {
                    lastReport = stopwatch.Elapsed;
                    double rate = total / stopwatch.Elapsed.TotalSeconds;
                    Console.WriteLine(
                        $"  rx {total:N0} samples ({total * bytesPerSample / 1024.0 / 1024.0:F1} MiB) " +
                        $"@ {rate / 1e6:F3} Msps, overflows={overflows}");
                }
            }

            // Stop continuous streaming if it was a continuous command.
            if (streamCmd.Mode == UhdNet.Native.UhdStreamMode.StartContinuous)
            {
                streamer.IssueStreamCommand(StreamCommand.StopContinuous);
                // Drain any in-flight packets so the streamer ends cleanly.
                DrainRemaining(streamer, sampleBuffer, bufferSamples, metadata, opts.Timeout);
            }

            output.Flush();
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"Done. Wrote {total:N0} samples ({total * bytesPerSample:N0} bytes) " +
                              $"in {stopwatch.Elapsed.TotalSeconds:F2}s ({total / stopwatch.Elapsed.TotalSeconds / 1e6:F3} Msps).");
            if (overflows > 0)
            {
                Console.WriteLine($"WARN: {overflows} overflow event(s); host could not keep up with the device.");
            }
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
            NativeMemory.AlignedFree(sampleBuffer);
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
        public double SampleRate = 1e6;
        public double Frequency = 2.4e9;
        public double Gain = 40;
        public double Bandwidth = 0;
        public string Antenna = string.Empty;
        public string ReferenceSource = string.Empty;
        public double Duration = 10;
        public long NumberOfSamples = 0;
        public double Timeout = 3.0;
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
                    case "--rate":     o.SampleRate = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--freq":     o.Frequency = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--gain":     o.Gain = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--bw":       o.Bandwidth = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--ant":      o.Antenna = Next(); break;
                    case "--ref":      o.ReferenceSource = Next(); break;
                    case "--duration": o.Duration = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--nsamps":   o.NumberOfSamples = long.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--timeout":  o.Timeout = double.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--channel":  o.Channel = (nuint)uint.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
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
            Console.WriteLine("  --buffer <n>   Buffer size in samples (default: max packet size)");
            Console.WriteLine("  --continue     Continue on non-fatal errors");
        }
    }
}
