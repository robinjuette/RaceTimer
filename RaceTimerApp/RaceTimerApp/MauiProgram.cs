using Microsoft.Extensions.Logging;
using RaceTimer.Shared.Services;
using RaceTimerApp.Shared.Services;

namespace RaceTimerApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Register configuration service first
            builder.Services.AddSingleton<AppConfigService>();

            // Register local Race Services (offline-first)
            builder.Services.AddMauiRaceServices();

            // Register app-specific services
            builder.Services.AddScoped<RaceService>();
            builder.Services.AddScoped<ParticipantService>();
            builder.Services.AddScoped<TimingService>();
            builder.Services.AddScoped<RankingService>();
            builder.Services.AddScoped<SettingsService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
