using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;
using Moq;
using PZPP_Grupa5.Models;
using PZPP_Grupa5.Services;
using System.IO;
using Xunit;

namespace TestProject4
{
    public class GeminiIntegrationTests
    {
        private readonly string _apiKey;

        public GeminiIntegrationTests()
        {
            
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _apiKey = config["GeminiSettings:ApiKey"];
        }

        [Fact]
        public async Task GetGeminiAsync_IntegracjaZGeminiAPI_ZwracaPoprawnaOdpowiedz()
        {
            // Arrange
            var mockPrefs = new Mock<IPreferences>();
            mockPrefs.Setup(p => p.Get("GeminiApiKey", It.IsAny<string>(), null)).Returns(_apiKey);

            var httpClient = new HttpClient();
            var service = new GeminiService(httpClient, mockPrefs.Object);

            var dane = new YouTubeDependency
            {
                Tekst = "Opowiedz krótko o zaletach programowania w C#.",
                CzyTylkoAudio = false
            };

            // Act
            var wynik = await service.GetGeminiAsync(dane, true, false, false);

            // Assert
            Assert.NotNull(wynik);
            Assert.Contains("C#", wynik);
        }
    }
}