#if DEBUG
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Sims3.Gameplay.Core;
using Sims3.UI;

/// <summary>
/// Custom Console class for debug builds, directly invoking native console functions via unmanaged interop
/// </summary>
public static class Console
{
    static Console()
    {
        if (!Commands.sGameCommands.mCommands.ContainsKey("Clear"))
        {
            Commands.sGameCommands.Register("ConsoleCreate", "- Creates console window.", Commands.CommandType.General, ConsoleCheats.OnCreate, true);
            Commands.sGameCommands.Register("ConsoleClose", "- Closes console window.", Commands.CommandType.General, ConsoleCheats.OnClose, true);
            Commands.sGameCommands.Register("ConsoleWriteLine", "- Writes text to a console window.", Commands.CommandType.General, ConsoleCheats.OnWriteLine, true);
            Commands.sGameCommands.Register("ConsoleClear", "- Clear console window.", Commands.CommandType.General, ConsoleCheats.OnClear, true);
            Commands.sGameCommands.Register("ConsoleStartLogging", "- Starts logging console window.", Commands.CommandType.General, ConsoleCheats.OnStartLogging, true);
            Commands.sGameCommands.Register("ConsoleStopLogging", "- Stops logging console window.", Commands.CommandType.General, ConsoleCheats.OnStopLogging, true);
            Commands.sGameCommands.Register("ConsoleBeep", "- Beeps using console window.", Commands.CommandType.General, ConsoleCheats.OnBeep, true);
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern bool _IsPresent();
    
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void _Create();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void _Close();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void _WriteLine(sbyte* utf8Text);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void _StartLogging(sbyte* filenameUtf8);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void _StopLogging(sbyte* filenameUtf8);
	
	[MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void _Clear();
	
	[MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void _Beep();
    
    public static unsafe void WriteLine(string text)
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin","Console is not present!");
            return;
        }
        using (Utf8Ptr utf8Ptr = text) // Implicit conversion from string to Utf8Ptr
        {
            _WriteLine(utf8Ptr);
        }
    }
    
    public static unsafe void StartLogging(string filename)
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin","Console is not present!");
            return;
        }
        using (Utf8Ptr utf8Ptr = filename) 
        {
            _StartLogging(utf8Ptr);
        }
    }
    
    public static unsafe void StopLogging(string filename)
    {
        using (Utf8Ptr utf8Ptr = filename)
        {
            _StopLogging(utf8Ptr);
        }
    }
    
    public static void Create()
    {
        if (_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin","Console is already present!");
        }
        else
        {
            _Create();
        }
            
    }
    public static void Close()
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin","Console is not present!");
        }
        else
        {
            _Close();
        }
            
    }
    public static void Clear()
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin","Console is not present!");
        }
        else
        {
            _Clear();
        }
            
    }

    public static void Beep()
    {
        if (!_IsPresent())
        {
            SimpleMessageDialog.Show("Sims3ConsolePlugin","Console is not present!");
        }
        else
        {
            _Beep();
        }
    }

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
            if (parameters != null && parameters.Length > 0)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] != null)
                    {
                        if (sb.Length > 0) sb.Append(" ");
                        sb.Append(parameters[i]);
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
            if (parameters != null && parameters.Length > 0 && parameters[0] != null)
            {
                StartLogging(parameters[0].ToString());
            }
            return 1;
        }

        public static int OnStopLogging(object[] parameters)
        {
            if (parameters != null && parameters.Length > 0 && parameters[0] != null)
            {
                StopLogging(parameters[0].ToString());
            }
            return 1;
        }
        
        public static int OnBeep(object[] parameters)
        {
            Beep();
            return 1;
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
	
	public static void Clear() { /* Dummy - does nothing */ }
	
	public static void Beep() { /* Dummy - does nothing */ }

}
#endif