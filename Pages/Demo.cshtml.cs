using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunctionalProgramming.Pages;

public class DemoModel : PageModel
{
    public string Title { get; private set; } = "API type not found";
    public string Description { get; private set; } = "Choose an API type from the home page.";
    public string Example { get; private set; } = "";
    public bool IsRestDemo { get; private set; }
    public string SelectedMethod { get; private set; } = "GET";
    public string RestRequestExample { get; private set; } = "";
    public string RestResponseExample { get; private set; } = "";
    public string RestStatelessNote { get; private set; } = "";
    public string RestUniformInterfaceNote { get; private set; } = "";
    public string RestUniformInterfaceExample { get; private set; } = "";

    public void OnGet([FromRoute] string? id, [FromQuery] string? method)
    {
        var key = (id ?? string.Empty).ToLowerInvariant();
        var selectedMethod = (method ?? "GET").ToUpperInvariant();
        if (selectedMethod is not ("GET" or "POST"))
        {
            selectedMethod = "GET";
        }

        SelectedMethod = selectedMethod;
        (Title, Description, Example) = key switch
        {
            "rest" => ("REST Demo", "Try GET/POST JSON endpoints.", ""),
            "soap" => ("SOAP Demo", "Post XML SOAP envelope.", "POST /api/soap\nContent-Type: text/xml"),
            "grpc" => ("gRPC Demo", "This sample focuses on protocol comparison. In production, define .proto contracts and HTTP/2 service endpoints.", "Example concept: rpc GetApiTypes (ApiRequest) returns (ApiReply);"),
            "graphql" => ("GraphQL Demo", "Send GraphQL-like query payload.", "POST /api/graphql { \"query\":\"{ apiTypes { name useCase } }\" }"),
            "webhook" => ("Webhook Demo", "Simulate a third-party callback.", "POST /api/webhook/orders { \"orderId\":123,\"status\":\"paid\" }"),
            "websocket" => ("WebSocket Demo", "Connect to bi-directional endpoint and send messages.", "Connect ws://localhost:5000/ws and send text frames."),
            _ => (Title, Description, Example)
        };

        IsRestDemo = key == "rest";
        if (!IsRestDemo)
        {
            return;
        }

        RestStatelessNote = "REST is stateless: the server does not remember client state between requests. Each request must include everything needed to process it (auth token, resource id, payload).";
        RestUniformInterfaceNote = "Uniform Interface means clients use consistent HTTP semantics on resources. The same URI works with different verbs to represent different actions.";
        RestUniformInterfaceExample = """
Resource URI: /api/messages/1

GET /api/messages/1      -> Read resource
PUT /api/messages/1      -> Replace/update resource
DELETE /api/messages/1   -> Remove resource
""";

        if (SelectedMethod == "GET")
        {
            RestRequestExample = """
GET /api/messages HTTP/1.1
Host: localhost:5000
Accept: application/json
""";

            RestResponseExample = """
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

[
  { "id": 1, "text": "Hello from REST", "createdAt": "2026-05-03T08:00:00Z" },
  { "id": 2, "text": "REST returns JSON", "createdAt": "2026-05-03T08:00:00Z" }
]
""";
            return;
        }

        RestRequestExample = """
POST /api/messages HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{ "text": "hello" }
""";

        RestResponseExample = """
HTTP/1.1 201 Created
Location: /api/messages/3
Content-Type: application/json; charset=utf-8

{ "id": 3, "text": "hello", "createdAt": "2026-05-03T08:01:00Z" }
""";
    }
}
