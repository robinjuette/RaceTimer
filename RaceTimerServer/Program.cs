using RaceTimer.Shared.Data;
using RaceTimer.Shared.Services;
using RaceTimerServer.Hubs;
using RaceTimerServer.Services;
using RaceTimerServer.Configuration;
using RaceTimerServer.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);
var databasePath = builder.Configuration["RaceTimer:DatabasePath"];
var authOptions = builder.Configuration.GetSection("Authentication").Get<AuthenticationOptions>() ?? new();
var authEnabled = authOptions.Enabled;
builder.Services.Configure<PublicAccessOptions>(builder.Configuration.GetSection("PublicAccess"));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddAuthorization(options =>
{
    if (authEnabled)
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build();
    }
    AuthorizationPolicies.AddRaceTimerPolicies(options, authEnabled);
});

if (authEnabled)
{
    var connectionString = authOptions.ConnectionString ?? builder.Configuration.GetConnectionString("RaceTimer");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Authentication:ConnectionString oder ConnectionStrings:RaceTimer muss gesetzt sein.");

    builder.Services.AddDbContextFactory<RaceTimerDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<CoreRaceRepository>();
    builder.Services.AddDbContext<RaceTimerIdentityDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddIdentity<RaceTimerUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.Password.RequiredLength = 12;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedAccount = false;
    }).AddEntityFrameworkStores<RaceTimerIdentityDbContext>().AddDefaultTokenProviders();

    builder.Services.AddScoped<BootstrapService>();
    builder.Services.AddOpenIddict()
        .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<RaceTimerIdentityDbContext>())
        .AddServer(options =>
        {
            options.SetIssuer(new Uri(authOptions.Issuer));
            options.SetAuthorizationEndpointUris("connect/authorize");
            options.SetTokenEndpointUris("connect/token");
            options.SetEndSessionEndpointUris("connect/logout");
            options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
            options.AllowRefreshTokenFlow();
            options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.OfflineAccess, "racetimer.read", "racetimer.manage");
            if (builder.Environment.IsDevelopment())
            {
                options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(authOptions.SigningCertificatePath) || string.IsNullOrWhiteSpace(authOptions.EncryptionCertificatePath))
                    throw new InvalidOperationException("Authentication:SigningCertificatePath und EncryptionCertificatePath müssen außerhalb der Entwicklung gesetzt sein.");
                options.AddSigningCertificate(new X509Certificate2(authOptions.SigningCertificatePath, authOptions.CertificatePassword));
                options.AddEncryptionCertificate(new X509Certificate2(authOptions.EncryptionCertificatePath, authOptions.CertificatePassword));
            }
            options.UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableTokenEndpointPassthrough().EnableEndSessionEndpointPassthrough();
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });
}

// Add SignalR for real-time change notifications
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB max message size
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

if (!authEnabled)
    builder.Services.AddLocalRaceServices(databasePath);
builder.Services.AddTransient<IRaceRepository>(sp => sp.GetRequiredService<CoreRaceRepository>());

// Add repository change notification service for SignalR broadcasting
builder.Services.AddScoped<RepositoryChangeNotificationService>();

// Add Swagger/Swashbuckle for OpenAPI documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "RaceTimer API",
        Version = "v1",
        Description = "REST API for RaceTimer - Race timing and participant management system"
    });
});

var app = builder.Build();

if (authEnabled)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceTimerIdentityDbContext>();
    await db.Database.MigrateAsync();
    if (authEnabled)
        await scope.ServiceProvider.GetRequiredService<IDbContextFactory<RaceTimerDbContext>>().CreateDbContext().Database.EnsureCreatedAsync();
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
    await OpenIddictSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceTimer API v1");
        c.RoutePrefix = "swagger";
    });
}

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", false))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hub
var hub = app.MapHub<RaceTimerHub>("/hubs/racetimer");
if (authEnabled)
    hub.RequireAuthorization();

app.MapControllers();

app.Run();
