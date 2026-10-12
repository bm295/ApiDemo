namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class GrpcDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "grpc";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "gRPC: Unary & Server Streaming";
        model.Description = "gRPC is contract-first: define versioned services, operation-specific request and response types, governed values, and field-level semantics in the .proto schema, then generate the integration code directly from that contract.";
        model.IsGrpcDemo = true;
        model.GrpcHighPerformanceNote = "The catalog v1/v2 contracts are build-verified. `dotnet build` runs a descriptor-based verifier that fails the build if their generated service names, RPC names, message types, enum values, or strict field rules drift from the governed schema.";
        model.GrpcRequestExample = """
Documented strict contract (.proto):

// Version 1 of the API catalog. This endpoint preserves the original governed value set.
service ApiCatalogV1 {
  // Lists the API catalog entries accepted by api-catalog.v1 integrations.
  rpc ListCatalogEntries (ListApiCatalogEntriesV1Request) returns (ListApiCatalogEntriesV1Response);
}

// Version 2 of the API catalog. This endpoint evolves the domain while v1 remains available.
service ApiCatalogV2 {
  // Lists API catalog entries accepted by api-catalog.v2 integrations, including lifecycle metadata.
  rpc ListCatalogEntries (ListApiCatalogEntriesV2Request) returns (ListApiCatalogEntriesV2Response);
}

message ListApiCatalogEntriesV2Request {
  string contract_version = 1 [
    (strict_required) = true,
    (strict_string_pattern) = "^api-catalog\\.v2$"
  ];

  string requesting_client_name = 2 [
    (strict_required) = true,
    (strict_string_pattern) = "^[A-Za-z][A-Za-z0-9-]{2,30}$"
  ];
}

message ApiCatalogEntryV2 {
  ApiCatalogEntryKindV2 entry_kind = 1 [(strict_required) = true];
  string display_name = 2 [(strict_required) = true];
  string integration_use_case = 3 [(strict_required) = true];
  string lifecycle_status = 4 [(strict_required) = true];
}

Generated v1 client call:
var v1Response = await v1Client.ListCatalogEntriesAsync(new ListApiCatalogEntriesV1Request
{
    ContractVersion = "api-catalog.v1"
});

Generated v2 client call:
var v2Response = await v2Client.ListCatalogEntriesAsync(new ListApiCatalogEntriesV2Request
{
    ContractVersion = "api-catalog.v2",
    RequestingClientName = "demo-client"
});

Build-time verification:
dotnet build
// MSBuild runs: ApiDemo.dll --verify-grpc-contract
""";

        model.GrpcResponseExample = """
V1 response stays narrow and explicit:
{
  "metadata": {
    "contractVersion": "api-catalog.v1",
    "schemaName": "api_catalog.ApiCatalogV1",
    "owner": "platform-contracts",
    "supportedContractVersions": [ "api-catalog.v1" ]
  },
  "catalogEntries": [
    {
      "entryKind": "API_CATALOG_ENTRY_KIND_V1_REST",
      "displayName": "REST",
      "integrationUseCase": "Resource CRUD over HTTP"
    }
  ]
}

V2 adds fields whose names explain the intent directly:
{
  "metadata": {
    "contractVersion": "api-catalog.v2",
    "schemaName": "api_catalog.ApiCatalogV2",
    "owner": "platform-contracts",
    "supportedContractVersions": [ "api-catalog.v1", "api-catalog.v2" ]
  },
  "catalogEntries": [
    {
      "entryKind": "API_CATALOG_ENTRY_KIND_V2_GRPC",
      "displayName": "gRPC",
      "integrationUseCase": "Fast binary service-to-service calls",
      "lifecycleStatus": "stable"
    },
    {
      "entryKind": "API_CATALOG_ENTRY_KIND_V2_ASYNCAPI",
      "displayName": "AsyncAPI",
      "integrationUseCase": "Event driven schema governance",
      "lifecycleStatus": "emerging"
    }
  ]
}
""";
        model.Description = "gRPC defines typed contracts in .proto and sends Protobuf messages over HTTP/2. Unary calls return one response; server streaming returns multiple messages for one request. This demo includes a simulated report export with live progress updates.";
        model.GrpcRequestExample += """


Server streaming contract (Protos/report_stream.proto):
service ReportStreaming {
  rpc StreamReportProgress (ReportProgressRequest)
      returns (stream ReportProgressUpdate);
}

// .NET client: process each update as it arrives.
using var channel = GrpcChannel.ForAddress("http://localhost:5090");
var client = new ReportStreaming.ReportStreamingClient(channel);
using var cts = new CancellationTokenSource();
using var call = client.StreamReportProgress(
    new ReportProgressRequest { ReportName = "Monthly sales report" },
    deadline: DateTime.UtcNow.AddSeconds(30),
    cancellationToken: cts.Token);
await foreach (var update in call.ResponseStream.ReadAllAsync(cts.Token))
{
    Console.WriteLine($"{update.PercentComplete}% - {update.Status}");
}
// cts.Cancel() stops the call and cancels server work.
""";
        model.Example = string.Empty;
    }
}
