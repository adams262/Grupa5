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
        private string videoUrl;

        [ObservableProperty]
        private string tekstWynikowy;

        [ObservableProperty]
        private bool chceStreszczenie;

        [ObservableProperty]
        private bool chceWniosek;

        [ObservableProperty]
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
        [RelayCommand]
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
                    return;
                }

                // [[[ Przetwarzanie danych przez Gemini AI Studio ]]]
                var wynikPrzetworzony = await _geminiService.GetGeminiAsync(youtubeDane, ChceStreszczenie, ChceWniosek, ChceTimestamps);
                TekstWynikowy = wynikPrzetworzony;

            }
            catch (Exception ex)
            {
                TekstWynikowy = $"Wystąpił błąd: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                IsResultVisible = true;
            }
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
