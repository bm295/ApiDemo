namespace FunctionalProgramming.Models;

public record ApiMessage(int Id, string Text, DateTime CreatedUtc);

public record ApiMessageInput(string Text);

public class GraphQlRequest
{
    public string? Query { get; set; }
}
