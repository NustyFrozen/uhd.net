using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Internal helpers for marshaling between managed and native UHD code without churning the GC.
/// </summary>
internal static unsafe class Interop
{
    /// <summary>Default scratch length for UHD pretty-print and error string buffers.</summary>
    public const int DefaultStringBuffer = 512;

    /// <summary>
    /// Throw a <see cref="UhdException"/> if <paramref name="code"/> is non-zero. The exception
    /// message is pulled from <c>uhd_get_last_error</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Check(UhdErrorCode code)
    {
        if (code != UhdErrorCode.None)
        {
            ThrowFromCode(code);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFromCode(UhdErrorCode code)
    {
        Span<byte> buf = stackalloc byte[DefaultStringBuffer];
        string message;
        fixed (byte* p = buf)
        {
            // Best-effort: ignore secondary errors here.
            _ = NativeMethods.uhd_get_last_error(p, (nuint)buf.Length);
            message = Utf8ToString(p);
        }
        if (string.IsNullOrEmpty(message))
        {
            message = $"UHD returned error code {(int)code} ({code}).";
        }

        throw new UhdException(code, message);
    }

    /// <summary>
    /// Convert a null-terminated UTF-8 byte string to a managed <see cref="string"/>.
    /// </summary>
    public static string Utf8ToString(byte* utf8)
    {
        if (utf8 == null) return string.Empty;
        int len = 0;
        while (utf8[len] != 0) len++;
        if (len == 0) return string.Empty;
        return Encoding.UTF8.GetString(utf8, len);
    }

    /// <summary>
    /// Convert a UHD pretty-print buffer to a string. The buffer is treated as null-terminated.
    /// </summary>
    public static string Utf8ToString(ReadOnlySpan<byte> buf)
    {
        int len = buf.IndexOf((byte)0);
        if (len < 0) len = buf.Length;
        if (len == 0) return string.Empty;
        return Encoding.UTF8.GetString(buf.Slice(0, len));
    }

    /// <summary>
    /// Encode <paramref name="s"/> as null-terminated UTF-8 into <paramref name="dest"/>.
    /// Returns a pointer to the start of the buffer or <c>null</c> if <paramref name="s"/> is null.
    /// </summary>
    /// <exception cref="ArgumentException">If the buffer is too small.</exception>
    public static byte* StringToUtf8(string? s, Span<byte> dest)
    {
        if (s is null) return null;
        if (s.Length == 0)
        {
            if (dest.Length < 1) throw new ArgumentException("Destination buffer too small.", nameof(dest));
            dest[0] = 0;
            fixed (byte* d = dest) return d;
        }
        // Encoding may need up to 4 bytes per char + 1 for terminator.
        int max = Encoding.UTF8.GetMaxByteCount(s.Length) + 1;
        if (dest.Length < max)
        {
            // Use the precise size to allow callers to give a tight buffer.
            int needed = Encoding.UTF8.GetByteCount(s) + 1;
            if (dest.Length < needed)
            {
                throw new ArgumentException(
                    $"Destination buffer too small ({dest.Length} bytes, need {needed}).", nameof(dest));
            }
        }
        int written = Encoding.UTF8.GetBytes(s, dest);
        dest[written] = 0;
        fixed (byte* d = dest) return d;
    }

    /// <summary>
    /// Suggest a stack-friendly UTF-8 buffer length for the given managed string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Utf8MaxBytes(string? s) => s is null ? 0 : Encoding.UTF8.GetMaxByteCount(s.Length) + 1;
}
