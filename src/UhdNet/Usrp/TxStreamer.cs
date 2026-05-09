using System;
using System.Runtime.CompilerServices;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps a <c>uhd_tx_streamer_handle</c>. Mirrors <see cref="RxStreamer"/> for the TX path.
/// </summary>
public sealed unsafe class TxStreamer : IDisposable
{
    private UhdTxStreamerHandle _handle;
    private readonly nuint _numChannels;
    private readonly nuint _maxSampsPerPacket;

    internal TxStreamer(UhdTxStreamerHandle handle)
    {
        _handle = handle;
        nuint n;
        Interop.Check(NativeMethods.uhd_tx_streamer_num_channels(_handle, &n));
        _numChannels = n;
        Interop.Check(NativeMethods.uhd_tx_streamer_max_num_samps(_handle, &n));
        _maxSampsPerPacket = n;
    }

    public UhdTxStreamerHandle Handle => _handle;

    public nuint NumberOfChannels => _numChannels;

    public nuint MaxSamplesPerPacket => _maxSampsPerPacket;

    /// <summary>Single-channel send from a raw sample pointer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint Send(
        void* buffer,
        nuint samplesPerBuffer,
        TxMetadata metadata,
        double timeoutSeconds = 0.1)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        void** buffs = stackalloc void*[1];
        buffs[0] = buffer;
        nuint sent;
        UhdTxMetadataHandle md = metadata.Handle;
        Interop.Check(NativeMethods.uhd_tx_streamer_send(
            _handle, buffs, samplesPerBuffer, &md, timeoutSeconds, &sent));
        return sent;
    }

    /// <summary>Multi-channel send. Lengths/widths follow the same rules as
    /// <see cref="RxStreamer.Receive(ReadOnlySpan{IntPtr}, nuint, RxMetadata, double, bool)"/>.</summary>
    public nuint Send(
        ReadOnlySpan<IntPtr> channelBuffers,
        nuint samplesPerBuffer,
        TxMetadata metadata,
        double timeoutSeconds = 0.1)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        if ((nuint)channelBuffers.Length != _numChannels)
        {
            throw new ArgumentException(
                $"Expected {_numChannels} channel buffers, got {channelBuffers.Length}.",
                nameof(channelBuffers));
        }

        nuint sent;
        UhdTxMetadataHandle md = metadata.Handle;
        fixed (IntPtr* p = channelBuffers)
        {
            Interop.Check(NativeMethods.uhd_tx_streamer_send(
                _handle, (void**)p, samplesPerBuffer, &md, timeoutSeconds, &sent));
        }
        return sent;
    }

    /// <summary>Single-channel send of <c>fc32</c> samples.</summary>
    public nuint Send(
        ReadOnlySpan<Complex32> samples,
        TxMetadata metadata,
        double timeoutSeconds = 0.1)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        if (_numChannels != 1)
        {
            throw new InvalidOperationException(
                $"Single-channel Send(ReadOnlySpan<Complex32>) called on a {_numChannels}-channel streamer.");
        }
        fixed (Complex32* p = samples)
        {
            return Send(p, (nuint)samples.Length, metadata, timeoutSeconds);
        }
    }

    /// <summary>Single-channel send of <c>sc16</c> samples.</summary>
    public nuint Send(
        ReadOnlySpan<Sc16Sample> samples,
        TxMetadata metadata,
        double timeoutSeconds = 0.1)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        if (_numChannels != 1)
        {
            throw new InvalidOperationException(
                $"Single-channel Send(ReadOnlySpan<Sc16Sample>) called on a {_numChannels}-channel streamer.");
        }
        fixed (Sc16Sample* p = samples)
        {
            return Send(p, (nuint)samples.Length, metadata, timeoutSeconds);
        }
    }

    /// <summary>
    /// Try to receive an asynchronous TX message (e.g. underflow notification).
    /// </summary>
    /// <returns><c>true</c> if a message was received and populated into <paramref name="metadata"/>.</returns>
    public bool TryReceiveAsync(AsyncMetadata metadata, double timeoutSeconds = 0.1)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        UhdAsyncMetadataHandle md = metadata.Handle;
        Interop.Check(NativeMethods.uhd_tx_streamer_recv_async_msg(
            _handle, &md, timeoutSeconds, out bool valid));
        return valid;
    }

    public string GetLastError()
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            _ = NativeMethods.uhd_tx_streamer_last_error(_handle, p, (nuint)buf.Length);
            return Interop.Utf8ToString(p);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(TxStreamer));
    }

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdTxStreamerHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_tx_streamer_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~TxStreamer() => Dispose();
}
