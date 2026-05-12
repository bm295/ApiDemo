using FunctionalProgramming.Grpc;
using Grpc.Net.Client;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class GrpcApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapPost("/grpc/request", async (HttpContext context, CancellationToken cancellationToken) =>
        {
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            using var channel = GrpcChannel.ForAddress(baseUrl);
            var client = new ApiCatalog.ApiCatalogClient(channel);
            var reply = await client.GetApiTypesAsync(new ApiRequest(), cancellationToken: cancellationToken);

            var result = reply.Items.Select(item => new
            {
                name = item.Name,
                use_case = item.UseCase
            });

            return Results.Ok(new { items = result });
        });
    }
}
