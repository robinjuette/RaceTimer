using Microsoft.Extensions.Logging;
using RaceTimerClientApp.Services;
using RaceTimerClientApp.Shared.Services;

namespace RaceTimerClientApp
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

            // Add device-specific services used by the RaceTimerClientApp.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

            // Register services used by Blazor client; configure HTTP and SignalR for MAUI
            builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("https://10.0.2.2:5001/") });
            // register MAUI-compatible RaceSignalRService and RaceStateService
            builder.Services.AddSingleton<RaceTimerClientApp.Web.Client.Services.RaceSignalRService>(sp => new RaceTimerClientApp.Web.Client.Services.RaceSignalRService("https://10.0.2.2:5001/raceHub"));
            builder.Services.AddSingleton<RaceTimerClientApp.Web.Client.Services.RaceStateService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
