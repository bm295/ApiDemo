using System.Net;
using FunctionalProgramming.Grpc;
using FunctionalProgramming.Services.Grpc;
using FunctionalProgramming.Endpoints.ApiTypes;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ApiDemo.Tests;

public sealed class GrpcStreamingTests
{
    [Fact]
    public async Task ReportStream_SendsIncrementalProgress_ValidatesAndCancelsCalls()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        await using var app = builder.Build();
        app.MapGrpcService<ReportStreamingGrpcService>();
        new GrpcApiDemoEndpoints().Map(app.MapGroup("/api"));
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var channel = GrpcChannel.ForAddress(address);
            var client = new ReportStreaming.ReportStreamingClient(channel);
            using var call = client.StreamReportProgress(new ReportProgressRequest { ReportName = "Sales" },
                deadline: DateTime.UtcNow.AddSeconds(15));
            // The initial message arrives before work finishes.
            Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
            Assert.Equal(0, call.ResponseStream.Current.PercentComplete);
            Assert.False(call.ResponseStream.Current.Completed);
            var updates = new List<ReportProgressUpdate> { call.ResponseStream.Current };
            await foreach (var update in call.ResponseStream.ReadAllAsync()) updates.Add(update);
            Assert.Equal(new[] { 0, 25, 50, 75, 100 }, updates.Select(update => update.PercentComplete));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, updates.Select(update => update.Sequence));
            Assert.All(updates, update => Assert.Equal("Sales", update.ReportName));
            Assert.True(updates[^1].Completed);

            using var invalid = client.StreamReportProgress(new ReportProgressRequest());
            var invalidError = await Assert.ThrowsAsync<RpcException>(() =>
                invalid.ResponseStream.MoveNext(CancellationToken.None));
            Assert.Equal(StatusCode.InvalidArgument, invalidError.StatusCode);

            using var cancellation = new CancellationTokenSource();
            using var cancelled = client.StreamReportProgress(new ReportProgressRequest { ReportName = "Sales" },
                cancellationToken: cancellation.Token);
            Assert.True(await cancelled.ResponseStream.MoveNext(CancellationToken.None));
            cancellation.Cancel();
            var cancellationError = await Assert.ThrowsAsync<RpcException>(() =>
                cancelled.ResponseStream.MoveNext(CancellationToken.None));
            Assert.Equal(StatusCode.Cancelled, cancellationError.StatusCode);

            // Exercise the browser bridge and verify that headers and the first
            // JSON line are available while later updates are still pending.
            app.Configuration["ApiDemo:GrpcAddress"] = address;
            using var http = new HttpClient { DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact };
            using var bridgeCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var bridge = await http.GetAsync(address + "/api/grpc/stream",
                HttpCompletionOption.ResponseHeadersRead, bridgeCancellation.Token);
            bridge.EnsureSuccessStatusCode();
            Assert.Equal("application/x-ndjson", bridge.Content.Headers.ContentType!.MediaType);
            using var reader = new StreamReader(await bridge.Content.ReadAsStreamAsync());
            using var first = JsonDocument.Parse((await reader.ReadLineAsync(bridgeCancellation.Token))!);
            Assert.Equal(1, first.RootElement.GetProperty("sequence").GetInt32());
            Assert.False(first.RootElement.TryGetProperty("completed", out var earlyCompleted)
                && earlyCompleted.GetBoolean());
            var lineCount = 1;
            var bridgeCompleted = false;
            while (await reader.ReadLineAsync(bridgeCancellation.Token) is { } line)
            {
                using var message = JsonDocument.Parse(line);
                Assert.False(message.RootElement.TryGetProperty("error", out _));
                bridgeCompleted = message.RootElement.TryGetProperty("completed", out var done) && done.GetBoolean();
                lineCount++;
            }
            Assert.Equal(5, lineCount);
            Assert.True(bridgeCompleted);
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
