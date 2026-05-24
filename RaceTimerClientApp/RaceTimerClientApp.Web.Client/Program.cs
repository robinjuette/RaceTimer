using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RaceTimerClientApp.Shared.Services;
using RaceTimerClientApp.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the RaceTimerClientApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

await builder.Build().RunAsync();
