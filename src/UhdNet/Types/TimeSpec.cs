using System;
using System.Runtime.CompilerServices;

namespace UhdNet;

/// <summary>
/// A high-precision time, mirroring <c>uhd::time_spec_t</c>: full-second integer plus a
/// fractional-second double. Pure struct - no native handle, no allocation.
/// </summary>
public readonly struct TimeSpec : IEquatable<TimeSpec>
{
    public long FullSeconds { get; }

    public double FractionalSeconds { get; }

    public TimeSpec(long fullSeconds, double fractionalSeconds)
    {
        FullSeconds = fullSeconds;
        FractionalSeconds = fractionalSeconds;
    }

    public static readonly TimeSpec Zero = default;

    public static TimeSpec FromSeconds(double seconds)
    {
        long full = (long)Math.Floor(seconds);
        double frac = seconds - full;
        return new TimeSpec(full, frac);
    }

    public double TotalSeconds
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => FullSeconds + FractionalSeconds;
    }

    public bool Equals(TimeSpec other) =>
        FullSeconds == other.FullSeconds && FractionalSeconds.Equals(other.FractionalSeconds);

    public override bool Equals(object? obj) => obj is TimeSpec t && Equals(t);

    public override int GetHashCode() => HashCode.Combine(FullSeconds, FractionalSeconds);

    public static bool operator ==(TimeSpec l, TimeSpec r) => l.Equals(r);

    public static bool operator !=(TimeSpec l, TimeSpec r) => !l.Equals(r);

    public override string ToString() => $"{TotalSeconds:F9}s";
}
