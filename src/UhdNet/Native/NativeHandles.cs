using System;
using System.Runtime.InteropServices;

namespace UhdNet.Native;

/// <summary>Opaque <c>uhd_usrp_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdUsrpHandle : IEquatable<UhdUsrpHandle>
{
    public readonly IntPtr Value;
    public UhdUsrpHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
    public bool Equals(UhdUsrpHandle other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is UhdUsrpHandle h && Equals(h);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(UhdUsrpHandle l, UhdUsrpHandle r) => l.Equals(r);
    public static bool operator !=(UhdUsrpHandle l, UhdUsrpHandle r) => !l.Equals(r);
}

/// <summary>Opaque <c>uhd_rx_streamer_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdRxStreamerHandle
{
    public readonly IntPtr Value;
    public UhdRxStreamerHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_tx_streamer_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdTxStreamerHandle
{
    public readonly IntPtr Value;
    public UhdTxStreamerHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_rx_metadata_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdRxMetadataHandle
{
    public readonly IntPtr Value;
    public UhdRxMetadataHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_tx_metadata_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdTxMetadataHandle
{
    public readonly IntPtr Value;
    public UhdTxMetadataHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_async_metadata_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdAsyncMetadataHandle
{
    public readonly IntPtr Value;
    public UhdAsyncMetadataHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_string_vector_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdStringVectorHandle
{
    public readonly IntPtr Value;
    public UhdStringVectorHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_meta_range_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdMetaRangeHandle
{
    public readonly IntPtr Value;
    public UhdMetaRangeHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_subdev_spec_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdSubdevSpecHandle
{
    public readonly IntPtr Value;
    public UhdSubdevSpecHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}

/// <summary>Opaque <c>uhd_sensor_value_handle</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UhdSensorValueHandle
{
    public readonly IntPtr Value;
    public UhdSensorValueHandle(IntPtr value) => Value = value;
    public bool IsNull => Value == IntPtr.Zero;
}
