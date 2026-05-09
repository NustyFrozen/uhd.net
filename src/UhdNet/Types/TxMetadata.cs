using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps an <c>uhd_tx_metadata_handle</c>. The metadata is constructed once with the desired
/// burst flags and reused on every send.
/// </summary>
public sealed unsafe class TxMetadata : IDisposable
{
    private UhdTxMetadataHandle _handle;

    public TxMetadata(bool startOfBurst = true, bool endOfBurst = true,
                      bool hasTimeSpec = false, TimeSpec time = default)
    {
        UhdTxMetadataHandle h = default;
        Interop.Check(NativeMethods.uhd_tx_metadata_make(
            &h, hasTimeSpec, time.FullSeconds, time.FractionalSeconds, startOfBurst, endOfBurst));
        _handle = h;
    }

    public UhdTxMetadataHandle Handle => _handle;

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdTxMetadataHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_tx_metadata_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~TxMetadata() => Dispose();
}
