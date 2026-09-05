using FunctionalProgramming.Models;
using System.Text.Json;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class GraphQlApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapPost("/graphql", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<GraphQlRequest>(context.Request.Body, cancellationToken: context.RequestAborted);
            var query = request?.Query ?? string.Empty;

            if (query.Contains("apiTypes", StringComparison.OrdinalIgnoreCase) &&
                query.Contains("useCase", StringComparison.OrdinalIgnoreCase))
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

            if (query.Contains("apiTypes", StringComparison.OrdinalIgnoreCase))
            {
                var response = new
                {
                    data = new
                    {
                        apiTypes = new[]
                        {
                            new { name = "REST" },
                            new { name = "SOAP" },
                            new { name = "gRPC" },
                            new { name = "GraphQL" },
                            new { name = "Webhook" },
                            new { name = "WebSocket" }
                        }
                    }
                };
                return Results.Ok(response);
            }

            return Results.BadRequest(new
            {
                errors = new[]
                {
                    new
                    {
                        message = "Try queries like: { apiTypes { name } } or { apiTypes { name useCase } } to choose fields."
                    }
                }
            });
        });
    }
}
