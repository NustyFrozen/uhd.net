using System;

namespace UhdNet;

/// <summary>
/// Arguments for creating an RX or TX streamer. Mirrors <c>uhd::stream_args_t</c>.
/// </summary>
/// <remarks>
/// The defaults (<c>"fc32"</c> CPU format, <c>"sc16"</c> over-the-wire format, channel 0) match
/// the most common single-channel complex-float streaming setup used by <c>rx_samples_to_file</c>.
/// </remarks>
public sealed class StreamArgs
{
    /// <summary>Format of host memory: e.g. <c>"fc32"</c>, <c>"fc64"</c>, <c>"sc16"</c>, <c>"sc8"</c>.</summary>
    public string CpuFormat { get; init; } = "fc32";

    /// <summary>Over-the-wire format: e.g. <c>"sc16"</c>, <c>"sc8"</c>, <c>"sc12"</c>.</summary>
    public string OtwFormat { get; init; } = "sc16";

    /// <summary>Comma-separated stream args (e.g. <c>"spp=2000,scalar=1024"</c>).</summary>
    public string Args { get; init; } = string.Empty;

    /// <summary>List of channel indices participating in the stream.</summary>
    public IReadOnlyList<nuint> Channels { get; init; } = new nuint[] { 0 };

    /// <summary>Convenience builder for a single-channel <c>fc32</c>/<c>sc16</c> stream.</summary>
    public static StreamArgs SingleChannelFc32(nuint channel = 0) => new()
    {
        CpuFormat = "fc32",
        OtwFormat = "sc16",
        Channels = new[] { channel },
    };

    /// <summary>Convenience builder for a single-channel <c>sc16</c>/<c>sc16</c> stream.</summary>
    public static StreamArgs SingleChannelSc16(nuint channel = 0) => new()
    {
        CpuFormat = "sc16",
        OtwFormat = "sc16",
        Channels = new[] { channel },
    };
}
