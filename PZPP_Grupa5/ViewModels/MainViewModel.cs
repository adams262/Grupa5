using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PZPP_Grupa5.Models;
using PZPP_Grupa5.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PZPP_Grupa5.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IYouTubeService _youtubeService;
        private readonly IGeminiService _geminiService;

        public ObservableCollection<ChatHistoryItem> HistoriaCzatow { get; set; } = new();

        public MainViewModel(IYouTubeService youtubeService, IGeminiService geminiService)
        {
            _youtubeService = youtubeService;
            _geminiService = geminiService;

            WczytajZapisanaHistorie();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessVideoCommand))]
        private string videoUrl;

        [ObservableProperty]
        private string tekstWynikowy;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessVideoCommand))]
        private bool chceStreszczenie;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessVideoCommand))]
        private bool chceWniosek;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessVideoCommand))]
        private bool chceTimestamps;

        [ObservableProperty]
        private bool isInputVisible = true;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isResultVisible = false;

        [ObservableProperty]
        private string videoTitle;

        [ObservableProperty]
        private string videoThumbnailUrl;

        [ObservableProperty]
        private bool isVideoInfoVisible;

        [ObservableProperty]
        private string _themeIcon = "\uf186";

        public string UserApiKey
        {
            get => Preferences.Default.Get("GeminiApiKey", string.Empty);
            set
            {
                Preferences.Default.Set("GeminiApiKey", value);
                OnPropertyChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanProcess))]
        private async Task ProcessVideo()
        {
            IsInputVisible = false;
            IsLoading = true;
            IsResultVisible = false;
            IsVideoInfoVisible = false;

            try
            {
                try
                {
                    var youtube = new YoutubeExplode.YoutubeClient();
                    var video = await youtube.Videos.GetAsync(VideoUrl);
                    VideoTitle = video.Title;
                    VideoThumbnailUrl = video.Thumbnails.OrderByDescending(t => t.Resolution.Width).FirstOrDefault()?.Url;
                    IsVideoInfoVisible = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    VideoThumbnailUrl = "no_image_available.jpg";
                    VideoTitle = "Nie udało się pobrać tytułu";
                    IsVideoInfoVisible = true;
                }

                var youtubeDane = await _youtubeService.GetYouTubeAsync(VideoUrl);
                TekstWynikowy = "Pobrano dane. Trwa analiza, proszę czekać...";

                if (!youtubeDane.CzyTylkoAudio && youtubeDane.Tekst.Contains("<style>"))
                {
                    TekstWynikowy = "Błąd: YouTube zablokował pobieranie napisów. Spróbuj innego filmu.";
                    return;
                }

                var wynikPrzetworzony = await _geminiService.GetGeminiAsync(youtubeDane, ChceStreszczenie, ChceWniosek, ChceTimestamps);
                TekstWynikowy = wynikPrzetworzony;

                ZapiszDoHistorii(VideoTitle, wynikPrzetworzony, VideoThumbnailUrl, VideoUrl);
            }
            catch (ApiKeyException)
            {
                TekstWynikowy = "Twój klucz API jest nieważny lub błędny. Sprawdź jego poprawność.";
                System.Diagnostics.Debug.WriteLine("Błąd klucza API");
            }
            catch (QuotaExceededException)
            {
                TekstWynikowy = "Wykorzystałeś darmowy limit zapytań. Poczekaj 60 sekund i spróbuj ponownie.";
            }
            catch (ServerOverloadedException)
            {
                TekstWynikowy = "Serwery Gemini są przeciążone. Spróbuj ponownie za chwilę.";
            }
            catch (InvalidYoutubeUrlException)
            {
                TekstWynikowy = "Niepoprawny link do video. Sprawdź poprawność i wklej go jeszcze raz.";
            }
            catch (Exception ex)
            {
                TekstWynikowy = ExplainError(ex.Message);
                System.Diagnostics.Debug.WriteLine($"Pełny błąd API: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsResultVisible = true;
            }
        }

        private bool CanProcess()
        {
            return !string.IsNullOrWhiteSpace(VideoUrl) && (ChceStreszczenie || ChceWniosek || ChceTimestamps);
        }

        private string ExplainError(string error)
        {
            var e = error.ToLower();

            if (e.Contains("network") || error.Contains("connection"))
                return "Problem z internetem. Sprawdź swoje połączenie.";

            if (e.Contains("overloaded") || e.Contains("503"))
                return "Serwery Gemini są przeciążone. Spróbuj ponownie za chwilę.";

            return "Wystąpił nieznany błąd, spróbuj ponownie";
        }

        [RelayCommand]
        private void BackToInput()
        {
            IsResultVisible = false;
            IsLoading = false;
            IsInputVisible = true;
        }

        [RelayCommand]
        private async Task SaveToFile()
        {
            if (string.IsNullOrWhiteSpace(TekstWynikowy))
                return;

            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TekstWynikowy));
                var fileSaverResult = await FileSaver.Default.SaveAsync("Analiza_Gemini.txt", stream, CancellationToken.None);

                if (fileSaverResult.IsSuccessful)
                {
                    await Shell.Current.DisplayAlert("Pobieranie", "Wynik został pobrany pomyślnie", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Błąd", "Nie udało się zapisać pliku: " + ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task CopyToClipboard()
        {
            if (string.IsNullOrWhiteSpace(TekstWynikowy))
            {
                return;
            }
            await Clipboard.Default.SetTextAsync(TekstWynikowy);
            await Shell.Current.DisplayAlert("Kopiowanie", "Wynik został skopiowany do schowka", "OK");
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            if (Application.Current.UserAppTheme == AppTheme.Dark)
                Application.Current.UserAppTheme = AppTheme.Light;
            else
                Application.Current.UserAppTheme = AppTheme.Dark;

            ThemeIcon = Application.Current.UserAppTheme == AppTheme.Dark ? "\uf186;" : "\uf185;";
        }

        private void WczytajZapisanaHistorie()
        {
            try
            {
                var savedHistory = Preferences.Default.Get("ChatHistoryJson", string.Empty);
                if (!string.IsNullOrWhiteSpace(savedHistory))
                {
                    var items = JsonSerializer.Deserialize<List<ChatHistoryItem>>(savedHistory);
                    if (items != null)
                    {
                        HistoriaCzatow.Clear();
                        foreach (var item in items)
                        {
                            HistoriaCzatow.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas wczytywania historii: {ex.Message}");
            }
        }

        private void ZapiszDoHistorii(string tytul, string wynik, string miniatura, string url)
        {
            var newItem = new ChatHistoryItem
            {
                TytulWideo = string.IsNullOrWhiteSpace(tytul) ? "Nieznane wideo" : tytul,
                DataUtworzenia = DateTime.Now,
                TekstWynikowy = wynik,
                VideoThumbnailUrl = miniatura,
                VideoUrl = url
            };

            HistoriaCzatow.Insert(0, newItem);

            try
            {
                var itemsToSave = HistoriaCzatow.Take(20).ToList();
                var json = JsonSerializer.Serialize(itemsToSave);
                Preferences.Default.Set("ChatHistoryJson", json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu historii: {ex.Message}");
            }
        }

        [RelayCommand]
        private void WczytajHistorie(ChatHistoryItem wybranaHistoria)
        {
            if (wybranaHistoria == null) return;

            VideoTitle = wybranaHistoria.TytulWideo;
            TekstWynikowy = wybranaHistoria.TekstWynikowy;
            VideoThumbnailUrl = wybranaHistoria.VideoThumbnailUrl;
            VideoUrl = wybranaHistoria.VideoUrl;

            IsVideoInfoVisible = true;
            IsInputVisible = false;
            IsLoading = false;
            IsResultVisible = true;
        }

        [RelayCommand]
        private void UsunHistorie(ChatHistoryItem itemDoUsuniecia)
        {
            if (itemDoUsuniecia != null && HistoriaCzatow.Contains(itemDoUsuniecia))
            {
                HistoriaCzatow.Remove(itemDoUsuniecia);

                try
                {
                    var itemsToSave = HistoriaCzatow.ToList();
                    var json = JsonSerializer.Serialize(itemsToSave);
                    Preferences.Default.Set("ChatHistoryJson", json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Błąd podczas usuwania historii: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task OtworzLinkWideo()
        {
            if (!string.IsNullOrWhiteSpace(VideoUrl))
            {
                await Launcher.Default.OpenAsync(VideoUrl);
            }
        }
    }
}