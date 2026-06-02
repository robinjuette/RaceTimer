using RaceTimerApp.Web.Components;
using RaceTimerApp.Shared.Services;
using RaceTimer.Shared.Http;
using RaceTimer.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Register RaceTimer services
builder.Services.AddMauiRaceServices();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<TimingService>();
builder.Services.AddScoped<RaceManagementService>();
builder.Services.AddScoped<ParticipantService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<TimepointCorrectionService>();
builder.Services.AddBlazorBootstrap();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(RaceTimerApp.Shared._Imports).Assembly,
        typeof(RaceTimerApp.Web.Client._Imports).Assembly);

app.Run();
