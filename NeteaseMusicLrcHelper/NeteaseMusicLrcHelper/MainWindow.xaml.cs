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
using System.Timers;
using System.Threading;
using System.Windows.Media.Animation;
using System.Windows.Forms;

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
            InitIcon();
            Init();
        }

        private NotifyIcon notifyIcon;
        private Process NeteaseMusicProcess;
        private ProcessModule NeteaseMusicDLL;
        private LrcHelper.LRC CurrentLRC;
        private Thread thd_ScrollSync;


        #region DependProperty
        public bool EnabledLrc
        {
            get { return (bool)GetValue(EnabledLrcProperty); }
            set { SetValue(EnabledLrcProperty, value); }
        }
        public static readonly DependencyProperty EnabledLrcProperty =
            DependencyProperty.Register("EnabledLrc", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public bool EnabledLrcAnimation
        {
            get { return (bool)GetValue(EnabledLrcAnimationProperty); }
            set { SetValue(EnabledLrcAnimationProperty, value); }
        }
        public static readonly DependencyProperty EnabledLrcAnimationProperty =
            DependencyProperty.Register("EnabledLrcAnimation", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
        #endregion


        private void InitIcon()
        {
            this.notifyIcon = new NotifyIcon();

            this.notifyIcon.Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri(this.Icon.ToString())).Stream);
            this.notifyIcon.Text = "NeteaseMusicLrcHelper";
            this.notifyIcon.BalloonTipTitle = "NeteaseMusicLrcHelper";
            this.notifyIcon.BalloonTipText = "Hello World";
            this.notifyIcon.Visible = true;
            this.notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();

            #region Menu
            System.Windows.Forms.MenuItem item;

            //Enable/Disable Lrc
            item = new System.Windows.Forms.MenuItem("开启/关闭", new EventHandler((object obj, EventArgs e) =>
            {
                ((System.Windows.Forms.MenuItem)obj).Checked = !((System.Windows.Forms.MenuItem)obj).Checked;
                EnabledLrc = ((System.Windows.Forms.MenuItem)obj).Checked;
            }));
            item.Checked = EnabledLrc;
            this.notifyIcon.ContextMenu.MenuItems.Add(item);

            //Lyrics Animation
            item = new System.Windows.Forms.MenuItem("歌词跟随（Beta）", new EventHandler((object obj, EventArgs e) =>
            {
                ((System.Windows.Forms.MenuItem)obj).Checked = !((System.Windows.Forms.MenuItem)obj).Checked;
                EnabledLrcAnimation = ((System.Windows.Forms.MenuItem)obj).Checked;
            }));
            item.Checked = EnabledLrcAnimation;
            this.notifyIcon.ContextMenu.MenuItems.Add(item);

            //Exit
            this.notifyIcon.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("退出", new EventHandler((object obj, EventArgs e) =>
            {
                thd_ScrollSync.Abort();
                System.Windows.Application.Current.Shutdown();
            })));
            #endregion

            this.notifyIcon.ShowBalloonTip(5000);
        }

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

            //LrcSyncSync Thread
            thd_ScrollSync = new Thread(LrcSync);
            thd_ScrollSync.Start();
        }

        #region Lrc Animation

        private delegate void lrcAniDelegate(double startPercent, long end);
        private void lrcAniAction(double startPercent, long end)
        {
            //Debuging
            Storyboard sb = (Storyboard)Resources["ScrollAnimation"];
            DoubleAnimationUsingKeyFrames da = (DoubleAnimationUsingKeyFrames)sb.Children[0];
            EasingDoubleKeyFrame startFrame = (EasingDoubleKeyFrame)da.KeyFrames[0];
            EasingDoubleKeyFrame endFrame = (EasingDoubleKeyFrame)da.KeyFrames[1];

            startFrame.Value = startPercent;
            endFrame.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(end));
            sb.Begin();

        }
        #endregion

        //LRC Sync
        private void LrcSync()
        {
            while (true)
            {
                //唤醒进程(但是并没有效果)
                MemHelper.NtResumeProcess(NeteaseMusicProcess.Handle);
                //读最新歌词
                CurrentLRC = LrcHelper.Parse(ReadLRC());
                //定位当前歌词
                int now = ReadPlayTime() / 10000;   //Convert 100ns(tick) to 1ms
                for (int i = 0; i < CurrentLRC.LrcLines.Count; i++)
                {
                    if (now >= CurrentLRC.LrcLines[i].StartTime)
                    {
                        if (i == CurrentLRC.LrcLines.Count - 1) //容错 最后一条歌词i+1会越界
                        {
                            //设置滚动动画
                            this.Dispatcher.Invoke(new lrcAniDelegate(lrcAniAction),
                                (now - CurrentLRC.LrcLines[i].StartTime) / (ReadEndTime() - CurrentLRC.LrcLines[i].StartTime),
                                (long)(10000 * (ReadEndTime() - CurrentLRC.LrcLines[i].StartTime)));
                        }
                        else
                        {
                            if (now < CurrentLRC.LrcLines[i + 1].StartTime) //继续定位歌词
                            {
                                this.Dispatcher.Invoke(new lrcAniDelegate(lrcAniAction),
                                    (now - CurrentLRC.LrcLines[i].StartTime) / (CurrentLRC.LrcLines[i + 1].StartTime - CurrentLRC.LrcLines[i].StartTime),
                                    (long)(10000 * (CurrentLRC.LrcLines[i + 1].StartTime - CurrentLRC.LrcLines[i].StartTime)));
                            }
                            else
                            {
                                continue;
                            }
                        }

                        //设置歌词
                        lbl_back.Dispatcher.Invoke(new Action(() =>
                        {
                            //back
                            lbl_back.Content = CurrentLRC.LrcLines[i].Text;
                            //front
                            lbl_front.Content = CurrentLRC.LrcLines[i].Text;
                        }));
                        break;
                    }
                }
                Thread.Sleep(100);
#if DEBUG
                Debug.WriteLine("sleep" + now);
#endif
            }
        }



        #region ReadMem

        private string ReadLRC()
        {
            //准备指针偏移
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B13588);
            Offsets.Add(0x30);
            Offsets.Add(0xA0);
            Offsets.Add(0x8);
            Offsets.Add(0x198);
            Offsets.Add(0xC);
            return MemHelper.ReadString(NeteaseMusicProcess.Handle, NeteaseMusicDLL.BaseAddress.ToInt64(), Offsets);
        }

        private string ReadTitle()
        {
            //准备指针偏移
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B0B248);
            Offsets.Add(0xF0);
            Offsets.Add(0xC);
            return MemHelper.ReadString(NeteaseMusicProcess.Handle, NeteaseMusicDLL.BaseAddress.ToInt64(), Offsets);
        }

        private string ReadTLRC()
        {
            //准备指针偏移
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B13588);
            Offsets.Add(0x30);
            Offsets.Add(0xA0);
            Offsets.Add(0x8);
            Offsets.Add(0x1A0);
            Offsets.Add(0xC);
            return MemHelper.ReadString(NeteaseMusicProcess.Handle, NeteaseMusicDLL.BaseAddress.ToInt64(), Offsets);
        }
        private int ReadPlayTime()
        {
            //准备指针偏移
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B0B248);
            Offsets.Add(0x28);
            return MemHelper.ReadInt(NeteaseMusicProcess.Handle, NeteaseMusicDLL.BaseAddress.ToInt64(), Offsets);
        }

        private double ReadEndTime()
        {
            //准备指针偏移
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B0B838);
            Offsets.Add(0x158);
            Offsets.Add(0x8);
            Offsets.Add(0x0);
            Offsets.Add(0x108);
            Offsets.Add(0x28);
            return MemHelper.ReadDouble(NeteaseMusicProcess.Handle, NeteaseMusicDLL.BaseAddress.ToInt64(), Offsets);
        }
        #endregion


    }
}
