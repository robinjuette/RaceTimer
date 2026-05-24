using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RaceTimerClientApp.Shared.Services;
using RaceTimerClientApp.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the RaceTimerClientApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<RaceSignalRService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<RaceStateService>();

var host = builder.Build();

// initialize RaceStateService before running the app
var raceState = host.Services.GetRequiredService<RaceStateService>();
await raceState.InitializeAsync();

await host.RunAsync();
