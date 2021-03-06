﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIVRichPresenceRunner.Memory
{
    /// <summary>
    ///     Memory.dll class. Full documentation at https://github.com/erfg12/memory.dll/wiki
    /// </summary>
    public class Mem
    {
        #region DllImports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId
        );

#if WINXP
#else
        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);

        public UIntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer)
        {
            UIntPtr retVal;

            // TODO: Need to change this to only check once.
            if (Is64Bit)
            {
                // 64 bit
                var tmp64 = new MEMORY_BASIC_INFORMATION64();
                retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64,
                    new UIntPtr((uint) Marshal.SizeOf(tmp64)));

                lpBuffer.BaseAddress = tmp64.BaseAddress;
                lpBuffer.AllocationBase = tmp64.AllocationBase;
                lpBuffer.AllocationProtect = tmp64.AllocationProtect;
                lpBuffer.RegionSize = (long) tmp64.RegionSize;
                lpBuffer.State = tmp64.State;
                lpBuffer.Protect = tmp64.Protect;
                lpBuffer.Type = tmp64.Type;

                return retVal;
            }

            var tmp32 = new MEMORY_BASIC_INFORMATION32();

            retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp32, new UIntPtr((uint) Marshal.SizeOf(tmp32)));

            lpBuffer.BaseAddress = tmp32.BaseAddress;
            lpBuffer.AllocationBase = tmp32.AllocationBase;
            lpBuffer.AllocationProtect = tmp32.AllocationProtect;
            lpBuffer.RegionSize = tmp32.RegionSize;
            lpBuffer.State = tmp32.State;
            lpBuffer.Protect = tmp32.Protect;
            lpBuffer.Type = tmp32.Type;

            return retVal;
        }

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
#endif

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("dbghelp.dll")]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            int ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallackParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll")]
        private static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
        );

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer,
            UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
            IntPtr nSize, out long lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
        );

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern bool _CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(
            IntPtr hObject
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
        );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern int WaitForSingleObject(
            IntPtr handle,
            int milliseconds
        );

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer,
            UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        // Added to avoid casting to UIntPtr
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer,
            UIntPtr nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            UIntPtr lpStartAddress, // raw Pointer into remote process  
            IntPtr lpParameter,
            uint dwCreationFlags,
            out IntPtr lpThreadId
        );

        [DllImport("kernel32")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // privileges
        private const int PROCESS_CREATE_THREAD = 0x0002;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        private const uint MEM_FREE = 0x10000;
        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;

        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_WRITECOPY = 0x08;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        private const uint PAGE_EXECUTE = 0x10;
        private const uint PAGE_EXECUTE_READ = 0x20;

        private const uint PAGE_GUARD = 0x100;
        private const uint PAGE_NOACCESS = 0x01;

        private readonly uint MEM_PRIVATE = 0x20000;
        private readonly uint MEM_IMAGE = 0x1000000;

        #endregion

        /// <summary>
        ///     The process handle that was opened. (Use OpenProcess function to populate this variable)
        /// </summary>
        public IntPtr pHandle;

        public Process theProc;
        public byte[] dumpBytes;

        internal enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        private bool IsDigitsOnly(string str)
        {
            foreach (var c in str)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }

        /// <summary>
        ///     Open the PC game process with all security and access rights.
        /// </summary>
        /// <param name="proc">Use process name or process ID here.</param>
        /// <returns></returns>
        public bool OpenProcess(int pid)
        {
            if (!isAdmin())
                Console.WriteLine(
                    "WARNING: You are NOT running this program as admin! Visit https://github.com/erfg12/memory.dll/wiki/Administrative-Privileges");

            try
            {
                if (theProc != null && theProc.Id == pid)
                    return true;

                if (pid <= 0)
                    return false;

                theProc = Process.GetProcessById(pid);

                if (theProc != null && !theProc.Responding)
                    return false;

                //pHandle = OpenProcess(0x1F0FFF, true, pid);
                pHandle = theProc.Handle;
                //Process.EnterDebugMode();

                if (pHandle == IntPtr.Zero)
                {
                    var eCode = Marshal.GetLastWin32Error();
                }

                mainModule = theProc.MainModule;

                getModules();

                // Lets set the process to 64bit or not here (cuts down on api calls)
                Is64Bit = Environment.Is64BitOperatingSystem && IsWow64Process(pHandle, out var retVal) && !retVal;

                Debug.WriteLine("Program is operating at Administrative level. Process #" + theProc +
                                " is open and modules are stored.");

                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Failed to open process: " + exc);
                return false;
            }
        }


        /// <summary>
        ///     Open the PC game process with all security and access rights.
        /// </summary>
        /// <param name="proc">Use process name or process ID here.</param>
        /// <returns></returns>
        public bool OpenProcess(string proc)
        {
            return OpenProcess(getProcIDFromName(proc));
        }

        /// <summary>
        ///     Check if program is running with administrative privileges. Read about it here:
        ///     https://github.com/erfg12/memory.dll/wiki/Administrative-Privileges
        /// </summary>
        /// <returns></returns>
        public bool isAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        ///     Check if opened process is 64bit. Used primarily for getCode().
        /// </summary>
        /// <returns>True if 64bit false if 32bit.</returns>
        public bool is64bit()
        {
            return Is64Bit;
        }

        public bool Is64Bit { get; private set; }


        /// <summary>
        ///     Builds the process modules dictionary (names with addresses).
        /// </summary>
        public void getModules()
        {
            if (theProc == null)
                return;

            modules.Clear();
            foreach (ProcessModule Module in theProc.Modules)
                if (!string.IsNullOrEmpty(Module.ModuleName) && !modules.ContainsKey(Module.ModuleName))
                    modules.Add(Module.ModuleName, Module.BaseAddress);
        }

        public void setFocus()
        {
            //int style = GetWindowLong(procs.MainWindowHandle, -16);
            //if ((style & 0x20000000) == 0x20000000) //minimized
            //    SendMessage(procs.Handle, 0x0112, (IntPtr)0xF120, IntPtr.Zero);
            SetForegroundWindow(theProc.MainWindowHandle);
        }

        /// <summary>
        ///     Get the process ID number by process name.
        /// </summary>
        /// <param name="name">Example: "eqgame". Use task manager to find the name. Do not include .exe</param>
        /// <returns></returns>
        public int getProcIDFromName(string name) //new 1.0.2 function
        {
            var processlist = Process.GetProcesses();

            if (name.Contains(".exe"))
                name = name.Replace(".exe", "");

            foreach (var theprocess in processlist)
                if (theprocess.ProcessName.Equals(name, StringComparison.CurrentCultureIgnoreCase)
                ) //find (name).exe in the process list (use task manager to find the name)
                    return theprocess.Id;

            return 0; //if we fail to find it
        }

        /// <summary>
        ///     Convert a byte array to a literal string
        /// </summary>
        /// <param name="buffer">Byte array to convert to byte string</param>
        /// <returns></returns>
        public string byteArrayToString(byte[] buffer)
        {
            var build = new StringBuilder();
            var i = 1;
            foreach (var b in buffer)
            {
                build.Append(string.Format("0x{0:X}", b));
                if (i < buffer.Count())
                    build.Append(" ");
                i++;
            }

            return build.ToString();
        }

        /// <summary>
        ///     Get code from ini file.
        /// </summary>
        /// <param name="name">label for address or code</param>
        /// <param name="file">path and name of ini file</param>
        /// <returns></returns>
        public string LoadCode(string name, string file)
        {
            var returnCode = new StringBuilder(1024);
            uint read_ini_result;

            if (file != "")
                read_ini_result =
                    GetPrivateProfileString("codes", name, "", returnCode, (uint) returnCode.Capacity, file);
            else
                returnCode.Append(name);

            return returnCode.ToString();
        }

        private int LoadIntCode(string name, string path)
        {
            try
            {
                var intValue = Convert.ToInt32(LoadCode(name, path), 16);
                if (intValue >= 0)
                    return intValue;
                return 0;
            }
            catch
            {
                Debug.WriteLine("ERROR: LoadIntCode function crashed!");
                return 0;
            }
        }

        /// <summary>
        ///     Dictionary with our opened process module names with addresses.
        /// </summary>
        public Dictionary<string, IntPtr> modules = new Dictionary<string, IntPtr>();

        /// <summary>
        ///     Make a named pipe (if not already made) and call to a remote function.
        /// </summary>
        /// <param name="func">remote function to call</param>
        /// <param name="name">name of the thread</param>
        public void ThreadStartClient(string func, string name)
        {
            //ManualResetEvent SyncClientServer = (ManualResetEvent)obj;
            using (var pipeStream = new NamedPipeClientStream(name))
            {
                if (!pipeStream.IsConnected)
                    pipeStream.Connect();

                //MessageBox.Show("[Client] Pipe connection established");
                using (var sw = new StreamWriter(pipeStream))
                {
                    if (!sw.AutoFlush)
                        sw.AutoFlush = true;
                    sw.WriteLine(func);
                }
            }
        }

        private ProcessModule mainModule;

        /// <summary>
        ///     Cut a string that goes on for too long or one that is possibly merged with another string.
        /// </summary>
        /// <param name="str">The string you want to cut.</param>
        /// <returns></returns>
        public string CutString(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
                else
                    break;
            return sb.ToString();
        }

        /// <summary>
        ///     Clean up a string that has bad characters in it.
        /// </summary>
        /// <param name="str">The string you want to sanitize.</param>
        /// <returns></returns>
        public string sanitizeString(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
            return sb.ToString();
        }

        #region readMemory

        /// <summary>
        ///     Reads up to `length ` bytes from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="length">The maximum bytes to read.</param>
        /// <param name="file">path and name of ini file.</param>
        /// <returns>The bytes read or null</returns>
        public byte[] readBytes(string code, long length, string file = "")
        {
            var memory = new byte[length];
            var theCode = getCode(code, file);

            if (!ReadProcessMemory(pHandle, theCode, memory, (UIntPtr) length, IntPtr.Zero))
                return null;

            return memory;
        }

        /// <summary>
        ///     Read a float value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public float readFloat(string code, string file = "")
        {
            var memory = new byte[4];

            UIntPtr theCode;
            theCode = getCode(code, file);
            if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr) 4, IntPtr.Zero))
            {
                var address = BitConverter.ToSingle(memory, 0);
                var returnValue = (float) Math.Round(address, 2);
                if (returnValue < -99999 || returnValue > 99999)
                    return 0;
                return returnValue;
            }

            return 0;
        }

        public string readString(long address)
        {
            return readString(address.ToString("X"));
        }

        /// <summary>
        ///     Read a string value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <param name="length">length of bytes to read (OPTIONAL)</param>
        /// <returns></returns>
        public string readString(string code, string file = "", int length = 32)
        {
            var memoryNormal = new byte[length];
            UIntPtr theCode;
            theCode = getCode(code, file);
            if (ReadProcessMemory(pHandle, theCode, memoryNormal, (UIntPtr) length, IntPtr.Zero))
                return Encoding.UTF8.GetString(memoryNormal);
            return "";
        }

        public int readUIntPtr(UIntPtr code)
        {
            var memory = new byte[4];
            if (ReadProcessMemory(pHandle, code, memory, (UIntPtr) 4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            return 0;
        }

        /// <summary>
        ///     Read an integer from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public int readInt(string code, string file = "")
        {
            var memory = new byte[4];
            UIntPtr theCode;
            theCode = getCode(code, file);
            if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr) 4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            return 0;
        }

        public long readLong(long address)
        {
            return readLong(address.ToString("X"));
        }

        /// <summary>
        ///     Read a long value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public long readLong(string code, string file = "")
        {
            var memory = new byte[16];
            UIntPtr theCode;

            theCode = getCode(code, file);

            if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr) 16, IntPtr.Zero))
                return BitConverter.ToInt64(memory, 0);
            return 0;
        }

        /// <summary>
        ///     Read a UInt value from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public ulong readUInt(string code, string file = "")
        {
            var memory = new byte[4];
            UIntPtr theCode;
            theCode = getCode(code, file);

            if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr) 4, IntPtr.Zero))
                return BitConverter.ToUInt64(memory, 0);
            return 0;
        }

        /// <summary>
        ///     Reads a 2 byte value from an address and moves the address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="moveQty">Quantity to move.</param>
        /// <param name="file">path and name of ini file (OPTIONAL)</param>
        /// <returns></returns>
        public int read2ByteMove(string code, int moveQty, string file = "")
        {
            var memory = new byte[4];
            UIntPtr theCode;
            theCode = getCode(code, file);

            var newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(pHandle, newCode, memory, (UIntPtr) 2, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            return 0;
        }

        /// <summary>
        ///     Reads an integer value from address and moves the address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="moveQty">Quantity to move.</param>
        /// <param name="file">path and name of ini file (OPTIONAL)</param>
        /// <returns></returns>
        public int readIntMove(string code, int moveQty, string file = "")
        {
            var memory = new byte[4];
            UIntPtr theCode;
            theCode = getCode(code, file);

            var newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(pHandle, newCode, memory, (UIntPtr) 4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            return 0;
        }

        /// <summary>
        ///     Get UInt and move to another address by moveQty. Use in a for loop.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="moveQty">Quantity to move.</param>
        /// <param name="file">path and name of ini file (OPTIONAL)</param>
        /// <returns></returns>
        public ulong readUIntMove(string code, int moveQty, string file = "")
        {
            var memory = new byte[8];
            UIntPtr theCode;
            theCode = getCode(code, file, 8);

            var newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(pHandle, newCode, memory, (UIntPtr) 8, IntPtr.Zero))
                return BitConverter.ToUInt64(memory, 0);
            return 0;
        }

        /// <summary>
        ///     Read a 2 byte value from an address. Returns an integer.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and file name to ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public int read2Byte(string code, string file = "")
        {
            var memoryTiny = new byte[4];

            UIntPtr theCode;
            theCode = getCode(code, file);

            if (ReadProcessMemory(pHandle, theCode, memoryTiny, (UIntPtr) 2, IntPtr.Zero))
                return BitConverter.ToInt32(memoryTiny, 0);
            return 0;
        }

        /// <summary>
        ///     Read 1 byte from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and file name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public int readByte(string code, string file = "")
        {
            var memoryTiny = new byte[4];

            UIntPtr theCode;
            theCode = getCode(code, file);

            if (ReadProcessMemory(pHandle, theCode, memoryTiny, (UIntPtr) 1, IntPtr.Zero))
                return BitConverter.ToInt32(memoryTiny, 0);
            return 0;
        }

        public int readPByte(UIntPtr address, string code, string file = "")
        {
            var memory = new byte[4];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memory, (UIntPtr) 1, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            return 0;
        }

        public float readPFloat(UIntPtr address, string code, string file = "")
        {
            var memory = new byte[4];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memory, (UIntPtr) 4, IntPtr.Zero))
            {
                var spawn = BitConverter.ToSingle(memory, 0);
                return (float) Math.Round(spawn, 2);
            }

            return 0;
        }

        public int readPInt(UIntPtr address, string code, string file = "")
        {
            var memory = new byte[4];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memory, (UIntPtr) 4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            return 0;
        }

        public string readPString(UIntPtr address, string code, string file = "")
        {
            var memoryNormal = new byte[32];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memoryNormal, (UIntPtr) 32, IntPtr.Zero))
                return CutString(Encoding.ASCII.GetString(memoryNormal));
            return "";
        }

        #endregion

        #region writeMemory

        /// <summary>
        ///     Write to memory address. See https://github.com/erfg12/memory.dll/wiki/writeMemory() for more information.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="type">byte, bytes, float, int, string or long.</param>
        /// <param name="write">value to write to address.</param>
        /// <param name="file">path and name of .ini file (OPTIONAL)</param>
        public bool writeMemory(string code, string type, string write, string file = "")
        {
            var memory = new byte[4];
            var size = 4;

            UIntPtr theCode;
            theCode = getCode(code, file);

            if (type == "float")
            {
                memory = BitConverter.GetBytes(Convert.ToSingle(write));
                size = 4;
            }
            else if (type == "int")
            {
                memory = BitConverter.GetBytes(Convert.ToInt32(write));
                size = 4;
            }
            else if (type == "byte")
            {
                memory = new byte[1];
                memory[0] = Convert.ToByte(write, 16);
                size = 1;
            }
            else if (type == "2bytes")
            {
                memory = new byte[2];
                memory[0] = (byte) (Convert.ToInt32(write) % 256);
                memory[1] = (byte) (Convert.ToInt32(write) / 256);
                size = 2;
            }
            else if (type == "bytes")
            {
                if (write.Contains(",") || write.Contains(" ")) //check if it's a proper array
                {
                    string[] stringBytes;
                    if (write.Contains(","))
                        stringBytes = write.Split(',');
                    else
                        stringBytes = write.Split(' ');
                    //Debug.WriteLine("write:" + write + " stringBytes:" + stringBytes);

                    var c = stringBytes.Count();
                    memory = new byte[c];
                    for (var i = 0; i < c; i++) memory[i] = Convert.ToByte(stringBytes[i], 16);
                    size = stringBytes.Count();
                }
                else //wasnt array, only 1 byte
                {
                    memory = new byte[1];
                    memory[0] = Convert.ToByte(write, 16);
                    size = 1;
                }
            }
            else if (type == "long")
            {
                memory = BitConverter.GetBytes(Convert.ToInt64(write));
                size = 16;
            }
            else if (type == "string")
            {
                memory = new byte[write.Length];
                memory = Encoding.UTF8.GetBytes(write);
                size = write.Length;
            }

            //Debug.Write("DEBUG: Writing bytes [TYPE:" + type + " ADDR:" + theCode + "] " + String.Join(",", memory) + Environment.NewLine);
            return WriteProcessMemory(pHandle, theCode, memory, (UIntPtr) size, IntPtr.Zero);
        }

        /// <summary>
        ///     Write to address and move by moveQty. Good for byte arrays. See
        ///     https://github.com/erfg12/memory.dll/wiki/Writing-a-Byte-Array for more information.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="type">byte, bytes, float, int, string or long.</param>
        /// <param name="write">byte to write</param>
        /// <param name="moveQty">quantity to move</param>
        /// <param name="file">path and name of .ini file (OPTIONAL)</param>
        /// <returns></returns>
        public bool writeMove(string code, string type, string write, int moveQty, string file = "")
        {
            var memory = new byte[4];
            var size = 4;

            UIntPtr theCode;
            theCode = getCode(code, file);

            if (type == "float")
            {
                memory = new byte[write.Length];
                memory = BitConverter.GetBytes(Convert.ToSingle(write));
                size = write.Length;
            }
            else if (type == "int")
            {
                memory = BitConverter.GetBytes(Convert.ToInt32(write));
                size = 4;
            }
            else if (type == "byte")
            {
                memory = new byte[1];
                memory[0] = Convert.ToByte(write, 16);
                size = 1;
            }
            else if (type == "string")
            {
                memory = new byte[write.Length];
                memory = Encoding.UTF8.GetBytes(write);
                size = write.Length;
            }

            var newCode = UIntPtr.Add(theCode, moveQty);

            Debug.Write("DEBUG: Writing bytes [TYPE:" + type + " ADDR:[O]" + theCode + " [N]" + newCode + " MQTY:" +
                        moveQty + "] " + string.Join(",", memory) + Environment.NewLine);
            Thread.Sleep(1000);
            return WriteProcessMemory(pHandle, newCode, memory, (UIntPtr) size, IntPtr.Zero);
        }

        /// <summary>
        ///     Write byte array to addresses.
        /// </summary>
        /// <param name="code">address to write to</param>
        /// <param name="write">byte array to write</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        public void writeBytes(string code, byte[] write, string file = "")
        {
            UIntPtr theCode;
            theCode = getCode(code, file);
            WriteProcessMemory(pHandle, theCode, write, (UIntPtr) write.Length, IntPtr.Zero);
        }

        /// <summary>
        ///     Write byte array to address
        /// </summary>
        /// <param name="address">Address to write to</param>
        /// <param name="write">Byte array to write to</param>
        public void writeBytes(IntPtr address, byte[] write)
        {
            WriteProcessMemory(pHandle, address, write, (UIntPtr) write.Length, out var bytesRead);
        }

        #endregion

        /// <summary>
        ///     Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file</param>
        /// <param name="path">path to ini file</param>
        /// <param name="size">size of address (default is 8)</param>
        /// <returns></returns>
        public UIntPtr getCode(string name, string path, int size = 8)
        {
            if (is64bit())
            {
                //Debug.WriteLine("Changing to 64bit code...");
                if (size == 8) size = 16; //change to 64bit
                return get64bitCode(name, path, size); //jump over to 64bit code grab
            }

            var theCode = LoadCode(name, path);

            if (theCode == "") return UIntPtr.Zero;

            if (!theCode.Contains("+") && !theCode.Contains(",")) return new UIntPtr(Convert.ToUInt32(theCode, 16));

            var newOffsets = theCode;

            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            var memoryAddress = new byte[size];

            if (newOffsets.Contains(','))
            {
                var offsetsList = new List<int>();

                var newerOffsets = newOffsets.Split(',');
                foreach (var oldOffsets in newerOffsets)
                {
                    var test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    offsetsList.Add(int.Parse(test, NumberStyles.HexNumber));
                }

                var offsets = offsetsList.ToArray();

                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    ReadProcessMemory(pHandle, (UIntPtr) ((int) mainModule.BaseAddress + offsets[0]), memoryAddress,
                        (UIntPtr) size, IntPtr.Zero);
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    var altModule = IntPtr.Zero;
                    if (!moduleName[0].Contains(".dll") && !moduleName[0].Contains(".exe"))
                    {
                        var theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr) int.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = modules[moduleName[0]];
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            Debug.WriteLine("Modules: " + string.Join(",", modules));
                        }
                    }

                    ReadProcessMemory(pHandle, (UIntPtr) ((int) altModule + offsets[0]), memoryAddress, (UIntPtr) size,
                        IntPtr.Zero);
                }
                else
                {
                    ReadProcessMemory(pHandle, (UIntPtr) offsets[0], memoryAddress, (UIntPtr) size, IntPtr.Zero);
                }

                var num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.

                var base1 = (UIntPtr) 0;

                for (var i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(num1 + Convert.ToUInt32(offsets[i]));
                    ReadProcessMemory(pHandle, base1, memoryAddress, (UIntPtr) size, IntPtr.Zero);
                    num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.
                }

                return base1;
            }

            {
                var trueCode = Convert.ToInt32(newOffsets, 16);
                var altModule = IntPtr.Zero;
                //Debug.WriteLine("newOffsets=" + newOffsets);
                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    altModule = mainModule.BaseAddress;
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    if (!moduleName[0].Contains(".dll") && !moduleName[0].Contains(".exe"))
                    {
                        var theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr) int.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = modules[moduleName[0]];
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            Debug.WriteLine("Modules: " + string.Join(",", modules));
                        }
                    }
                }
                else
                {
                    altModule = modules[theCode.Split('+')[0]];
                }

                return (UIntPtr) ((int) altModule + trueCode);
            }
        }

        /// <summary>
        ///     Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file</param>
        /// <param name="path">path to ini file</param>
        /// <param name="size">size of address (default is 16)</param>
        /// <returns></returns>
        private UIntPtr get64bitCode(string name, string path, int size = 16)
        {
            var theCode = LoadCode(name, path);
            if (theCode == "")
                return UIntPtr.Zero;
            var newOffsets = theCode;
            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            var memoryAddress = new byte[size];

            if (!theCode.Contains("+") && !theCode.Contains(",")) return new UIntPtr(Convert.ToUInt64(theCode, 16));

            if (newOffsets.Contains(','))
            {
                var offsetsList = new List<long>();

                var newerOffsets = newOffsets.Split(',');
                foreach (var oldOffsets in newerOffsets)
                {
                    var test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    offsetsList.Add(long.Parse(test, NumberStyles.HexNumber));
                }

                var offsets = offsetsList.ToArray();

                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    ReadProcessMemory(pHandle, (UIntPtr) ((long) mainModule.BaseAddress + offsets[0]), memoryAddress,
                        (UIntPtr) size, IntPtr.Zero);
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    var altModule = IntPtr.Zero;
                    if (!moduleName[0].Contains(".dll") && !moduleName[0].Contains(".exe"))
                        altModule = (IntPtr) long.Parse(moduleName[0], NumberStyles.HexNumber);
                    else
                        try
                        {
                            altModule = modules[moduleName[0]];
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            Debug.WriteLine("Modules: " + string.Join(",", modules));
                        }

                    ReadProcessMemory(pHandle, (UIntPtr) ((long) altModule + offsets[0]), memoryAddress, (UIntPtr) size,
                        IntPtr.Zero);
                }
                else
                {
                    ReadProcessMemory(pHandle, (UIntPtr) offsets[0], memoryAddress, (UIntPtr) size, IntPtr.Zero);
                }

                var num1 = BitConverter.ToUInt64(memoryAddress, 0);

                var base1 = (UIntPtr) 0;

                for (var i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(num1 + Convert.ToUInt64(offsets[i]));
                    ReadProcessMemory(pHandle, base1, memoryAddress, (UIntPtr) size, IntPtr.Zero);
                    num1 = BitConverter.ToUInt64(memoryAddress, 0);
                }

                return base1;
            }

            {
                var trueCode = Convert.ToInt64(newOffsets, 16);
                var altModule = IntPtr.Zero;
                if (theCode.Contains("base") || theCode.Contains("main"))
                {
                    altModule = mainModule.BaseAddress;
                }
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    var moduleName = theCode.Split('+');
                    if (!moduleName[0].Contains(".dll") && !moduleName[0].Contains(".exe"))
                    {
                        var theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr) long.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = modules[moduleName[0]];
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            Debug.WriteLine("Modules: " + string.Join(",", modules));
                        }
                    }
                }
                else
                {
                    altModule = modules[theCode.Split('+')[0]];
                }

                return (UIntPtr) ((long) altModule + trueCode);
            }
        }

        /// <summary>
        ///     Close the process when finished.
        /// </summary>
        public void closeProcess()
        {
            if (pHandle == null)
                return;

            CloseHandle(pHandle);
            theProc = null;
        }

        /// <summary>
        ///     Inject a DLL file.
        /// </summary>
        /// <param name="strDLLName">path and name of DLL file.</param>
        public void InjectDLL(string strDLLName)
        {
            IntPtr bytesout;

            foreach (ProcessModule pm in theProc.Modules)
                if (pm.ModuleName.StartsWith("inject", StringComparison.InvariantCultureIgnoreCase))
                    return;

            if (!theProc.Responding)
                return;

            var LenWrite = strDLLName.Length + 1;
            var AllocMem = VirtualAllocEx(pHandle, (IntPtr) null, (uint) LenWrite, MEM_COMMIT | MEM_RESERVE,
                PAGE_READWRITE);

            WriteProcessMemory(pHandle, AllocMem, strDLLName, (UIntPtr) LenWrite, out bytesout);
            var Injector = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
                return;

            var hThread = CreateRemoteThread(pHandle, (IntPtr) null, 0, Injector, AllocMem, 0, out bytesout);
            if (hThread == null)
                return;

            var Result = WaitForSingleObject(hThread, 10 * 1000);
            if (Result == 0x00000080L || Result == 0x00000102L)
            {
                if (hThread != null)
                    CloseHandle(hThread);
                return;
            }

            VirtualFreeEx(pHandle, AllocMem, (UIntPtr) 0, 0x8000);

            if (hThread != null)
                CloseHandle(hThread);
        }

