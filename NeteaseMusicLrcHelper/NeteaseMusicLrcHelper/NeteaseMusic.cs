using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using static NeteaseMusicLrcHelper.LrcHelper;

namespace NeteaseMusicLrcHelper
{
    class NeteaseMusic : IDisposable
    {
        public Process TargetProcess { get; }
        public Process PlaybackProcess { get; }
        public Int64 PlaybackProcess_hTHREADSTACK0 { get; }

        private ProcessModule pmod_SharedLibrary_dll;
        private ProcessModule pmod_NeteaseMusic_dll;
        private Task watcher;

        public NeteaseMusic()
        {
            try
            {
                //Open Process NeteaseMusic.exe
                this.TargetProcess = Process.GetProcessesByName("NeteaseMusic")[0];
                //Open Process Windows.Media.BackgroundPlayback.exe
                this.PlaybackProcess = Process.GetProcessesByName("Windows.Media.BackgroundPlayback")[0];
            }
            catch (Exception)
            {
                this.Dead = true;
                return;
            }

            //Find Module
            foreach (ProcessModule module in this.TargetProcess.Modules)
            {
                if (module.ModuleName == "SharedLibrary.dll") { pmod_SharedLibrary_dll = module; }
                if (module.ModuleName == "NeteaseMusic.dll") { pmod_NeteaseMusic_dll = module; }
            }


            //Get THREADSTACK0
            this.PlaybackProcess_hTHREADSTACK0 = MemHelper.GetThreadStack0(PlaybackProcess);

            //Watch it
            watcher = new Task(this.watch);
            watcher.Start();
        }



        /// <summary>
        /// 监视进程状态 更新维护变量
        /// </summary>
        private void watch()
        {
            Int64 currentsong = 0;
            while (!TargetProcess.HasExited && !Dead)
            {
                try
                {
                    //更新变量
                    currentsong = GetSongID();
                    if (this.SongID != currentsong)
                    {
                        this.SongID = currentsong;
                    }
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }
                Thread.Sleep(1000);
            }
            this.Dead = true;
            Dispose();
        }

        public void Stop()
        {
            this.Dead = true;
            watcher.Wait();

        }

        public void Dispose()
        {
            this.TargetProcess.Dispose();
            this.PlaybackProcess.Dispose();
            pmod_NeteaseMusic_dll.Dispose();
            pmod_SharedLibrary_dll.Dispose();
            try
            {
                MemHelper.CloseHandle((IntPtr)this.PlaybackProcess_hTHREADSTACK0);
                watcher.Dispose();
            }
            catch (Exception)
            {
                //throw;
            }
        }

        #region Properites
        public bool Dead { get; private set; } = false;

        /// <summary>
        /// 当前歌曲id
        /// </summary>
        public Int64 SongID { get; private set; }

        /// <summary>
        /// 当前时间
        /// </summary>
        public Int64 CurrentTime
        {
            get
            {
                List<Int64> Offsets = new List<Int64>();
                Offsets.Add(-0x00000960);
                Offsets.Add(0x358);
                Offsets.Add(0x938);
                Offsets.Add(0x38);
                Offsets.Add(0x4C8);
                Offsets.Add(0xC0);
                Offsets.Add(0x8);
                Offsets.Add(0x10);
                Offsets.Add(0x8);
                Offsets.Add(0x13A8);
                return MemHelper.ReadInt64(this.PlaybackProcess.Handle, this.PlaybackProcess_hTHREADSTACK0, Offsets);
            }
        }


        /// <summary>
        /// 歌曲结束时间
        /// </summary>
        public Int64 EndTime
        {
            get
            {
                List<Int64> Offsets = new List<Int64>();
                Offsets.Add(0x002BAE48);
                Offsets.Add(0x8);
                Offsets.Add(0x10);
                Offsets.Add(0x50);
                Offsets.Add(0x0);
                Offsets.Add(0x30);
                return MemHelper.ReadInt64(this.TargetProcess.Handle, this.pmod_SharedLibrary_dll.BaseAddress.ToInt64(), Offsets);
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 通过API获取歌词
        /// </summary>
        /// <param name="songID">歌曲id</param>
        /// <returns>统一歌词结构</returns>
        public static Lrcs GetLrc_API(Int64 songID)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp("https://music.163.com/api/song/lyric?id=" + songID + "&lv=-1&kv=-1&tv=-1");
            req.Method = "GET";
            using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
            {
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader r = new StreamReader(res.GetResponseStream());
                    string raw = r.ReadToEnd();

                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    dynamic json = jss.Deserialize<dynamic>(raw);

                    Lrcs lrcs = new Lrcs();

                    try
                    {
                        lrcs.lrc = json["lrc"]["lyric"];
                        lrcs.tlrc = json["tlyric"]["lyric"];
                    }
                    catch (KeyNotFoundException)
                    {
                        lrcs.nolyric = true;
                    }

                    if ((lrcs.lrc == null || lrcs.lrc == "") &&
                        (lrcs.tlrc == null || lrcs.tlrc == ""))
                    {
                        lrcs.nolyric = true;
                    }
#if DEBUG
                    Console.WriteLine("http Conetent: " + raw);
#endif
                    r.Dispose();
                    return lrcs;

                }
                else
                {
#if DEBUG
                    Console.Error.WriteLine("lrc http Code: " + res.StatusCode);
                    Console.Error.WriteLine("Content: ");
                    res.GetResponseStream().CopyTo(Console.OpenStandardError());
#endif
                    throw new Exception("lrc http Code: " + res.StatusCode);
                }
            }
        }

        /// <summary>
        /// 获取当前歌曲歌词
        /// </summary>
        /// <returns>统一歌词结构</returns>
        public LrcHelper.Lrcs GetCurrentLrc()
        {
            return GetLrc_API(this.SongID);
        }

        public Int64 GetSongID()
        {
            List<Int64> Offsets = new List<Int64>();
            Offsets.Add(0x00B235E8);
            Offsets.Add(0x28);
            Offsets.Add(0x40);
            return MemHelper.ReadInt64(this.TargetProcess.Handle, this.pmod_NeteaseMusic_dll.BaseAddress.ToInt64(), Offsets);
        }

        #endregion
    }
}
