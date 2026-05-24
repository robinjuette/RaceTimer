using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// SignalR for real-time updates
builder.Services.AddSignalR();

// Allow simple cross-origin access for development (adjust in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Repository using EF Core DbContext
builder.Services.AddScoped<RaceTimerServer.Services.RaceRepository>();

// Register HTTP API client builder for typed clients (clients will set BaseAddress)
builder.Services.AddHttpClient<RaceTimer.Shared.Http.RaceTimerApiClient>();

// Configure EF Core DbContext: default to local SQLite file, optional SQL Server when ConnectionStrings:SqlServer is set
builder.Services.AddDbContext<RaceTimerServer.Data.RaceTimerDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var sqlServerConn = configuration.GetConnectionString("SqlServer");
    if (!string.IsNullOrWhiteSpace(sqlServerConn))
    {
        options.UseSqlServer(sqlServerConn);
    }
    else
    {
        // default: local sqlite file in app data folder
        var sqlitePath = configuration.GetValue<string>("Sqlite:FilePath") ?? "Data/racetimer.db";
        var sqliteConn = $"Data Source={sqlitePath}";
        options.UseSqlite(sqliteConn);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceTimerServer.Data.RaceTimerDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// SignalR hubs
app.MapHub<RaceTimerServer.Hubs.RaceHub>("/raceHub");

app.Run();
