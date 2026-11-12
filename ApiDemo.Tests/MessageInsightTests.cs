using System.Net;
using System.Text;
using Cqrs.RetailerIsolation;
using FunctionalProgramming.Agents;
using FunctionalProgramming.Infrastructure.Persistence;
using FunctionalProgramming.Services.Agents;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ApiDemo.Tests;

public sealed class MessageInsightTests
{
    [Fact]
    public async Task Reader_enforces_retailer_filter_and_bounds_context()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApiDemoDbContext>().UseSqlite(connection).Options;
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await using var db = new ApiDemoDbContext(options, new Retailer(a));
        await db.Database.EnsureCreatedAsync();
        db.Messages.AddRange(
            new MessageEntity { RetailerId = a, Text = "refund " + new string('x', 500), AttributesJson = "{}" },
            new MessageEntity { RetailerId = a, Text = "refund second", AttributesJson = "{}" },
            new MessageEntity { RetailerId = b, Text = "refund SECRET", AttributesJson = "{}" });
        await db.SaveChangesAsync();
        var reader = new EfMessageInsightReader(db);
        var result = await reader.ReadAsync("refund", 1, default);
        Assert.Equal(2, result.MatchCount);
        Assert.Single(result.Evidence);
        Assert.DoesNotContain("SECRET", result.Evidence[0].Excerpt);
        var all = await reader.ReadAsync("", 20, default);
        Assert.All(all.Evidence, x => Assert.True(x.Excerpt.Length <= 300));
        // Literal query input is parameterized, not executed as SQL.
        Assert.Equal(0, (await reader.ReadAsync("' OR 1=1 --", 20, default)).MatchCount);
    }

    [Theory]
    [InlineData("messages.delete", "")]
    [InlineData("other-retailer.search", "")]
    [InlineData("messages.search", null)]
    public async Task Unsupported_plan_never_calls_reader(string tool, string? query)
    {
        var reader = new Reader();
        var agent = new MessageInsightAgent(new Planner(new(tool, query!)), reader);
        await Assert.ThrowsAsync<InvalidInsightPlanException>(() => agent.RunAsync(new("analyse"), default));
        Assert.Equal(0, reader.Calls);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("search", 0)]
    [InlineData("search", 21)]
    public async Task Invalid_requests_are_rejected_before_planning(string goal, int limit)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new MessageInsightAgent(new DemoInsightPlanner(), new Reader())
            .RunAsync(new(goal, limit), default));
    }

    [Fact]
    public async Task Demo_count_is_grounded_and_excludes_excerpts()
    {
        var reader = new Reader();
        var result = await new MessageInsightAgent(new DemoInsightPlanner(), reader).RunAsync(new("count:refund"), default);
        Assert.Equal("refund", reader.Query);
        Assert.Equal("demo", result.Mode);
        Assert.Equal(2, result.MatchCount);
        Assert.Empty(result.Evidence);
        Assert.Equal(4, result.Steps.Count);
        Assert.False(string.IsNullOrWhiteSpace(result.TraceId));
    }

    [Fact]
    public async Task Cancellation_prevents_execution()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var reader = new Reader();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new MessageInsightAgent(new DemoInsightPlanner(), reader)
            .RunAsync(new("search:refund"), cts.Token));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Http_planner_parses_model_plan()
    {
        var handler = new Handler("{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"{\\\"tool\\\":\\\"messages.search\\\",\\\"query\\\":\\\"refund\\\"}\"}}]}");
        var plan = await CreatePlanner(handler).PlanAsync("Find refund messages", default);
        Assert.Equal("messages.search", plan.Tool);
        Assert.Equal("refund", plan.Query);
        Assert.Contains("Find refund messages", handler.Body);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("{\"choices\":[]}")]
    [InlineData("{\"choices\":[{\"finish_reason\":\"length\"}]}")]
    public async Task Malformed_model_responses_fail_closed(string response)
    {
        await Assert.ThrowsAsync<InvalidInsightPlanException>(() => CreatePlanner(new Handler(response)).PlanAsync("goal", default));
    }

    [Fact]
    public async Task Oversized_model_response_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidInsightPlanException>(() => CreatePlanner(new Handler(new string('x', 17000))).PlanAsync("goal", default));
    }

    private static HttpInsightPlanner CreatePlanner(Handler handler) => new(new HttpClient(handler),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Agent:Endpoint"] = "https://planner.example/chat/completions",
            ["Agent:Model"] = "test-model", ["Agent:ApiKey"] = "test-key"
        }).Build());

    private sealed record Retailer(Guid RetailerId) : IRetailerContext;
    private sealed class Planner(InsightPlan plan) : IInsightPlanner
    {
        public string Mode => "test";
        public Task<InsightPlan> PlanAsync(string goal, CancellationToken cancellationToken) => Task.FromResult(plan);
    }
    private sealed class Reader : IMessageInsightReader
    {
        public int Calls { get; private set; }
        public string? Query { get; private set; }
        public Task<MessageToolResult> ReadAsync(string query, int limit, CancellationToken cancellationToken)
        {
            Calls++; Query = query;
            return Task.FromResult(new MessageToolResult(2, [new(1, "refund")]));
        }
    }
    private sealed class Handler(string response) : HttpMessageHandler
    {
        public string Body { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
