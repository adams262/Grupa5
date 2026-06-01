using Xunit;
using PZPP_Grupa5.Services;
using PZPP_Grupa5.ViewModels;
using YoutubeExplode;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Moq;
using Microsoft.Maui.Storage;
using System.IO;

namespace TestProject4
{
    // Oznaczamy klasę jako integracyjną, aby łatwo było ją filtrować
    [Trait("Category", "Integration")]
    public class FullIntegrationTests
    {
        [Fact]
        public async Task FullIntegration_ProcessVideo_PobieraZYoutubeIAnalizujeWGemini()
        {
            // Arrange
            
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var apiKey = config["GeminiSettings:ApiKey"];

            // Tworzymy prawdziwe instancje serwisów
            var youtubeClient = new YoutubeClient();
            var ytService = new YouTubeService(youtubeClient);

            var mockPrefs = new Mock<IPreferences>();
            mockPrefs.Setup(p => p.Get("GeminiApiKey", It.IsAny<string>(), null)).Returns(apiKey);

            var geminiService = new GeminiService(new HttpClient(), mockPrefs.Object);

            // Inicjalizujemy ViewModel prawdziwymi serwisami
            var viewModel = new MainViewModel(ytService, geminiService);

            // Ustawiamy dane wejściowe
            viewModel.VideoUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
            viewModel.ChceStreszczenie = true;

            // Act 
            await viewModel.ProcessVideoCommand.ExecuteAsync(null);

            // Assert
            
            Assert.False(viewModel.IsLoading);
            Assert.True(viewModel.IsResultVisible);
            Assert.NotNull(viewModel.TekstWynikowy);
            Assert.NotEmpty(viewModel.TekstWynikowy);
        }
    }
}