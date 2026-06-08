using Xunit;
using PZPP_Grupa5.Services;
using YoutubeExplode;

namespace TestProject4
{
    public class YouTubeIntegrationTests
    {
        [Fact]
        public async Task GetYouTubeAsync_PrawdziwyLink_PobieraMetadaneZYouTube()
        {
            // Arrange
            var youtubeClient = new YoutubeClient();
            var service = new YouTubeService(youtubeClient);
            var realUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw"; // Krótkie wideo testowe

            // Act
            var result = await service.GetYouTubeAsync(realUrl);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Tekst)); // Sprawdzamy czy transkrypcja się pobrała
        }

        [Fact]
        public async Task GetYouTubeAsync_NieistniejacyFilm_RzucaWyjatek()
        {
            // Arrange
            var youtubeClient = new YoutubeClient();
            var service = new YouTubeService(youtubeClient);
            var badUrl = "https://www.youtube.com/watch?v=FILM_KTORY_NIE_ISTNIEJE_123";

            // Act & Assert
            
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => service.GetYouTubeAsync(badUrl));

            // Assert
            Assert.NotNull(exception);
        }
    }
}