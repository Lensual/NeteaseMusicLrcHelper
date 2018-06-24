using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NeteaseMusicLrcHelper
{
    public static class MemHelper
    {

        public enum ProcessAccess : uint
        {
            /// <summary>
            /// Required to terminate a process using TerminateProcess.
            /// </summary>
            Terminate = 0x1,

            /// <summary>
            /// Required to create a thread.
            /// </summary>
            CreateThread = 0x2,

            /// <summary>
            /// Undocumented.
            /// </summary>
            SetSessionId = 0x4,

            /// <summary>
            /// Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
            /// </summary>
            VmOperation = 0x8,

            /// <summary>
            /// Required to read memory in a process using ReadProcessMemory.
            /// </summary>
            VmRead = 0x10,

            /// <summary>
            /// Required to write to memory in a process using WriteProcessMemory.
            /// </summary>
            VmWrite = 0x20,

            /// <summary>
            /// Required to duplicate a handle using DuplicateHandle.
            /// </summary>
            DupHandle = 0x40,

            /// <summary>
            /// Required to create a process.
            /// </summary>
            CreateProcess = 0x80,

            /// <summary>
            /// Required to set memory limits using SetProcessWorkingSetSize.
            /// </summary>
            SetQuota = 0x100,

            /// <summary>
            /// Required to set certain information about a process, such as its priority class (see SetPriorityClass).
            /// </summary>
            SetInformation = 0x200,

            /// <summary>
            /// Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken, GetExitCodeProcess, GetPriorityClass, and IsProcessInJob).
            /// </summary>
            QueryInformation = 0x400,

            /// <summary>
            /// Undocumented.
            /// </summary>
            SetPort = 0x800,

            /// <summary>
            /// Required to suspend or resume a process.
            /// </summary>
            SuspendResume = 0x800,

            /// <summary>
            /// Required to retrieve certain information about a process (see QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
            /// </summary>
            QueryLimitedInformation = 0x1000,

            /// <summary>
            /// Required to wait for the process to terminate using the wait functions.
            /// </summary>
            Synchronize = 0x100000
        }

        //OpenProcess
        [DllImportAttribute("kernel32.dll", EntryPoint = "OpenProcess")]
        private static extern IntPtr OpenProcess
        (
           ProcessAccess iAccess,
           bool Handle,
           int ProcessID
        );

        //GetModuleHandle
        [DllImport("kernel32")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        //ReadProcessMemory
        [DllImportAttribute("kernel32.dll", EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory
        (
            IntPtr lpProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int nSize,
            IntPtr BytesRead
        );

        //CloseHandle   
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        public static extern void CloseHandle
        (
            IntPtr hObject
        );

        //CalcAddr
        public static Int64 CalcAddr(IntPtr lpProcess, Int64 BaseAddr, List<Int64> Offsets)
        {
            if (Offsets.Count > 1)
            {
                byte[] buffer = new byte[8];
                ReadProcessMemory(lpProcess, (IntPtr)(BaseAddr + Offsets[0]), Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 8, IntPtr.Zero);
                Offsets.RemoveAt(0);
                return CalcAddr(lpProcess, BitConverter.ToInt64(buffer, 0), Offsets);
            }
            return BaseAddr + Offsets[0];
        }

        //ReadString
        public static string ReadString(IntPtr lpProcess, Int64 BaseAddr, List<Int64> Offsets)
        {
            //CalcAddress
            IntPtr addr = (IntPtr)CalcAddr(lpProcess, BaseAddr, Offsets);
            //Reading 2byte loop
            List<byte> lst = new List<byte>();
            byte[] buffer = new byte[2];
            while (true)
            {
                ReadProcessMemory(lpProcess, addr, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 2, IntPtr.Zero);
                if (buffer[0] == 0 && buffer[1] == 0 || lst.Count >= 10240) { break; }
                lst.AddRange(buffer);
                addr = IntPtr.Add(addr, 2);
            }

            return Encoding.Unicode.GetString(lst.ToArray());
        }

        //ReadDouble
        public static double ReadDouble(IntPtr lpProcess, Int64 BaseAddr, List<Int64> Offsets)
        {
            //CalcAddress
            IntPtr addr = (IntPtr)CalcAddr(lpProcess, BaseAddr, Offsets);
            //Reading 8byte
            byte[] buffer = new byte[8];
            ReadProcessMemory(lpProcess, addr, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 8, IntPtr.Zero);

            return BitConverter.ToDouble(buffer, 0);
        }

        //ReadInt
        public static int ReadInt(IntPtr lpProcess, Int64 BaseAddr, List<Int64> Offsets)
        {
            //CalcAddress
            IntPtr addr = (IntPtr)CalcAddr(lpProcess, BaseAddr, Offsets);
            //Reading 8byte
            byte[] buffer = new byte[4];
            ReadProcessMemory(lpProcess, addr, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 4, IntPtr.Zero);

            return BitConverter.ToInt32(buffer, 0);
        }

        //NTResumeProcess
        [DllImport("ntdll.dll")]
        private static extern uint NtResumeProcess([In] IntPtr processHandle);

        public static uint ResumeProcess(int PID)
        {
            IntPtr h= OpenProcess(ProcessAccess.SuspendResume, false, PID);
            return NtResumeProcess(h);
        }

    }
}
