using FunctionalProgramming.Agents;
using FunctionalProgramming.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FunctionalProgramming.Services.Agents;

public sealed class EfMessageInsightReader(ApiDemoDbContext db) : IMessageInsightReader
{
    public async Task<MessageToolResult> ReadAsync(string query, int limit, CancellationToken cancellationToken)
    {
        // Existing EF global query filter enforces the authenticated retailer boundary.
        // Contains is translated with parameters; model output never becomes executable SQL.
        var matches = db.Messages.AsNoTracking().Where(x => x.Text.Contains(query));
        var count = await matches.CountAsync(cancellationToken);
        var evidence = await matches.OrderByDescending(x => x.Id).Take(limit)
            .Select(x => new MessageEvidence(x.Id, x.Text.Substring(0, Math.Min(x.Text.Length, 300))))
            .ToArrayAsync(cancellationToken);
        return new MessageToolResult(count, evidence);
    }
}
