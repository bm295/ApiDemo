using FunctionalProgramming.Services;
using System.Text;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class WebhookApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapPost("/webhook/orders", async (HttpContext context, IWebhookLogger logger) =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync(context.RequestAborted);
            await logger.LogOrderPayloadAsync(payload, context.RequestAborted);
            return Results.Ok(new { received = true, at = DateTime.UtcNow });
        });
    }
}
