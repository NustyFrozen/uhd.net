using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Request to tune the RF chain to a particular frequency. Mirrors <c>uhd::tune_request_t</c>.
/// </summary>
public readonly struct TuneRequest
{
    public double TargetFrequency { get; init; }
    public UhdTuneRequestPolicy RfFrequencyPolicy { get; init; }
    public double RfFrequency { get; init; }
    public UhdTuneRequestPolicy DspFrequencyPolicy { get; init; }
    public double DspFrequency { get; init; }
    public string? Args { get; init; }

    public TuneRequest(double targetFrequency)
    {
        TargetFrequency = targetFrequency;
        RfFrequencyPolicy = UhdTuneRequestPolicy.Auto;
        RfFrequency = 0;
        DspFrequencyPolicy = UhdTuneRequestPolicy.Auto;
        DspFrequency = 0;
        Args = null;
    }

    public TuneRequest(double targetFrequency, double lo_offset)
    {
        TargetFrequency = targetFrequency;
        RfFrequencyPolicy = UhdTuneRequestPolicy.Manual;
        RfFrequency = targetFrequency + lo_offset;
        DspFrequencyPolicy = UhdTuneRequestPolicy.Auto;
        DspFrequency = 0;
        Args = null;
    }
}

/// <summary>
/// The result of a tune request, describing actual achieved frequencies.
/// </summary>
public readonly struct TuneResult
{
    public double ClippedRfFrequency { get; init; }
    public double TargetRfFrequency { get; init; }
    public double ActualRfFrequency { get; init; }
    public double TargetDspFrequency { get; init; }
    public double ActualDspFrequency { get; init; }

    internal static TuneResult FromNative(in UhdTuneResultNative n) => new()
    {
        ClippedRfFrequency = n.ClippedRfFreq,
        TargetRfFrequency = n.TargetRfFreq,
        ActualRfFrequency = n.ActualRfFreq,
        TargetDspFrequency = n.TargetDspFreq,
        ActualDspFrequency = n.ActualDspFreq,
    };
}
