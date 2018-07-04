using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NeteaseMusicLrcHelper
{
    public static class LrcHelper
    {
        public struct Lrcs
        {
            public bool nolyric;
            public string lrc;
            public string tlrc;
        }

        //Parse
        public static LRC Parse(string strlrc)
        {
            LRC lrc = new LRC();
            List<LrcLine> lines = new List<LrcLine>();
            lrc.LrcLines = lines;
            //Split lines
            string[] strlines = strlrc.Split(new char[] { '\r', '\n' });
            //分析标签
            foreach (string line in strlines)
            {
                //查找标签
                int strstart = line.IndexOf('[');
                int strend = line.IndexOf(']');
                if (strstart != -1 && strend != -1)  //容错 标签缺失
                {
                    string label = line.Substring(strstart + 1, strend - strstart - 1);
                    string[] splitLabel = label.Split(':');
                    switch (splitLabel[0])
                    {
                        case "ar":  //艺人名
                            lrc.ar = splitLabel[1];
                            break;
                        case "ti":  //曲名
                            lrc.ti = splitLabel[1];
                            break;
                        case "al":  //专辑名
                            lrc.al = splitLabel[1];
                            break;
                        case "by":  //LRC编者
                            lrc.by = splitLabel[1];
                            break;
                        case "offset":  //补偿时值
                            lrc.offset = splitLabel[1];
                            break;
                        default:
                            if (Regex.IsMatch(splitLabel[0], @"^[+-]?\d*[.]?\d*$")) //判断是不是数字
                            {
                                //!!迭句识别没做
                                //当作时间处理 计算成毫秒
                                Int64 starttime = (Int64)(Convert.ToInt32(splitLabel[0]) * 60 + Convert.ToDouble(splitLabel[1])) * 1000;
                                lines.Add(new LrcLine() {StartTime = starttime, Text = line.Substring(strend + 1,line.Length - strend -1)});
                            }
                            else
                            {
#if DEBUG
                                throw new Exception();
#endif
                            }
                            break;
                    }
                }
            }
            lines.Sort(new SortByStartTime());
            //处理添加EndTime Duration
            for (int i = 0; i < lines.Count; i++)
            {
                LrcLine l = lines[i];
                if (i+1<lines.Count)
                {
                    l.EndTime = lines[i + 1].StartTime;
                    l.Duration = l.EndTime - l.StartTime;
                }
                else
                {
                    l.EndTime = -1; //到歌曲结束
                    l.Duration = -1;
                }
                lines[i] = l;
            }
            return lrc;
        }
        //LrcLine
        public struct LrcLine
        {
            public Int64 StartTime;
            public Int64 EndTime;
            public Int64 Duration;
            public string Text;
        }

        //LRC
        public struct LRC
        {
            public List<LrcLine> LrcLines;
            public string ar;
            public string ti;
            public string al;
            public string by;
            public string offset;
        }

        //LRC Comparer
        public class SortByStartTime : IComparer<LrcLine>
        {
            int IComparer<LrcLine>.Compare(LrcLine x, LrcLine y)
            {
                return x.StartTime.CompareTo(y.StartTime);
            }
        }

        /// <summary>
        /// 获取当前时间歌词的index
        /// </summary>
        /// <param name="lrc"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static int GetNowIndex(LRC lrc,Int64 time)
        {
            for (int i = 0; i < lrc.LrcLines.Count; i++)
            {
                if (time >= lrc.LrcLines[i].StartTime &&
                    time < lrc.LrcLines[i].EndTime)
                {
                    return i;
                }
            }
            return 0;
        }

    }
}
