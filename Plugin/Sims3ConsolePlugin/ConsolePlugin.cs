using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// To use this code as a plugin, it needs to be compiled ahead of time as a native x86 dll
// dotnet publish -c Release -r win-x86 /p:PlatformTarget=x86
namespace Sims3Console
{
    public static unsafe class NativeExports
    {
        // --- Input polling state ---
        private static readonly object inputLock = new object();
        private static readonly Queue<string> inputQueue = new Queue<string>();
        private static Thread? inputThread;
        private static bool inputRunning;

        // --- Logging state ---
        private static readonly object logLock = new object();
        private static Dictionary<string, StreamWriter> logWriters = new();

        // --- Window state ---
        private static bool _isTopmost = false;

        // -------------------------------------------------------------------------
        // Console presence / creation / destruction
        // -------------------------------------------------------------------------

        private static bool IsConsolePresent() => GetConsoleWindow() != IntPtr.Zero;

        [UnmanagedCallersOnly(EntryPoint = "ConsoleIsPresent", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int ConsoleIsPresent() => IsConsolePresent() ? 1 : 0;

        private static void CreateConsole()
        {
            if (IsConsolePresent()) return;

            AllocConsole();

            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8));
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            UpdateConsoleTitle();

            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                // Start minimized
                ShowWindow(hWnd, SW_SHOWMINNOACTIVE);

                // Make topmost once the user focuses the window
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

            StartInputThread();
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleCreate", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleCreate() => CreateConsole();

        [UnmanagedCallersOnly(EntryPoint = "ConsoleClose", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleClose()
        {
            StopInputThread();

            if (IsConsolePresent())
                FreeConsole();

            lock (logLock)
            {
                foreach (var writer in logWriters.Values)
                    writer.Close();
                logWriters.Clear();
            }
        }

        // -------------------------------------------------------------------------
        // Input polling
        // -------------------------------------------------------------------------

        private static void StartInputThread()
        {
            if (inputThread != null) return;

            inputRunning = true;
            inputThread = new Thread(InputLoop) { IsBackground = true };
            inputThread.Start();
        }

        private static void StopInputThread()
        {
            inputRunning = false;
            inputThread = null;
        }

        private static void InputLoop()
        {
            while (inputRunning)
            {
                try
                {
                    string? line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        lock (inputLock)
                            inputQueue.Enqueue(line);
                    }
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// Returns a pointer to a null-terminated UTF-8 string if input is available,
        /// or IntPtr.Zero if the queue is empty. Caller must free the pointer with ConsoleFreeString.
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "ConsolePollInput", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static IntPtr ConsolePollInput()
        {
            lock (inputLock)
            {
                if (inputQueue.Count == 0) return IntPtr.Zero;

                string str = inputQueue.Dequeue();
                byte[] bytes = Encoding.UTF8.GetBytes(str + "\0");
                IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                return ptr;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleFreeString", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleFreeString(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
                Marshal.FreeHGlobal(ptr);
        }

        // -------------------------------------------------------------------------
        // Output
        // -------------------------------------------------------------------------

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
                        writer.WriteLine($"[{DateTime.Now:g}] OUTPUT: {str ?? "<null>"}");
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("console_output_errors.log", $"[{DateTime.Now:u}] {ex}\n");
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleClear", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleClear()
        {
            if (IsConsolePresent())
                Console.Clear();
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleBeep", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleBeep()
        {
            if (IsConsolePresent())
                Console.Beep();
        }

        // -------------------------------------------------------------------------
        // Logging
        // -------------------------------------------------------------------------

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
                        logWriters[name] = new StreamWriter(logPath, true, Encoding.UTF8) { AutoFlush = true };
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

        private static void UpdateConsoleTitle()
        {
            lock (logLock)
            {
                Console.Title = logWriters.Count > 0
                    ? "[The Sims 3 Console] Logging: " + string.Join(", ", logWriters.Keys)
                    : "[The Sims 3 Console] (No active logs)";
            }
        }

        // -------------------------------------------------------------------------
        // ICall registration — all methods exposed to the game engine
        // -------------------------------------------------------------------------

        [UnmanagedCallersOnly(EntryPoint = "ICallSetup", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void ICallSetup(delegate* unmanaged[Cdecl]<byte*, void*, void> addInternalCall)
        {
            var method1  = "Console::_Create"u8;
            var method2  = "Console::_Close"u8;
            var method3  = "Console::_WriteLine"u8;
            var method4  = "Console::_PollInput"u8;
            var method5  = "Console::_FreeString"u8;
            var method6  = "Console::_IsPresent"u8;
            var method7  = "Console::_StartLogging"u8;
            var method8  = "Console::_StopLogging"u8;
            var method9  = "Console::_Clear"u8;
            var method10 = "Console::_Beep"u8;

            delegate* unmanaged[Stdcall]<void>          createPtr       = &ConsoleCreate;
            delegate* unmanaged[Stdcall]<void>          closePtr        = &ConsoleClose;
            delegate* unmanaged[Stdcall]<sbyte*, void>  writePtr        = &ConsoleWriteLine;
            delegate* unmanaged[Stdcall]<IntPtr>        pollPtr         = &ConsolePollInput;
            delegate* unmanaged[Stdcall]<IntPtr, void>  freePtr         = &ConsoleFreeString;
            delegate* unmanaged[Stdcall]<int>           isPresentPtr    = &ConsoleIsPresent;
            delegate* unmanaged[Stdcall]<sbyte*, void>  startLogPtr     = &ConsoleStartLogging;
            delegate* unmanaged[Stdcall]<sbyte*, void>  stopLogPtr      = &ConsoleStopLogging;
            delegate* unmanaged[Stdcall]<void>          clearPtr        = &ConsoleClear;
            delegate* unmanaged[Stdcall]<void>          beepPtr         = &ConsoleBeep;

            fixed (byte* p1  = method1)
            fixed (byte* p2  = method2)
            fixed (byte* p3  = method3)
            fixed (byte* p4  = method4)
            fixed (byte* p5  = method5)
            fixed (byte* p6  = method6)
            fixed (byte* p7  = method7)
            fixed (byte* p8  = method8)
            fixed (byte* p9  = method9)
            fixed (byte* p10 = method10)
            {
                addInternalCall(p1,  createPtr);
                addInternalCall(p2,  closePtr);
                addInternalCall(p3,  writePtr);
                addInternalCall(p4,  pollPtr);
                addInternalCall(p5,  freePtr);
                addInternalCall(p6,  isPresentPtr);
                addInternalCall(p7,  startLogPtr);
                addInternalCall(p8,  stopLogPtr);
                addInternalCall(p9,  clearPtr);
                addInternalCall(p10, beepPtr);
            }

            CreateConsole();
        }

        // -------------------------------------------------------------------------
        // Native imports
        // -------------------------------------------------------------------------

        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        [DllImport("kernel32.dll")] private static extern bool FreeConsole();
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]   private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]   private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE    = 0x0002;
        private const uint SWP_NOSIZE    = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int  SW_SHOWMINNOACTIVE = 7;
    }
}