using System;
using System.Runtime.CompilerServices;
using System.Text;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// High-level wrapper around <c>uhd::usrp::multi_usrp</c>. The class is the entry point for
/// device discovery, control, and streamer creation. Always dispose to release the underlying
/// native handle.
/// </summary>
/// <example>
/// <code>
/// using var usrp = Usrp.Make("type=b200");
/// usrp.SetRxRate(1_000_000, 0);
/// usrp.SetRxFrequency(new TuneRequest(2_400_000_000), 0, out _);
/// usrp.SetRxGain(40, 0);
/// using var rx = usrp.GetRxStream(StreamArgs.SingleChannelFc32());
/// </code>
/// </example>
public sealed unsafe class Usrp : IDisposable
{
    private UhdUsrpHandle _handle;

    private Usrp(UhdUsrpHandle handle)
    {
        _handle = handle;
    }

    public UhdUsrpHandle Handle => _handle;

    /// <summary>
    /// Construct a USRP from device args (e.g. <c>"type=b200"</c>, <c>"addr=192.168.10.2"</c>).
    /// </summary>
    public static Usrp Make(string args = "")
    {
        Span<byte> argBuf = stackalloc byte[Interop.Utf8MaxBytes(args)];
        UhdUsrpHandle h = default;
        fixed (byte* a = argBuf)
        {
            byte* aPtr = Interop.StringToUtf8(args, argBuf);
            Interop.Check(NativeMethods.uhd_usrp_make(&h, aPtr));
        }
        return new Usrp(h);
    }

