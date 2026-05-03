using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunctionalProgramming.Pages;

public class DemoModel : PageModel
{
    public string Title { get; private set; } = "API type not found";
    public string Description { get; private set; } = "Choose an API type from the home page.";
    public string Example { get; private set; } = "";

    public void OnGet([FromRoute] string? id)
    {
        var key = (id ?? string.Empty).ToLowerInvariant();
        (Title, Description, Example) = key switch
        {
            "rest" => ("REST Demo", "Try GET/POST JSON endpoints.", "GET /api/rest/messages\nPOST /api/rest/messages { \"text\":\"hello\" }"),
            "soap" => ("SOAP Demo", "Post XML SOAP envelope.", "POST /api/soap\nContent-Type: text/xml"),
            "grpc" => ("gRPC Demo", "This sample focuses on protocol comparison. In production, define .proto contracts and HTTP/2 service endpoints.", "Example concept: rpc GetApiTypes (ApiRequest) returns (ApiReply);"),
            "graphql" => ("GraphQL Demo", "Send GraphQL-like query payload.", "POST /api/graphql { \"query\":\"{ apiTypes { name useCase } }\" }"),
            "webhook" => ("Webhook Demo", "Simulate a third-party callback.", "POST /api/webhook/orders { \"orderId\":123,\"status\":\"paid\" }"),
            "websocket" => ("WebSocket Demo", "Connect to bi-directional endpoint and send messages.", "Connect ws://localhost:5000/ws and send text frames."),
            _ => (Title, Description, Example)
        };
    }
}
