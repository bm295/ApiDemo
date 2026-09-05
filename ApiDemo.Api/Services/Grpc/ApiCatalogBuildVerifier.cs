using FunctionalProgramming.Grpc;
using Google.Protobuf.Reflection;
using Grpc.Core;
using ProtoServiceDescriptor = Google.Protobuf.Reflection.ServiceDescriptor;

namespace FunctionalProgramming.Services.Grpc;

public static class ApiCatalogBuildVerifier
{
    public static int Run()
    {
        try
        {
            VerifyGeneratedTypeSurface();
            VerifyServiceContracts();
            VerifyMessageContracts();
            VerifyEnumEvolution();
            Console.WriteLine("gRPC contract verification passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"gRPC contract verification failed: {exception.Message}");
            return 1;
        }
    }

    private static void VerifyGeneratedTypeSurface()
    {
        var v1Request = new ListApiCatalogEntriesV1Request
        {
            ContractVersion = ApiCatalogContract.V1Version
        };

        var v1Response = new ListApiCatalogEntriesV1Response
        {
            Metadata = ApiCatalogContract.CreateV1Metadata()
        };
        v1Response.CatalogEntries.Add(new ApiCatalogEntryV1
        {
            EntryKind = ApiCatalogEntryKindV1.Rest,
            DisplayName = "REST",
            IntegrationUseCase = "Resource CRUD over HTTP"
        });

        var v2Request = new ListApiCatalogEntriesV2Request
        {
            ContractVersion = ApiCatalogContract.V2Version,
            RequestingClientName = "build-verifier"
        };

        var v2Response = new ListApiCatalogEntriesV2Response
        {
            Metadata = ApiCatalogContract.CreateV2Metadata()
        };
        v2Response.CatalogEntries.Add(new ApiCatalogEntryV2
        {
            EntryKind = ApiCatalogEntryKindV2.Asyncapi,
            DisplayName = "AsyncAPI",
            IntegrationUseCase = "Event driven schema governance",
            LifecycleStatus = "emerging"
        });

        ApiCatalogContract.EnsureValidRequest(v1Request);
        ApiCatalogContract.EnsureValidReply(v1Response);
        ApiCatalogContract.EnsureValidRequest(v2Request);
        ApiCatalogContract.EnsureValidReply(v2Response);

        _ = typeof(ApiCatalogV1.ApiCatalogV1Client);
        _ = typeof(ApiCatalogV2.ApiCatalogV2Client);
        _ = typeof(V1BuildGuardService);
        _ = typeof(V2BuildGuardService);
    }

    private static void VerifyServiceContracts()
    {
        VerifyService(
            ApiCatalogV1.Descriptor,
            "api_catalog.ApiCatalogV1",
            "ListCatalogEntries",
            ListApiCatalogEntriesV1Request.Descriptor,
            ListApiCatalogEntriesV1Response.Descriptor);

        VerifyService(
            ApiCatalogV2.Descriptor,
            "api_catalog.ApiCatalogV2",
            "ListCatalogEntries",
            ListApiCatalogEntriesV2Request.Descriptor,
            ListApiCatalogEntriesV2Response.Descriptor);
    }

    private static void VerifyMessageContracts()
    {
        VerifyMessageContract(ListApiCatalogEntriesV1Request.Descriptor, ApiCatalogContract.V1Version);
        VerifyMessageContract(ListApiCatalogEntriesV2Request.Descriptor, ApiCatalogContract.V2Version);
        VerifyMessageContract(ApiCatalogContractMetadataV1.Descriptor, ApiCatalogContract.V1Version);
        VerifyMessageContract(ApiCatalogContractMetadataV2.Descriptor, ApiCatalogContract.V2Version);
        VerifyMessageContract(ApiCatalogEntryV1.Descriptor, ApiCatalogContract.V1Version);
        VerifyMessageContract(ApiCatalogEntryV2.Descriptor, ApiCatalogContract.V2Version);
        VerifyMessageContract(ListApiCatalogEntriesV1Response.Descriptor, ApiCatalogContract.V1Version);
        VerifyMessageContract(ListApiCatalogEntriesV2Response.Descriptor, ApiCatalogContract.V2Version);

        VerifyStringField(ListApiCatalogEntriesV1Request.Descriptor, "contract_version", "^api-catalog\\.v1$", "contract-version");
        VerifyStringField(ListApiCatalogEntriesV2Request.Descriptor, "contract_version", "^api-catalog\\.v2$", "contract-version");
        VerifyStringField(
            ListApiCatalogEntriesV2Request.Descriptor,
            "requesting_client_name",
            "^[A-Za-z][A-Za-z0-9-]{2,30}$",
            "integrating-client-name");

        VerifyRepeatedStringField(ApiCatalogContractMetadataV1.Descriptor, "supported_contract_versions", "^api-catalog\\.v1$", 1);
        VerifyRepeatedStringField(ApiCatalogContractMetadataV2.Descriptor, "supported_contract_versions", "^api-catalog\\.v[12]$", 2);

        VerifyEnumField(ApiCatalogEntryV1.Descriptor, "entry_kind", "governed-api-kind");
        VerifyStringField(ApiCatalogEntryV1.Descriptor, "display_name", "^(REST|SOAP|gRPC|GraphQL|Webhook|WebSocket)$", "display-name");
        VerifyStringField(ApiCatalogEntryV1.Descriptor, "integration_use_case", "^[A-Z][A-Za-z0-9 -]+$", "business-use-case");

        VerifyEnumField(ApiCatalogEntryV2.Descriptor, "entry_kind", "governed-api-kind");
        VerifyStringField(ApiCatalogEntryV2.Descriptor, "display_name", "^(REST|SOAP|gRPC|GraphQL|Webhook|WebSocket|AsyncAPI)$", "display-name");
        VerifyStringField(ApiCatalogEntryV2.Descriptor, "integration_use_case", "^[A-Z][A-Za-z0-9 -]+$", "business-use-case");
        VerifyStringField(ApiCatalogEntryV2.Descriptor, "lifecycle_status", "^(stable|emerging)$", "domain-lifecycle");

        VerifyRepeatedMessageField(ListApiCatalogEntriesV1Response.Descriptor, "catalog_entries", 1);
        VerifyRepeatedMessageField(ListApiCatalogEntriesV2Response.Descriptor, "catalog_entries", 1);
    }

    private static void VerifyEnumEvolution()
    {
        if ((int)ApiCatalogEntryKindV1.Rest != 1 || (int)ApiCatalogEntryKindV1.Websocket != 6)
        {
            throw new InvalidOperationException("ApiCatalogEntryKindV1 numeric values changed.");
        }

        if (Enum.IsDefined(typeof(ApiCatalogEntryKindV1), "Asyncapi"))
        {
            throw new InvalidOperationException("ApiCatalogEntryKindV1 must not accept the v2-only AsyncAPI value.");
        }

        if ((int)ApiCatalogEntryKindV2.Asyncapi != 7)
        {
            throw new InvalidOperationException("ApiCatalogEntryKindV2.AsyncAPI must remain value 7.");
        }
    }

    private static void VerifyService(
        ProtoServiceDescriptor service,
        string fullName,
        string methodName,
        MessageDescriptor inputType,
        MessageDescriptor outputType)
    {
        if (service.FullName != fullName)
        {
            throw new InvalidOperationException($"Service '{service.FullName}' should be '{fullName}'.");
        }

        var method = service.Methods.SingleOrDefault(candidate => candidate.Name == methodName)
            ?? throw new InvalidOperationException($"Service '{fullName}' is missing '{methodName}'.");

        if (method.InputType != inputType || method.OutputType != outputType)
        {
            throw new InvalidOperationException($"Service '{fullName}.{methodName}' has an unexpected request or response type.");
        }
    }

    private static void VerifyMessageContract(MessageDescriptor descriptor, string expectedVersion)
    {
        var options = descriptor.GetOptions();
        var actualVersion = options.HasExtension(ApiCatalogExtensions.StrictContract)
            ? options.GetExtension(ApiCatalogExtensions.StrictContract)
            : string.Empty;

        if (actualVersion != expectedVersion)
        {
            throw new InvalidOperationException($"{descriptor.Name} must declare strict_contract '{expectedVersion}'.");
        }
    }

    private static void VerifyStringField(
        MessageDescriptor descriptor,
        string fieldName,
        string expectedPattern,
        string expectedSemanticType)
    {
        var field = FindField(descriptor, fieldName);
        if (field.FieldType != FieldType.String || field.IsRepeated)
        {
            throw new InvalidOperationException($"{descriptor.Name}.{fieldName} must be a singular string.");
        }

        VerifyRequired(field);
        VerifyPattern(field, expectedPattern);
        VerifySemanticType(field, expectedSemanticType);
    }

    private static void VerifyRepeatedStringField(
        MessageDescriptor descriptor,
        string fieldName,
        string expectedPattern,
        int expectedMinItems)
    {
        var field = FindField(descriptor, fieldName);
        if (field.FieldType != FieldType.String || !field.IsRepeated)
        {
            throw new InvalidOperationException($"{descriptor.Name}.{fieldName} must be a repeated string.");
        }

        VerifyRequired(field);
        VerifyPattern(field, expectedPattern);
        VerifyMinItems(field, expectedMinItems);
    }

    private static void VerifyEnumField(
        MessageDescriptor descriptor,
        string fieldName,
        string expectedSemanticType)
    {
        var field = FindField(descriptor, fieldName);
        if (field.FieldType != FieldType.Enum || field.IsRepeated)
        {
            throw new InvalidOperationException($"{descriptor.Name}.{fieldName} must be a singular enum.");
        }

        VerifyRequired(field);
        VerifySemanticType(field, expectedSemanticType);
    }

    private static void VerifyRepeatedMessageField(
        MessageDescriptor descriptor,
        string fieldName,
        int expectedMinItems)
    {
        var field = FindField(descriptor, fieldName);
        if (field.FieldType != FieldType.Message || !field.IsRepeated)
        {
            throw new InvalidOperationException($"{descriptor.Name}.{fieldName} must be a repeated message.");
        }

        VerifyRequired(field);
        VerifyMinItems(field, expectedMinItems);
    }

    private static FieldDescriptor FindField(MessageDescriptor descriptor, string fieldName) =>
        descriptor.Fields.InFieldNumberOrder().SingleOrDefault(field => field.Name == fieldName)
        ?? throw new InvalidOperationException($"{descriptor.Name} is missing field '{fieldName}'.");

    private static void VerifyRequired(FieldDescriptor field)
    {
        var options = field.GetOptions();
        var required = options.HasExtension(ApiCatalogExtensions.StrictRequired)
            && options.GetExtension(ApiCatalogExtensions.StrictRequired);

        if (!required)
        {
            throw new InvalidOperationException($"{field.ContainingType.Name}.{field.Name} must be marked strict_required.");
        }
    }

    private static void VerifyPattern(FieldDescriptor field, string expectedPattern)
    {
        var options = field.GetOptions();
        var pattern = options.HasExtension(ApiCatalogExtensions.StrictStringPattern)
            ? options.GetExtension(ApiCatalogExtensions.StrictStringPattern)
            : string.Empty;

        if (pattern != expectedPattern)
        {
            throw new InvalidOperationException($"{field.ContainingType.Name}.{field.Name} has an unexpected strict_string_pattern.");
        }
    }

    private static void VerifySemanticType(FieldDescriptor field, string expectedSemanticType)
    {
        var options = field.GetOptions();
        var semanticType = options.HasExtension(ApiCatalogExtensions.StrictSemanticType)
            ? options.GetExtension(ApiCatalogExtensions.StrictSemanticType)
            : string.Empty;

        if (semanticType != expectedSemanticType)
        {
            throw new InvalidOperationException($"{field.ContainingType.Name}.{field.Name} has an unexpected strict_semantic_type.");
        }
    }

    private static void VerifyMinItems(FieldDescriptor field, int expectedMinItems)
    {
        var options = field.GetOptions();
        var minItems = options.HasExtension(ApiCatalogExtensions.StrictMinItems)
            ? options.GetExtension(ApiCatalogExtensions.StrictMinItems)
            : 0;

        if (minItems != expectedMinItems)
        {
            throw new InvalidOperationException($"{field.ContainingType.Name}.{field.Name} has an unexpected strict_min_items.");
        }
    }

    private sealed class V1BuildGuardService : ApiCatalogV1.ApiCatalogV1Base
    {
        public override Task<ListApiCatalogEntriesV1Response> ListCatalogEntries(
            ListApiCatalogEntriesV1Request request,
            ServerCallContext context) =>
            Task.FromResult(new ListApiCatalogEntriesV1Response());
    }

    private sealed class V2BuildGuardService : ApiCatalogV2.ApiCatalogV2Base
    {
        public override Task<ListApiCatalogEntriesV2Response> ListCatalogEntries(
            ListApiCatalogEntriesV2Request request,
            ServerCallContext context) =>
            Task.FromResult(new ListApiCatalogEntriesV2Response());
    }
}
