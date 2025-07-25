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
        private static Dictionary<string, StreamWriter> logWriters = new Dictionary<string, StreamWriter>();

        internal static void AutoConsoleInitialize()
        {
            CreateConsoleManaged();
        }

        private static void CreateConsoleManaged()
        {
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "ICallSetup", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void ICallSetup(delegate* unmanaged[Cdecl]<byte*, void*, void> addInternalCall)
        {
            var method1 = "Console::Create"u8;
            var method2 = "Console::WriteLine"u8;
            var method3 = "Console::Close"u8;
            var method4 = "Console::StartLogging"u8;
            var method5 = "Console::StopLogging"u8;

            delegate* unmanaged[Stdcall]<void> createPtr = &ConsoleCreate;
            delegate* unmanaged[Stdcall]<sbyte*, void> writePtr = &ConsoleWriteLine;
            delegate* unmanaged[Stdcall]<void> closePtr = &ConsoleClose;
            delegate* unmanaged[Stdcall]<sbyte*, void> startLoggingPtr = &ConsoleStartLogging;
            delegate* unmanaged[Stdcall]<sbyte*, void> stopLoggingPtr = &ConsoleStopLogging;

            fixed (byte* pName1 = method1)
            fixed (byte* pName2 = method2)
            fixed (byte* pName3 = method3)
            fixed (byte* pName4 = method4)
            fixed (byte* pName5 = method5)
            {
                addInternalCall(pName1, createPtr);
                addInternalCall(pName2, writePtr);
                addInternalCall(pName3, closePtr);
                addInternalCall(pName4, startLoggingPtr);
                addInternalCall(pName5, stopLoggingPtr);
            }

            CreateConsoleManaged();
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleCreate", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleCreate()
        {
            CreateConsoleManaged();
        }

        [UnmanagedCallersOnly(EntryPoint = "ConsoleClose", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static void ConsoleClose()
        {
            if (GetConsoleWindow() != IntPtr.Zero)
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
                        writer.WriteLine($"[{DateTime.Now:u}] OUTPUT: {str ?? "<null>"}");
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
            try
            {
                string baseName = Marshal.PtrToStringUTF8((IntPtr)filenameUtf8) ?? "console";
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logsDir);

                string logPath = Path.Combine(logsDir, $"{baseName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

                lock (logLock)
                {
                    if (!logWriters.ContainsKey(logPath))
                    {
                        var writer = new StreamWriter(logPath, true, Encoding.UTF8)
                        {
                            AutoFlush = true
                        };
                        logWriters.Add(logPath, writer);
                    }
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
                string targetFile = Marshal.PtrToStringUTF8((IntPtr)filenameUtf8) ?? "console";

                lock (logLock)
                {
                    List<string> toRemove = new List<string>();

                    foreach (var kvp in logWriters)
                    {
                        if (Path.GetFileNameWithoutExtension(kvp.Key).StartsWith(targetFile))
                        {
                            kvp.Value.Close();
                            toRemove.Add(kvp.Key);
                        }
                    }

                    foreach (var key in toRemove)
                    {
                        logWriters.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("logging_errors.log", $"[{DateTime.Now:u}] {ex}\n");
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
    }
}
