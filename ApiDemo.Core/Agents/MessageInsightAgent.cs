using System.Diagnostics;

namespace FunctionalProgramming.Agents;

public sealed record InsightRequest(string Goal, int Limit = 10);
public sealed record InsightPlan(string Tool, string Query);
public sealed record MessageEvidence(int Id, string Excerpt);
public sealed record MessageToolResult(int MatchCount, IReadOnlyList<MessageEvidence> Evidence);
public sealed record InsightStep(string Name, string Status);
public sealed record InsightResult(string TraceId, string Mode, string Answer, int MatchCount,
    IReadOnlyList<MessageEvidence> Evidence, IReadOnlyList<InsightStep> Steps, double ElapsedMs);

public interface IInsightPlanner
{
    string Mode { get; }
    Task<InsightPlan> PlanAsync(string goal, CancellationToken cancellationToken);
}

// Implementations must obtain retailer scope from trusted server-side identity.
// No tenant ID, SQL, arbitrary URL or write operation is accepted from a model.
public interface IMessageInsightReader
{
    Task<MessageToolResult> ReadAsync(string query, int limit, CancellationToken cancellationToken);
}

public sealed class InvalidInsightPlanException(string message) : Exception(message);

public sealed class MessageInsightAgent(IInsightPlanner planner, IMessageInsightReader reader)
{
    public async Task<InsightResult> RunAsync(InsightRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goal) || request.Goal.Length > 1000 || request.Limit is < 1 or > 20)
            throw new ArgumentException("Goal must contain 1–1000 characters; limit must be 1–20.");

        var watch = Stopwatch.StartNew();
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var plan = await planner.PlanAsync(request.Goal, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (plan is null || plan.Tool is not ("messages.search" or "messages.count") ||
            plan.Query is null || plan.Query.Length > 100)
            throw new InvalidInsightPlanException("Planner returned an unsupported tool or query.");

        // Exactly one allowlisted read, followed by deterministic validation/output.
        var result = await reader.ReadAsync(plan.Query, request.Limit, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var evidence = plan.Tool == "messages.count" ? Array.Empty<MessageEvidence>() : result.Evidence;
        var answer = plan.Tool == "messages.count"
            ? $"Found {result.MatchCount} matching messages in your retailer."
            : $"Found {result.MatchCount} matching messages in your retailer; showing {evidence.Count} excerpts.";
        return new InsightResult(traceId, planner.Mode, answer, result.MatchCount, evidence,
            [new("plan", "completed"), new("validate-tool", "completed"),
             new(plan.Tool, "completed"), new("compose-grounded-result", "completed")], watch.Elapsed.TotalMilliseconds);
    }
}

// Offline fixture: explicitly selected syntax, not an LLM or a claim of semantic understanding.
public sealed class DemoInsightPlanner : IInsightPlanner
{
    public string Mode => "demo";
    public Task<InsightPlan> PlanAsync(string goal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parts = goal.Trim().Split(':', 2);
        var tool = parts[0].Trim().ToLowerInvariant() switch
        {
            "search" => "messages.search",
            "count" => "messages.count",
            _ => throw new ArgumentException("Demo mode supports search:<text> or count:<text> (empty text matches all).")
        };
        return Task.FromResult(new InsightPlan(tool, parts.Length == 2 ? parts[1].Trim() : ""));
    }
}
