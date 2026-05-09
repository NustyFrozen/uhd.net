using System;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Thrown when the underlying UHD C API returns a non-zero error code.
/// </summary>
public sealed class UhdException : Exception
{
    public UhdErrorCode Code { get; }

    public UhdException(UhdErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public UhdException(UhdErrorCode code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }

    public override string ToString() => $"UHD {Code}: {Message}";
}
