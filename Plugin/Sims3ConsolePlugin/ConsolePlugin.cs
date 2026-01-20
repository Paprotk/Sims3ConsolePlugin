using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

//To use this code as a plugin, it needs to be compiled ahead of time as a native x86 dll
//dotnet publish -c Release -r win-x86 /p:PlatformTarget=x86
namespace Sims3Console
{
    public static unsafe class NativeExports
    {
        private static readonly object logLock = new object();
        private static Dictionary<string, StreamWriter> logWriters = new();
        private static bool IsConsolePresent() => GetConsoleWindow() != IntPtr.Zero;
        private static bool _isTopmost = false; // New: relevant for behavior
        
        [UnmanagedCallersOnly(EntryPoint = "ConsoleIsPresent", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int ConsoleIsPresent()
        {
            return IsConsolePresent() ? 1 : 0;
        }
        
        private static void CreateConsole()
        {
            if (!IsConsolePresent())
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                Console.OutputEncoding = Encoding.UTF8;
                UpdateConsoleTitle();
                
                IntPtr hWnd = GetConsoleWindow();
                if (hWnd != IntPtr.Zero)
                {
                    // Start minimized
                    ShowWindow(hWnd, SW_SHOWMINNOACTIVE);

                    // Monitor for focus to set topmost
                    Thread monitor = new Thread(() => 
                    {
                        while (IsConsolePresent())
                        {
                            if (GetForegroundWindow() == hWnd && !_isTopmost)
                            {
                                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                                    SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                                _isTopmost = true;
                                break;
                            }
                            Thread.Sleep(500);
                        }
                    });
                    monitor.IsBackground = true;
                    monitor.Start();
                }
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ICallSetup", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void ICallSetup(delegate* unmanaged[Cdecl]<byte*, void*, void> addInternalCall)
        {
            var method1 = "Console::_Create"u8;
            var method2 = "Console::_WriteLine"u8;
            var method3 = "Console::_Close"u8;
            var method4 = "Console::_StartLogging"u8;
            var method5 = "Console::_StopLogging"u8;
            var method6 = "Console::_Clear"u8;
            var method7 = "Console::_Beep"u8;
            var method8 = "Console::_IsPresent"u8;

            delegate* unmanaged[Stdcall]<void> createPtr = &ConsoleCreate;
            delegate* unmanaged[Stdcall]<sbyte*, void> writePtr = &ConsoleWriteLine;
            delegate* unmanaged[Stdcall]<void> closePtr = &ConsoleClose;
            delegate* unmanaged[Stdcall]<sbyte*, void> startLoggingPtr = &ConsoleStartLogging;
            delegate* unmanaged[Stdcall]<sbyte*, void> stopLoggingPtr = &ConsoleStopLogging;
            delegate* unmanaged[Stdcall]<void> clearPtr = &ConsoleClear;
            delegate* unmanaged[Stdcall]<void> beepPtr = &ConsoleBeep;
            delegate* unmanaged[Stdcall]<int> isPresentPtr = &ConsoleIsPresent;

            fixed (byte* pName1 = method1)
            fixed (byte* pName2 = method2)
            fixed (byte* pName3 = method3)
            fixed (byte* pName4 = method4)
            fixed (byte* pName5 = method5)
            fixed (byte* pName6 = method6)
            fixed (byte* pName7 = method7)
            fixed (byte* pName8 = method8)
            {
                addInternalCall(pName1, createPtr);
                addInternalCall(pName2, writePtr);
                addInternalCall(pName3, closePtr);
                addInternalCall(pName4, startLoggingPtr);
                addInternalCall(pName5, stopLoggingPtr);
                addInternalCall(pName6, clearPtr);
                addInternalCall(pName7, beepPtr);
                addInternalCall(pName8, isPresentPtr);
            }

            CreateConsole();
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleCreate", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleCreate()
        {
            CreateConsole();
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleClose", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleClose()
        {
            if (IsConsolePresent())
            {
                FreeConsole();
            }

            lock (logLock)
            {
                foreach (var writer in logWriters.Values)
                {
                    writer.Close();
                }
                logWriters.Clear();
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleWriteLine", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleWriteLine(sbyte* utf8Str)
        {
            try
            {
                string? str = Marshal.PtrToStringUTF8((IntPtr)utf8Str);
                Console.WriteLine(str ?? "<null>");

                lock (logLock)
                {
                    foreach (var writer in logWriters.Values)
                    {
                        writer.WriteLine($"[{DateTime.Now:G}] OUTPUT: {str ?? "<null>"}");
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("console_output_errors.log", $"[{DateTime.Now:u}] {ex}\n");
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleStartLogging", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleStartLogging(sbyte* filenameUtf8)
        {
            if (!IsConsolePresent()) return;

            try
            {
                string name = Marshal.PtrToStringUTF8((IntPtr)filenameUtf8) ?? "console";
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logsDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string logPath = Path.Combine(logsDir, $"{name}_{timestamp}.log");

                lock (logLock)
                {
                    if (!logWriters.ContainsKey(name))
                    {
                        var writer = new StreamWriter(logPath, true, Encoding.UTF8)
                        {
                            AutoFlush = true
                        };
                        logWriters[name] = writer;
                    }
                    UpdateConsoleTitle();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("logging_errors.log", $"[{DateTime.Now:u}] {ex}\n");
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleStopLogging", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleStopLogging(sbyte* filenameUtf8)
        {
            try
            {
                string name = Marshal.PtrToStringUTF8((IntPtr)filenameUtf8) ?? "console";

                lock (logLock)
                {
                    if (logWriters.TryGetValue(name, out var writer))
                    {
                        writer.Close();
                        logWriters.Remove(name);
                    }
                    UpdateConsoleTitle();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("logging_errors.log", $"[{DateTime.Now:u}] {ex}\n");
            }
        }
        
        [UnmanagedCallersOnly(EntryPoint = "ConsoleClear", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleClear()
        {
            if (IsConsolePresent())
            {
                Console.Clear();
            }
        }
        
        [UnmanagedCallersOnly(EntryPoint = "ConsoleBeep", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleBeep()
        {
            if (IsConsolePresent())
            {
                Console.Beep();
            }
        }
        
        private static void UpdateConsoleTitle()
        {
            lock (logLock)
            {
                if (logWriters.Count > 0)
                {
                    Console.Title = "[The Sims 3 Console] Logging: " + string.Join(", ", logWriters.Keys);
                }
                else
                {
                    Console.Title = "[The Sims 3 Console] (No active logs)";
                }
            }
        }


        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_SHOWMINNOACTIVE = 7;
    }
}