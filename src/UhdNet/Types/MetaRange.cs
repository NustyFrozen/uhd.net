using System;
using System.Runtime.CompilerServices;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps a <c>uhd_meta_range_handle</c>. Disposable - frees the underlying handle.
/// </summary>
public sealed unsafe class MetaRange : IDisposable
{
    private UhdMetaRangeHandle _handle;
    private bool _ownsHandle;

    /// <summary>Create a new empty meta range.</summary>
    public MetaRange()
    {
        UhdMetaRangeHandle h = default;
        Interop.Check(NativeMethods.uhd_meta_range_make(&h));
        _handle = h;
        _ownsHandle = true;
    }

    private MetaRange(UhdMetaRangeHandle h, bool owns)
    {
        _handle = h;
        _ownsHandle = owns;
    }

    /// <summary>Native handle. Do not dispose externally if <c>this</c> owns it.</summary>
    public UhdMetaRangeHandle Handle => _handle;

    public double Start
    {
        get
        {
            ThrowIfDisposed();
            double v;
            Interop.Check(NativeMethods.uhd_meta_range_start(_handle, &v));
            return v;
        }
    }

    public double Stop
    {
        get
        {
            ThrowIfDisposed();
            double v;
            Interop.Check(NativeMethods.uhd_meta_range_stop(_handle, &v));
            return v;
        }
    }

    public double Step
    {
        get
        {
            ThrowIfDisposed();
            double v;
            Interop.Check(NativeMethods.uhd_meta_range_step(_handle, &v));
            return v;
        }
    }

    public nuint Count
    {
        get
        {
            ThrowIfDisposed();
            nuint v;
            Interop.Check(NativeMethods.uhd_meta_range_size(_handle, &v));
            return v;
        }
    }

    public Range this[nuint index]
    {
        get
        {
            ThrowIfDisposed();
            UhdRangeNative n;
            Interop.Check(NativeMethods.uhd_meta_range_at(_handle, index, &n));
            return Range.FromNative(n);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(MetaRange));
    }

    public void Dispose()
    {
        if (_ownsHandle && !_handle.IsNull)
        {
            UhdMetaRangeHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_meta_range_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~MetaRange() => Dispose();
}
