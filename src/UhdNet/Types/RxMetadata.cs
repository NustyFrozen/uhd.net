using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps an <c>uhd_rx_metadata_handle</c>. Created on the managed side once per stream and
/// reused across <see cref="RxStreamer.Receive"/> calls to avoid allocations in the hot path.
/// </summary>
public sealed unsafe class RxMetadata : IDisposable
{
    private UhdRxMetadataHandle _handle;

    public RxMetadata()
    {
        UhdRxMetadataHandle h = default;
        Interop.Check(NativeMethods.uhd_rx_metadata_make(&h));
        _handle = h;
    }

    public UhdRxMetadataHandle Handle => _handle;

    public bool HasTimeSpec
    {
        get
        {
            ThrowIfDisposed();
            Interop.Check(NativeMethods.uhd_rx_metadata_has_time_spec(_handle, out bool result));
            return result;
        }
    }

    public TimeSpec Time
    {
        get
        {
            ThrowIfDisposed();
            long full;
            double frac;
            Interop.Check(NativeMethods.uhd_rx_metadata_time_spec(_handle, &full, &frac));
            return new TimeSpec(full, frac);
        }
    }

    public bool MoreFragments
    {
        get
        {
            ThrowIfDisposed();
            Interop.Check(NativeMethods.uhd_rx_metadata_more_fragments(_handle, out bool result));
            return result;
        }
    }

    public nuint FragmentOffset
    {
        get
        {
            ThrowIfDisposed();
            nuint off;
            Interop.Check(NativeMethods.uhd_rx_metadata_fragment_offset(_handle, &off));
            return off;
        }
    }

    public bool StartOfBurst
    {
        get
        {
            ThrowIfDisposed();
            Interop.Check(NativeMethods.uhd_rx_metadata_start_of_burst(_handle, out bool result));
            return result;
        }
    }

    public bool EndOfBurst
    {
        get
        {
            ThrowIfDisposed();
            Interop.Check(NativeMethods.uhd_rx_metadata_end_of_burst(_handle, out bool result));
            return result;
        }
    }

    public bool OutOfSequence
    {
        get
        {
            ThrowIfDisposed();
            Interop.Check(NativeMethods.uhd_rx_metadata_out_of_sequence(_handle, out bool result));
            return result;
        }
    }

    public UhdRxMetadataErrorCode ErrorCode
    {
        get
        {
            ThrowIfDisposed();
            UhdRxMetadataErrorCode code;
            Interop.Check(NativeMethods.uhd_rx_metadata_error_code(_handle, &code));
            return code;
        }
    }

    /// <summary>Pretty-printed metadata description.</summary>
    public string ToPrettyString()
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_rx_metadata_to_pp_string(_handle, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    /// <summary>Human-readable form of <see cref="ErrorCode"/>.</summary>
    public string GetErrorString()
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_rx_metadata_strerror(_handle, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(RxMetadata));
    }

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdRxMetadataHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_rx_metadata_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~RxMetadata() => Dispose();
}
