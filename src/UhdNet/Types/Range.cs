using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>Closed range of doubles with a minimum granularity.</summary>
public readonly struct Range : IEquatable<Range>
{
    public double Start { get; }
    public double Stop { get; }
    public double Step { get; }

    public Range(double start, double stop, double step)
    {
        Start = start;
        Stop = stop;
        Step = step;
    }

    internal static Range FromNative(in UhdRangeNative n) => new(n.Start, n.Stop, n.Step);

    public bool Equals(Range other) =>
        Start.Equals(other.Start) && Stop.Equals(other.Stop) && Step.Equals(other.Step);

    public override bool Equals(object? obj) => obj is Range r && Equals(r);

    public override int GetHashCode() => HashCode.Combine(Start, Stop, Step);

    public static bool operator ==(Range l, Range r) => l.Equals(r);

    public static bool operator !=(Range l, Range r) => !l.Equals(r);

    public override string ToString() => $"[{Start}..{Stop}, step={Step}]";
}
