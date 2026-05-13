using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Alerts;  
using CommunityToolkit.Maui.Core;    
using PZPP_Grupa5.Services;
using System.Windows.Input; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;

namespace PZPP_Grupa5.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IYouTubeService _youtubeService;
        private readonly IGeminiService _geminiService;

        // [[[ Dependency Injection serwisów YouTubeService i GeminiService ]]]
        public MainViewModel(IYouTubeService youtubeService, IGeminiService geminiService)
        {
            _youtubeService = youtubeService;
            _geminiService = geminiService;
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


        public string UserApiKey
        {
            get => Preferences.Default.Get("GeminiApiKey", string.Empty);
            set
            {
                Preferences.Default.Set("GeminiApiKey", value);
                OnPropertyChanged();
            }
        }

        // [[[ Komenda do przetwarzania wideo ]]]
        [RelayCommand(CanExecute = nameof(CanProcess))]
        private async Task ProcessVideo()
        {
            IsInputVisible = false;
            IsLoading = true;
            IsResultVisible = false;


            if (string.IsNullOrWhiteSpace(VideoUrl))
            {
                TekstWynikowy = "Proszę wprowadzić poprawny URL wideo z YouTube.";
                IsLoading = false;
                IsResultVisible = true;

                return;
            }

            try
            {
                // [[[ Pobieranie danych z YouTube ]]]
                var youtubeDane = await _youtubeService.GetYouTubeAsync(VideoUrl);
                TekstWynikowy = "Pobrano dane. Trwa analiza, proszę czekać...";

                if (!youtubeDane.CzyTylkoAudio && youtubeDane.Tekst.Contains("<style>"))
                {
                    TekstWynikowy = "Błąd: YouTube zablokował pobieranie napisów. Spróbuj innego filmu.";
                    IsLoading = false; IsResultVisible = true;
                    return;
                }

                // [[[ Przetwarzanie danych przez Gemini AI Studio ]]]
                var wynikPrzetworzony = await _geminiService.GetGeminiAsync(youtubeDane, ChceStreszczenie, ChceWniosek, ChceTimestamps);
                TekstWynikowy = wynikPrzetworzony;

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
            if (e.Contains("api_key_invalid") || e.Contains("api key not valid") || e.Contains("400"))
                return "Twój klucz API jest nieważny lub błędny. Sprawdź jego poprawność.";

            if (e.Contains("429") || e.Contains("quota") || e.Contains("limit"))
                return "Wykorzystałeś darmowy limit zapytań. Poczekaj 60 sekund i spróbuh ponownie.";

            if (e.Contains("overloaded") || e.Contains("503"))
                return "Serwery Gemini są przeciążone. Spróbuj ponownie za chwilę.";

            if (error.Contains("network") || error.Contains("connection"))
                return "Problem z internetem. Sprawdź swoje połączenie.";

            if (e.Contains("safety") || e.Contains("blocked"))
                return "AI uznało, że ten film jest zbyt kontrowersyjny i odmówiło analizy.";

            if (e.Contains("invalid youtube video id") || e.Contains("invalid url"));
                return "Niepoprawny link do video. Sprawdź poprawność i wklej go jeszcze raz.";

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
    }
}
