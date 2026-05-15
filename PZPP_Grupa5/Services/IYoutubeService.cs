using PZPP_Grupa5.Models;

namespace PZPP_Grupa5.Services
{
    public interface IYouTubeService
    {
        Task<YouTubeDependency> GetYouTubeAsync(string videoUrl);
    }
}
