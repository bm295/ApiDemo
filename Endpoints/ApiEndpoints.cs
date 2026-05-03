using System.Text;
using System.Text.Json;
using FunctionalProgramming.Models;
using FunctionalProgramming.Services;

namespace FunctionalProgramming.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/messages", (IMessageService service) => Results.Ok(service.GetAll()));
        api.MapGet("/messages/{id:int}", (int id, IMessageService service) =>
        {
            var message = service.GetById(id);
            return message is null ? Results.NotFound(new { error = "Message not found." }) : Results.Ok(message);
        });
        api.MapPost("/messages", async (HttpContext context, IMessageService service) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ApiMessageInput>(context.Request.Body, cancellationToken: context.RequestAborted);
            if (body is null || string.IsNullOrWhiteSpace(body.Text))
            {
                return Results.BadRequest(new { error = "Text is required." });
            }

            var message = service.Add(body.Text);
            return Results.Created($"/api/messages/{message.Id}", message);
        });
        api.MapPut("/messages/{id:int}", async (int id, HttpContext context, IMessageService service) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ApiMessageInput>(context.Request.Body, cancellationToken: context.RequestAborted);
            if (body is null || string.IsNullOrWhiteSpace(body.Text))
            {
                return Results.BadRequest(new { error = "Text is required." });
            }

            var message = service.Update(id, body.Text);
            return message is null ? Results.NotFound(new { error = "Message not found." }) : Results.Ok(message);
        });
        api.MapDelete("/messages/{id:int}", (int id, IMessageService service) =>
        {
            return service.Delete(id)
                ? Results.NoContent()
                : Results.NotFound(new { error = "Message not found." });
        });

        api.MapPost("/webhook/orders", async (HttpContext context, IWebhookLogger logger) =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync(context.RequestAborted);
            await logger.LogOrderPayloadAsync(payload, context.RequestAborted);
            return Results.Ok(new { received = true, at = DateTime.UtcNow });
        });

        api.MapPost("/soap", async (HttpContext context) =>
        {
            var xmlRequest = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
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

        api.MapPost("/graphql", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<GraphQlRequest>(context.Request.Body, cancellationToken: context.RequestAborted);
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

        return app;
    }
}
