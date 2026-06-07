using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Maui.Storage;
using PZPP_Grupa5.Services;
using PZPP_Grupa5.Models;

namespace TestProject4
{
    public class GeminiServiceTests
    {
        [Fact]
        public async Task GetGeminiAsync_GdyBrakKluczaApi_RzucaApiKeyException()
        {
            // Arrange
            var mockPreferences = new Mock<IPreferences>();
            // Udajemy, że w ustawieniach klucz jest pusty
            mockPreferences.Setup(p => p.Get("GeminiApiKey", It.IsAny<string>(), null)).Returns("");

            var service = new GeminiService(preferences: mockPreferences.Object);
            var dane = new YouTubeDependency { Tekst = "Jakis tekst", CzyTylkoAudio = false };

            // Act & Assert
            await Assert.ThrowsAsync<ApiKeyException>(() =>
                service.GetGeminiAsync(dane, true, false, false));
        }

        [Fact]
        public async Task GetGeminiAsync_GdySerwerZwracaBlad429_RzucaQuotaExceededException()
        {
            // Arrange
            var mockPreferences = new Mock<IPreferences>();
            mockPreferences.Setup(p => p.Get("GeminiApiKey", It.IsAny<string>(), null)).Returns("JAKIS_KLUCZ");

            // Udajemy błąd 429 od Google
            var mockHttp = new MockHttpMessageHandler(
                "{\"error\": {\"message\": \"Quota exceeded\"}}",
                HttpStatusCode.TooManyRequests);
            var mockHttpClient = new HttpClient(mockHttp);

            var service = new GeminiService(mockHttpClient, mockPreferences.Object);
            var dane = new YouTubeDependency { Tekst = "Tekst", CzyTylkoAudio = false };

            // Act & Assert
            await Assert.ThrowsAsync<QuotaExceededException>(() =>
                service.GetGeminiAsync(dane, true, false, false));
        }

        [Fact]
        public async Task GetGeminiAsync_PoprawnaOdpowiedz_ZwracaPrzetworzonyTekst()
        {
            // Arrange
            var mockPreferences = new Mock<IPreferences>();
            mockPreferences.Setup(p => p.Get("GeminiApiKey", It.IsAny<string>(), null)).Returns("JAKIS_KLUCZ");

            // Udajemy poprawny JSON, jaki normalnie zwraca Gemini
            string fakeJson = @"
            {
                ""candidates"": [
                    {
                        ""content"": {
                            ""parts"": [ { ""text"": ""Oczekiwane podsumowanie wideo."" } ]
                        }
                    }
                ]
            }";

            var mockHttp = new MockHttpMessageHandler(fakeJson, HttpStatusCode.OK);
            var mockHttpClient = new HttpClient(mockHttp);

            var service = new GeminiService(mockHttpClient, mockPreferences.Object);
            var dane = new YouTubeDependency { Tekst = "Test", CzyTylkoAudio = false };

            // Act
            var wynik = await service.GetGeminiAsync(dane, true, false, false);

            // Assert
            Assert.Equal("Oczekiwane podsumowanie wideo.", wynik);
        }

        [Fact]
        public async Task GetGeminiAsync_GdySerwerZwracaBlad503_RzucaServerOverloadedException()
        {
            // Arrange
            var mockPreferences = new Mock<IPreferences>();
            mockPreferences.Setup(p => p.Get("GeminiApiKey", It.IsAny<string>(), null)).Returns("JAKIS_KLUCZ");

            // Udajemy błąd 503 Service Unavailable od Google
            var mockHttp = new MockHttpMessageHandler(
                "{\"error\": {\"message\": \"Service Unavailable\"}}",
                HttpStatusCode.ServiceUnavailable);
            var mockHttpClient = new HttpClient(mockHttp);

            var service = new GeminiService(mockHttpClient, mockPreferences.Object);
            var dane = new YouTubeDependency { Tekst = "Tekst", CzyTylkoAudio = false };

            // Act & Assert
            await Assert.ThrowsAsync<ServerOverloadedException>(() =>
                service.GetGeminiAsync(dane, true, false, false));
        }
    }
}