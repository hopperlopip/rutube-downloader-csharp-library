using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RutubeDownloader
{
    public class Downloader
    {
        public delegate void DownloadHandler(int progressPercent);
        public event DownloadHandler? OnProgressChanged;
        readonly HttpClient httpClient = new HttpClient();
        readonly string ffmpegPath = string.Empty;
        readonly bool customName = false;
        readonly string customVideoName = string.Empty;
        string title = string.Empty;
        string videoName = string.Empty;
        string ID
        {
            get
            {
                return GetVideoID(rutubeVideoLink.ToString());
            }
        }

        readonly Uri rutubeVideoLink;

        public Downloader(Uri rutubeVideoLink, string ffmpegPath, string videoName) : this(rutubeVideoLink, ffmpegPath)
        {
            customName = true;
            customVideoName = videoName;
        }

        public Downloader(string rutubeVideoLink, string ffmpegPath, string videoName) : this(rutubeVideoLink, ffmpegPath)
        {
            customName = true;
            customVideoName = videoName;
        }

        public Downloader(Uri rutubeVideoLink, string ffmpegPath)
        {
            if (!IsValidLink(rutubeVideoLink))
            {
                throw new RutubeInvalidLinkException();
            }
            this.rutubeVideoLink = rutubeVideoLink;
            this.ffmpegPath = ffmpegPath;
        }

        public Downloader(string rutubeVideoLink, string ffmpegPath)
        {
            if (string.IsNullOrEmpty(rutubeVideoLink))
            {
                throw new RutubeInvalidLinkException();
            }
            if (!IsValidLink(rutubeVideoLink))
            {
                throw new RutubeInvalidLinkException();
            }
            this.rutubeVideoLink = new Uri(rutubeVideoLink);
            this.ffmpegPath = ffmpegPath;
        }

        public async Task<PlayOptions> GetPlayOptions()
        {
            string jsonString = await httpClient.GetStringAsync($"https://rutube.ru/api/play/options/{ID}/?format=json");
            PlayOptions? jsonPlayOptions = JsonConvert.DeserializeObject<PlayOptions>(jsonString);
            if (jsonPlayOptions == null)
            {
                throw new Exception("Couldn't deserialize json string.");
            }
            return jsonPlayOptions;
        }

        private static string GetVideoID(string videoLink)
        {
            var match = Regex.Match(videoLink, @"video/([^/]*)");
            return match.Groups[1].Value;
        }

        public async Task<Uri> GetM3U8Link(PlayOptions playOptions)
        {
            title = playOptions.title;
            string m3u8List = await httpClient.GetStringAsync(playOptions.video_balancer.defaultLink);
            var matches = Regex.Matches(m3u8List, @"https://.*?\.m3u8");
            string m3u8Link = matches[matches.Count - 1].Groups[0].Value;
            return new Uri(m3u8Link);
        }

        public async Task DownloadVideo(string path, CancellationToken cancellationToken = default)
        {
            Uri m3u8Link = await GetM3U8Link(await GetPlayOptions());
            M3U8 m3u8 = new M3U8(m3u8Link);
            string[] tsSegments = await m3u8.GetSegmentsURL();
            MemoryStream memoryStream = new MemoryStream();
            videoName = GetVideoName();
            for (int i = 0; i < tsSegments.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    OnProgressChanged?.Invoke(0);
                    return;
                }
                string tsSegment = tsSegments[i];
                var response = await httpClient.GetAsync(tsSegment, HttpCompletionOption.ResponseHeadersRead);
                var content = await response.Content.ReadAsStreamAsync();
                await content.CopyToAsync(memoryStream);
                OnProgressChanged?.Invoke((int)((i + 1) / (float)tsSegments.Length * 100));
            }
            byte[] tsFile = memoryStream.ToArray();
            string tsFileName = $"{videoName}.ts";
            string tsPath = Path.Combine(path, tsFileName);
            await File.WriteAllBytesAsync(tsPath, tsFile);
            await ConvertTsToMp4(tsPath, path, ffmpegPath);
            File.Delete(tsPath);
        }

        private string GetVideoName()
        {
            if (!customName)
            {
                return title;
            }
            else
            {
                return customVideoName;
            }
        }

        private bool IsValidLink(Uri rutubeVideoLink)
        {
            return IsValidLink(rutubeVideoLink.ToString());
        }

        private bool IsValidLink(string rutubeVideoLink)
        {
            if (!Regex.IsMatch(rutubeVideoLink, @"^https://rutube.ru/video/[^/]*/?$"))
            {
                return false;
            }
            return true;
        }

        private async Task ConvertTsToMp4(string tsPath, string outputPath, string ffmpegPath)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = ffmpegPath;
            string mp4FileName = $"{videoName}.mp4";
            ffmpeg.StartInfo.Arguments = $"-i \"{tsPath}\" -c copy \"{Path.Combine(outputPath, mp4FileName)}\"";
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.Start();
            await ffmpeg.WaitForExitAsync();
        }
    }
}