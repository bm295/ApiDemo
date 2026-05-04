namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class RestDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "rest";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "REST Demo";
        model.Description = "Try GET/POST JSON endpoints.";
        model.Example = string.Empty;
        model.IsRestDemo = true;

        model.RestStatelessNote = "REST is stateless: the server does not remember client state between requests. Each request must include everything needed to process it (auth token, resource id, payload).";
        model.RestUniformInterfaceNote = "Uniform Interface means clients use consistent HTTP semantics on resources. The same URI works with different verbs to represent different actions.";
        model.RestUniformInterfaceExample = """
Resource URI: /api/messages/1

GET /api/messages/1      -> Read resource
PUT /api/messages/1      -> Replace/update resource
DELETE /api/messages/1   -> Remove resource
""";

        if (method == "GET")
        {
            model.RestRequestExample = """
GET /api/messages HTTP/1.1
Host: localhost:5000
Accept: application/json
""";

            model.RestResponseExample = """
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

[
  { "id": 1, "text": "Hello from REST", "createdAt": "2026-05-03T08:00:00Z" },
  { "id": 2, "text": "REST returns JSON", "createdAt": "2026-05-03T08:00:00Z" }
]
""";
            return;
        }

        model.RestRequestExample = """
POST /api/messages HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{ "text": "hello" }
""";

        model.RestResponseExample = """
HTTP/1.1 201 Created
Location: /api/messages/3
Content-Type: application/json; charset=utf-8

{ "id": 3, "text": "hello", "createdAt": "2026-05-03T08:01:00Z" }
""";
    }
}
