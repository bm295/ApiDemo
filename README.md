# API Demo Hub (C# ASP.NET Core, .NET 10)

This repository is rewritten as a C# web application that demonstrates multiple API styles:
- REST
- SOAP
- gRPC (versioned, self-documenting strict contract demo with protobuf descriptor validation)
- GraphQL
- Webhook
- WebSocket

## Run

```bash
dotnet restore
dotnet run --project ApiDemo.Api
```

This project targets `.NET 10`.

The default launch profile uses `Development`. If no JWT signing key is configured,
the app generates a temporary random key for that process so local demos can start.
To validate tokens from your issuer, set `Authentication__Jwt__SigningKey` to the
issuer's key. Other environments require an explicitly configured key.

Open http://localhost:5089 and choose an API type from the home page. The gRPC demo uses a separate local HTTP/2 endpoint at http://localhost:5090 for the real gRPC calls and exposes v1/v2 contract bridge routes.

## gRPC server streaming example

Open `/Demo/grpc` and click **Start streaming** to follow a simulated monthly
report export: five progress messages from 0% to 100%, about one second apart.
**Stop streaming** cancels the HTTP request and the underlying gRPC call.
This illustrates progress monitoring for long-running jobs; no report file is generated.

`Protos/report_stream.proto` defines `ReportStreaming.StreamReportProgress` with
`returns (stream ReportProgressUpdate)`. The service writes each update using
`IServerStreamWriter`; the .NET client reads updates using `ResponseStream.ReadAllAsync`.
See the [ASP.NET Core streaming documentation](https://learn.microsoft.com/en-us/aspnet/core/grpc/services?view=aspnetcore-10.0).

The browser calls `GET /api/grpc/stream`, an HTTP bridge that calls the real gRPC
service on port 5090 and flushes each update as NDJSON. The page includes a .NET
client example for calling the gRPC service directly. Existing unary v1/v2 catalog
examples remain available. The catalog build verifier covers the catalog contracts.

## Message API authentication

`/api/messages` uses JWT Bearer authentication and requires a `retailer_id` claim containing a UUID. Configure `Authentication__Jwt__SigningKey` as an environment secret; the issuer and audience default to `ApiDemo` and `ApiDemoClients` in `appsettings.json`. Tokens are expected from a trusted issuer—the application does not issue tokens from a caller-provided retailer ID.

## Run behind Envoy

The included Docker Compose topology starts two API instances and an Envoy load
balancer. Envoy exposes one public listener and routes requests by protocol:

- HTTP/1.1, Razor Pages, REST, SOAP, GraphQL, webhook, and WebSocket traffic goes
  to port `5089` on each API instance.
- Requests whose `content-type` starts with `application/grpc` go to the HTTP/2
  listener on port `5090`.
- WebSocket upgrades on `/ws` pass through the same public listener.

Start the topology with:

```bash
docker compose up --build
```

Open http://localhost:8080 for the application. Envoy's administration endpoint
is available only on the host loopback interface at http://localhost:9901. The
application instances are not published to the host.

The application exposes `GET /healthz` for Envoy's active HTTP health checks.
Compose sets `ApiDemo__BindAddress=0.0.0.0`, but it does not publish the backend
ports to the host. Envoy supplies `X-Forwarded-For` and `X-Forwarded-Proto`, and
Compose enables their processing with `ApiDemo__TrustForwardedHeaders=true`.
Without these settings, direct `dotnet run` keeps its loopback-only behavior and
does not trust proxy headers.

`dotnet build` also runs a strict gRPC contract verifier. The build fails if the generated service names, RPC signatures, message fields, enum values, or embedded strict metadata drift from the governed schema.

## Project Structure

The solution is split into three projects:

- `ApiDemo.Api` contains the ASP.NET Core host, API endpoint mappers, Razor Pages, WebSocket routes, persistence, and gRPC transport code.
- `ApiDemo.Core` contains the reusable domain model and service layer (`Models/ApiMessage.cs`, `Services/MessageService.cs`, and `Services/WebhookLogger.cs`).
- `ApiDemo.Tests` contains the xUnit tests for the core service behavior.

Run the complete test suite with:

```bash
dotnet test ApiDemo.sln
```

## Documentation

The `docs` folder contains supporting design notes and implementation guides for the application.
