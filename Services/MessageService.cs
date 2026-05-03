using FunctionalProgramming.Models;

namespace FunctionalProgramming.Services;

public interface IMessageService
{
    IReadOnlyList<ApiMessage> GetAll();
    ApiMessage? GetById(int id);
    ApiMessage Add(string text);
    ApiMessage? Update(int id, string text);
    bool Delete(int id);
}

public sealed class MessageService : IMessageService
{
    private readonly List<ApiMessage> _messages =
    [
        new(1, "Hello from REST", DateTime.UtcNow),
        new(2, "REST returns JSON", DateTime.UtcNow)
    ];

    public IReadOnlyList<ApiMessage> GetAll() => _messages;
    public ApiMessage? GetById(int id) => _messages.FirstOrDefault(x => x.Id == id);

    public ApiMessage Add(string text)
    {
        var message = new ApiMessage(_messages.Count + 1, text, DateTime.UtcNow);
        _messages.Add(message);
        return message;
    }

    public ApiMessage? Update(int id, string text)
    {
        var index = _messages.FindIndex(x => x.Id == id);
        if (index < 0)
        {
            return null;
        }

        var updated = _messages[index] with { Text = text };
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
}
