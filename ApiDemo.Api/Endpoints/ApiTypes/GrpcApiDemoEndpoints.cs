using FunctionalProgramming.Grpc;
using FunctionalProgramming.Services.Grpc;
using Google.Protobuf;
using Grpc.Net.Client;
using Grpc.Core;
using System.Text.Json;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class GrpcApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapPost("/grpc/request", CallV2Async);
        api.MapPost("/grpc/v1/request", CallV1Async);
        api.MapPost("/grpc/v2/request", CallV2Async);
        api.MapGet("/grpc/stream", StreamReportAsync);
    }

    private static async Task StreamReportAsync(HttpContext context, CancellationToken cancellationToken)
    {
        using var channel = GrpcChannel.ForAddress(ResolveGrpcAddress(context));
        var client = new ReportStreaming.ReportStreamingClient(channel);
        using var call = client.StreamReportProgress(new ReportProgressRequest
        {
            ReportName = "Monthly sales report"
        }, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: cancellationToken);

        context.Response.ContentType = "application/x-ndjson; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        try
        {
            await foreach (var update in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                await context.Response.WriteAsync(JsonFormatter.Default.Format(update) + "\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser disconnected or stopped the stream.
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
        }
        catch (RpcException exception)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = $"gRPC stream failed: {exception.StatusCode}"
            }) + "\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
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
