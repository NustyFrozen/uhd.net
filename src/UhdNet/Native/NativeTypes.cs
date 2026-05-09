using System.Runtime.InteropServices;

namespace UhdNet.Native;

/// <summary>Mirrors the <c>uhd_error</c> enum from <c>uhd/error.h</c>.</summary>
public enum UhdErrorCode
{
    None = 0,
    InvalidDevice = 1,
    Index = 10,
    Key = 11,
    NotImplemented = 20,
    Usb = 21,
    Io = 30,
    Os = 31,
    Assertion = 40,
    Lookup = 41,
    Type = 42,
    Value = 43,
    Runtime = 44,
    Environment = 45,
    System = 46,
    Except = 47,
    BoostExcept = 60,
    StdExcept = 70,
    Unknown = 100,
}

/// <summary>Mirrors <c>uhd_stream_mode_t</c>.</summary>
public enum UhdStreamMode
{
    StartContinuous = 97,
    StopContinuous = 111,
    NumSampsAndDone = 100,
    NumSampsAndMore = 109,
}

/// <summary>Mirrors <c>uhd_tune_request_policy_t</c>.</summary>
public enum UhdTuneRequestPolicy
{
    None = 78,
    Auto = 65,
    Manual = 77,
}

/// <summary>Mirrors <c>uhd_rx_metadata_error_code_t</c>.</summary>
[Flags]
public enum UhdRxMetadataErrorCode
{
    None = 0x0,
    Timeout = 0x1,
    LateCommand = 0x2,
    BrokenChain = 0x4,
    Overflow = 0x8,
    Alignment = 0xC,
    BadPacket = 0xF,
}

/// <summary>Mirrors <c>uhd_async_metadata_event_code_t</c>.</summary>
[Flags]
public enum UhdAsyncEventCode
{
    BurstAck = 0x1,
    Underflow = 0x2,
    SeqError = 0x4,
    TimeError = 0x8,
    UnderflowInPacket = 0x10,
    SeqErrorInBurst = 0x20,
    UserPayload = 0x40,
}

/// <summary>Mirrors <c>uhd_sensor_value_data_type_t</c>.</summary>
public enum UhdSensorDataType
{
    Boolean = 98,
    Integer = 105,
    RealNumber = 114,
    String = 115,
}

/// <summary>Mirrors <c>uhd_stream_args_t</c>. UTF-8 ANSI strings owned by caller.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UhdStreamArgsNative
{
    public byte* CpuFormat;
    public byte* OtwFormat;
    public byte* Args;
    public nuint* ChannelList;
    public int NChannels;
}

/// <summary>Mirrors <c>uhd_stream_cmd_t</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdStreamCmdNative
{
    public UhdStreamMode StreamMode;
    public nuint NumSamps;
    [MarshalAs(UnmanagedType.U1)] public bool StreamNow;
    public long TimeSpecFullSecs;
    public double TimeSpecFracSecs;
}

/// <summary>Mirrors <c>uhd_tune_request_t</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UhdTuneRequestNative
{
    public double TargetFreq;
    public UhdTuneRequestPolicy RfFreqPolicy;
    public double RfFreq;
    public UhdTuneRequestPolicy DspFreqPolicy;
    public double DspFreq;
    public byte* Args;
}

/// <summary>Mirrors <c>uhd_tune_result_t</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdTuneResultNative
{
    public double ClippedRfFreq;
    public double TargetRfFreq;
    public double ActualRfFreq;
    public double TargetDspFreq;
    public double ActualDspFreq;
}

/// <summary>Mirrors <c>uhd_range_t</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdRangeNative
{
    public double Start;
    public double Stop;
    public double Step;
}

/// <summary>Mirrors <c>uhd_subdev_spec_pair_t</c>. UHD allocates the strings; free with
/// <c>uhd_subdev_spec_pair_free</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UhdSubdevSpecPairNative
{
    public byte* DbName;
    public byte* SdName;
}

/// <summary>Mirrors <c>uhd_usrp_rx_info_t</c>. UHD allocates the strings; free with
/// <c>uhd_usrp_rx_info_free</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UhdUsrpRxInfoNative
{
    public byte* MboardId;
    public byte* MboardName;
    public byte* MboardSerial;
    public byte* RxId;
    public byte* RxSubdevName;
    public byte* RxSubdevSpec;
    public byte* RxSerial;
    public byte* RxAntenna;
}

/// <summary>Mirrors <c>uhd_usrp_tx_info_t</c>. UHD allocates the strings; free with
/// <c>uhd_usrp_tx_info_free</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UhdUsrpTxInfoNative
{
    public byte* MboardId;
    public byte* MboardName;
    public byte* MboardSerial;
    public byte* TxId;
    public byte* TxSubdevName;
    public byte* TxSubdevSpec;
    public byte* TxSerial;
    public byte* TxAntenna;
}

/// <summary>Mirrors <c>uhd_usrp_register_info_t</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdUsrpRegisterInfoNative
{
    public nuint Bitwidth;
    [MarshalAs(UnmanagedType.U1)] public bool Readable;
    [MarshalAs(UnmanagedType.U1)] public bool Writable;
}
