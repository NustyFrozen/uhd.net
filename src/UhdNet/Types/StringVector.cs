using System;
using System.Collections;
using System.Collections.Generic;
using UhdNet.Native;

namespace UhdNet;

/// <summary>
/// Wraps a <c>uhd_string_vector_handle</c>. Disposable. Acts as a read-only enumeration of
/// strings the UHD library returned (e.g. antenna names, sensor names).
/// </summary>
public sealed unsafe class UhdStringVector : IDisposable, IReadOnlyList<string>
{
    private UhdStringVectorHandle _handle;

    public UhdStringVector()
    {
        UhdStringVectorHandle h = default;
        Interop.Check(NativeMethods.uhd_string_vector_make(&h));
        _handle = h;
    }

    /// <summary>Adopt an existing handle (e.g. one returned by a UHD getter).</summary>
    internal UhdStringVector(UhdStringVectorHandle h)
    {
        _handle = h;
    }

    public UhdStringVectorHandle Handle => _handle;

    public int Count
    {
        get
        {
            ThrowIfDisposed();
            nuint sz;
            Interop.Check(NativeMethods.uhd_string_vector_size(_handle, &sz));
            return checked((int)sz);
        }
    }

    public string this[int index]
    {
        get
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            ThrowIfDisposed();
            Span<byte> buf = stackalloc byte[Interop.DefaultStringBuffer];
            fixed (byte* p = buf)
            {
                Interop.Check(NativeMethods.uhd_string_vector_at(_handle, (nuint)index, p, (nuint)buf.Length));
                return Interop.Utf8ToString(p);
            }
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        int n = Count;
        for (int i = 0; i < n; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void ThrowIfDisposed()
    {
        if (_handle.IsNull) throw new ObjectDisposedException(nameof(UhdStringVector));
    }

    public void Dispose()
    {
        if (!_handle.IsNull)
        {
            UhdStringVectorHandle h = _handle;
            _handle = default;
            _ = NativeMethods.uhd_string_vector_free(&h);
        }
        GC.SuppressFinalize(this);
    }

    ~UhdStringVector() => Dispose();
}
