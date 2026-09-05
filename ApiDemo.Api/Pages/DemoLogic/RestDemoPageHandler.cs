namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class RestDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "rest";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "REST Demo";
        model.Description = "This REST demo uses a loose contract that can evolve additively: older clients can keep sending flat JSON, newer clients can use a data/attributes envelope, and callers can opt into richer response views without changing the resource URI.";
        model.Example = string.Empty;
        model.IsRestDemo = true;

        model.RestStatelessNote = "REST is stateless: the server does not remember client state between requests. Each request carries its own representation, so the implementation can add fields or alternate shapes while clients keep using the parts they understand.";
        model.RestUniformInterfaceNote = "Loose REST contracts stay evolvable by keeping URI and method semantics stable while treating JSON as a tolerant representation. Clients can ignore new fields, preserve extension fields, or request a richer view when they are ready.";
        model.RestUniformInterfaceExample = """
Resource URI: /api/messages/1

GET /api/messages/1                -> Read the flat representation
GET /api/messages/1?view=expanded  -> Read a richer evolved representation
PUT /api/messages/1                -> Send either flat JSON or a data/attributes envelope
DELETE /api/messages/1             -> Remove the resource
""";

        if (method == "GET")
        {
            model.RestRequestExample = """
GET /api/messages?view=expanded HTTP/1.1
Host: localhost:5000
Accept: application/json
""";

            model.RestResponseExample = """
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "items": [
    {
      "data": {
        "id": 1,
        "text": "Hello from REST",
        "createdAt": "2026-05-03T08:00:00Z"
      },
      "attributes": {
        "audience": "general",
        "channel": "demo"
      },
      "_links": {
        "self": "/api/messages/1",
        "collection": "/api/messages"
      },
      "_meta": {
        "representation": "expanded",
        "acceptedTextFields": ["text", "message", "content", "body"],
        "acceptedPayloadShapes": ["plain-string", "flat-object", "data-attributes-envelope"],
        "evolutionPolicy": "additive-fields-and-alt-shapes"
      }
    }
  ],
  "_links": {
    "self": "/api/messages?view=expanded",
    "collection": "/api/messages"
  },
  "_meta": {
    "representation": "expanded",
    "acceptedTextFields": ["text", "message", "content", "body"],
    "acceptedPayloadShapes": ["plain-string", "flat-object", "data-attributes-envelope"],
    "evolutionPolicy": "additive-fields-and-alt-shapes"
  }
}
""";
            return;
        }

        if (method == "POST")
        {
            model.RestRequestExample = """
POST /api/messages?view=expanded HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
  "data": {
    "message": "hello"
  },
  "attributes": {
    "priority": "low",
    "sourceSystem": "mobile-app"
  },
  "meta": {
    "clientTraceId": "rest-123"
  }
}
""";

            model.RestResponseExample = """
HTTP/1.1 201 Created
Location: /api/messages/3
Content-Type: application/json; charset=utf-8

{
  "data": {
    "id": 3,
    "text": "hello",
    "createdAt": "2026-05-03T08:01:00Z"
  },
  "attributes": {
    "priority": "low",
    "sourceSystem": "mobile-app"
  },
  "_links": {
    "self": "/api/messages/3",
    "collection": "/api/messages"
  },
  "_meta": {
    "representation": "expanded",
    "acceptedTextFields": ["text", "message", "content", "body"],
    "acceptedPayloadShapes": ["plain-string", "flat-object", "data-attributes-envelope"],
    "evolutionPolicy": "additive-fields-and-alt-shapes"
  }
}
""";
            return;
        }

        model.RestRequestExample = """
PUT /api/messages/1?view=expanded HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
  "content": "updated text",
  "attributes": {
    "region": "apac",
    "deliveryMode": "async"
  }
}
""";

        model.RestResponseExample = """
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "data": {
    "id": 1,
    "text": "updated text",
    "createdAt": "2026-05-03T08:00:00Z"
  },
  "attributes": {
    "audience": "general",
    "channel": "demo",
    "region": "apac",
    "deliveryMode": "async"
  },
  "_links": {
    "self": "/api/messages/1",
    "collection": "/api/messages"
  },
  "_meta": {
    "representation": "expanded",
    "acceptedTextFields": ["text", "message", "content", "body"],
    "acceptedPayloadShapes": ["plain-string", "flat-object", "data-attributes-envelope"],
    "evolutionPolicy": "additive-fields-and-alt-shapes"
  }
}
""";
    }
}
