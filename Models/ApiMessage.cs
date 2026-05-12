using System.Text.Json.Nodes;

namespace FunctionalProgramming.Models;

public sealed record ApiMessage(int Id, string Text, DateTime CreatedUtc, JsonObject Attributes);

public class GraphQlRequest
{
    public string? Query { get; set; }
}
