namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class GrpcDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "grpc";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "gRPC Demo";
        model.Description = "gRPC is contract-first: define service and message schema in a .proto file first, then generate server/client code from that contract.";
        model.IsGrpcDemo = true;
        model.GrpcHighPerformanceNote = "gRPC has native streaming over HTTP/2: client-streaming, server-streaming, and bidirectional streaming are first-class RPC patterns in the contract.";
        model.GrpcRequestExample = """
Contract (.proto) first:

syntax = "proto3";
service ApiCatalog {
  rpc GetApiTypes (ApiRequest) returns (ApiReply);
  rpc UploadApiEvents (stream ApiEvent) returns (UploadSummary);          // client streaming
  rpc WatchApiChanges (ApiRequest) returns (stream ApiChange);            // server streaming
  rpc ChatApiStatus (stream ApiStatusRequest) returns (stream ApiStatus); // bidirectional streaming
}
message ApiRequest {}
message ApiReply {
  repeated ApiType items = 1;
}
message ApiType {
  string name = 1;
  string use_case = 2;
}

Generated client call (concept):
var reply = await client.GetApiTypesAsync(new ApiRequest());

Streaming calls (concept):
using var upload = client.UploadApiEvents();
await upload.RequestStream.WriteAsync(new ApiEvent { Name = "REST" });
await upload.RequestStream.CompleteAsync();
var uploadSummary = await upload.ResponseAsync;

using var watch = client.WatchApiChanges(new ApiRequest());
await foreach (var change in watch.ResponseStream.ReadAllAsync()) { /* receive stream */ }
""";

        model.GrpcResponseExample = """
ApiReply (decoded from Protobuf):
{
  "items": [
    { "name": "REST", "use_case": "Resource CRUD over HTTP" },
    { "name": "SOAP", "use_case": "Contract-first enterprise integration" },
    { "name": "gRPC", "use_case": "Fast binary service-to-service calls" },
    { "name": "GraphQL", "use_case": "Flexible client-driven queries" },
    { "name": "Webhook", "use_case": "Server push notifications" },
    { "name": "WebSocket", "use_case": "Realtime two-way communication" }
  ]
}

Streaming response shape (concept):
- UploadApiEvents -> single UploadSummary after client stream completes.
- WatchApiChanges -> multiple ApiChange messages over time.
- ChatApiStatus -> both sides continuously send/receive ApiStatus messages on one connection.
""";
        model.Example = string.Empty;
    }
}