#if WINXP
#else
        /// <summary>
        ///     Creates a code cave to write custom opcodes in target process
        /// </summary>
        /// <param name="code">Address to create the trampoline</param>
        /// <param name="newBytes">The opcodes to write in the code cave</param>
        /// <param name="replaceCount">The number of bytes being replaced</param>
        /// <param name="size">size of the allocated region</param>
        /// <param name="file">ini file to look in</param>
        /// <remarks>
        ///     Please ensure that you use the proper replaceCount
        ///     if you replace halfway in an instruction you may cause bad things
        /// </remarks>
        /// <returns>IntPtr to created code cave for use for later deallocation</returns>
        public IntPtr CreateCodeCave(string code, byte[] newBytes, int replaceCount, int size = 0x10000,
            string file = "")
        {
            if (replaceCount < 5)
                return IntPtr.Zero; // returning IntPtr.Zero instead of throwing an exception
            // to better match existing code

            UIntPtr theCode;
            theCode = getCode(code, file);
            var address = new IntPtr((long) theCode);

            // if x64 we need to try to allocate near the address so we dont run into the +-2GB limit of the 0xE9 jmp

            var caveAddress = IntPtr.Zero;
            var prefered = address;

            for (var i = 0; i < 10 && caveAddress == IntPtr.Zero; i++)
            {
                caveAddress = VirtualAllocEx(pHandle, FindFreeBlockForRegion(prefered, (uint) newBytes.Length),
                    (uint) size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

                if (caveAddress == IntPtr.Zero)
                    prefered = IntPtr.Add(prefered, 0x10000);
            }

            // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
            if (caveAddress == IntPtr.Zero)
                caveAddress = VirtualAllocEx(pHandle, IntPtr.Zero, (uint) size, MEM_COMMIT | MEM_RESERVE,
                    PAGE_EXECUTE_READWRITE);

            var nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

            // (to - from - 5)
            var offset = (int) ((long) caveAddress - (long) address - 5);

            var jmpBytes = new byte[5 + nopsNeeded];
            jmpBytes[0] = 0xE9;
            BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

            for (var i = 5; i < jmpBytes.Length; i++) jmpBytes[i] = 0x90;
            writeBytes(address, jmpBytes);

            var caveBytes = new byte[5 + newBytes.Length];
            offset = (int) ((long) address + jmpBytes.Length - ((long) caveAddress + newBytes.Length) - 5);

            newBytes.CopyTo(caveBytes, 0);
            caveBytes[newBytes.Length] = 0xE9;
            BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

            writeBytes(caveAddress, caveBytes);

            return caveAddress;
        }

        private IntPtr FindFreeBlockForRegion(IntPtr baseAddress, uint size)
        {
            var minAddress = IntPtr.Subtract(baseAddress, 0x70000000);
            var maxAddress = IntPtr.Add(baseAddress, 0x70000000);

            var ret = IntPtr.Zero;
            var tmpAddress = IntPtr.Zero;

            GetSystemInfo(out var si);

            if (Is64Bit)
            {
                if ((long) minAddress > (long) si.maximumApplicationAddress ||
                    (long) minAddress < (long) si.minimumApplicationAddress)
                    minAddress = si.minimumApplicationAddress;

                if ((long) maxAddress < (long) si.minimumApplicationAddress ||
                    (long) maxAddress > (long) si.maximumApplicationAddress)
                    maxAddress = si.maximumApplicationAddress;
            }
            else
            {
                minAddress = si.minimumApplicationAddress;
                maxAddress = si.maximumApplicationAddress;
            }

            MEMORY_BASIC_INFORMATION mbi;

            var current = minAddress;
            var previous = current;

            while (VirtualQueryEx(pHandle, current, out mbi).ToUInt64() != 0)
            {
                if ((long) mbi.BaseAddress > (long) maxAddress)
                    return IntPtr.Zero; // No memory found, let windows handle

                if (mbi.State == MEM_FREE && mbi.RegionSize > size)
                {
                    if ((long) mbi.BaseAddress % si.allocationGranularity > 0)
                    {
                        // The whole size can not be used
                        tmpAddress = mbi.BaseAddress;
                        var offset = (int) (si.allocationGranularity -
                                            (long) tmpAddress % si.allocationGranularity);

                        // Check if there is enough left
                        if (mbi.RegionSize - offset >= size)
                        {
                            // yup there is enough
                            tmpAddress = IntPtr.Add(tmpAddress, offset);

                            if ((long) tmpAddress < (long) baseAddress)
                            {
                                tmpAddress = IntPtr.Add(tmpAddress, (int) (mbi.RegionSize - offset - size));

                                if ((long) tmpAddress > (long) baseAddress)
                                    tmpAddress = baseAddress;

                                // decrease tmpAddress until its alligned properly
                                tmpAddress = IntPtr.Subtract(tmpAddress,
                                    (int) ((long) tmpAddress % si.allocationGranularity));
                            }

                            // if the difference is closer then use that
                            if (Math.Abs((long) tmpAddress - (long) baseAddress) <
                                Math.Abs((long) ret - (long) baseAddress))
                                ret = tmpAddress;
                        }
                    }
                    else
                    {
                        tmpAddress = mbi.BaseAddress;

                        if ((long) tmpAddress < (long) baseAddress) // try to get it the cloest possible 
                            // (so to the end of the region - size and
                            // aligned by system allocation granularity)
                        {
                            tmpAddress = IntPtr.Add(tmpAddress, (int) (mbi.RegionSize - size));

                            if ((long) tmpAddress > (long) baseAddress)
                                tmpAddress = baseAddress;

                            // decrease until aligned properly
                            tmpAddress =
                                IntPtr.Subtract(tmpAddress, (int) ((long) tmpAddress % si.allocationGranularity));
                        }

                        if (Math.Abs((long) tmpAddress - (long) baseAddress) <
                            Math.Abs((long) ret - (long) baseAddress))
                            ret = tmpAddress;
                    }
                }

                if (mbi.RegionSize % si.allocationGranularity > 0)
                    mbi.RegionSize += si.allocationGranularity - mbi.RegionSize % si.allocationGranularity;

                previous = current;
                current = IntPtr.Add(mbi.BaseAddress, (int) mbi.RegionSize);

                if ((long) current > (long) maxAddress)
                    return ret;

                if ((long) previous > (long) current)
                    return ret; // Overflow
            }

            return ret;
        }
#endif

        [Flags]
        public enum ThreadAccess
        {
            TERMINATE = 0x0001,
            SUSPEND_RESUME = 0x0002,
            GET_CONTEXT = 0x0008,
            SET_CONTEXT = 0x0010,
            SET_INFORMATION = 0x0020,
            QUERY_INFORMATION = 0x0040,
            SET_THREAD_TOKEN = 0x0080,
            IMPERSONATE = 0x0100,
            DIRECT_IMPERSONATION = 0x0200
        }

        public static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint) pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint) pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                }
                while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

