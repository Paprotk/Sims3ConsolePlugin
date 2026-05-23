#if DEBUG
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Sims3.Gameplay.Core;
using Sims3.UI;
//DO NOT PUT NAMESPACE HERE
/// <summary>
/// Custom Console class for debug builds, directly invoking native console functions via unmanaged interop
/// </summary>
internal static class Console
{
    static Console()
    {
        if (!Commands.sGameCommands.mCommands.ContainsKey("ConsoleCreate"))
        {
            Commands.sGameCommands.Register("ConsoleCreate",      "Manually creates the console window (if not already opened). Usage: ConsoleCreate",                    Commands.CommandType.General, ConsoleCheats.OnCreate,       false);
            Commands.sGameCommands.Register("ConsoleClose",       "Closes the console window and stops all logging. Usage: ConsoleClose",                                 Commands.CommandType.General, ConsoleCheats.OnClose,        false);
            Commands.sGameCommands.Register("ConsoleWriteLine",   "Prints a message to the native console window. Usage: ConsoleWriteLine <message>",                     Commands.CommandType.General, ConsoleCheats.OnWriteLine,    false);
            Commands.sGameCommands.Register("ConsoleClear",       "Clears the console buffer and corresponding console window of display information. Usage: ConsoleClear", Commands.CommandType.General, ConsoleCheats.OnClear,        false);
            Commands.sGameCommands.Register("ConsoleStartLogging","Starts logging all console output to a file. Usage: ConsoleStartLogging <filename>",                   Commands.CommandType.General, ConsoleCheats.OnStartLogging, false);
            Commands.sGameCommands.Register("ConsoleStopLogging", "Stops logging to the specified log file. Usage: ConsoleStopLogging <filename>",                        Commands.CommandType.General, ConsoleCheats.OnStopLogging,  false);
            Commands.sGameCommands.Register("ConsoleBeep",        "Plays the sound of a beep through the console speaker. Usage: ConsoleBeep",                            Commands.CommandType.General, ConsoleCheats.OnBeep,         false);
        }
    }

    // -------------------------------------------------------------------------
    // Internal calls — implemented in the native plugin (NativeExports.cs)
    // -------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern bool _IsPresent();

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void _Create();

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void _Close();

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void _WriteLine(sbyte* utf8Text);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void _StartLogging(sbyte* filenameUtf8);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void _StopLogging(sbyte* filenameUtf8);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void _Clear();

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void _Beep();

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern IntPtr _PollInput();

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void _FreeString(IntPtr ptr);

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public static bool IsPresent => _IsPresent();

    public static void Create()
    {
        if (_IsPresent())
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[Create] The Console is already present!");
        else
            _Create();
    }

    public static void Close()
    {
        if (!_IsPresent())
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[Close] The Console is not present!");
        else
            _Close();
    }

