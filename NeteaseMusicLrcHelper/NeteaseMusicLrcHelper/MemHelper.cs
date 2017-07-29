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
        //OpenProcess
        [DllImportAttribute("kernel32.dll", EntryPoint = "OpenProcess")]
        public static extern IntPtr OpenProcess
       (
           int iAccess,
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
        public static Int64 CalcAddr(IntPtr lpProcess, Int64 BaseAddr, List<Int64> Offsets){
            if (Offsets.Count > 1)
            {
                byte[] buffer = new byte[8];
                ReadProcessMemory(lpProcess, (IntPtr)(BaseAddr + Offsets[0]), Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 8, IntPtr.Zero);
                Offsets.RemoveAt(0);
                return CalcAddr(lpProcess, BitConverter.ToInt64(buffer,0), Offsets);
            }
            return BaseAddr + Offsets[0];
        }
        //ReadString
        public static string ReadString(IntPtr lpProcess,Int64 BaseAddr, List<Int64> Offsets)
        {
            //CalcAddress
            IntPtr addr = (IntPtr)CalcAddr(lpProcess, BaseAddr, Offsets);
            //Reading 2byte loop
            List<byte> lst = new List<byte>();
            byte[] buffer = new byte[2];
            while(true)
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
    }
}
