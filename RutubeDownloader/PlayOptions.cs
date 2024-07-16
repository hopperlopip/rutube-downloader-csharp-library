using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutubeDownloader
{
    public class PlayOptions
    {
        public string title = string.Empty;
        public VideoBalancer video_balancer = new();
    }

    public class VideoBalancer
    {
        [JsonProperty("default")]
        public string defaultLink = string.Empty;
        public string m3u8 = string.Empty;
    }
}
