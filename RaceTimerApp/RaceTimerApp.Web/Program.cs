using RaceTimerApp.Web.Components;
using RaceTimerApp.Shared.Services;
using RaceTimer.Shared.Http;
using RaceTimer.Shared.Services;
using RaceTimerApp.Shared.Models;
using RaceTimerApp.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

var hostedOptions = builder.Configuration.GetSection("RaceTimer").Get<HostedServerOptions>() ?? new HostedServerOptions();
if (hostedOptions.IsHosted && !Uri.TryCreate(hostedOptions.ServerUrl, UriKind.Absolute, out _))
{
    throw new InvalidOperationException("RaceTimer:ServerUrl muss im Hosted-Modus eine gültige absolute URL sein.");
}
builder.Services.Configure<HostedServerOptions>(builder.Configuration.GetSection("RaceTimer"));
var oidcAuthority = builder.Configuration["Authentication:Authority"];
var oidcEnabled = hostedOptions.IsHosted && Uri.TryCreate(oidcAuthority, UriKind.Absolute, out _);

if (hostedOptions.IsHosted)
{
    builder.Services.AddHttpClient("RaceTimerServer", client =>
    {
        client.BaseAddress = new Uri(hostedOptions.ServerUrl!);
    });
}

builder.Services.AddHttpContextAccessor();
if (oidcEnabled)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }).AddCookie(options => options.LoginPath = "/account/login")
      .AddOpenIdConnect(options =>
      {
          options.Authority = oidcAuthority!;
          options.ClientId = builder.Configuration["Authentication:ClientId"] ?? "racetimer-web";
          options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
          options.ResponseType = "code";
          options.UsePkce = true;
          options.SaveTokens = true;
          options.GetClaimsFromUserInfoEndpoint = true;
          options.Scope.Add("openid");
          options.Scope.Add("profile");
          options.Scope.Add("offline_access");
          options.Scope.Add("racetimer.read");
          options.Scope.Add("racetimer.manage");
          options.RequireHttpsMetadata = builder.Environment.IsProduction();
           options.Events.OnRemoteFailure = context =>
           {
               context.Response.Redirect("/account/error");
               context.HandleResponse();
               return Task.CompletedTask;
           };
           options.Events.OnAccessDenied = context =>
           {
               context.Response.Redirect("/account/forbidden");
               context.HandleResponse();
               return Task.CompletedTask;
           };
      });
    builder.Services.AddCascadingAuthenticationState();
}
else
{
    builder.Services.AddAuthentication().AddCookie();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register RaceTimer services
builder.Services.AddLocalRaceServices();
builder.Services.AddServerRaceServices();
builder.Services.AddTransient<AccessTokenHandler>();
builder.Services.AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>()
    .AddHttpMessageHandler<AccessTokenHandler>();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/account/login", (HttpContext context) =>
    Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));
app.MapGet("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
    return Results.Empty;
});

app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(RaceTimerApp.Shared._Imports).Assembly);

app.Run();
