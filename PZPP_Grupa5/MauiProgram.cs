using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;


namespace PZPP_Grupa5
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontAwesomeSolid");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // [[[ Dependency Injection klienta YouTube ]]]
            builder.Services.AddSingleton<YoutubeExplode.YoutubeClient>();

            // [[[ Rejestracja widoków i logiki UI ]]]
            builder.Services.AddTransient<PZPP_Grupa5.ViewModels.MainViewModel>();
            builder.Services.AddTransient<PZPP_Grupa5.Views.MainPage>();

            // [[[ Rejestracja serwisów ]]]
            builder.Services.AddSingleton<PZPP_Grupa5.Services.IYouTubeService, PZPP_Grupa5.Services.YouTubeService>();
            builder.Services.AddSingleton<PZPP_Grupa5.Services.IGeminiService, PZPP_Grupa5.Services.GeminiService>();

            return builder.Build();
        }
    }
}
