using FunctionalProgramming.Models;
using FunctionalProgramming.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class RestApiDemoEndpoints : IApiDemoEndpointMapper
{
    private const string ExpandedView = "expanded";
    private static readonly string[] TextAliases = ["text", "message", "content", "body"];
    private static readonly string[] AcceptedPayloadShapes =
    [
        "plain-string",
        "flat-object",
        "data-attributes-envelope"
    ];
    private static readonly HashSet<string> ReservedAttributeFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "text",
        "createdAt",
        "_links",
        "_meta",
        "meta",
        "data",
        "attributes"
    };

    public void Map(RouteGroupBuilder api)
    {
        var messages = api.MapGroup("/messages").RequireAuthorization("RetailerAccess");

        messages.MapGet("/analytics/windows", (int m, decimal d, IMessageService service) =>
        {
            if (m <= 0)
            {
                return Results.BadRequest(new { error = "m must be greater than zero." });
            }

            var orderedMessages = service.GetAll()
                .OrderBy(message => message.CreatedUtc)
                .ThenBy(message => message.Id)
                .ToArray();

            var amounts = new decimal[orderedMessages.Length];
            for (var index = 0; index < orderedMessages.Length; index++)
            {
                if (!TryGetAmount(orderedMessages[index], out amounts[index]))
                {
                    return Results.UnprocessableEntity(new
                    {
                        error = "Every message in the analysis must have a numeric attributes.amount value.",
                        messageId = orderedMessages[index].Id
                    });
                }
            }

            var count = MessageWindowAnalytics.CountWindowsWithSum(amounts, m, d);
            return Results.Ok(new
            {
                windowLength = m,
                targetAmount = d,
                messageCount = orderedMessages.Length,
                matchingWindowCount = count
            });
        });

        messages.MapGet("", (HttpRequest request, IMessageService service) =>
        {
            return Results.Ok(BuildCollection(service.GetAll(), request));
        });

        messages.MapGet("/{id:int}", (HttpRequest request, int id, IMessageService service) =>
        {
            var message = service.GetById(id);
            return message is null
                ? Results.NotFound(new { error = "Message not found." })
                : Results.Ok(BuildResource(message, request));
        });

        messages.MapPost("", async (HttpContext context, IMessageService service) =>
        {
            var payload = await ReadPayloadAsync(context);
            if (payload.ErrorResult is not null)
            {
                return payload.ErrorResult;
            }

            if (string.IsNullOrWhiteSpace(payload.Text))
            {
                return Results.BadRequest(new
                {
                    error = "Provide message content using one of: text, message, content, or body."
                });
            }

            var message = service.Add(payload.Text, payload.Attributes);
            var resource = BuildResource(message, context.Request);

            return Results.Created($"/api/messages/{message.Id}", resource);
        });

        messages.MapPut("/{id:int}", async (HttpContext context, int id, IMessageService service) =>
        {
            var payload = await ReadPayloadAsync(context);
            if (payload.ErrorResult is not null)
            {
                return payload.ErrorResult;
            }

            if (string.IsNullOrWhiteSpace(payload.Text) && payload.Attributes.Count == 0)
            {
                return Results.BadRequest(new
                {
                    error = "Provide at least one known message field or an extension field to update."
                });
            }

            var message = service.Update(id, payload.Text, payload.Attributes);
            if (message is null)
            {
                return Results.NotFound(new { error = "Message not found." });
            }

            return Results.Ok(BuildResource(message, context.Request));
        });

        messages.MapDelete("/{id:int}", (int id, IMessageService service) =>
        {
            return service.Delete(id)
                ? Results.NoContent()
                : Results.NotFound(new { error = "Message not found." });
        });
    }

    private static async Task<RestPayload> ReadPayloadAsync(HttpContext context)
    {
        JsonNode? node;

        try
        {
            node = await JsonNode.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
        }
        catch (JsonException)
        {
            return new RestPayload(
                null,
                new JsonObject(),
                Results.BadRequest(new { error = "Body must be valid JSON." }));
        }

        if (node is null)
        {
            return new RestPayload(null, new JsonObject(), null);
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var scalarText))
        {
            return new RestPayload(scalarText, new JsonObject(), null);
        }

        if (node is not JsonObject jsonObject)
        {
            return new RestPayload(
                null,
                new JsonObject(),
                Results.BadRequest(new { error = "Body must be a JSON object or string." }));
        }

        var text = ExtractText(jsonObject);
        var attributes = ExtractAttributes(jsonObject);

        if (TryGetObject(jsonObject, "data", out var dataObject, out var dataError))
        {
            if (dataError is not null)
            {
                return new RestPayload(null, new JsonObject(), dataError);
            }

            if (dataObject is not null)
            {
                text ??= ExtractText(dataObject);
                MergeAttributes(attributes, ExtractAttributes(dataObject));
            }
        }

        if (TryGetObject(jsonObject, "attributes", out var attributesObject, out var attributesError))
        {
            if (attributesError is not null)
            {
                return new RestPayload(null, new JsonObject(), attributesError);
            }

            if (attributesObject is not null)
            {
                MergeAttributes(attributes, CloneAttributes(attributesObject));
            }
        }

        return new RestPayload(text, attributes, null);
    }

    private static string? ExtractText(JsonObject jsonObject)
    {
        foreach (var alias in TextAliases)
        {
            var property = jsonObject.FirstOrDefault(entry => string.Equals(entry.Key, alias, StringComparison.OrdinalIgnoreCase));
            if (property.Value is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static JsonObject ExtractAttributes(JsonObject jsonObject)
    {
        var attributes = new JsonObject();

        foreach (var property in jsonObject)
        {
            if (TextAliases.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ReservedAttributeFields.Contains(property.Key))
            {
                continue;
            }

            attributes[property.Key] = property.Value?.DeepClone();
        }

        return attributes;
    }

    private static JsonNode BuildCollection(IEnumerable<ApiMessage> messages, HttpRequest request)
    {
        if (!WantsExpandedView(request))
        {
            var legacyItems = new JsonArray();
            foreach (var message in messages)
            {
                legacyItems.Add(BuildFlatResource(message, includeMeta: false));
            }

            return legacyItems;
        }

        var expandedItems = new JsonArray();
        foreach (var message in messages)
        {
            expandedItems.Add(BuildExpandedResource(message));
        }

        return new JsonObject
        {
            ["items"] = expandedItems,
            ["_links"] = new JsonObject
            {
                ["self"] = "/api/messages?view=expanded",
                ["collection"] = "/api/messages"
            },
            ["_meta"] = BuildMetadata(ExpandedView)
        };
    }

    private static JsonObject BuildResource(ApiMessage message, HttpRequest request)
    {
        return WantsExpandedView(request)
            ? BuildExpandedResource(message)
            : BuildFlatResource(message, includeMeta: WantsMetadata(request));
    }

    private static JsonObject BuildFlatResource(ApiMessage message, bool includeMeta)
    {
        var resource = new JsonObject
        {
            ["id"] = message.Id,
            ["text"] = message.Text,
            ["createdAt"] = message.CreatedUtc,
            ["_links"] = new JsonObject
            {
                ["self"] = $"/api/messages/{message.Id}",
                ["collection"] = "/api/messages"
            }
        };

        foreach (var attribute in message.Attributes)
        {
            if (ReservedAttributeFields.Contains(attribute.Key))
            {
                continue;
            }

            resource[attribute.Key] = attribute.Value?.DeepClone();
        }

        if (includeMeta)
        {
            resource["_meta"] = BuildMetadata("flat");
        }

        return resource;
    }

    private static JsonObject BuildExpandedResource(ApiMessage message)
    {
        return new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["id"] = message.Id,
                ["text"] = message.Text,
                ["createdAt"] = message.CreatedUtc
            },
            ["attributes"] = CloneAttributes(message.Attributes),
            ["_links"] = new JsonObject
            {
                ["self"] = $"/api/messages/{message.Id}",
                ["collection"] = "/api/messages"
            },
            ["_meta"] = BuildMetadata(ExpandedView)
        };
    }

    private static JsonObject BuildMetadata(string representation)
    {
        return new JsonObject
        {
            ["representation"] = representation,
            ["acceptedTextFields"] = BuildStringArray(TextAliases),
            ["acceptedPayloadShapes"] = BuildStringArray(AcceptedPayloadShapes),
            ["evolutionPolicy"] = "additive-fields-and-alt-shapes"
        };
    }

    private static JsonArray BuildStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static bool WantsExpandedView(HttpRequest request)
    {
        return string.Equals(request.Query["view"], ExpandedView, StringComparison.OrdinalIgnoreCase);
    }

    private static bool WantsMetadata(HttpRequest request)
    {
        foreach (var rawValue in request.Query["include"])
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            foreach (var value in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.Equals(value, "meta", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetObject(JsonObject jsonObject, string propertyName, out JsonObject? nestedObject, out IResult? errorResult)
    {
        if (!TryGetProperty(jsonObject, propertyName, out var node) || node is null)
        {
            nestedObject = null;
            errorResult = null;
            return false;
        }

        if (node is not JsonObject objectNode)
        {
            nestedObject = null;
            errorResult = Results.BadRequest(new { error = $"{propertyName} must be a JSON object when provided." });
            return true;
        }

        nestedObject = objectNode;
        errorResult = null;
        return true;
    }

    private static bool TryGetProperty(JsonObject jsonObject, string propertyName, out JsonNode? node)
    {
        foreach (var property in jsonObject)
        {
            if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                node = property.Value;
                return true;
            }
        }

        node = null;
        return false;
    }

    private static JsonObject CloneAttributes(JsonObject attributes)
    {
        var clone = new JsonObject();
        MergeAttributes(clone, attributes);
        return clone;
    }

    private static void MergeAttributes(JsonObject target, JsonObject source)
    {
        foreach (var property in source)
        {
            target[property.Key] = property.Value?.DeepClone();
        }
    }

    private static bool TryGetAmount(ApiMessage message, out decimal amount)
    {
        amount = default;
        return message.Attributes.TryGetPropertyValue("amount", out var node)
            && node is JsonValue value
            && value.TryGetValue(out amount);
    }

    private sealed record RestPayload(string? Text, JsonObject Attributes, IResult? ErrorResult);
}
