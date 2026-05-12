using FunctionalProgramming.Models;
using System.Text.Json.Nodes;

namespace FunctionalProgramming.Services;

public interface IMessageService
{
    IReadOnlyList<ApiMessage> GetAll();
    ApiMessage? GetById(int id);
    ApiMessage Add(string text, JsonObject attributes);
    ApiMessage? Update(int id, string? text, JsonObject attributes);
    bool Delete(int id);
}

public sealed class MessageService : IMessageService
{
    private readonly List<ApiMessage> _messages =
    [
        new(1, "Hello from REST", DateTime.UtcNow, new JsonObject
        {
            ["audience"] = "general",
            ["channel"] = "demo"
        }),
        new(2, "REST returns JSON", DateTime.UtcNow, new JsonObject
        {
            ["format"] = "json",
            ["clientHint"] = "tolerant-reader"
        })
    ];

    public IReadOnlyList<ApiMessage> GetAll() => _messages;
    public ApiMessage? GetById(int id) => _messages.FirstOrDefault(x => x.Id == id);

    public ApiMessage Add(string text, JsonObject attributes)
    {
        var message = new ApiMessage(_messages.Count + 1, text, DateTime.UtcNow, CloneAttributes(attributes));
        _messages.Add(message);
        return message;
    }

    public ApiMessage? Update(int id, string? text, JsonObject attributes)
    {
        var index = _messages.FindIndex(x => x.Id == id);
        if (index < 0)
        {
            return null;
        }

        var existing = _messages[index];
        var mergedAttributes = CloneAttributes(existing.Attributes);
        MergeAttributes(mergedAttributes, attributes);

        var updated = existing with
        {
            Text = string.IsNullOrWhiteSpace(text) ? existing.Text : text,
            Attributes = mergedAttributes
        };
        _messages[index] = updated;
        return updated;
    }

    public bool Delete(int id)
    {
        var index = _messages.FindIndex(x => x.Id == id);
        if (index < 0)
        {
            return false;
        }

        _messages.RemoveAt(index);
        return true;
    }

    private static JsonObject CloneAttributes(JsonObject attributes)
    {
        var clone = new JsonObject();
        foreach (var property in attributes)
        {
            clone[property.Key] = property.Value?.DeepClone();
        }

        return clone;
    }

    private static void MergeAttributes(JsonObject target, JsonObject source)
    {
        foreach (var property in source)
        {
            target[property.Key] = property.Value?.DeepClone();
        }
    }
}
