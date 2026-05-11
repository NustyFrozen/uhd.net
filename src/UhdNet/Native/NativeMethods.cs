using System;
using System.Runtime.InteropServices;

namespace UhdNet.Native;

/// <summary>
/// Direct P/Invoke surface for the UHD C API. Hand-translated from <c>uhd/usrp/usrp.h</c>,
/// <c>uhd/types/*.h</c>, <c>uhd/error.h</c>, and <c>uhd/version.h</c>. All entry points
/// are <c>cdecl</c> and use UTF-8 null-terminated strings.
/// </summary>
public static unsafe partial class NativeMethods
{
    private const string Lib = UhdNativeLibrary.LibraryName;

    // ---- Error / version / thread ----------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_get_last_error(byte* error_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_get_abi_string(byte* abi_string_out, nuint buffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_get_version_string(byte* version_out, nuint buffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_set_thread_priority(float priority, [MarshalAs(UnmanagedType.U1)] bool realtime);

    // ---- USRP make / free ------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_find(byte* args, UhdStringVectorHandle* strings_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_make(UhdUsrpHandle* h, byte* args);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_free(UhdUsrpHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_last_error(UhdUsrpHandle h, byte* error_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_pp_string(UhdUsrpHandle h, byte* pp_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_mboard_name(UhdUsrpHandle h, nuint mboard, byte* name_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_num_mboards(UhdUsrpHandle h, nuint* num_mboards_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_master_clock_rate(UhdUsrpHandle h, nuint mboard, double* clock_rate_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_master_clock_rate(UhdUsrpHandle h, double rate, nuint mboard);

    // ---- Time / clock / sources ------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_time_now(UhdUsrpHandle h, nuint mboard, long* full_secs_out, double* frac_secs_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_time_last_pps(UhdUsrpHandle h, nuint mboard, long* full_secs_out, double* frac_secs_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_time_now(UhdUsrpHandle h, long full_secs, double frac_secs, nuint mboard);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_time_next_pps(UhdUsrpHandle h, long full_secs, double frac_secs, nuint mboard);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_time_unknown_pps(UhdUsrpHandle h, long full_secs, double frac_secs);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_time_synchronized(UhdUsrpHandle h, [MarshalAs(UnmanagedType.U1)] out bool result_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_command_time(UhdUsrpHandle h, long full_secs, double frac_secs, nuint mboard);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_clear_command_time(UhdUsrpHandle h, nuint mboard);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_time_source(UhdUsrpHandle h, byte* time_source, nuint mboard);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_time_source(UhdUsrpHandle h, nuint mboard, byte* time_source_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_time_sources(UhdUsrpHandle h, nuint mboard, UhdStringVectorHandle* time_sources_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_clock_source(UhdUsrpHandle h, byte* clock_source, nuint mboard);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_clock_source(UhdUsrpHandle h, nuint mboard, byte* clock_source_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_clock_sources(UhdUsrpHandle h, nuint mboard, UhdStringVectorHandle* clock_sources_out);

    // ---- Streams ---------------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_stream(UhdUsrpHandle h, UhdStreamArgsNative* stream_args, UhdRxStreamerHandle h_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_stream(UhdUsrpHandle h, UhdStreamArgsNative* stream_args, UhdTxStreamerHandle h_out);

