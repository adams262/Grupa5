using PZPP_Grupa5.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text; 

namespace PZPP_Grupa5.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> GetGeminiAsync(YouTubeDependency dane, bool streszczenie, bool wniosek, bool timestamps)
        {
            // Pobranie klucza API z ustawień aplikacji
            string apiKey = Preferences.Default.Get("GeminiApiKey", string.Empty);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Błąd: Brak klucza API. Wprowadź go w panelu bocznym aplikacji (ikona ≡ w lewym górnym rogu).";
            }

            string Url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            // --- BUDOWANIE INTELIGENTNEGO PROMPTU ---
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Jesteś ekspertem od analizy treści. Twoim zadaniem jest przeanalizowanie dostarczonego materiału (transkrypcji lub audio) z YouTube.");
            promptBuilder.AppendLine("Odpowiadaj ZAWSZE w języku polskim.");
            promptBuilder.AppendLine("WAŻNE: Zwróć WYŁĄCZNIE sekcje, o które proszę poniżej. Nie dodawaj absolutnie żadnych ogólnych wstępów (np. 'Oto analiza...'), podsumowań ani innych informacji, jeśli nie zostały wyraźnie zaznaczone.");
            promptBuilder.AppendLine();

            if (streszczenie)
            {
                promptBuilder.AppendLine("## Skrócony opis");
                promptBuilder.AppendLine("Napisz zwięzłe i konkretne streszczenie całego materiału.");
                promptBuilder.AppendLine("WAŻNE: Po każdym punkcie (wniosku) dodaj jedną pustą linię odstępu, aby tekst był bardziej przejrzysty.");
                promptBuilder.AppendLine();
            }

            if (wniosek)
            {
                promptBuilder.AppendLine("## Kluczowe wnioski");
                promptBuilder.AppendLine("Wypunktuj najważniejsze konkluzje, lekcje i przemyślenia wynikające z tego materiału. Użyj listy wypunktowanej (-).");
                promptBuilder.AppendLine("WAŻNE: Po każdym punkcie (wniosku) dodaj jedną pustą linię odstępu, aby tekst był bardziej przejrzysty.");
                promptBuilder.AppendLine();
            }

            if (timestamps)
            {
                promptBuilder.AppendLine("## Ważne punkty (Timestamps)");
                promptBuilder.AppendLine("Stwórz listę najważniejszych momentów z materiału. Przedstaw je w formie czytelnej listy, np. w formacie 'MM:SS - Krótki opis wydarzenia'.");
                promptBuilder.AppendLine("WAŻNE: Po każdym punkcie (wniosku) dodaj jedną pustą linię odstępu, aby tekst był bardziej przejrzysty.");
                promptBuilder.AppendLine();
            }

            string finalnyPrompt = promptBuilder.ToString();

            // Zabezpieczenie przed wysłaniem pustego żądania (gdy użytkownik nic nie zaznaczy)
            if (!streszczenie && !wniosek && !timestamps)
            {
                return "Błąd: Wybierz co najmniej jedną opcję analizy przed przetworzeniem wideo.";
            }

            object payload;

            // Pakowanie danych audio lub tekstowych
            if (dane.CzyTylkoAudio)
            {
                var bajtyAudio = await File.ReadAllBytesAsync(dane.SciezkaAudio);
                var base64Audio = Convert.ToBase64String(bajtyAudio);

                payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = finalnyPrompt },
                                new { inline_data = new { mime_type = "audio/mp4", data = base64Audio } }
                            }
                        }
                    }
                };
            }
            else
            {
                payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = finalnyPrompt + "\n\nOto transkrypcja do analizy:\n" + dane.Tekst }
                            }
                        }
                    }
                };
            }

            // Wysyłanie żądania do Gemini AI Studio
            var odpowiedz = await _httpClient.PostAsJsonAsync(Url, payload);
            var json = await odpowiedz.Content.ReadAsStringAsync();

            if (!odpowiedz.IsSuccessStatusCode)
            {
                throw new Exception(json);
            }


            try
            {
                using var doc = JsonDocument.Parse(json);

                // Sprawdzanie, czy odpowiedź zawiera błąd po stronie API (np. zły klucz)
                if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement))
                {
                    var errorMessage = errorElement.GetProperty("message").GetString();
                    return $"Błąd API Gemini: {errorMessage}";
                }

                var wynik = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return wynik ?? "Brak odpowiedzi od Gemini AI Studio.";
            }
            catch (Exception ex)
            {
                throw new Exception("Błąd parsowania: " + json);
            }
        }
    }
}
