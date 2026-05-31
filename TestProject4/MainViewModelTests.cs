using System.Threading.Tasks;
using Xunit;
using Moq;
using PZPP_Grupa5.ViewModels;
using PZPP_Grupa5.Services;
using PZPP_Grupa5.Models;

namespace TestProject4
{
    public class MainViewModelTests
    {
        [Fact]
        public void CanProcess_KiedyBrakUrl_ZwracaFalse()
        {
            // Arrange
            var mockYoutube = new Mock<IYouTubeService>();
            var mockGemini = new Mock<IGeminiService>();
            var viewModel = new MainViewModel(mockYoutube.Object, mockGemini.Object);

            viewModel.VideoUrl = ""; // Pusty URL
            viewModel.ChceStreszczenie = true; // Zaznaczona opcja

            // Act
            bool mozeUruchomic = viewModel.ProcessVideoCommand.CanExecute(null);

            // Assert
            Assert.False(mozeUruchomic);
        }

        [Fact]
        public void CanProcess_KiedyJestUrlIJednaOpcja_ZwracaTrue()
        {
            // Arrange
            var mockYoutube = new Mock<IYouTubeService>();
            var mockGemini = new Mock<IGeminiService>();
            var viewModel = new MainViewModel(mockYoutube.Object, mockGemini.Object);

            viewModel.VideoUrl = "https://youtube.com/watch?v=123";
            viewModel.ChceWniosek = true; // Zaznaczona przynajmniej jedna opcja

            // Act
            bool mozeUruchomic = viewModel.ProcessVideoCommand.CanExecute(null);

            // Assert
            Assert.True(mozeUruchomic);
        }

        [Fact]
        public async Task ProcessVideo_KiedyLimitZapytanPrzekroczony_UstawiaOdpowiedniKomunikat()
        {
            // Arrange
            var mockYoutube = new Mock<IYouTubeService>();
            var mockGemini = new Mock<IGeminiService>();

            // Ustawiamy YouTubeService tak, by udawał, że pobrał dane pomyślnie
            mockYoutube.Setup(s => s.GetYouTubeAsync(It.IsAny<string>()))
                       .ReturnsAsync(new YouTubeDependency { Tekst = "Jakis tekst", CzyTylkoAudio = false });

            // Ustawiamy GeminiService tak, by celowo rzucił Twój błąd QuotaExceededException
            mockGemini.Setup(s => s.GetGeminiAsync(It.IsAny<YouTubeDependency>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                      .ThrowsAsync(new QuotaExceededException());

            var viewModel = new MainViewModel(mockYoutube.Object, mockGemini.Object)
            {
                VideoUrl = "https://youtube.com/watch?v=123",
                ChceStreszczenie = true
            };

            // Act
            await viewModel.ProcessVideoCommand.ExecuteAsync(null);

            // Assert
            // Sprawdzamy, czy blok catch() w View Modelu złapał ten wyjątek i ustawił poprawny komunikat
            Assert.Equal("Wykorzystałeś darmowy limit zapytań. Poczekaj 60 sekund i spróbuj ponownie.", viewModel.TekstWynikowy);
            Assert.False(viewModel.IsLoading);
            Assert.True(viewModel.IsResultVisible);
        }

        [Fact]
        public async Task ProcessVideo_KiedyZlyLinkYoutube_UstawiaOdpowiedniKomunikat()
        {
            // Arrange
            var mockYoutube = new Mock<IYouTubeService>();
            var mockGemini = new Mock<IGeminiService>();

            // Symulujemy, że serwis YouTube rzuca błąd złego linku
            mockYoutube.Setup(s => s.GetYouTubeAsync(It.IsAny<string>()))
                       .ThrowsAsync(new InvalidYoutubeUrlException());

            var viewModel = new MainViewModel(mockYoutube.Object, mockGemini.Object)
            {
                VideoUrl = "zly_link",
                ChceStreszczenie = true
            };

            // Act
            await viewModel.ProcessVideoCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Niepoprawny link do video. Sprawdź poprawność i wklej go jeszcze raz.", viewModel.TekstWynikowy);
            Assert.False(viewModel.IsLoading);
            Assert.True(viewModel.IsResultVisible);
        }

        [Fact]
        public async Task ProcessVideo_KiedySerwerGeminiPrzeciazony_UstawiaOdpowiedniKomunikat()
        {
            // Arrange
            var mockYoutube = new Mock<IYouTubeService>();
            var mockGemini = new Mock<IGeminiService>();

            mockYoutube.Setup(s => s.GetYouTubeAsync(It.IsAny<string>()))
                       .ReturnsAsync(new YouTubeDependency { Tekst = "Dane", CzyTylkoAudio = false });

            // Symulujemy błąd przeciążenia z Gemini
            mockGemini.Setup(s => s.GetGeminiAsync(It.IsAny<YouTubeDependency>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                      .ThrowsAsync(new ServerOverloadedException());

            var viewModel = new MainViewModel(mockYoutube.Object, mockGemini.Object)
            {
                VideoUrl = "https://youtube.com/watch?v=123",
                ChceWniosek = true
            };

            // Act
            await viewModel.ProcessVideoCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Serwery Gemini są przeciążone. Spróbuj ponownie za chwilę.", viewModel.TekstWynikowy);
        }

        [Fact]
        public void BackToInput_KiedyWywolano_ResetujeStanInterfejsu()
        {
            // Arrange
            var mockYoutube = new Mock<IYouTubeService>();
            var mockGemini = new Mock<IGeminiService>();
            var viewModel = new MainViewModel(mockYoutube.Object, mockGemini.Object);

            // Ustawiamy stan tak, jakbyśmy byli na ekranie z wynikami
            viewModel.IsResultVisible = true;
            viewModel.IsLoading = true;
            viewModel.IsInputVisible = false;

            // Act
            viewModel.BackToInputCommand.Execute(null);

            // Assert
            // Sprawdzamy, czy aplikacja wróciła do początkowego stanu
            Assert.False(viewModel.IsResultVisible);
            Assert.False(viewModel.IsLoading);
            Assert.True(viewModel.IsInputVisible);
        }
    }


}