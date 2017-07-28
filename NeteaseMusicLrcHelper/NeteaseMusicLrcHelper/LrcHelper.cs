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
        //Parse
        public static LRC Parse(string strlrc)
        {
            //Split lines
            string[] strlines = strlrc.Split(new char[] { '\r', '\n' });
            //分析标签
            List<LrcLine> lines = new List<LrcLine>();
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
                            break;
                        case "ti":  //曲名
                            break;
                        case "al":  //专辑名
                            break;
                        case "by":  //LRC编者
                            break;
                        case "offset":  //补偿时值
                            break;
                        default:
                            if (Regex.IsMatch(splitLabel[0], @"^[+-]?\d*[.]?\d*$")) //判断是不是数字 抄的 不懂正则
                            {
                                //!!迭句识别没做
                                //当作时间处理 计算成毫秒
                                Double ms = (Convert.ToInt32(splitLabel[0]) * 60 + Convert.ToDouble(splitLabel[1])) * 1000;  //!!浮点误差BUG
                                lines.Add(new LrcLine() {StartTime = ms ,Text = line.Substring(strend + 1,line.Length - strend -1)});
                            }
                            else
                            {
                                //!!意外的
                            }
                            break;
                    }
                }
            }
            lines.Sort(new SortByStartTime());
            return new LRC() { LrcLines = lines};

        }
        //LrcLine
        public struct LrcLine
        {
            public Double StartTime;
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
    }
}