    /// <summary>
    /// Find all USRP devices reachable via the given <paramref name="args"/> hint (may be empty).
    /// Returns a string vector of device descriptors.
    /// </summary>
    public static UhdStringVector Find(string args = "")
    {
        var sv = new UhdStringVector();
        try
        {
            Span<byte> argBuf = stackalloc byte[Interop.Utf8MaxBytes(args)];
            byte* aPtr = Interop.StringToUtf8(args, argBuf);
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_find(aPtr, &h));
            return sv;
        }
        catch
        {
            sv.Dispose();
            throw;
        }
    }

    /// <summary>Pretty-print description of the device.</summary>
    public string GetPrettyString()
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[2048];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_pp_string(_handle, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public string GetMotherboardName(nuint mboard = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_mboard_name(_handle, mboard, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public nuint NumberOfMotherboards
    {
        get
        {
            ThrowIfDisposed();
            nuint v;
            Interop.Check(NativeMethods.uhd_usrp_get_num_mboards(_handle, &v));
            return v;
        }
    }

    // ---- Master clock ----------------------------------------------------------------------

    public double GetMasterClockRate(nuint mboard = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_master_clock_rate(_handle, mboard, &v));
        return v;
    }

    public void SetMasterClockRate(double rate, nuint mboard = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_master_clock_rate(_handle, rate, mboard));
    }

    // ---- Time ------------------------------------------------------------------------------

    public TimeSpec GetTimeNow(nuint mboard = 0)
    {
        ThrowIfDisposed();
        long full;
        double frac;
        Interop.Check(NativeMethods.uhd_usrp_get_time_now(_handle, mboard, &full, &frac));
        return new TimeSpec(full, frac);
    }

    public TimeSpec GetTimeLastPps(nuint mboard = 0)
    {
        ThrowIfDisposed();
        long full;
        double frac;
        Interop.Check(NativeMethods.uhd_usrp_get_time_last_pps(_handle, mboard, &full, &frac));
        return new TimeSpec(full, frac);
    }

    public void SetTimeNow(TimeSpec time, nuint mboard = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_time_now(
            _handle, time.FullSeconds, time.FractionalSeconds, mboard));
    }

    public void SetTimeNextPps(TimeSpec time, nuint mboard = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_time_next_pps(
            _handle, time.FullSeconds, time.FractionalSeconds, mboard));
    }

    public void SetTimeUnknownPps(TimeSpec time)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_time_unknown_pps(
            _handle, time.FullSeconds, time.FractionalSeconds));
    }

    public bool IsTimeSynchronized
    {
        get
        {
            ThrowIfDisposed();
            Interop.Check(NativeMethods.uhd_usrp_get_time_synchronized(_handle, out bool v));
            return v;
        }
    }

    public void SetCommandTime(TimeSpec time, nuint mboard = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_command_time(
            _handle, time.FullSeconds, time.FractionalSeconds, mboard));
    }

    public void ClearCommandTime(nuint mboard = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_clear_command_time(_handle, mboard));
    }

    // ---- Sources ---------------------------------------------------------------------------

    public void SetTimeSource(string source, nuint mboard = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(source)];
        byte* p = Interop.StringToUtf8(source, buf);
        Interop.Check(NativeMethods.uhd_usrp_set_time_source(_handle, p, mboard));
    }

    public string GetTimeSource(nuint mboard = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_time_source(_handle, mboard, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public UhdStringVector GetTimeSources(nuint mboard = 0)
    {
        ThrowIfDisposed();
        var sv = new UhdStringVector();
        try
        {
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_get_time_sources(_handle, mboard, &h));
            return sv;
        }
        catch { sv.Dispose(); throw; }
    }

    public void SetClockSource(string source, nuint mboard = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(source)];
        byte* p = Interop.StringToUtf8(source, buf);
        Interop.Check(NativeMethods.uhd_usrp_set_clock_source(_handle, p, mboard));
    }

    public string GetClockSource(nuint mboard = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_clock_source(_handle, mboard, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public UhdStringVector GetClockSources(nuint mboard = 0)
    {
        ThrowIfDisposed();
        var sv = new UhdStringVector();
        try
        {
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_get_clock_sources(_handle, mboard, &h));
            return sv;
        }
        catch { sv.Dispose(); throw; }
    }

    // ---- Stream factories ------------------------------------------------------------------

    /// <summary>Create an RX streamer using the given args.</summary>
    public RxStreamer GetRxStream(StreamArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ThrowIfDisposed();

        UhdRxStreamerHandle streamer = default;
        Interop.Check(NativeMethods.uhd_rx_streamer_make(&streamer));

        try
        {
            BuildAndCallStreamFactory(args, streamer.Value, isRx: true);
            return new RxStreamer(streamer);
        }
        catch
        {
            UhdRxStreamerHandle s = streamer;
            _ = NativeMethods.uhd_rx_streamer_free(&s);
            throw;
        }
    }

    /// <summary>Create a TX streamer using the given args.</summary>
    public TxStreamer GetTxStream(StreamArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ThrowIfDisposed();

        UhdTxStreamerHandle streamer = default;
        Interop.Check(NativeMethods.uhd_tx_streamer_make(&streamer));

        try
        {
            BuildAndCallStreamFactory(args, streamer.Value, isRx: false);
            return new TxStreamer(streamer);
        }
        catch
        {
            UhdTxStreamerHandle s = streamer;
            _ = NativeMethods.uhd_tx_streamer_free(&s);
            throw;
        }
    }

    private void BuildAndCallStreamFactory(StreamArgs args, IntPtr streamerHandleValue, bool isRx)
    {
        // Stack-pin the channel list and the three string fields (cpu_format, otw_format, args).
        Span<byte> cpuBuf = stackalloc byte[Interop.Utf8MaxBytes(args.CpuFormat)];
        Span<byte> otwBuf = stackalloc byte[Interop.Utf8MaxBytes(args.OtwFormat)];
        Span<byte> argsBuf = stackalloc byte[Interop.Utf8MaxBytes(args.Args)];

        // The channel list is small enough for stack allocation in practical setups.
        int chanCount = args.Channels.Count;
        Span<nuint> chans = chanCount <= 32
            ? stackalloc nuint[32].Slice(0, chanCount)
            : new nuint[chanCount];
        for (int i = 0; i < chanCount; i++) chans[i] = args.Channels[i];

        fixed (byte* cpu = cpuBuf)
        fixed (byte* otw = otwBuf)
        fixed (byte* aa = argsBuf)
        fixed (nuint* ch = chans)
        {
            byte* cpuPtr = Interop.StringToUtf8(args.CpuFormat, cpuBuf);
            byte* otwPtr = Interop.StringToUtf8(args.OtwFormat, otwBuf);
            byte* argsPtr = Interop.StringToUtf8(args.Args, argsBuf);

            UhdStreamArgsNative native = new()
            {
                CpuFormat = cpuPtr,
                OtwFormat = otwPtr,
                Args = argsPtr,
                ChannelList = ch,
                NChannels = chanCount,
            };

            if (isRx)
            {
                Interop.Check(NativeMethods.uhd_usrp_get_rx_stream(
                    _handle, &native, new UhdRxStreamerHandle(streamerHandleValue)));
            }
            else
            {
                Interop.Check(NativeMethods.uhd_usrp_get_tx_stream(
                    _handle, &native, new UhdTxStreamerHandle(streamerHandleValue)));
            }
        }
    }

    // ---- RX channel control ----------------------------------------------------------------

    public nuint NumberOfRxChannels
    {
        get
        {
            ThrowIfDisposed();
            nuint v;
            Interop.Check(NativeMethods.uhd_usrp_get_rx_num_channels(_handle, &v));
            return v;
        }
    }

    public string GetRxSubdevName(nuint chan = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_rx_subdev_name(_handle, chan, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public void SetRxRate(double sampleRate, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_rx_rate(_handle, sampleRate, chan));
    }

    public double GetRxRate(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_rx_rate(_handle, chan, &v));
        return v;
    }

    public void SetRxFrequency(in TuneRequest request, nuint chan, out TuneResult result)
    {
        ThrowIfDisposed();
        Span<byte> argBuf = stackalloc byte[Interop.Utf8MaxBytes(request.Args)];
        byte* argsPtr = Interop.StringToUtf8(request.Args, argBuf);

        UhdTuneRequestNative req = new()
        {
            TargetFreq = request.TargetFrequency,
            RfFreqPolicy = request.RfFrequencyPolicy,
            RfFreq = request.RfFrequency,
            DspFreqPolicy = request.DspFrequencyPolicy,
            DspFreq = request.DspFrequency,
            Args = argsPtr,
        };
        UhdTuneResultNative res;
        Interop.Check(NativeMethods.uhd_usrp_set_rx_freq(_handle, &req, chan, &res));
        result = TuneResult.FromNative(res);
    }

    public double GetRxFrequency(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_rx_freq(_handle, chan, &v));
        return v;
    }

    public void SetRxGain(double gain, nuint chan = 0, string? gainName = null)
    {
        ThrowIfDisposed();
        Span<byte> nameBuf = stackalloc byte[Interop.Utf8MaxBytes(gainName ?? string.Empty) + 1];
        byte* p = Interop.StringToUtf8(gainName ?? string.Empty, nameBuf);
        Interop.Check(NativeMethods.uhd_usrp_set_rx_gain(_handle, gain, chan, p));
    }

    public double GetRxGain(nuint chan = 0, string? gainName = null)
    {
        ThrowIfDisposed();
        Span<byte> nameBuf = stackalloc byte[Interop.Utf8MaxBytes(gainName ?? string.Empty) + 1];
        byte* p = Interop.StringToUtf8(gainName ?? string.Empty, nameBuf);
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_rx_gain(_handle, chan, p, &v));
        return v;
    }

    public void SetNormalizedRxGain(double gain01, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_normalized_rx_gain(_handle, gain01, chan));
    }

    public double GetNormalizedRxGain(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_normalized_rx_gain(_handle, chan, &v));
        return v;
    }

    public void SetRxAgc(bool enable, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_rx_agc(_handle, enable, chan));
    }

    public void SetRxAntenna(string antenna, nuint chan = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(antenna)];
        byte* p = Interop.StringToUtf8(antenna, buf);
        Interop.Check(NativeMethods.uhd_usrp_set_rx_antenna(_handle, p, chan));
    }

    public string GetRxAntenna(nuint chan = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_rx_antenna(_handle, chan, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public UhdStringVector GetRxAntennas(nuint chan = 0)
    {
        ThrowIfDisposed();
        var sv = new UhdStringVector();
        try
        {
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_get_rx_antennas(_handle, chan, &h));
            return sv;
        }
        catch { sv.Dispose(); throw; }
    }

    public void SetRxBandwidth(double hz, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_rx_bandwidth(_handle, hz, chan));
    }

    public double GetRxBandwidth(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_rx_bandwidth(_handle, chan, &v));
        return v;
    }

    public void SetRxDcOffsetEnabled(bool enabled, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_rx_dc_offset_enabled(_handle, enabled, chan));
    }

    public void SetRxIqBalanceEnabled(bool enabled, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_rx_iq_balance_enabled(_handle, enabled, chan));
    }

    public MetaRange GetRxFrequencyRange(nuint chan = 0)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Interop.Check(NativeMethods.uhd_usrp_get_rx_freq_range(_handle, chan, r.Handle));
        return r;
    }

    public MetaRange GetRxGainRange(nuint chan = 0, string? gainName = null)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(gainName ?? string.Empty) + 1];
        byte* p = Interop.StringToUtf8(gainName ?? string.Empty, buf);
        Interop.Check(NativeMethods.uhd_usrp_get_rx_gain_range(_handle, p, chan, r.Handle));
        return r;
    }

    public MetaRange GetRxRates(nuint chan = 0)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Interop.Check(NativeMethods.uhd_usrp_get_rx_rates(_handle, chan, r.Handle));
        return r;
    }

    public MetaRange GetRxBandwidthRange(nuint chan = 0)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Interop.Check(NativeMethods.uhd_usrp_get_rx_bandwidth_range(_handle, chan, r.Handle));
        return r;
    }

    // ---- TX channel control ----------------------------------------------------------------

    public nuint NumberOfTxChannels
    {
        get
        {
            ThrowIfDisposed();
            nuint v;
            Interop.Check(NativeMethods.uhd_usrp_get_tx_num_channels(_handle, &v));
            return v;
        }
    }

    public void SetTxRate(double sampleRate, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_tx_rate(_handle, sampleRate, chan));
    }

    public double GetTxRate(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_tx_rate(_handle, chan, &v));
        return v;
    }

    public void SetTxFrequency(in TuneRequest request, nuint chan, out TuneResult result)
    {
        ThrowIfDisposed();
        Span<byte> argBuf = stackalloc byte[Interop.Utf8MaxBytes(request.Args)];
        byte* argsPtr = Interop.StringToUtf8(request.Args, argBuf);

        UhdTuneRequestNative req = new()
        {
            TargetFreq = request.TargetFrequency,
            RfFreqPolicy = request.RfFrequencyPolicy,
            RfFreq = request.RfFrequency,
            DspFreqPolicy = request.DspFrequencyPolicy,
            DspFreq = request.DspFrequency,
            Args = argsPtr,
        };
        UhdTuneResultNative res;
        Interop.Check(NativeMethods.uhd_usrp_set_tx_freq(_handle, &req, chan, &res));
        result = TuneResult.FromNative(res);
    }

    public double GetTxFrequency(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_tx_freq(_handle, chan, &v));
        return v;
    }

    public void SetTxGain(double gain, nuint chan = 0, string? gainName = null)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(gainName ?? string.Empty) + 1];
        byte* p = Interop.StringToUtf8(gainName ?? string.Empty, buf);
        Interop.Check(NativeMethods.uhd_usrp_set_tx_gain(_handle, gain, chan, p));
    }

    public double GetTxGain(nuint chan = 0, string? gainName = null)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(gainName ?? string.Empty) + 1];
        byte* p = Interop.StringToUtf8(gainName ?? string.Empty, buf);
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_tx_gain(_handle, chan, p, &v));
        return v;
    }

    public void SetNormalizedTxGain(double gain01, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_normalized_tx_gain(_handle, gain01, chan));
    }

    public double GetNormalizedTxGain(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_normalized_tx_gain(_handle, chan, &v));
        return v;
    }

    public void SetTxAntenna(string antenna, nuint chan = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(antenna)];
        byte* p = Interop.StringToUtf8(antenna, buf);
        Interop.Check(NativeMethods.uhd_usrp_set_tx_antenna(_handle, p, chan));
    }

    public string GetTxAntenna(nuint chan = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            Interop.Check(NativeMethods.uhd_usrp_get_tx_antenna(_handle, chan, p, (nuint)buf.Length));
            return Interop.Utf8ToString(p);
        }
    }

    public void SetTxBandwidth(double hz, nuint chan = 0)
    {
        ThrowIfDisposed();
        Interop.Check(NativeMethods.uhd_usrp_set_tx_bandwidth(_handle, hz, chan));
    }

    public double GetTxBandwidth(nuint chan = 0)
    {
        ThrowIfDisposed();
        double v;
        Interop.Check(NativeMethods.uhd_usrp_get_tx_bandwidth(_handle, chan, &v));
        return v;
    }

    public MetaRange GetTxFrequencyRange(nuint chan = 0)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Interop.Check(NativeMethods.uhd_usrp_get_tx_freq_range(_handle, chan, r.Handle));
        return r;
    }

    public MetaRange GetTxGainRange(nuint chan = 0, string? gainName = null)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Span<byte> buf = stackalloc byte[Interop.Utf8MaxBytes(gainName ?? string.Empty) + 1];
        byte* p = Interop.StringToUtf8(gainName ?? string.Empty, buf);
        Interop.Check(NativeMethods.uhd_usrp_get_tx_gain_range(_handle, p, chan, r.Handle));
        return r;
    }

    public MetaRange GetTxRates(nuint chan = 0)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Interop.Check(NativeMethods.uhd_usrp_get_tx_rates(_handle, chan, r.Handle));
        return r;
    }

    public MetaRange GetTxBandwidthRange(nuint chan = 0)
    {
        ThrowIfDisposed();
        var r = new MetaRange();
        Interop.Check(NativeMethods.uhd_usrp_get_tx_bandwidth_range(_handle, chan, r.Handle));
        return r;
    }

    // ---- Sensors ---------------------------------------------------------------------------

    public UhdStringVector GetMotherboardSensorNames(nuint mboard = 0)
    {
        ThrowIfDisposed();
        var sv = new UhdStringVector();
        try
        {
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_get_mboard_sensor_names(_handle, mboard, &h));
            return sv;
        }
        catch { sv.Dispose(); throw; }
    }

    public UhdStringVector GetRxSensorNames(nuint chan = 0)
    {
        ThrowIfDisposed();
        var sv = new UhdStringVector();
        try
        {
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_get_rx_sensor_names(_handle, chan, &h));
            return sv;
        }
        catch { sv.Dispose(); throw; }
    }

    public UhdStringVector GetTxSensorNames(nuint chan = 0)
    {
        ThrowIfDisposed();
        var sv = new UhdStringVector();
        try
        {
            UhdStringVectorHandle h = sv.Handle;
            Interop.Check(NativeMethods.uhd_usrp_get_tx_sensor_names(_handle, chan, &h));
            return sv;
        }
        catch { sv.Dispose(); throw; }
    }

    public string GetLastError()
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
        fixed (byte* p = buf)
        {
            _ = NativeMethods.uhd_usrp_last_error(_handle, p, (nuint)buf.Length);
            return Interop.Utf8ToString(p);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(Usrp));
    }

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdUsrpHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_usrp_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~Usrp() => Dispose();
}
