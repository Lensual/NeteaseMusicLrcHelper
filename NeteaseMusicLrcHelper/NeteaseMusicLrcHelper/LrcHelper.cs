using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeteaseMusicLrcHelper
{
    public static class LrcHelper
    {
        public static void Parse(string strlrc)
        {
            //Split lines
            string[] lines = strlrc.Split(new char[] { '\r', '\n' });
            //分析标签
            //List<Double> lrc = new List<Double>();
            List<LRC> lrc = new List<LRC>();
            foreach (string line in lines)
            {
                //查找标签
                int strstart = line.IndexOf('[');
                int strend = line.IndexOf(']');
                if (strstart != -1 && strend != -1)  //容错 标签缺失
                {
                    string strtime = line.Substring(strstart + 1, strend - strstart - 1);
                    //计算成毫秒
                    string[] splitstr = strtime.Split(':');
                    Double ms = (Convert.ToInt32(splitstr[0]) * 60 + Convert.ToDouble(splitstr[1])) * 1000;
                    lrc.Add(new LRC() {StartTime = ms ,Text = line.Substring(strend + 1,line.Length - strend -1)});
                }
            }
            lrc.Sort();


            //Sort
        }

        public struct LRC:IComparable<LRC>
        {
            public Double StartTime;
            public string Text;
            int IComparable<LRC>.CompareTo(LRC other)
            {
                
                //throw new NotImplementedException();
            }
        }
    }
}
