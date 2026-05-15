

using PZPP_Grupa5.Models;

namespace PZPP_Grupa5.Services
{
    public interface IGeminiService
    {
        // Metody serwisu Gemini 
        Task<string> GetGeminiAsync(YouTubeDependency dane, bool streszczenie, bool wniosek, bool timestamps);
    }
}