    public static unsafe void WriteLine(string text)
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[WriteLine] The Console is not present!");
            return;
        }

        using Utf8Ptr utf8Ptr = text;
        _WriteLine(utf8Ptr);
    }

    public static unsafe void StartLogging(string filename)
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[StartLogging] The Console is not present!");
            return;
        }

        using Utf8Ptr utf8Ptr = filename;
        _StartLogging(utf8Ptr);
    }

    public static unsafe void StopLogging(string filename)
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[StopLogging] The console is not present! Any logging done before the console was closed is already saved to a file.");
            return;
        }

        using Utf8Ptr utf8Ptr = filename;
        _StopLogging(utf8Ptr);
    }

    public static void Clear()
    {
        if (!_IsPresent())
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[Clear] The Console is not present!");
        else
            _Clear();
    }

    public static void Beep()
    {
        if (!_IsPresent())
            SimpleMessageDialog.Show("Sims3ConsolePlugin", "[Beep] The Console is not present!");
        else
            _Beep();
    }

    /// <summary>
    /// Returns the next line of user input typed into the console, or null if nothing is queued.
    /// The caller must never free the returned string — that is handled internally.
    /// </summary>
    public static string PollInput()
    {
        if (!_IsPresent())
            return null;

        IntPtr ptr = _PollInput();
        if (ptr == IntPtr.Zero)
            return null;

        try
        {
            return PtrToStringUTF8(ptr);
        }
        finally
        {
            _FreeString(ptr);
        }
    }

    private static unsafe string PtrToStringUTF8(IntPtr ptr)
    {
        byte* p = (byte*)ptr;
        int len = 0;
        while (p[len] != 0) len++;

        byte[] managed = new byte[len];
        Marshal.Copy(ptr, managed, 0, len);
        return Encoding.UTF8.GetString(managed);
    }

    // -------------------------------------------------------------------------
    // Cheat commands (registered in the static constructor above)
    // -------------------------------------------------------------------------

    public static class ConsoleCheats
    {
        public static int OnCreate(object[] parameters)
        {
            Create();
            return 1;
        }

        public static int OnClose(object[] parameters)
        {
            Close();
            return 1;
        }

        public static int OnWriteLine(object[] parameters)
        {
            if (parameters == null)
            {
                SimpleMessageDialog.Show("Sims3ConsolePlugin", "[WriteLine] No parameters provided!");
                return 1;
            }

            if (parameters is { Length: > 0 })
            {
                StringBuilder sb = new StringBuilder();
                foreach (var t in parameters)
                {
                    if (t != null)
                    {
                        if (sb.Length > 0) sb.Append(" ");
                        sb.Append(t);
                    }
                }
                WriteLine(sb.ToString());
            }
            return 1;
        }

        public static int OnClear(object[] parameters)
        {
            Clear();
            return 1;
        }

        public static int OnStartLogging(object[] parameters)
        {
            if (parameters.Length == 0)
            {
                SimpleMessageDialog.Show("Sims3ConsolePlugin", "[StartLogging] No filename provided!");
                return 1;
            }
            if (parameters[0] != null)
                StartLogging(parameters[0].ToString());
            return 1;
        }

        public static int OnStopLogging(object[] parameters)
        {
            if (parameters.Length == 0)
            {
                SimpleMessageDialog.Show("Sims3ConsolePlugin", "[StopLogging] No filename provided!");
                return 1;
            }
            if (parameters[0] != null)
                StopLogging(parameters[0].ToString());
            return 1;
        }

        public static int OnBeep(object[] parameters)
        {
            Beep();
            return 1;
        }
    }
}

// -------------------------------------------------------------------------
// UTF-8 pinned string helper
// -------------------------------------------------------------------------

public unsafe struct Utf8Ptr : IDisposable
{
    private byte[] _bytes;
    private GCHandle _handle;

    public Utf8Ptr(string str)
    {
        _bytes = Encoding.UTF8.GetBytes(str + "\0");
        _handle = GCHandle.Alloc(_bytes, GCHandleType.Pinned);
        Pointer = (sbyte*)_handle.AddrOfPinnedObject().ToPointer();
    }

    public sbyte* Pointer { get; private set; }

    public void Dispose()
    {
        if (_handle.IsAllocated)
            _handle.Free();
        _bytes = null;
        Pointer = null;
    }

    public static implicit operator sbyte*(Utf8Ptr utf8Ptr) => utf8Ptr.Pointer;
    public static implicit operator Utf8Ptr(string str)     => new Utf8Ptr(str);
}

#endif

// -------------------------------------------------------------------------
// Release build stub — all methods are no-ops
// -------------------------------------------------------------------------

#if !DEBUG
/// <summary>
/// Dummy class for release builds
/// </summary>
internal static class Console
{
    public static bool IsPresent    => false;
    public static void Create()                    { /* no-op */ }
    public static void Close()                     { /* no-op */ }
    public static void WriteLine(string text)      { /* no-op */ }
    public static void StartLogging(string filename){ /* no-op */ }
    public static void StopLogging(string filename) { /* no-op */ }
    public static void Clear()                     { /* no-op */ }
    public static void Beep()                      { /* no-op */ }
    public static string PollInput()               { return null; }
}
#endif