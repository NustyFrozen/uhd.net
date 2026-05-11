using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps a <c>uhd_rx_streamer_handle</c>. Optimized for zero-GC streaming: <see cref="Receive"/>
/// overloads accept either raw pointers or pre-pinned spans, and <see cref="IssueStreamCommand"/>
/// stack-allocates its native struct.
/// </summary>
/// <remarks>
/// Sample formats follow the UHD CPU format set:
/// <list type="bullet">
///   <item><c>fc32</c>: interleaved <see cref="float"/> I/Q pairs (8 bytes per sample).</item>
///   <item><c>fc64</c>: interleaved <see cref="double"/> I/Q pairs (16 bytes per sample).</item>
///   <item><c>sc16</c>: interleaved <see cref="short"/> I/Q pairs (4 bytes per sample).</item>
///   <item><c>sc8</c>: interleaved <see cref="sbyte"/> I/Q pairs (2 bytes per sample).</item>
/// </list>
/// All buffer sizes throughout the API are in <em>samples</em> (one I/Q pair), not bytes.
/// </remarks>
public sealed unsafe class RxStreamer : IDisposable
{
    private UhdRxStreamerHandle _handle;
    private readonly nuint _numChannels;
    private readonly nuint _maxSampsPerPacket;

    internal RxStreamer(UhdRxStreamerHandle handle)
    {
        _handle = handle;
        nuint n;
        Interop.Check(NativeMethods.uhd_rx_streamer_num_channels(_handle, &n));
        _numChannels = n;
        Interop.Check(NativeMethods.uhd_rx_streamer_max_num_samps(_handle, &n));
        _maxSampsPerPacket = n;
    }

    public UhdRxStreamerHandle Handle => _handle;

    public nuint NumberOfChannels => _numChannels;

    public nuint MaxSamplesPerPacket => _maxSampsPerPacket;

    /// <summary>Issue a stream command (start, stop, finite). Stack-allocated, no GC.</summary>
    public void IssueStreamCommand(in StreamCommand command)
    {
        ThrowIfDisposed();
        UhdStreamCmdNative n = command.ToNative();
        Interop.Check(NativeMethods.uhd_rx_streamer_issue_stream_cmd(_handle, &n));
    }

    /// <summary>
    /// Hot path. Single-channel receive into a pre-pinned buffer. Pass the number of samples
    /// the buffer can hold (one sample = one I/Q pair, regardless of CPU format).
    /// </summary>
    /// <param name="buffer">Pointer to native sample memory. Must be at least
    /// <paramref name="samplesPerBuffer"/> samples wide.</param>
    /// <param name="samplesPerBuffer">Maximum samples to receive into the buffer.</param>
    /// <param name="metadata">Reusable metadata handle.</param>
    /// <param name="timeoutSeconds">How long to wait for a packet.</param>
    /// <param name="onePacket">If <c>true</c>, return after a single radio packet; otherwise
    /// keep filling the buffer until full or timeout.</param>
    /// <returns>The number of samples actually received.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint Receive(
        void* buffer,
        nuint samplesPerBuffer,
        RxMetadata metadata,
        double timeoutSeconds = 0.1,
        bool onePacket = false)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        void** buffs = stackalloc void*[1];
        buffs[0] = buffer;
        nuint received;
        UhdRxMetadataHandle md = metadata.Handle;
        Interop.Check(NativeMethods.uhd_rx_streamer_recv(
            _handle, buffs, samplesPerBuffer, &md, timeoutSeconds, onePacket, &received));
        return received;
    }

    /// <summary>
    /// Zero-check hot path for single-channel receive. Skips all null/disposed guards.
    /// Caller contract: streamer and metadata handle are valid for the duration of the call.
    /// <para>
    /// Set up once before the recv loop, then reuse each iteration:
    /// <code>
    ///   void** buffs = stackalloc void*[1];
    ///   UhdRxMetadataHandle mdH = metadata.Handle;
    ///   // per iteration:
    ///   buffs[0] = myBuffer;
    ///   streamer.ReceiveFast(buffs, want, &amp;mdH, timeout, false);
    /// </code>
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public nuint ReceiveFast(
        void** buffs,
        nuint samplesPerBuffer,
        UhdRxMetadataHandle* md,
        double timeoutSeconds,
        bool onePacket)
    {
        nuint received;
        Interop.Check(NativeMethods.uhd_rx_streamer_recv_fast(
            _handle, buffs, samplesPerBuffer, md, timeoutSeconds, onePacket, &received));
        return received;
    }

    /// <summary>
    /// Multi-channel receive. <paramref name="channelBuffers"/> must contain one pointer per
    /// channel of the streamer; each buffer must be at least
    /// <paramref name="samplesPerBuffer"/> samples wide.
    /// </summary>
    public nuint Receive(
        ReadOnlySpan<IntPtr> channelBuffers,
        nuint samplesPerBuffer,
        RxMetadata metadata,
        double timeoutSeconds = 0.1,
        bool onePacket = false)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        if ((nuint)channelBuffers.Length != _numChannels)
        {
            throw new ArgumentException(
                $"Expected {_numChannels} channel buffers, got {channelBuffers.Length}.",
                nameof(channelBuffers));
        }

        nuint received;
        UhdRxMetadataHandle md = metadata.Handle;
        fixed (IntPtr* p = channelBuffers)
        {
            Interop.Check(NativeMethods.uhd_rx_streamer_recv(
                _handle, (void**)p, samplesPerBuffer, &md, timeoutSeconds, onePacket, &received));
        }
        return received;
    }

    /// <summary>
    /// Single-channel receive into a span of complex floats (<c>fc32</c>). The span is pinned
    /// for the duration of the native call. The streamer must have been built with
    /// <c>cpu_format = "fc32"</c> and a single channel.
    /// </summary>
    public nuint Receive(
        Span<Complex32> samples,
        RxMetadata metadata,
        double timeoutSeconds = 0.1,
        bool onePacket = false)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        if (_numChannels != 1)
        {
            throw new InvalidOperationException(
                $"Single-channel Receive(Span<Complex32>) called on a {_numChannels}-channel streamer.");
        }
        fixed (Complex32* p = samples)
        {
            return Receive(p, (nuint)samples.Length, metadata, timeoutSeconds, onePacket);
        }
    }

    /// <summary>
    /// Single-channel receive into a span of <c>sc16</c> samples (interleaved
    /// <see cref="short"/> I/Q pairs). The streamer must have been built with
    /// <c>cpu_format = "sc16"</c>.
    /// </summary>
    public nuint Receive(
        Span<Sc16Sample> samples,
        RxMetadata metadata,
        double timeoutSeconds = 0.1,
        bool onePacket = false)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ThrowIfDisposed();
        if (_numChannels != 1)
        {
            throw new InvalidOperationException(
                $"Single-channel Receive(Span<Sc16Sample>) called on a {_numChannels}-channel streamer.");
        }
        fixed (Sc16Sample* p = samples)
        {
            return Receive(p, (nuint)samples.Length, metadata, timeoutSeconds, onePacket);
        }
    }

    public string GetLastError()
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            _ = NativeMethods.uhd_rx_streamer_last_error(_handle, p, (nuint)buf.Length);
            return Interop.Utf8ToString(p);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(RxStreamer));
    }

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdRxStreamerHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_rx_streamer_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~RxStreamer() => Dispose();
}
