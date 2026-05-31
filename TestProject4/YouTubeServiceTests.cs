using System.Threading.Tasks;
using Xunit;
using YoutubeExplode;
using PZPP_Grupa5.Services;
using PZPP_Grupa5.Models;

namespace TestProject4
{
    public class YouTubeServiceTests
    {
        [Fact]
        public async Task GetYouTubeAsync_KiedyUrlJestNiepoprawny_RzucaInvalidYoutubeUrlException()
        {
            // Arrange (Przygotowanie)
            var youtubeClient = new YoutubeClient();
            var youtubeService = new YouTubeService(youtubeClient);
            var zlyUrl = "to_nie_jest_link_do_youtube";

            // Act & Assert (Działanie i Sprawdzenie)
            // Upewniamy się, że serwis wyrzuci dokładnie ten błąd, który zaprogramowałeś
            await Assert.ThrowsAsync<InvalidYoutubeUrlException>(() =>
                youtubeService.GetYouTubeAsync(zlyUrl));
        }
    }
}