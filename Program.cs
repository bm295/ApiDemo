using System.Text;
using System.Text.Json;
using FunctionalProgramming.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

var restMessages = new List<ApiMessage>
{
    new(1, "Hello from REST", DateTime.UtcNow),
    new(2, "REST returns JSON", DateTime.UtcNow)
};

app.MapGet("/api/rest/messages", () => Results.Ok(restMessages));
app.MapPost("/api/rest/messages", async (HttpContext context) =>
{
    var body = await JsonSerializer.DeserializeAsync<ApiMessageInput>(context.Request.Body);
    if (body is null || string.IsNullOrWhiteSpace(body.Text))
    {
        return Results.BadRequest(new { error = "Text is required." });
    }

    var message = new ApiMessage(restMessages.Count + 1, body.Text, DateTime.UtcNow);
    restMessages.Add(message);
    return Results.Created($"/api/rest/messages/{message.Id}", message);
});

app.MapPost("/api/webhook/orders", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    var payload = await reader.ReadToEndAsync();
    var log = $"[{DateTime.UtcNow:O}] Webhook payload: {payload}{Environment.NewLine}";
    await File.AppendAllTextAsync("webhook.log", log);
    return Results.Ok(new { received = true, at = DateTime.UtcNow });
});

app.MapPost("/api/soap", async (HttpContext context) =>
{
    var xmlRequest = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var escaped = System.Security.SecurityElement.Escape(xmlRequest);

    var xmlResponse = $"""
                      <?xml version="1.0" encoding="utf-8"?>
                      <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                        <soap:Body>
                          <DemoSoapResponse xmlns="https://functionalprogramming.demo/soap">
                            <Message>SOAP endpoint received your XML request.</Message>
                            <Echo>{escaped}</Echo>
                          </DemoSoapResponse>
                        </soap:Body>
                      </soap:Envelope>
                      """;

    context.Response.ContentType = "text/xml; charset=utf-8";
    await context.Response.WriteAsync(xmlResponse);
});

app.MapPost("/api/graphql", async (HttpContext context) =>
{
    var request = await JsonSerializer.DeserializeAsync<GraphQlRequest>(context.Request.Body);
    var query = request?.Query ?? string.Empty;

    if (query.Contains("apiTypes", StringComparison.OrdinalIgnoreCase))
    {
        var response = new
        {
            data = new
            {
                apiTypes = new[]
                {
                    new { name = "REST", useCase = "Resource CRUD over HTTP" },
                    new { name = "SOAP", useCase = "Contract-first enterprise integration" },
                    new { name = "gRPC", useCase = "Fast binary service-to-service calls" },
                    new { name = "GraphQL", useCase = "Flexible client-driven queries" },
                    new { name = "Webhook", useCase = "Server push notifications" },
                    new { name = "WebSocket", useCase = "Realtime two-way communication" }
                }
            }
        };
        return Results.Ok(response);
    }

    return Results.BadRequest(new { errors = new[] { new { message = "Try querying: { apiTypes { name useCase } }" } } });
});

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Expected a WebSocket request.");
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[1024 * 4];

    while (true)
    {
        var result = await webSocket.ReceiveAsync(buffer, context.RequestAborted);
        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closed by client", context.RequestAborted);
            break;
        }

        var receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var responseText = $"Server echo: {receivedText}";
        var responseBytes = Encoding.UTF8.GetBytes(responseText);
        await webSocket.SendAsync(responseBytes, System.Net.WebSockets.WebSocketMessageType.Text, true, context.RequestAborted);
    }
});

app.UseWebSockets();
app.MapRazorPages();

app.Run();
