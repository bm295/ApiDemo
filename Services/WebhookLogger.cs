using System.Text;

namespace FunctionalProgramming.Services;

public interface IWebhookLogger
{
    Task LogOrderPayloadAsync(string payload, CancellationToken cancellationToken = default);
}

public sealed class WebhookLogger : IWebhookLogger
{
    private const string LogPath = "webhook.log";

    public Task LogOrderPayloadAsync(string payload, CancellationToken cancellationToken = default)
    {
        var log = $"[{DateTime.UtcNow:O}] Webhook payload: {payload}{Environment.NewLine}";
        return File.AppendAllTextAsync(LogPath, log, Encoding.UTF8, cancellationToken);
    }
}