#if WINXP
#else
        private async Task PutTaskDelay(int delay)
        {
            await Task.Delay(delay);
        }
#endif

        private void AppendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public byte[] fileToBytes(string path, bool dontDelete = false)
        {
            var newArray = File.ReadAllBytes(path);
            if (!dontDelete)
                File.Delete(path);
            return newArray;
        }

        public string mSize()
        {
            if (is64bit())
                return "x16";
            return "x8";
        }

        /// <summary>
        ///     Convert a byte array to hex values in a string.
        /// </summary>
        /// <param name="ba">your byte array to convert</param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            var i = 1;
            foreach (var b in ba)
            {
                if (i == 16)
                {
                    hex.AppendFormat("{0:x2}{1}", b, Environment.NewLine);
                    i = 0;
                }
                else
                {
                    hex.AppendFormat("{0:x2} ", b);
                }

                i++;
            }

            return hex.ToString().ToUpper();
        }

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba) hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }

#if WINXP
#else

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            private ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        public struct MEMORY_BASIC_INFORMATION32
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public struct MEMORY_BASIC_INFORMATION64
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public long RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public ulong getMinAddress()
        {
            SYSTEM_INFO SI;
            GetSystemInfo(out SI);
            return (ulong) SI.minimumApplicationAddress;
        }

        /// <summary>
        ///     Dump memory page by page to a dump.dmp file. Can be used with Cheat Engine.
        /// </summary>
        public bool DumpMemory(string file = "dump.dmp")
        {
            Debug.Write("[DEBUG] memory dump starting... (" + DateTime.Now.ToString("h:mm:ss tt") + ")" +
                        Environment.NewLine);
            var sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            var proc_min_address = sys_info.minimumApplicationAddress;
            var proc_max_address = sys_info.maximumApplicationAddress;

            // saving the values as long ints so I won't have to do a lot of casts later
            var proc_min_address_l = (long) proc_min_address; //(Int64)procs.MainModule.BaseAddress;
            var proc_max_address_l = theProc.VirtualMemorySize64 + proc_min_address_l;

            //int arrLength = 0;
            if (File.Exists(file))
                File.Delete(file);


            var memInfo = new MEMORY_BASIC_INFORMATION();
            while (proc_min_address_l < proc_max_address_l)
            {
                VirtualQueryEx(pHandle, proc_min_address, out memInfo);
                var buffer = new byte[memInfo.RegionSize];
                var test = (UIntPtr) memInfo.RegionSize;
                var test2 = (UIntPtr) (long) memInfo.BaseAddress;

                ReadProcessMemory(pHandle, test2, buffer, test, IntPtr.Zero);

                AppendAllBytes(file, buffer); //due to memory limits, we have to dump it then store it in an array.
                //arrLength += buffer.Length;

                proc_min_address_l += memInfo.RegionSize;
                proc_min_address = new IntPtr(proc_min_address_l);
            }


            Debug.Write("[DEBUG] memory dump completed. Saving dump file to " + file + ". (" +
                        DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);
            return true;
        }

        /// <summary>
        ///     Array of byte scan.
        /// </summary>
        /// <param name="search">array of bytes to search for, OR your ini code label.</param>
        /// <param name="writable">Include writable addresses in scan</param>
        /// <param name="executable">Include executable addresses in scan</param>
        /// <param name="file">ini file (OPTIONAL)</param>
        /// <returns>IEnumerable of all addresses found.</returns>
        public async Task<IEnumerable<long>> AoBScan(string search, bool writable = false, bool executable = true,
            string file = "")
        {
            return await AoBScan(0, long.MaxValue, search, writable, executable, file);
        }

        private struct MemoryRegionResult
        {
            public IntPtr CurrentBaseAddress { get; set; }
            public long RegionSize { get; set; }
            public IntPtr RegionBase { get; set; }
        }

        /// <summary>
        ///     Array of Byte scan.
        /// </summary>
        /// <param name="start">Your starting address.</param>
        /// <param name="end">ending address</param>
        /// <param name="search">array of bytes to search for, OR your ini code label.</param>
        /// <param name="file">ini file (OPTIONAL)</param>
        /// <param name="writable">Include writable addresses in scan</param>
        /// <param name="executable">Include executable addresses in scan</param>
        /// <returns>IEnumerable of all addresses found.</returns>
        public async Task<IEnumerable<long>> AoBScan(long start, long end, string search, bool writable = false,
            bool executable = true, string file = "")
        {
            var memRegionList = new List<MemoryRegionResult>();

            var memCode = LoadCode(search, file);

            var stringByteArray = memCode.Split(' ');
            var mask = new byte[stringByteArray.Length];

            for (var i = 0; i < stringByteArray.Length; i++)
            {
                var ba = stringByteArray[i];

                if (ba == "??")
                {
                    mask[i] = 0x00;
                    stringByteArray[i] = "0x00";
                }
                else if (char.IsLetterOrDigit(ba[0]) && ba[1] == '?')
                {
                    mask[i] = 0xF0;
                    stringByteArray[i] = ba[0] + "0";
                }
                else if (char.IsLetterOrDigit(ba[1]) && ba[0] == '?')
                {
                    mask[i] = 0x0F;
                    stringByteArray[i] = "0" + ba[1];
                }
                else
                {
                    mask[i] = 0xFF;
                }
            }

            var sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            var proc_min_address = sys_info.minimumApplicationAddress;
            var proc_max_address = sys_info.maximumApplicationAddress;

            if (start < proc_min_address.ToInt64())
                start = proc_min_address.ToInt64();

            if (end > proc_max_address.ToInt64())
                end = proc_max_address.ToInt64();

            Debug.Write("[DEBUG] memory scan starting... (min:0x" + proc_min_address.ToInt64().ToString(mSize()) +
                        " max:0x" + proc_max_address.ToInt64().ToString(mSize()) + " time:" +
                        DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);

            var currentBaseAddress = new IntPtr(start);

            var memInfo = new MEMORY_BASIC_INFORMATION();
            while (VirtualQueryEx(pHandle, currentBaseAddress, out memInfo).ToUInt64() != 0 &&
                   (ulong) currentBaseAddress.ToInt64() < (ulong) end &&
                   (ulong) currentBaseAddress.ToInt64() + (ulong) memInfo.RegionSize >
                   (ulong) currentBaseAddress.ToInt64())
            {
                var isValid = memInfo.State == MEM_COMMIT;
                isValid &= (ulong) memInfo.BaseAddress.ToInt64() < (ulong) proc_max_address.ToInt64();
                isValid &= (memInfo.Protect & PAGE_GUARD) == 0;
                isValid &= (memInfo.Protect & PAGE_NOACCESS) == 0;
                isValid &= memInfo.Type == MEM_PRIVATE || memInfo.Type == MEM_IMAGE;

                if (isValid)
                {
                    var isWritable = (memInfo.Protect & PAGE_READWRITE) > 0 ||
                                     (memInfo.Protect & PAGE_WRITECOPY) > 0 ||
                                     (memInfo.Protect & PAGE_EXECUTE_READWRITE) > 0 ||
                                     (memInfo.Protect & PAGE_EXECUTE_WRITECOPY) > 0;

                    var isExecutable = (memInfo.Protect & PAGE_EXECUTE) > 0 ||
                                       (memInfo.Protect & PAGE_EXECUTE_READ) > 0 ||
                                       (memInfo.Protect & PAGE_EXECUTE_READWRITE) > 0 ||
                                       (memInfo.Protect & PAGE_EXECUTE_WRITECOPY) > 0;

                    isWritable &= writable;
                    isExecutable &= executable;

                    isValid &= isWritable || isExecutable;
                }

                if (!isValid)
                {
                    currentBaseAddress = new IntPtr(memInfo.BaseAddress.ToInt64() + memInfo.RegionSize);
                    continue;
                }


                var memRegion = new MemoryRegionResult
                {
                    CurrentBaseAddress = currentBaseAddress,
                    RegionSize = memInfo.RegionSize,
                    RegionBase = memInfo.BaseAddress
                };

                currentBaseAddress =
                    new IntPtr(memInfo.BaseAddress.ToInt64() + memInfo.RegionSize);

                if (memRegionList.Count > 0)
                {
                    var previousRegion = memRegionList[memRegionList.Count - 1];

                    if ((long) previousRegion.RegionBase + previousRegion.RegionSize == (long) memInfo.BaseAddress)
                    {
                        memRegionList[memRegionList.Count - 1] = new MemoryRegionResult
                        {
                            CurrentBaseAddress = previousRegion.CurrentBaseAddress,
                            RegionBase = previousRegion.RegionBase,
                            RegionSize = previousRegion.RegionSize + memInfo.RegionSize
                        };

                        continue;
                    }
                }

                memRegionList.Add(memRegion);
            }

            var bagResult = new ConcurrentBag<long>();

            Parallel.ForEach(memRegionList,
                (item, parallelLoopState, index) =>
                {
                    var compareResults = CompareScan(item, stringByteArray, mask);

                    foreach (var result in compareResults)
                        bagResult.Add(result);
                });

            return bagResult.ToList().OrderBy(c => c);
        }

        /// <summary>
        ///     Array of bytes scan
        /// </summary>
        /// <param name="code">Starting address or ini label</param>
        /// <param name="end">ending address</param>
        /// <param name="search">array of bytes to search for or your ini code label</param>
        /// <param name="file">ini file</param>
        /// <returns>First address found</returns>
        public async Task<long> AoBScan(string code, long end, string search, string file = "")
        {
            var start = (long) getCode(code, file).ToUInt64();

            return (await AoBScan(start, end, search, true, true, file)).FirstOrDefault();
        }

        private long[] CompareScan(MemoryRegionResult item, string[] aobToFind, byte[] mask)
        {
            if (mask.Length != aobToFind.Length)
                throw new ArgumentException($"{nameof(aobToFind)}.Length != {nameof(mask)}.Length");

            var buffer = new byte[item.RegionSize];
            ReadProcessMemory(pHandle, item.CurrentBaseAddress, buffer, (IntPtr) item.RegionSize, out var bytesRead);


            var aobPattern = new byte[aobToFind.Length];

            for (var i = 0; i < aobToFind.Length; i++)
                aobPattern[i] = (byte) (Convert.ToByte(aobToFind[i], 16) & mask[i]);

            var result = 0 - aobToFind.Length;
            var ret = new List<long>();
            do
            {
                result = FindPattern(buffer, aobPattern, mask, result + aobToFind.Length);

                if (result >= 0)
                    ret.Add((long) item.CurrentBaseAddress + result);
            }
            while (result != -1);

            return ret.ToArray();
        }

        private int FindPattern(byte[] body, byte[] pattern, byte[] masks, int start = 0)
        {
            var foundIndex = -1;

            if (body.Length <= 0 || pattern.Length <= 0 || start > body.Length - pattern.Length ||
                pattern.Length > body.Length) return foundIndex;

            for (var index = start; index <= body.Length - pattern.Length; index++)
            {
                if (index == 0x154620)
                    index = 0x154620;

                if ((body[index] & masks[0]) == (pattern[0] & masks[0]))
                {
                    var match = true;
                    for (var index2 = 1; index2 <= pattern.Length - 1; index2++)
                    {
                        if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;
                        match = false;
                        break;
                    }

                    if (!match) continue;

                    foundIndex = index;
                    break;
                }
            }

            return foundIndex;
        }

#endif
    }
}