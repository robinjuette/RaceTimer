using Microsoft.Extensions.DependencyInjection;
using RaceTimer.Shared.Http;

namespace RaceTimerMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { })
            .RegisterBlazorWebView();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<RaceTimer.Shared.Http.RaceTimerApiClient>(sp => new RaceTimerApiClient(new HttpClient { BaseAddress = new Uri("https://localhost:5001/") }));

        return builder.Build();
    }
}
