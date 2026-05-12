using System.Collections;
using System.Text.RegularExpressions;
using FunctionalProgramming.Grpc;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace FunctionalProgramming.Services.Grpc;

public static class ApiCatalogContract
{
    public const string Owner = "platform-contracts";
    public const string V1SchemaName = "api_catalog.ApiCatalogV1";
    public const string V2SchemaName = "api_catalog.ApiCatalogV2";

    public static string V1Version => GetContractVersion(ListApiCatalogEntriesV1Response.Descriptor);
    public static string V2Version => GetContractVersion(ListApiCatalogEntriesV2Response.Descriptor);

    public static ApiCatalogContractMetadataV1 CreateV1Metadata()
    {
        var metadata = new ApiCatalogContractMetadataV1
        {
            ContractVersion = V1Version,
            SchemaName = V1SchemaName,
            Owner = Owner
        };
        metadata.SupportedContractVersions.Add(V1Version);
        return metadata;
    }

    public static ApiCatalogContractMetadataV2 CreateV2Metadata()
    {
        var metadata = new ApiCatalogContractMetadataV2
        {
            ContractVersion = V2Version,
            SchemaName = V2SchemaName,
            Owner = Owner
        };
        metadata.SupportedContractVersions.Add(V1Version);
        metadata.SupportedContractVersions.Add(V2Version);
        return metadata;
    }

    public static void EnsureValidRequest(ListApiCatalogEntriesV1Request request) =>
        EnsureValid(request, StatusCode.InvalidArgument, $"Request violates {V1Version}");

    public static void EnsureValidRequest(ListApiCatalogEntriesV2Request request) =>
        EnsureValid(request, StatusCode.InvalidArgument, $"Request violates {V2Version}");

    public static void EnsureValidReply(ListApiCatalogEntriesV1Response reply) =>
        EnsureValid(reply, StatusCode.Internal, $"Server emitted data outside {V1Version}");

    public static void EnsureValidReply(ListApiCatalogEntriesV2Response reply) =>
        EnsureValid(reply, StatusCode.Internal, $"Server emitted data outside {V2Version}");

    public static IReadOnlyList<string> Validate(IMessage message)
    {
        var expectedContract = GetContractVersion(message.Descriptor);
        var violations = new List<string>();
        ValidateMessage(message, message.Descriptor.Name, expectedContract, violations);
        return violations;
    }

    private static void EnsureValid(IMessage message, StatusCode failureCode, string failurePrefix)
    {
        var violations = Validate(message);
        if (violations.Count > 0)
        {
            throw new RpcException(new Status(
                failureCode,
                $"{failurePrefix}: {string.Join("; ", violations)}"));
        }
    }

    private static void ValidateMessage(
        IMessage? message,
        string path,
        string expectedContract,
        List<string> violations)
    {
        if (message is null)
        {
            violations.Add($"{path} is required");
            return;
        }

        var contract = GetContractVersion(message.Descriptor);
        if (!string.IsNullOrWhiteSpace(contract) && contract != expectedContract)
        {
            violations.Add($"{path} belongs to contract '{contract}', expected '{expectedContract}'");
        }

        foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
        {
            ValidateField(message, field, $"{path}.{field.Name}", expectedContract, violations);
        }
    }

    private static void ValidateField(
        IMessage message,
        FieldDescriptor field,
        string path,
        string expectedContract,
        List<string> violations)
    {
        var options = field.GetOptions();
        var required = options.HasExtension(ApiCatalogExtensions.StrictRequired)
            && options.GetExtension(ApiCatalogExtensions.StrictRequired);
        var minItems = options.HasExtension(ApiCatalogExtensions.StrictMinItems)
            ? options.GetExtension(ApiCatalogExtensions.StrictMinItems)
            : 0;
        var pattern = options.HasExtension(ApiCatalogExtensions.StrictStringPattern)
            ? options.GetExtension(ApiCatalogExtensions.StrictStringPattern)
            : string.Empty;
        var semanticType = options.HasExtension(ApiCatalogExtensions.StrictSemanticType)
            ? options.GetExtension(ApiCatalogExtensions.StrictSemanticType)
            : string.Empty;

        var value = field.Accessor.GetValue(message);
        var displayPath = string.IsNullOrWhiteSpace(semanticType)
            ? path
            : $"{path} ({semanticType})";

        if (field.IsRepeated)
        {
            ValidateRepeatedField(value, field, displayPath, minItems, required, pattern, expectedContract, violations);
            return;
        }

        if (required && IsMissingRequiredValue(field, value))
        {
            violations.Add($"{displayPath} is required");
            return;
        }

        ValidateScalarValue(value, field, displayPath, pattern, violations);

        if (field.FieldType == FieldType.Message && value is IMessage nestedMessage)
        {
            ValidateMessage(nestedMessage, path, expectedContract, violations);
        }
    }

    private static void ValidateRepeatedField(
        object value,
        FieldDescriptor field,
        string path,
        int minItems,
        bool required,
        string pattern,
        string expectedContract,
        List<string> violations)
    {
        var items = value is IEnumerable enumerable
            ? enumerable.Cast<object>().ToArray()
            : Array.Empty<object>();

        if (required && items.Length == 0)
        {
            violations.Add($"{path} is required");
        }

        if (minItems > 0 && items.Length < minItems)
        {
            violations.Add($"{path} must contain at least {minItems} item(s)");
        }

        for (var index = 0; index < items.Length; index++)
        {
            var itemPath = $"{path}[{index}]";
            var item = items[index];

            if (field.FieldType == FieldType.Message && item is IMessage nestedMessage)
            {
                ValidateMessage(nestedMessage, itemPath, expectedContract, violations);
                continue;
            }

            ValidateScalarValue(item, field, itemPath, pattern, violations);
        }
    }

    private static void ValidateScalarValue(
        object? value,
        FieldDescriptor field,
        string path,
        string pattern,
        List<string> violations)
    {
        if (field.FieldType == FieldType.String && value is string text && !string.IsNullOrEmpty(pattern))
        {
            if (!Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant))
            {
                violations.Add($"{path} value '{text}' does not match '{pattern}'");
            }
        }

        if (field.FieldType == FieldType.Enum && value is not null)
        {
            var enumNumber = Convert.ToInt32(value);
            if (field.EnumType.FindValueByNumber(enumNumber) is null)
            {
                violations.Add($"{path} value '{enumNumber}' is not a governed enum value");
            }
        }
    }

    private static bool IsMissingRequiredValue(FieldDescriptor field, object? value)
    {
        return field.FieldType switch
        {
            FieldType.String => string.IsNullOrWhiteSpace(value as string),
            FieldType.Message => value is null,
            FieldType.Enum => value is null || Convert.ToInt32(value) == 0,
            _ => value is null
        };
    }

    private static string GetContractVersion(MessageDescriptor descriptor) =>
        descriptor.GetOptions().HasExtension(ApiCatalogExtensions.StrictContract)
            ? descriptor.GetOptions().GetExtension(ApiCatalogExtensions.StrictContract)
            : string.Empty;
}
