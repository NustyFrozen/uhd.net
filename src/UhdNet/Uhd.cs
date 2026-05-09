using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Top-level UHD environment helpers: version queries, last error access, thread priority.
/// Mirrors the global functions in <c>uhd/version.h</c>, <c>uhd/error.h</c>, and
/// <c>uhd/utils/thread_priority.h</c>.
/// </summary>
public static unsafe class Uhd
{
    /// <summary>The UHD library version string (e.g. <c>"4.6.0.0"</c>).</summary>
    public static string VersionString
    {
        get
        {
            Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
            fixed (byte* p = buf)
            {
                Interop.Check(NativeMethods.uhd_get_version_string(p, (nuint)buf.Length));
                return Interop.Utf8ToString(p);
            }
        }
    }

    /// <summary>The ABI version string of the loaded UHD library.</summary>
    public static string AbiString
    {
        get
        {
            Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
            fixed (byte* p = buf)
            {
                Interop.Check(NativeMethods.uhd_get_abi_string(p, (nuint)buf.Length));
                return Interop.Utf8ToString(p);
            }
        }
    }

    /// <summary>
    /// Get the last error string emitted by the UHD library on this thread (or any thread; the
    /// underlying buffer is process-wide). Useful for diagnostic logging.
    /// </summary>
    public static string GetLastError()
    {
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer * 4];
        fixed (byte* p = buf)
        {
            _ = NativeMethods.uhd_get_last_error(p, (nuint)buf.Length);
            return Interop.Utf8ToString(p);
        }
    }

    /// <summary>
    /// Set the calling thread's priority. <paramref name="priority"/> ranges in [-1.0, 1.0]; 1.0
    /// is the highest. Set <paramref name="realtime"/> to enable real-time scheduling.
    /// </summary>
    public static void SetThreadPriority(float priority, bool realtime)
    {
        Interop.Check(NativeMethods.uhd_set_thread_priority(priority, realtime));
    }

    /// <summary>
    /// Force the native UHD library to be loaded immediately. Useful for surfacing load errors
    /// at startup rather than on first API call.
    /// </summary>
    public static void EnsureNativeLoaded() => UhdNativeLibrary.EnsureLoaded();
}
