using System.Text;

namespace FunctionalProgramming.Endpoints;

public static class WebSocketEndpoints
{
    public static IEndpointRouteBuilder MapWebSocketEndpoints(this IEndpointRouteBuilder app)
    {
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

        return app;
    }
}
