#if DEBUG
using System;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Custom Console class for debug builds, directly invoking native console functions via unmanaged interop
/// </summary>
public static class Console
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void Create();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void Close();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void WriteLine(sbyte* utf8Text);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void StartLogging(sbyte* filenameUtf8);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void StopLogging(sbyte* filenameUtf8);
    
    public static unsafe void WriteLine(string text)
    {
        using (Utf8Ptr utf8Ptr = text) // Implicit conversion from string to Utf8Ptr
        {
            WriteLine(utf8Ptr);
        }
    }
    
    public static unsafe void StartLogging(string filename)
    {
        using (Utf8Ptr utf8Ptr = filename) 
        {
            StartLogging(utf8Ptr);
        }
    }
    
    public static unsafe void StopLogging(string filename)
    {
        using (Utf8Ptr utf8Ptr = filename)
        {
            StopLogging(utf8Ptr);
        }
    }
}

public unsafe struct Utf8Ptr : IDisposable
{
    private byte[] _bytes;
    private sbyte* _ptr;

    public Utf8Ptr(string str)
    {
        _bytes = Encoding.UTF8.GetBytes(str + "\0");
        fixed (byte* ptr = _bytes)
        {
            _ptr = (sbyte*)ptr;
        }
    }

    public sbyte* Pointer => _ptr;

    public void Dispose()
    {
        _bytes = null;
        _ptr = null;
    }

    // Implicit conversion from Utf8Ptr to sbyte*
    public static implicit operator sbyte*(Utf8Ptr utf8Ptr)
    {
        return utf8Ptr.Pointer;
    }

    // Implicit conversion from string to Utf8Ptr
    public static implicit operator Utf8Ptr(string str)
    {
        return new Utf8Ptr(str);
    }
}
#endif

#if !DEBUG
/// <summary>
/// Dummy class for release builds
/// </summary>
public static class Console
{
    public static void Create() { /* Dummy - does nothing */ }
    
    public static void Close() { /* Dummy - does nothing */ }
    
    public static void WriteLine() { /* Dummy - does nothing */ }
    
    public static void StartLogging() { /* Dummy - does nothing */ }
    
    public static void StopLogging() { /* Dummy - does nothing */ }
    public static void WriteLine(string text) { /* Dummy - does nothing */ }
    
    public static void StartLogging(string filename) { /* Dummy - does nothing */ }
    
    public static void StopLogging(string filename) { /* Dummy - does nothing */ }
}
#endif