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
                throw new ApiKeyException();

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            // budowanie promptu
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
            var odpowiedz = await _httpClient.PostAsJsonAsync(url, payload);
            var json = await odpowiedz.Content.ReadAsStringAsync();
            
            // obsługa błędów
            if (!odpowiedz.IsSuccessStatusCode)
            {
                var errorJson = JsonDocument.Parse(json);
                var errorMessage = errorJson.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString() ?? json;

                if (odpowiedz.StatusCode == System.Net.HttpStatusCode.BadRequest && json.Contains("API_KEY_INVALID"))
                    throw new ApiKeyException(errorMessage);

                if (odpowiedz.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    throw new QuotaExceededException();

                if (odpowiedz.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    throw new ServerOverloadedException();

                throw new Exception(errorMessage);
            }

            // parsowanie
            try
            {
                using var doc = JsonDocument.Parse(json);

                var wynik = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return wynik ?? "Brak odpowiedzi od Gemini AI Studio.";
            }
            catch (Exception ex)
            {
                return $"Błąd parsowania: {ex.Message}";
            }
        }
    }
}
