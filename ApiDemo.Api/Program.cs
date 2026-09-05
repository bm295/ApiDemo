using FunctionalProgramming.Services.Grpc;
using FunctionalProgramming.Endpoints;
using FunctionalProgramming.Services;
using FunctionalProgramming.Services.Authentication;
using FunctionalProgramming.Infrastructure.Persistence;
using Cqrs.RetailerIsolation;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? throw new InvalidOperationException("Authentication:Jwt:Issuer is required.");
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? throw new InvalidOperationException("Authentication:Jwt:Audience is required.");
var jwtSigningKey = builder.Configuration["Authentication:Jwt:SigningKey"] ?? throw new InvalidOperationException("Authentication:Jwt:SigningKey is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorizationBuilder().AddPolicy("RetailerAccess", policy => policy
    .RequireAuthenticatedUser()
    .RequireAssertion(context => context.User.HasClaim(claim =>
        claim.Type == ClaimRetailerContext.RetailerIdClaimType && Guid.TryParse(claim.Value, out _))));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRetailerContext, ClaimRetailerContext>();
builder.Services.AddScoped<RetailerSaveChangesInterceptor>();
builder.Services.AddDbContext<ApiDemoDbContext>((serviceProvider, options) =>
{
    var databasePath = builder.Configuration["ApiDemo:DatabasePath"] ?? "api-demo.db";
    options.UseSqlite($"Data Source={databasePath}");
    options.AddInterceptors(serviceProvider.GetRequiredService<RetailerSaveChangesInterceptor>());
});
builder.Services.AddScoped<IMessageService, EfMessageService>();
builder.Services.AddSingleton<IWebhookLogger, WebhookLogger>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApiDemoDbContext>().Database.EnsureCreated();
}

if (builder.Configuration.GetValue<bool>("ApiDemo:TrustForwardedHeaders"))
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
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
app.UseAuthentication();
app.UseAuthorization();
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
