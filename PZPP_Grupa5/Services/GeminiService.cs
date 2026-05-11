using PZPP_Grupa5.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace PZPP_Grupa5.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> GetGeminiAsync(YouTubeDependency dane, bool streszczenie, bool wniosek, bool timestamps)
        {
            // [[[ Implementacja metody IGeminiService ]]]

            //Pobranie kluczu API z ustawień aplikacji
            string apiKey = Preferences.Default.Get("GeminiApiKey", string.Empty);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Błąd: Brak klucza API. Wprowadź go w ustawieniach aplikacji.";
            }

            string Url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";


            //  Polecenie dla AI
            var prompt = "Przeanalizuj ten materiał z YouTube w jezyku polskim. \n";
            if(streszczenie) prompt += "Stwórz streszczenie tego materiału. \n";
            if(wniosek) prompt += "Podaj wnioski wynikające z tego materiału. \n";
            if(timestamps) prompt += "Podaj znaczniki czasowe dla ważnych momentów w tym materiale. \n";

            object payload;

            // Pakowanie danych audio lub tekst
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
                                new {text = prompt},
                                new {inline_data = new { mime_type = "audio/mp4", data = base64Audio} }
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
                                new {text = prompt + "\n\nTranskrypcja: " + dane.Tekst},
                            }
                        }
                    }
                };
            }

            // [[[ Wysyłanie żądania do Gemini AI Studio ]]]
            var odpowiedz = await _httpClient.PostAsJsonAsync(Url, payload);
            var json = await odpowiedz.Content.ReadAsStringAsync();

            if (!odpowiedz.IsSuccessStatusCode)
            {
                throw new Exception(json);
            }


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
                throw new Exception("Błąd parsowania: " + json);
            }
        }
    }
}
