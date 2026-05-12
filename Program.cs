using FunctionalProgramming.Services.Grpc;
using FunctionalProgramming.Endpoints;
using FunctionalProgramming.Services;
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

    options.ListenLocalhost(httpPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });

    options.ListenLocalhost(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddRazorPages();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<IWebhookLogger, WebhookLogger>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets();
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
