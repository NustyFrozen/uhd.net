using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace UhdNet;

/// <summary>
/// Single complex sample, interleaved <see cref="float"/> I/Q. Layout matches UHD's
/// <c>fc32</c> CPU format. 8 bytes wide, blittable, suitable for direct streaming.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly struct Complex32 : IEquatable<Complex32>
{
    public readonly float I;
    public readonly float Q;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Complex32(float i, float q) { I = i; Q = q; }

    public static readonly Complex32 Zero = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float MagnitudeSquared() => I * I + Q * Q;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Magnitude() => MathF.Sqrt(I * I + Q * Q);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Complex32 operator +(Complex32 a, Complex32 b) => new(a.I + b.I, a.Q + b.Q);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Complex32 operator -(Complex32 a, Complex32 b) => new(a.I - b.I, a.Q - b.Q);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Complex32 operator *(Complex32 a, Complex32 b) =>
        new(a.I * b.I - a.Q * b.Q, a.I * b.Q + a.Q * b.I);

    public bool Equals(Complex32 other) => I.Equals(other.I) && Q.Equals(other.Q);
    public override bool Equals(object? obj) => obj is Complex32 c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(I, Q);
    public static bool operator ==(Complex32 a, Complex32 b) => a.Equals(b);
    public static bool operator !=(Complex32 a, Complex32 b) => !a.Equals(b);
    public override string ToString() => $"{I:F6}{(Q >= 0 ? "+" : "")}{Q:F6}j";
}

/// <summary>
/// Single complex sample, interleaved <see cref="short"/> I/Q. Layout matches UHD's
/// <c>sc16</c> CPU format. 4 bytes wide, blittable.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 4)]
public readonly struct Sc16Sample : IEquatable<Sc16Sample>
{
    public readonly short I;
    public readonly short Q;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Sc16Sample(short i, short q) { I = i; Q = q; }

    public static readonly Sc16Sample Zero = default;

    public bool Equals(Sc16Sample other) => I == other.I && Q == other.Q;
    public override bool Equals(object? obj) => obj is Sc16Sample s && Equals(s);
    public override int GetHashCode() => HashCode.Combine(I, Q);
    public static bool operator ==(Sc16Sample a, Sc16Sample b) => a.Equals(b);
    public static bool operator !=(Sc16Sample a, Sc16Sample b) => !a.Equals(b);
    public override string ToString() => $"{I}{(Q >= 0 ? "+" : "")}{Q}j";
}

/// <summary>
/// Vectorized helpers over batches of <see cref="Complex32"/> / <see cref="Sc16Sample"/>.
/// All routines are SIMD-accelerated when the runtime exposes hardware vectors.
/// </summary>
public static unsafe class SampleOps
{
    /// <summary>
    /// Compute the sum of squared magnitudes (I² + Q²) over <paramref name="samples"/>. Useful
    /// for power / RSSI estimation. Uses <see cref="Vector{T}"/> when available.
    /// </summary>
    public static double SumPower(ReadOnlySpan<Complex32> samples)
    {
        if (samples.IsEmpty) return 0;

        // Treat the IQ-interleaved span as a span of floats; squaring and summing yields the
        // same result regardless of which float is I and which is Q.
        ReadOnlySpan<float> floats = MemoryMarshal.Cast<Complex32, float>(samples);

        double sum = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && floats.Length >= Vector<float>.Count * 2)
        {
            int width = Vector<float>.Count;
            Vector<float> acc = Vector<float>.Zero;
            int end = floats.Length - (floats.Length % width);
            ref float baseRef = ref MemoryMarshal.GetReference(floats);
            for (; i < end; i += width)
            {
                Vector<float> v = Vector.LoadUnsafe(ref Unsafe.Add(ref baseRef, i));
                acc += v * v;
            }
            sum += Vector.Sum(acc);
        }

        for (; i < floats.Length; i++)
        {
            float f = floats[i];
            sum += f * f;
        }
        return sum;
    }

    /// <summary>
    /// Find the maximum magnitude squared (I² + Q²) over the buffer. Typically used to detect
    /// clipping. SIMD-vectorized when available.
    /// </summary>
    public static float MaxMagnitudeSquared(ReadOnlySpan<Complex32> samples)
    {
        if (samples.IsEmpty) return 0;
        ReadOnlySpan<float> floats = MemoryMarshal.Cast<Complex32, float>(samples);

        float maxMagSq = 0;
        int i = 0;

        if (Vector.IsHardwareAccelerated && floats.Length >= Vector<float>.Count * 2)
        {
            int width = Vector<float>.Count;
            Vector<float> accMax = Vector<float>.Zero;
            int end = floats.Length - (floats.Length % (width * 2));
            ref float baseRef = ref MemoryMarshal.GetReference(floats);
            for (; i < end; i += width * 2)
            {
                Vector<float> a = Vector.LoadUnsafe(ref Unsafe.Add(ref baseRef, i));
                Vector<float> b = Vector.LoadUnsafe(ref Unsafe.Add(ref baseRef, i + width));
                Vector<float> magSq = a * a + b * b;
                accMax = Vector.Max(accMax, magSq);
            }
            for (int k = 0; k < width; k++)
            {
                if (accMax[k] > maxMagSq) maxMagSq = accMax[k];
            }
        }

        // Tail: walk in (I, Q) pairs.
        for (int s = i / 2; s < samples.Length; s++)
        {
            float msq = samples[s].MagnitudeSquared();
            if (msq > maxMagSq) maxMagSq = msq;
        }
        return maxMagSq;
    }
}
