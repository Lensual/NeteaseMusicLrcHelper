using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NeteaseMusicLrcHelper
{
    public static class MemHelper
    {
        //ReadProcessMemory
        [DllImportAttribute("kernel32.dll", EntryPoint = "ReadProcessMemory")]
        private static extern bool ReadProcessMemory
        (
            IntPtr lpProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int nSize,
            IntPtr BytesRead
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
                if (buffer[0] == 0 && buffer[1] == 0) { break; }
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
            //Reading 4byte
            byte[] buffer = new byte[4];
            ReadProcessMemory(lpProcess, addr, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 4, IntPtr.Zero);

            return BitConverter.ToInt32(buffer, 0);
        }

        //ReadInt64
        public static Int64 ReadInt64(IntPtr lpProcess, Int64 BaseAddr, List<Int64> Offsets)
        {
            //CalcAddress
            IntPtr addr = (IntPtr)CalcAddr(lpProcess, BaseAddr, Offsets);
            //Reading 8byte
            byte[] buffer = new byte[8];
            ReadProcessMemory(lpProcess, addr, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 8, IntPtr.Zero);

            return BitConverter.ToInt64(buffer, 0);
        }

        //GetThreadStackAddr
        public static Int64 GetThreadStack0(Process process)
        {
            //Open Thread
            IntPtr hThread = MemHelper.OpenThread(MemHelper.ThreadAccess.QueryInformation, false, (uint)process.Threads[0].Id);

            //Read TBI
            THREAD_BASIC_INFORMATION tbi = new THREAD_BASIC_INFORMATION();
            int status = NtQueryInformationThread(hThread, ThreadInfoClass.ThreadBasicInformation, out tbi, Marshal.SizeOf(tbi), IntPtr.Zero);

            //Read StackBaseAddr ptr from TIB
            IntPtr tib = tbi.TebBaseAddress; //TIB
            int blocksize = 8; //x64
                               //int blocksize = 4; //x86
            IntPtr pStackBottomAddr = tib + blocksize; //StackBase of TIB

            //Get StackBottom Address
            byte[] buffer = new byte[blocksize];
            ReadProcessMemory(process.Handle, pStackBottomAddr, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), blocksize, IntPtr.Zero);
            UInt64 stackBottom = BitConverter.ToUInt64(buffer, 0);

            //Get kernel32.dll base addr
            ProcessModule m = null;
            foreach (ProcessModule pm in process.Modules)
            {
                if (pm.ModuleName == "KERNEL32.DLL")
                    m = pm;
            }
            //Search kernel32.dll address from Stack bottom
            for (UInt64 i = (UInt64)stackBottom - (UInt64)blocksize; i > (UInt64)stackBottom - 4096; i -= (UInt64)blocksize)
            {
                ReadProcessMemory(process.Handle, (IntPtr)i, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), blocksize, IntPtr.Zero);
                UInt64 val = BitConverter.ToUInt64(buffer, 0);
                if (val >= (UInt64)m.BaseAddress &&
                    val <= (UInt64)m.BaseAddress + (UInt64)m.ModuleMemorySize)
                {
                    return (Int64)i;
                }

            }
            throw new Exception();
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryInformationThread(
            IntPtr threadHandle,
            ThreadInfoClass threadInformationClass,
            out THREAD_BASIC_INFORMATION threadInformation,
            int threadInformationLength,
            IntPtr returnLengthPtr);

        private enum ThreadInfoClass : int
        {
            ThreadBasicInformation = 0,
            ThreadQuerySetWin32StartAddress = 9
        }

        private struct THREAD_BASIC_INFORMATION
        {
            public IntPtr ExitStatus; // original: LONG NTSTATUS
            public IntPtr TebBaseAddress; // original: PVOID
            public CLIENT_ID ClientId;
            public IntPtr AffinityMask; // original: ULONG_PTR
            public uint Priority; // original: DWORD
            public uint BasePriority; // original: DWORD
        }

        private struct CLIENT_ID
        {
            public IntPtr UniqueProcess; // original: PVOID
            public IntPtr UniqueThread; // original: PVOID
        }

        //OpenThread
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [Flags]
        public enum ThreadAccess : int
        {
            Terminate = 0x0001,
            SuspendResume = 0x0002,
            GetContext = 0x0008,
            SetContext = 0x0010,
            SetInformation = 0x0020,
            QueryInformation = 0x0040,
            SetThreadToken = 0x0080,
            Impersonate = 0x0100,
            DirectImpersonation = 0x0200
        }

        ////CloseHandle
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

    }
}
