# API Demo Hub (C# ASP.NET Core)

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
dotnet run
```

Open http://localhost:5089 and choose an API type from the home page. The gRPC demo uses a separate local HTTP/2 endpoint at http://localhost:5090 for the real gRPC calls and exposes v1/v2 contract bridge routes.

`dotnet build` also runs a strict gRPC contract verifier. The build fails if the generated service names, RPC signatures, message fields, enum values, or embedded strict metadata drift from the governed schema.
