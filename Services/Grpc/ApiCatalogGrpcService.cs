using FunctionalProgramming.Grpc;
using Grpc.Core;

namespace FunctionalProgramming.Services.Grpc;

public sealed class ApiCatalogV1GrpcService : ApiCatalogV1.ApiCatalogV1Base
{
    public override Task<ListApiCatalogEntriesV1Response> ListCatalogEntries(
        ListApiCatalogEntriesV1Request request,
        ServerCallContext context)
    {
        ApiCatalogContract.EnsureValidRequest(request);

        var response = new ListApiCatalogEntriesV1Response
        {
            Metadata = ApiCatalogContract.CreateV1Metadata()
        };

        response.CatalogEntries.AddRange(
        [
            new ApiCatalogEntryV1
            {
                EntryKind = ApiCatalogEntryKindV1.Rest,
                DisplayName = "REST",
                IntegrationUseCase = "Resource CRUD over HTTP"
            },
            new ApiCatalogEntryV1
            {
                EntryKind = ApiCatalogEntryKindV1.Soap,
                DisplayName = "SOAP",
                IntegrationUseCase = "Contract-first enterprise integration"
            },
            new ApiCatalogEntryV1
            {
                EntryKind = ApiCatalogEntryKindV1.Grpc,
                DisplayName = "gRPC",
                IntegrationUseCase = "Fast binary service-to-service calls"
            },
            new ApiCatalogEntryV1
            {
                EntryKind = ApiCatalogEntryKindV1.Graphql,
                DisplayName = "GraphQL",
                IntegrationUseCase = "Flexible client-driven queries"
            },
            new ApiCatalogEntryV1
            {
                EntryKind = ApiCatalogEntryKindV1.Webhook,
                DisplayName = "Webhook",
                IntegrationUseCase = "Server push notifications"
            },
            new ApiCatalogEntryV1
            {
                EntryKind = ApiCatalogEntryKindV1.Websocket,
                DisplayName = "WebSocket",
                IntegrationUseCase = "Realtime two-way communication"
            }
        ]);

        ApiCatalogContract.EnsureValidReply(response);

        return Task.FromResult(response);
    }
}

public sealed class ApiCatalogV2GrpcService : ApiCatalogV2.ApiCatalogV2Base
{
    public override Task<ListApiCatalogEntriesV2Response> ListCatalogEntries(
        ListApiCatalogEntriesV2Request request,
        ServerCallContext context)
    {
        ApiCatalogContract.EnsureValidRequest(request);

        var response = new ListApiCatalogEntriesV2Response
        {
            Metadata = ApiCatalogContract.CreateV2Metadata()
        };

        response.CatalogEntries.AddRange(
        [
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Rest,
                DisplayName = "REST",
                IntegrationUseCase = "Resource CRUD over HTTP",
                LifecycleStatus = "stable"
            },
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Soap,
                DisplayName = "SOAP",
                IntegrationUseCase = "Contract-first enterprise integration",
                LifecycleStatus = "stable"
            },
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Grpc,
                DisplayName = "gRPC",
                IntegrationUseCase = "Fast binary service-to-service calls",
                LifecycleStatus = "stable"
            },
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Graphql,
                DisplayName = "GraphQL",
                IntegrationUseCase = "Flexible client-driven queries",
                LifecycleStatus = "stable"
            },
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Webhook,
                DisplayName = "Webhook",
                IntegrationUseCase = "Server push notifications",
                LifecycleStatus = "stable"
            },
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Websocket,
                DisplayName = "WebSocket",
                IntegrationUseCase = "Realtime two-way communication",
                LifecycleStatus = "stable"
            },
            new ApiCatalogEntryV2
            {
                EntryKind = ApiCatalogEntryKindV2.Asyncapi,
                DisplayName = "AsyncAPI",
                IntegrationUseCase = "Event driven schema governance",
                LifecycleStatus = "emerging"
            }
        ]);

        ApiCatalogContract.EnsureValidReply(response);

        return Task.FromResult(response);
    }
}