    // ---- RX streamer -----------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_make(UhdRxStreamerHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_free(UhdRxStreamerHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_num_channels(UhdRxStreamerHandle h, nuint* out_n);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_max_num_samps(UhdRxStreamerHandle h, nuint* out_n);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_recv(
        UhdRxStreamerHandle h,
        void** buffs,
        nuint samps_per_buff,
        UhdRxMetadataHandle* md,
        double timeout,
        [MarshalAs(UnmanagedType.U1)] bool one_packet,
        nuint* items_recvd);

    // Suppress the GC transition for the hot recv path. Safe when called inside
    // GC.TryStartNoGCRegion: GC is already pinned, and in normal streaming the call
    // returns within a packet period (µs-range), not the full timeout.
    [LibraryImport(Lib, EntryPoint = "uhd_rx_streamer_recv")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [SuppressGCTransition]
    internal static partial UhdErrorCode uhd_rx_streamer_recv_fast(
        UhdRxStreamerHandle h,
        void** buffs,
        nuint samps_per_buff,
        UhdRxMetadataHandle* md,
        double timeout,
        [MarshalAs(UnmanagedType.U1)] bool one_packet,
        nuint* items_recvd);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_issue_stream_cmd(UhdRxStreamerHandle h, UhdStreamCmdNative* cmd);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_streamer_last_error(UhdRxStreamerHandle h, byte* error_out, nuint strbuffer_len);

    // ---- TX streamer -----------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_make(UhdTxStreamerHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_free(UhdTxStreamerHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_num_channels(UhdTxStreamerHandle h, nuint* out_n);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_max_num_samps(UhdTxStreamerHandle h, nuint* out_n);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_send(
        UhdTxStreamerHandle h,
        void** buffs,
        nuint samps_per_buff,
        UhdTxMetadataHandle* md,
        double timeout,
        nuint* items_sent);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_recv_async_msg(
        UhdTxStreamerHandle h,
        UhdAsyncMetadataHandle* md,
        double timeout,
        [MarshalAs(UnmanagedType.U1)] out bool valid);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_streamer_last_error(UhdTxStreamerHandle h, byte* error_out, nuint strbuffer_len);

    // ---- RX metadata -----------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_make(UhdRxMetadataHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_free(UhdRxMetadataHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_has_time_spec(UhdRxMetadataHandle h, [MarshalAs(UnmanagedType.U1)] out bool result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_time_spec(UhdRxMetadataHandle h, long* full_secs, double* frac_secs);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_more_fragments(UhdRxMetadataHandle h, [MarshalAs(UnmanagedType.U1)] out bool result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_fragment_offset(UhdRxMetadataHandle h, nuint* offset);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_start_of_burst(UhdRxMetadataHandle h, [MarshalAs(UnmanagedType.U1)] out bool result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_end_of_burst(UhdRxMetadataHandle h, [MarshalAs(UnmanagedType.U1)] out bool result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_out_of_sequence(UhdRxMetadataHandle h, [MarshalAs(UnmanagedType.U1)] out bool result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_to_pp_string(UhdRxMetadataHandle h, byte* out_str, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [SuppressGCTransition] // reads a field on an already-live native object — always sub-µs
    public static partial UhdErrorCode uhd_rx_metadata_error_code(UhdRxMetadataHandle h, UhdRxMetadataErrorCode* code);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_strerror(UhdRxMetadataHandle h, byte* out_str, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_rx_metadata_last_error(UhdRxMetadataHandle h, byte* out_str, nuint strbuffer_len);

    // ---- TX metadata -----------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_metadata_make(
        UhdTxMetadataHandle* h,
        [MarshalAs(UnmanagedType.U1)] bool has_time_spec,
        long full_secs,
        double frac_secs,
        [MarshalAs(UnmanagedType.U1)] bool start_of_burst,
        [MarshalAs(UnmanagedType.U1)] bool end_of_burst);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_tx_metadata_free(UhdTxMetadataHandle* h);

    // ---- Async metadata --------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_async_metadata_make(UhdAsyncMetadataHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_async_metadata_free(UhdAsyncMetadataHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_async_metadata_channel(UhdAsyncMetadataHandle h, nuint* channel);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_async_metadata_event_code(UhdAsyncMetadataHandle h, UhdAsyncEventCode* code);

    // ---- String vector ---------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_string_vector_make(UhdStringVectorHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_string_vector_free(UhdStringVectorHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_string_vector_size(UhdStringVectorHandle h, nuint* size_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_string_vector_at(UhdStringVectorHandle h, nuint index, byte* value_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_string_vector_push_back(UhdStringVectorHandle* h, byte* value);

    // ---- RX channel control ----------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_num_channels(UhdUsrpHandle h, nuint* num_channels);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_subdev_name(UhdUsrpHandle h, nuint chan, byte* name_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_rate(UhdUsrpHandle h, double rate, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_rate(UhdUsrpHandle h, nuint chan, double* rate_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_freq(UhdUsrpHandle h, UhdTuneRequestNative* req, nuint chan, UhdTuneResultNative* result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_freq(UhdUsrpHandle h, nuint chan, double* freq_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_gain(UhdUsrpHandle h, double gain, nuint chan, byte* gain_name);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_gain(UhdUsrpHandle h, nuint chan, byte* gain_name, double* gain_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_normalized_rx_gain(UhdUsrpHandle h, double gain, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_normalized_rx_gain(UhdUsrpHandle h, nuint chan, double* gain_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_agc(UhdUsrpHandle h, [MarshalAs(UnmanagedType.U1)] bool enable, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_antenna(UhdUsrpHandle h, byte* ant, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_antenna(UhdUsrpHandle h, nuint chan, byte* ant_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_antennas(UhdUsrpHandle h, nuint chan, UhdStringVectorHandle* antennas_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_bandwidth(UhdUsrpHandle h, double bw, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_bandwidth(UhdUsrpHandle h, nuint chan, double* bw_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_dc_offset_enabled(UhdUsrpHandle h, [MarshalAs(UnmanagedType.U1)] bool enb, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_rx_iq_balance_enabled(UhdUsrpHandle h, [MarshalAs(UnmanagedType.U1)] bool enb, nuint chan);

    // ---- TX channel control ----------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_num_channels(UhdUsrpHandle h, nuint* num_channels);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_tx_rate(UhdUsrpHandle h, double rate, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_rate(UhdUsrpHandle h, nuint chan, double* rate_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_tx_freq(UhdUsrpHandle h, UhdTuneRequestNative* req, nuint chan, UhdTuneResultNative* result);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_freq(UhdUsrpHandle h, nuint chan, double* freq_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_tx_gain(UhdUsrpHandle h, double gain, nuint chan, byte* gain_name);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_gain(UhdUsrpHandle h, nuint chan, byte* gain_name, double* gain_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_normalized_tx_gain(UhdUsrpHandle h, double gain, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_normalized_tx_gain(UhdUsrpHandle h, nuint chan, double* gain_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_tx_antenna(UhdUsrpHandle h, byte* ant, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_antenna(UhdUsrpHandle h, nuint chan, byte* ant_out, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_set_tx_bandwidth(UhdUsrpHandle h, double bw, nuint chan);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_bandwidth(UhdUsrpHandle h, nuint chan, double* bw_out);

    // ---- Sensors ---------------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_sensor_names(UhdUsrpHandle h, nuint chan, UhdStringVectorHandle* names_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_sensor_names(UhdUsrpHandle h, nuint chan, UhdStringVectorHandle* names_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_mboard_sensor_names(UhdUsrpHandle h, nuint mboard, UhdStringVectorHandle* names_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_sensor(UhdUsrpHandle h, byte* name, nuint chan, UhdSensorValueHandle* sensor_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_sensor(UhdUsrpHandle h, byte* name, nuint chan, UhdSensorValueHandle* sensor_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_mboard_sensor(UhdUsrpHandle h, byte* name, nuint mboard, UhdSensorValueHandle* sensor_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_make(UhdSensorValueHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_free(UhdSensorValueHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_to_bool(UhdSensorValueHandle h, [MarshalAs(UnmanagedType.U1)] out bool value_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_to_int(UhdSensorValueHandle h, int* value_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_to_realnum(UhdSensorValueHandle h, double* value_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_name(UhdSensorValueHandle h, byte* out_str, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_value(UhdSensorValueHandle h, byte* out_str, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_unit(UhdSensorValueHandle h, byte* out_str, nuint strbuffer_len);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_sensor_value_data_type(UhdSensorValueHandle h, UhdSensorDataType* type_out);

    // ---- Meta range ------------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_make(UhdMetaRangeHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_free(UhdMetaRangeHandle* h);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_start(UhdMetaRangeHandle h, double* start);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_stop(UhdMetaRangeHandle h, double* stop);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_step(UhdMetaRangeHandle h, double* step);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_size(UhdMetaRangeHandle h, nuint* size);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_meta_range_at(UhdMetaRangeHandle h, nuint idx, UhdRangeNative* range_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_rates(UhdUsrpHandle h, nuint chan, UhdMetaRangeHandle rates_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_freq_range(UhdUsrpHandle h, nuint chan, UhdMetaRangeHandle range_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_gain_range(UhdUsrpHandle h, byte* name, nuint chan, UhdMetaRangeHandle range_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_bandwidth_range(UhdUsrpHandle h, nuint chan, UhdMetaRangeHandle range_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_rates(UhdUsrpHandle h, nuint chan, UhdMetaRangeHandle rates_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_freq_range(UhdUsrpHandle h, nuint chan, UhdMetaRangeHandle range_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_gain_range(UhdUsrpHandle h, byte* name, nuint chan, UhdMetaRangeHandle range_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_bandwidth_range(UhdUsrpHandle h, nuint chan, UhdMetaRangeHandle range_out);

    // ---- USRP info -------------------------------------------------------------------------

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_rx_info(UhdUsrpHandle h, nuint chan, UhdUsrpRxInfoNative* info_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_get_tx_info(UhdUsrpHandle h, nuint chan, UhdUsrpTxInfoNative* info_out);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_rx_info_free(UhdUsrpRxInfoNative* info);

    [LibraryImport(Lib)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial UhdErrorCode uhd_usrp_tx_info_free(UhdUsrpTxInfoNative* info);
}
