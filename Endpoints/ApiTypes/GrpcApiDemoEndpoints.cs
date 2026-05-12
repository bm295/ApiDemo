using FunctionalProgramming.Grpc;
using FunctionalProgramming.Services.Grpc;
using Google.Protobuf;
using Grpc.Net.Client;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class GrpcApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapPost("/grpc/request", CallV2Async);
        api.MapPost("/grpc/v1/request", CallV1Async);
        api.MapPost("/grpc/v2/request", CallV2Async);
    }

    private static async Task<IResult> CallV1Async(HttpContext context, CancellationToken cancellationToken)
    {
        var grpcAddress = ResolveGrpcAddress(context);
        using var channel = GrpcChannel.ForAddress(grpcAddress);
        var client = new ApiCatalogV1.ApiCatalogV1Client(channel);
        var request = new ListApiCatalogEntriesV1Request
        {
            ContractVersion = ApiCatalogContract.V1Version
        };

        ApiCatalogContract.EnsureValidRequest(request);
        var response = await client.ListCatalogEntriesAsync(request, cancellationToken: cancellationToken);
        ApiCatalogContract.EnsureValidReply(response);

        return Results.Content(JsonFormatter.Default.Format(response), "application/json");
    }

    private static async Task<IResult> CallV2Async(HttpContext context, CancellationToken cancellationToken)
    {
        var grpcAddress = ResolveGrpcAddress(context);
        using var channel = GrpcChannel.ForAddress(grpcAddress);
        var client = new ApiCatalogV2.ApiCatalogV2Client(channel);
        var request = new ListApiCatalogEntriesV2Request
        {
            ContractVersion = ApiCatalogContract.V2Version,
            RequestingClientName = "demo-client"
        };

        ApiCatalogContract.EnsureValidRequest(request);
        var response = await client.ListCatalogEntriesAsync(request, cancellationToken: cancellationToken);
        ApiCatalogContract.EnsureValidReply(response);

        return Results.Content(JsonFormatter.Default.Format(response), "application/json");
    }

    private static string ResolveGrpcAddress(HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        return configuration["ApiDemo:GrpcAddress"] ?? "http://localhost:5090";
    }
}
