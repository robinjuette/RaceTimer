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

            // Register local Race Services with SQLite (offline-first)
            builder.Services.AddLocalRaceServices();

            // Register ConfiguredConnectionRepository as runtime switcher singleton
            builder.Services.AddConfiguredConnectionRepository();

            // Register configuration service with DI provider and configured repository
            builder.Services.AddSingleton<AppConfigService>(provider =>
            {
                var configuredRepo = provider.GetRequiredService<ConfiguredConnectionRepository>();
                var logger = provider.GetService<ILogger<SignalRSyncService>>();
                return new AppConfigService(
                    null, null, logger, provider, configuredRepo);
            });

            // Register app-specific services
            builder.Services.AddScoped<RaceManagementService>();
            builder.Services.AddScoped<ParticipantService>();
            builder.Services.AddSingleton<TimingService>();
            builder.Services.AddScoped<RankingService>();
            builder.Services.AddScoped<SettingsService>();
            builder.Services.AddScoped<TimepointCorrectionService>();
            builder.Services.AddBlazorBootstrap();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
