using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NeteaseMusicLrcHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private Process NeteaseMusicProcess;
        private ProcessModule NeteaseMusicDLL;

        private void Init()
        {
            //Open Process
            NeteaseMusicProcess = Process.GetProcessesByName("NeteaseMusic")[0];
            MemHelper.OpenProcess(0x0010, false, NeteaseMusicProcess.Id);

            //Find NeteaseMusic.dll
            foreach (ProcessModule module in NeteaseMusicProcess.Modules)
            {
                if (module.ModuleName == "NeteaseMusic.dll") { NeteaseMusicDLL = module; }
            }

            //Read LRC
            this.text.Text = ReadLRC();
            
            
            
        }

        private string ReadLRC()
        {
            //准备指针
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B13588);
            Offsets.Add(0x30);
            Offsets.Add(0xA0);
            Offsets.Add(0x8);
            Offsets.Add(0x198);
            Offsets.Add(0xC);
            Int64 lrcAddr = MemHelper.CalcAddr(NeteaseMusicProcess.Handle, NeteaseMusicDLL.BaseAddress.ToInt64(),Offsets);
            //Reading
            List<byte> lrc = new List<byte>();
            byte[] buffer = new byte[2];
            IntPtr p = (IntPtr)lrcAddr;
            do
            {
                MemHelper.ReadProcessMemory(NeteaseMusicProcess.Handle, p, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), 2, IntPtr.Zero);
                lrc.AddRange(buffer);
                p = IntPtr.Add(p, 2);
            } while (!(buffer[0] == 0 && buffer[1] == 0 || lrc.Count >= 4096));

            return Encoding.Unicode.GetString(lrc.ToArray());
        }

    }
}
