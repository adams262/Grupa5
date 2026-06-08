using PZPP_Grupa5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace PZPP_Grupa5.Services
{
    public class YouTubeService : IYouTubeService
    {
        private readonly YoutubeClient _youtube;
        public YouTubeService(YoutubeClient youtube)
        {
            _youtube = youtube;
        }


        public async Task<YouTubeDependency> GetYouTubeAsync(string videoUrl)
        {   
            // Walidacja URL
            VideoId videoId;
            try
            {
                videoId = VideoId.Parse(videoUrl);
            }
            catch
            {
                throw new InvalidYoutubeUrlException();
            }

            // Pobieranie manifestu napisow
            var trackManifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(videoId);
            var trackInfo = trackManifest.TryGetByLanguage("pl") ?? trackManifest.Tracks.FirstOrDefault();

            // transkrybcja
            if (trackInfo != null)
            {
                var track = await _youtube.Videos.ClosedCaptions.GetAsync(trackInfo);
                var pelnyTekst = string.Join(" ", track.Captions.Select(c => c.Text));

                return new YouTubeDependency { Tekst = pelnyTekst, CzyTylkoAudio = false };
            }

            // Pobieranie pliku audio
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            // zapisywanie pliku audio w temp
            var filePath = Path.Combine(FileSystem.CacheDirectory, $"{videoId}.mp4");

            await _youtube.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);

            return new YouTubeDependency { SciezkaAudio = filePath, CzyTylkoAudio = true };
        }
    }
}
