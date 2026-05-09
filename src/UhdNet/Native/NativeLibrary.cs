using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UhdNet.Native;

/// <summary>
/// Resolves the native UHD shared library across platforms.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
///   <item>An override path provided via <see cref="OverridePath"/>.</item>
///   <item>The <c>UHD_NATIVE_LIBRARY</c> environment variable.</item>
///   <item>The OS default search (PATH on Windows, ld.so on Linux).</item>
///   <item>Well-known install locations (e.g. <c>C:\Program Files\UHD\bin\uhd.dll</c>).</item>
/// </list>
/// </remarks>
public static class UhdNativeLibrary
{
    internal const string LibraryName = "uhd";

    private static string? s_overridePath;
    private static int s_initialized;

    /// <summary>
    /// Optional explicit absolute path to the UHD shared library. Must be set before any UHD
    /// API call. Once UHD is loaded, the value is frozen.
    /// </summary>
    public static string? OverridePath
    {
        get => s_overridePath;
        set
        {
            if (System.Threading.Volatile.Read(ref s_initialized) != 0)
            {
                throw new InvalidOperationException(
                    "Native UHD library is already loaded; OverridePath must be set before the first UHD API call.");
            }

            s_overridePath = value;
        }
    }

    static UhdNativeLibrary()
    {
        NativeLibrary.SetDllImportResolver(typeof(UhdNativeLibrary).Assembly, Resolve);
    }

    /// <summary>
    /// Forces eager loading of the native library. Optional - the resolver runs automatically
    /// the first time a P/Invoke is made.
    /// </summary>
    public static void EnsureLoaded()
    {
        // Touching any P/Invoke triggers the resolver. Using an inexpensive call.
        Span<byte> buf = stackalloc byte[256];
        unsafe
        {
            fixed (byte* p = buf)
            {
                _ = NativeMethods.uhd_get_version_string(p, (nuint)buf.Length);
            }
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            return IntPtr.Zero;
        }

        IntPtr handle;

        if (!string.IsNullOrEmpty(s_overridePath) && NativeLibrary.TryLoad(s_overridePath!, out handle))
        {
            System.Threading.Volatile.Write(ref s_initialized, 1);
            return handle;
        }

        var envOverride = Environment.GetEnvironmentVariable("UHD_NATIVE_LIBRARY");
        if (!string.IsNullOrEmpty(envOverride) && NativeLibrary.TryLoad(envOverride, out handle))
        {
            System.Threading.Volatile.Write(ref s_initialized, 1);
            return handle;
        }

        foreach (var candidate in PlatformCandidates())
        {
            if (NativeLibrary.TryLoad(candidate, out handle))
            {
                System.Threading.Volatile.Write(ref s_initialized, 1);
                return handle;
            }
        }

        // Final fallback: let the runtime do its default mapping (libuhd.so / uhd.dll / libuhd.dylib).
        if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out handle))
        {
            System.Threading.Volatile.Write(ref s_initialized, 1);
            return handle;
        }

        throw new DllNotFoundException(BuildNotFoundMessage());
    }

    private static IEnumerable<string> PlatformCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return "uhd.dll";

            var pf = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(pf))
            {
                yield return Path.Combine(pf, "UHD", "bin", "uhd.dll");
            }

            var pfx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!string.IsNullOrEmpty(pfx86))
            {
                yield return Path.Combine(pfx86, "UHD", "bin", "uhd.dll");
            }

            yield return @"C:\Program Files\UHD\bin\uhd.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            yield return "libuhd.so";
            yield return "libuhd.so.4";
            yield return "libuhd.so.4.6";
            yield return "/usr/lib/libuhd.so";
            yield return "/usr/lib/x86_64-linux-gnu/libuhd.so";
            yield return "/usr/lib/aarch64-linux-gnu/libuhd.so";
            yield return "/usr/local/lib/libuhd.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "libuhd.dylib";
            yield return "/usr/local/lib/libuhd.dylib";
            yield return "/opt/homebrew/lib/libuhd.dylib";
        }
    }

    private static string BuildNotFoundMessage()
    {
        var os = RuntimeInformation.OSDescription;
        return $"Unable to locate the UHD native library on {os}. " +
               "Install UHD (https://files.ettus.com/manual/page_install.html), or set the " +
               "UHD_NATIVE_LIBRARY environment variable / UhdNativeLibrary.OverridePath to its full path.";
    }
}
