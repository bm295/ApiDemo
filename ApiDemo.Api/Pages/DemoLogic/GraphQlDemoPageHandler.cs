namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class GraphQlDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "graphql";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "GraphQL Demo";
        model.Description = "GraphQL is query-based: client asks for exact fields and gets only those fields.";
        model.Example = string.Empty;
        model.IsGraphQlDemo = true;

        if (example == "1")
        {
            model.GraphQlRequestExample = """
POST /api/graphql HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{ "query":"{ __type(name: \"ApiType\") { name fields { name type { kind name ofType { name kind } } } } }" }
""";

            model.GraphQlResponseExample = """
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "data": {
    "__type": {
      "name": "ApiType",
      "fields": [
        { "name": "name", "type": { "kind": "SCALAR", "name": "String", "ofType": null } },
        { "name": "useCase", "type": { "kind": "SCALAR", "name": "String", "ofType": null } }
      ]
    }
  }
}
""";
            return;
        }

        model.GraphQlRequestExample = """
POST /api/graphql HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{ "query":"{ apiTypes { name description } }" }
""";

        model.GraphQlResponseExample = """
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8

{
  "errors": [
    {
      "message": "Cannot query field \"description\" on type \"ApiType\".",
      "locations": [
        { "line": 1, "column": 19 }
      ],
      "extensions": {
        "code": "GRAPHQL_VALIDATION_FAILED"
      }
    }
  ],
  "data": {
    "apiTypes": [
      { "name": "REST" },
      { "name": "SOAP" },
      { "name": "gRPC" },
      { "name": "GraphQL" },
      { "name": "Webhook" },
      { "name": "WebSocket" }
    ]
  }
}
""";
    }
}
