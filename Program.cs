using FunctionalProgramming.Services.Grpc;
using FunctionalProgramming.Endpoints;
using FunctionalProgramming.Services;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;

if (args.Contains("--verify-grpc-contract", StringComparer.Ordinal))
{
    Environment.Exit(ApiCatalogBuildVerifier.Run());
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    var httpPort = TryReadPort(builder.Configuration["ApiDemo:HttpPort"], 5089);
    var grpcPort = TryReadPort(builder.Configuration["ApiDemo:GrpcPort"], 5090);
    var bindAddress = TryReadAddress(builder.Configuration["ApiDemo:BindAddress"]);

    options.Listen(bindAddress, httpPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });

    options.Listen(bindAddress, grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddRazorPages();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<IWebhookLogger, WebhookLogger>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("ApiDemo:TrustForwardedHeaders"))
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets();
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
app.MapGrpcService<FunctionalProgramming.Services.Grpc.ApiCatalogV1GrpcService>();
app.MapGrpcService<FunctionalProgramming.Services.Grpc.ApiCatalogV2GrpcService>();
app.MapApiEndpoints();
app.MapWebSocketEndpoints();
app.MapRazorPages();

app.Run();

static int TryReadPort(string? value, int fallback)
{
    return int.TryParse(value, out var port) ? port : fallback;
}

static IPAddress TryReadAddress(string? value)
{
    return IPAddress.TryParse(value, out var address) ? address : IPAddress.Loopback;
}
