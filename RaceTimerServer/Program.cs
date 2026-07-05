using RaceTimer.Shared.Data;
using RaceTimer.Shared.Services;
using RaceTimerServer.Hubs;
using RaceTimerServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add SignalR for real-time change notifications
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB max message size
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

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

app.UseHttpsRedirection();

app.UseAuthorization();

// Map SignalR hub
app.MapHub<RaceTimerHub>("/hubs/racetimer");

app.MapControllers();

app.Run();
