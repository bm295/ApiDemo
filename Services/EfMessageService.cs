using System.Text.Json.Nodes;
using FunctionalProgramming.Infrastructure.Persistence;
using FunctionalProgramming.Models;
using Microsoft.EntityFrameworkCore;

namespace FunctionalProgramming.Services;

public sealed class EfMessageService(ApiDemoDbContext dbContext) : IMessageService
{
    public IReadOnlyList<ApiMessage> GetAll() => dbContext.Messages.AsNoTracking().OrderBy(x => x.Id).AsEnumerable().Select(ToModel).ToArray();

    public ApiMessage? GetById(int id)
    {
        var entity = dbContext.Messages.AsNoTracking().SingleOrDefault(x => x.Id == id);
        return entity is null ? null : ToModel(entity);
    }

    public ApiMessage Add(string text, JsonObject attributes)
    {
        var entity = new MessageEntity { Text = text, CreatedUtc = DateTime.UtcNow, AttributesJson = attributes.ToJsonString() };
        dbContext.Messages.Add(entity);
        dbContext.SaveChanges();
        return ToModel(entity);
    }

    public ApiMessage? Update(int id, string? text, JsonObject attributes)
    {
        var entity = dbContext.Messages.SingleOrDefault(x => x.Id == id);
        if (entity is null) return null;
        var mergedAttributes = DeserializeAttributes(entity.AttributesJson);
        MergeAttributes(mergedAttributes, attributes);
        entity.Text = string.IsNullOrWhiteSpace(text) ? entity.Text : text;
        entity.AttributesJson = mergedAttributes.ToJsonString();
        dbContext.SaveChanges();
        return ToModel(entity);
    }

    public bool Delete(int id)
    {
        var entity = dbContext.Messages.SingleOrDefault(x => x.Id == id);
        if (entity is null) return false;
        dbContext.Messages.Remove(entity);
        dbContext.SaveChanges();
        return true;
    }

    private static ApiMessage ToModel(MessageEntity entity) => new(entity.Id, entity.Text, entity.CreatedUtc, DeserializeAttributes(entity.AttributesJson));
    private static JsonObject DeserializeAttributes(string json) => JsonNode.Parse(json) as JsonObject ?? new JsonObject();

    private static void MergeAttributes(JsonObject target, JsonObject source)
    {
        foreach (var property in source) target[property.Key] = property.Value?.DeepClone();
    }
}
