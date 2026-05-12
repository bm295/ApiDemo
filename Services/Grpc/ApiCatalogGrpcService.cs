using FunctionalProgramming.Grpc;
using Grpc.Core;

namespace FunctionalProgramming.Services.Grpc;

public sealed class ApiCatalogGrpcService : ApiCatalog.ApiCatalogBase
{
    public override Task<ApiReply> GetApiTypes(ApiRequest request, ServerCallContext context)
    {
        var reply = new ApiReply();
        reply.Items.AddRange(
        [
            new ApiType { Name = "REST", UseCase = "Resource CRUD over HTTP" },
            new ApiType { Name = "SOAP", UseCase = "Contract-first enterprise integration" },
            new ApiType { Name = "gRPC", UseCase = "Fast binary service-to-service calls" },
            new ApiType { Name = "GraphQL", UseCase = "Flexible client-driven queries" },
            new ApiType { Name = "Webhook", UseCase = "Server push notifications" },
            new ApiType { Name = "WebSocket", UseCase = "Realtime two-way communication" }
        ]);

        return Task.FromResult(reply);
    }
}
