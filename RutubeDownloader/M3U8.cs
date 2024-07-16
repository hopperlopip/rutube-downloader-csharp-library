using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RutubeDownloader
{
    internal class M3U8
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly Uri link;

        public M3U8(string link) : this(new Uri(link)) { }

        public M3U8(Uri link)
        {
            this.link = link;
        }

        public async Task<string[]> GetSegmentsURL()
        {
            string m3u8 = await httpClient.GetStringAsync(link);
            string[] m3u8Lines = m3u8.Split('\n');
            List<string> tsLines = new();
            foreach (string m3u8Line in m3u8Lines)
            {
                if (!m3u8Line.StartsWith('#') && m3u8Line.EndsWith(".ts"))
                {
                    string tsFile = Path.GetFileName(m3u8Line);
                    string mp4Link = Regex.Replace(link.ToString(), @"\.m3u8.*", string.Empty);
                    string tsLink = $"{mp4Link}/{tsFile}";
                    tsLines.Add(tsLink);
                }
            }
            return tsLines.ToArray();
        }
    }
}
