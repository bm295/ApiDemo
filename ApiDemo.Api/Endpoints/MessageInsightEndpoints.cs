using System.Diagnostics;
using System.Diagnostics.Metrics;
using FunctionalProgramming.Agents;

namespace FunctionalProgramming.Endpoints;

public static class MessageInsightEndpoints
{
    private static readonly ActivitySource Activities = new("ApiDemo.MessageInsight");
    private static readonly Meter Meter = new("ApiDemo.MessageInsight");
    private static readonly Counter<long> Runs = Meter.CreateCounter<long>("agent.runs");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("agent.duration", "ms");

    public static IEndpointRouteBuilder MapMessageInsightEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/agent/message-insights", async (InsightRequest request,
            MessageInsightAgent agent, HttpContext context, ILogger<MessageInsightAgent> logger) =>
        {
            using var activity = Activities.StartActivity("message-insight");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var watch = Stopwatch.StartNew();
            var outcome = "failed";
            try
            {
                var result = await agent.RunAsync(request, timeout.Token);
                outcome = "completed";
                logger.LogInformation("Message insight {TraceId} completed in {ElapsedMs}ms using {Mode}",
                    result.TraceId, result.ElapsedMs, result.Mode);
                return Results.Ok(result);
            }
            catch (ArgumentException ex) { outcome = "invalid-request"; return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidInsightPlanException) { outcome = "invalid-plan"; return Results.Problem("Planner returned an unsupported or invalid plan.", statusCode: 502); }
            catch (HttpRequestException) { outcome = "provider-error"; return Results.Problem("Planner provider is unavailable.", statusCode: 502); }
            catch (InvalidOperationException) { outcome = "configuration-error"; return Results.Problem("Agent is unavailable; check server configuration.", statusCode: 503); }
            catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
            { outcome = "timeout"; return Results.Problem("Agent exceeded its execution deadline.", statusCode: 504); }
            finally
            {
                Runs.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
                Duration.Record(watch.Elapsed.TotalMilliseconds);
                activity?.SetTag("agent.outcome", outcome);
                // Deliberately omit goals, message content, keys and provider bodies from logs.
                if (outcome != "completed") logger.LogWarning("Message insight finished with {Outcome}", outcome);
            }
        }).RequireAuthorization("RetailerAccess").WithName("MessageInsight");
        return app;
    }
}
