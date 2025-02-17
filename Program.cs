using System.Net.WebSockets;
using WebsocketTest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<MessageService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseWebSockets();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapHealthChecks("/healtz");
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

if (app.Environment.IsProduction())
{
    app.Map("/ws", async (context) =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var ws = await context.WebSockets.AcceptWebSocketAsync("wss");
        var authentication = await ws.ReadObject<AuthenticateData>(CancellationToken.None);
        if (authentication == null || authentication.Username != "abcd" || authentication.Password != "efgh")
        {
            await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, null, CancellationToken.None);
            return;
        }

        while (true)
        {
            if (ws.State == System.Net.WebSockets.WebSocketState.Closed)
                break;

            await ws.WriteObject(new MessageData { Message = "Hello world" });
            await Task.Delay(1000 * 15);
        }
    });
}

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

class AuthenticateData
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

class MessageData
{
    public string? Message { get; set; }
}

internal class MessageService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ws = new ClientWebSocket();
        ws.Options.AddSubProtocol("wss");
        await ws.ConnectAsync(new Uri("wss://websockettest.sliplane.app/ws"), stoppingToken);
        await ws.WriteObject(new AuthenticateData { Username = "abcd", Password = "efgh" });

        await Task.Run(async () =>
        {
            while (true)
            {
                var message = await ws.ReadObject<MessageData>(stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                Console.WriteLine($"Received: {message.Message}");
            }
        });
    }
}