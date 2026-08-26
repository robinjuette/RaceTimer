using RaceTimerApp.Web.Components;
using RaceTimerApp.Shared.Services;
using RaceTimer.Shared.Http;
using RaceTimer.Shared.Services;
using RaceTimerApp.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

var hostedOptions = builder.Configuration.GetSection("RaceTimer").Get<HostedServerOptions>() ?? new HostedServerOptions();
if (hostedOptions.IsHosted && !Uri.TryCreate(hostedOptions.ServerUrl, UriKind.Absolute, out _))
{
    throw new InvalidOperationException("RaceTimer:ServerUrl muss im Hosted-Modus eine gültige absolute URL sein.");
}
builder.Services.Configure<HostedServerOptions>(builder.Configuration.GetSection("RaceTimer"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register RaceTimer services
builder.Services.AddLocalRaceServices();
builder.Services.AddServerRaceServices();
builder.Services.AddConfiguredConnectionRepository();
builder.Services.AddScoped<IRaceRepository>(serviceProvider =>
{
    var repository = serviceProvider.GetRequiredService<ServerRaceRepository>();
    repository.ServerUri = new Uri(hostedOptions.ServerUrl!);
    return repository;
});
builder.Services.AddScoped<IRepositoryChangeNotifier>(serviceProvider =>
    serviceProvider.GetRequiredService<ServerRaceRepository>());
builder.Services.AddScoped<TimingService>();
builder.Services.AddScoped<RaceManagementService>();
builder.Services.AddScoped<ParticipantService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<TimepointCorrectionService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddScoped<AppConfigService>();
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
    if (builder.Configuration.GetValue("HttpsRedirection:Enabled", false))
    {
        app.UseHsts();
    }
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (builder.Configuration.GetValue("HttpsRedirection:Enabled", false))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(RaceTimerApp.Shared._Imports).Assembly);

app.Run();
