using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Command issued to a streamer to control sample flow. Mirrors <c>uhd::stream_cmd_t</c>.
/// </summary>
public readonly struct StreamCommand
{
    public UhdStreamMode Mode { get; init; }
    public nuint NumberOfSamples { get; init; }
    public bool StreamNow { get; init; }
    public TimeSpec Time { get; init; }

    /// <summary>Stream samples indefinitely starting immediately.</summary>
    public static readonly StreamCommand StartContinuousNow = new()
    {
        Mode = UhdStreamMode.StartContinuous,
        StreamNow = true,
    };

    /// <summary>Stop continuous streaming.</summary>
    public static readonly StreamCommand StopContinuous = new()
    {
        Mode = UhdStreamMode.StopContinuous,
        StreamNow = true,
    };

    /// <summary>Stream a fixed number of samples and finish (start immediately).</summary>
    public static StreamCommand FiniteNow(nuint numSamps) => new()
    {
        Mode = UhdStreamMode.NumSampsAndDone,
        NumberOfSamples = numSamps,
        StreamNow = true,
    };

    /// <summary>Stream a fixed number of samples and finish at the given device time.</summary>
    public static StreamCommand FiniteAt(nuint numSamps, TimeSpec time) => new()
    {
        Mode = UhdStreamMode.NumSampsAndDone,
        NumberOfSamples = numSamps,
        StreamNow = false,
        Time = time,
    };

    internal UhdStreamCmdNative ToNative() => new()
    {
        StreamMode = Mode,
        NumSamps = NumberOfSamples,
        StreamNow = StreamNow,
        TimeSpecFullSecs = Time.FullSeconds,
        TimeSpecFracSecs = Time.FractionalSeconds,
    };
}
