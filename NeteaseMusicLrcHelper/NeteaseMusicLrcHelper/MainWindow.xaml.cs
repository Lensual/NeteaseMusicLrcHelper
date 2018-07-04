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
using static NeteaseMusicLrcHelper.MemHelper;
using static NeteaseMusicLrcHelper.LrcHelper;

namespace NeteaseMusicLrcHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NeteaseMusic neteaseMusic;
        //private Lrcs currentLrcs;

        public MainWindow()
        {
            InitializeComponent();
            InitIcon();

            neteaseMusic = new NeteaseMusic();
            thd_ScrollSync = new Thread(ScrollSync);
            thd_ScrollSync.Start();

            //暂时用不上
            //SettingsWindow settingsWnd = new SettingsWindow();
            //settingsWnd.DataContext = this;
            //settingsWnd.Show();
        }

        private NotifyIcon notifyIcon;
        private Thread thd_ScrollSync;
        private bool exitRequest = false;

        private void ScrollSync()
        {
            Int64 recentSongID = 0;
            Lrcs currentLrcs = new Lrcs();
            LRC lyric = new LRC();
            LrcLine recentLine = new LrcLine();
            while (!this.exitRequest)
            {
                while (!this.exitRequest && neteaseMusic.Dead)
                {
                    ShowLyric("等待网易云音乐UWP");
                    neteaseMusic = new NeteaseMusic();
                    Thread.Sleep(3000);
                }
                try
                {
                    #region 处理过程
                    if (neteaseMusic.SongID != recentSongID)
                    {
                        recentSongID = neteaseMusic.SongID;
                        ShowLyric("歌词下载中");
                        //读最新歌词
                        currentLrcs = neteaseMusic.GetCurrentLrc();
                        if (!currentLrcs.nolyric || currentLrcs.lrc == "")
                            lyric = LrcHelper.Parse(currentLrcs.lrc);
                    }
                    if (currentLrcs.nolyric || currentLrcs.lrc == null || currentLrcs.lrc == "")
                    {
                        ShowLyric("纯音乐 无歌词");
                        Thread.Sleep(1000);
                        continue;
                    }
                    //定位当前歌词
                    Int64 now = neteaseMusic.CurrentTime / 10000;   //Convert 100ns(tick) to 1ms
                    LrcLine line = lyric.LrcLines[LrcHelper.GetNowIndex(lyric, now)];   //偏移未处理
                    if (!line.Equals(recentLine))
                    {
                        //歌词换行
                        recentLine = line;
                        //计算动画
                        if ((bool)this.Dispatcher.Invoke(new GetEnabledLrcAnimationDelegate(GetEnabledLrcAnimation)))
                        {
                            Int64 aniDuration;
                            Double percent;
                            if (line.EndTime == -1)
                            {
                                //最后一句歌词
                                aniDuration = neteaseMusic.EndTime / 10000 - now;
                                percent = (Double)(now - line.StartTime) / (neteaseMusic.EndTime - line.StartTime);
                            }
                            else
                            {
                                aniDuration = line.EndTime - now;
                                percent = (Double)(now - line.StartTime) / line.Duration;
                            }
                            if (aniDuration > 0)
                            {
                                this.Dispatcher.Invoke(new lrcAniDelegate(lrcAniAction), percent, aniDuration);
                            }
                        }
                        //设置歌词
                        ShowLyric(line.Text);
                    }
                    Thread.Sleep(200);
                    #endregion
                }
                catch (Exception)
                {
                    if (!neteaseMusic.Dead)
                        throw;
                }
            }
        }


        private void ShowLyric(string text)
        {
            lbl_back.Dispatcher.Invoke(new Action(() =>
            {
                //back
                lbl_back.Content = text;
                //front
                lbl_front.Content = text;
            }));
        }

        #region DependProperty
        /// <summary>
        /// 启用歌词
        /// </summary>
        public bool EnabledLrc
        {
            get { return (bool)GetValue(EnabledLrcProperty); }
            set { SetValue(EnabledLrcProperty, value); }
        }
        public static readonly DependencyProperty EnabledLrcProperty =
            DependencyProperty.Register("EnabledLrc", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        /// <summary>
        /// 启用歌词滚动
        /// </summary>
        public bool EnabledLrcAnimation
        {
            get { return (bool)GetValue(EnabledLrcAnimationProperty); }
            set { SetValue(EnabledLrcAnimationProperty, value); }
        }
        public static readonly DependencyProperty EnabledLrcAnimationProperty =
            DependencyProperty.Register("EnabledLrcAnimation", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        //public bool EnabledWakeupProcess
        //{
        //    get { return (bool)GetValue(EnabledWakeupProcessProperty); }
        //    set { SetValue(EnabledWakeupProcessProperty, value); }
        //}
        //public static readonly DependencyProperty EnabledWakeupProcessProperty =
        //    DependencyProperty.Register("EnabledWakeupProcess", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


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
                notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            })));
            #endregion

            this.notifyIcon.ShowBalloonTip(5000);
        }

        #region Lrc Animation

        private delegate void lrcAniDelegate(double startPercent, long end);
        /// <summary>
        /// 立刻设置并激活歌词滚动
        /// </summary>
        /// <param name="startPercent">起始百分比</param>
        /// <param name="duration">动画时长 ms</param>
        private void lrcAniAction(double startPercent, long duration)
        {
            //Debuging
            Storyboard sb = (Storyboard)Resources["ScrollAnimation"];
            DoubleAnimationUsingKeyFrames da = (DoubleAnimationUsingKeyFrames)sb.Children[0];
            EasingDoubleKeyFrame startFrame = (EasingDoubleKeyFrame)da.KeyFrames[0];
            EasingDoubleKeyFrame endFrame = (EasingDoubleKeyFrame)da.KeyFrames[1];

            startFrame.Value = startPercent;
            endFrame.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(duration * 10000));
            sb.Begin();

        }

        private delegate bool GetEnabledLrcAnimationDelegate();
        private bool GetEnabledLrcAnimation()
        {
            return EnabledLrcAnimation;
        }
        #endregion


        /// <summary>
        /// 歌词拖动
        /// </summary>
        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Cursor = System.Windows.Input.Cursors.SizeAll;
                this.DragMove();
                this.Cursor = System.Windows.Input.Cursors.AppStarting;
            }
        }
    }
}
