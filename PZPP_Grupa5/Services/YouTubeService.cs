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
            var videoId = YoutubeExplode.Videos.VideoId.Parse(videoUrl);

            // Pobieranie manifestu napisow
            var trackManifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(videoId);
            var trackInfo = trackManifest.TryGetByLanguage("pl") ?? trackManifest.Tracks.FirstOrDefault();

            // Pobieramy napisy i zwracamy sam tekst, jesli sa dostepne
            if (trackInfo != null)
            {
                var track = await _youtube.Videos.ClosedCaptions.GetAsync(trackInfo);
                var pelnyTekst = string.Join(" ", track.Captions.Select(c => c.Text));

                return new YouTubeDependency { Tekst = pelnyTekst, CzyTylkoAudio = false };
            }

            // Pobieramy plik audio
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            // Pobieramy i zapisujemy plik audio w katalogu tymczasowym
            var filePath = Path.Combine(FileSystem.CacheDirectory, $"{videoId}.mp4");

            await _youtube.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);

            return new YouTubeDependency { SciezkaAudio = filePath, CzyTylkoAudio = true };
        }
    }
}
