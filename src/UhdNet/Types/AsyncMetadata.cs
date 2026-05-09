using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps an <c>uhd_async_metadata_handle</c> for receiving asynchronous TX events.
/// </summary>
public sealed unsafe class AsyncMetadata : IDisposable
{
    private UhdAsyncMetadataHandle _handle;

    public AsyncMetadata()
    {
        UhdAsyncMetadataHandle h = default;
        Interop.Check(NativeMethods.uhd_async_metadata_make(&h));
        _handle = h;
    }

    public UhdAsyncMetadataHandle Handle => _handle;

    public nuint Channel
    {
        get
        {
            ThrowIfDisposed();
            nuint v;
            Interop.Check(NativeMethods.uhd_async_metadata_channel(_handle, &v));
            return v;
        }
    }

    public UhdAsyncEventCode EventCode
    {
        get
        {
            ThrowIfDisposed();
            UhdAsyncEventCode v;
            Interop.Check(NativeMethods.uhd_async_metadata_event_code(_handle, &v));
            return v;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(AsyncMetadata));
    }

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdAsyncMetadataHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_async_metadata_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~AsyncMetadata() => Dispose();
}
