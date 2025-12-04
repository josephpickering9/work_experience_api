using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.DiscoveryEngine.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Auth;
using Microsoft.Extensions.Options;

namespace Work_Experience_Search.Services.VertexAi;

public class VertexAiOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "global";
    public string Collection { get; set; } = "default_collection";
    public string Branch { get; set; } = "0";
    public string? CredentialsFile { get; set; }
    public string? CredentialsJson { get; set; }
}

public interface IVertexChatbotClient
{
    Task InitialiseCachesAsync(string tenantId, bool ensureSchema = false, CancellationToken cancellationToken = default);
    Task UpsertFeatureAsync<T>(string tenantId, string featureType, string documentId, T value, string? jsonSchema = null, bool ensureSchema = false, CancellationToken cancellationToken = default);
    Task DeleteFeatureAsync(string tenantId, string featureType, string documentId, CancellationToken cancellationToken = default);
    Task ImportDocumentsAsync(string tenantId, IEnumerable<string> gcsUris, CancellationToken cancellationToken = default);
    Task DeleteDocumentsAsync(string tenantId, IEnumerable<string> documentIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight C# port of the Kotlin Vertex AI client: manages engines, data stores, schemas and document lifecycle.
/// Does not include the conversational layer; wire your LLM calls separately and ensure they reference the created data stores.
/// </summary>
public class VertexChatbotClient : IVertexChatbotClient
{
    private readonly VertexAiOptions _options;
    private readonly ILogger<VertexChatbotClient> _logger;
    private readonly EngineServiceClient _engineClient;
    private readonly DataStoreServiceClient _dataStoreClient;
    private readonly DocumentServiceClient _documentClient;
    private readonly SchemaServiceClient _schemaClient;

    public VertexChatbotClient(IOptions<VertexAiOptions> options, ILogger<VertexChatbotClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        var credential = BuildCredential(_options);
        _engineClient = new EngineServiceClientBuilder { ChannelCredentials = credential.ToChannelCredentials() }.Build();
        _dataStoreClient = new DataStoreServiceClientBuilder { ChannelCredentials = credential.ToChannelCredentials() }.Build();
        _documentClient = new DocumentServiceClientBuilder { ChannelCredentials = credential.ToChannelCredentials() }.Build();
        _schemaClient = new SchemaServiceClientBuilder { ChannelCredentials = credential.ToChannelCredentials() }.Build();
    }

    public async Task InitialiseCachesAsync(string tenantId, bool ensureSchema = false, CancellationToken cancellationToken = default)
    {
        var dataStores = await GetOrCreateDataStoresAsync(tenantId, ensureSchema, cancellationToken);
        await GetOrCreateEngineAsync(tenantId, dataStores, cancellationToken);
    }

    public async Task UpsertFeatureAsync<T>(string tenantId, string featureType, string documentId, T value, string? jsonSchema = null, bool ensureSchema = false, CancellationToken cancellationToken = default)
    {
        var dataStoreId = GetFeatureDataStoreId(tenantId, featureType);
        await GetOrCreateDataStoreAsync(tenantId, dataStoreId, $"{featureType} Structured JSON", DataStore.Types.ContentConfig.NoContent, featureType, jsonSchema, ensureSchema, cancellationToken);

        var docName = GetDocumentName(dataStoreId, documentId);
        var document = new Document
        {
            Name = docName,
            StructData = MapToStruct(value)
        };

        try
        {
            await _documentClient.UpdateDocumentAsync(new UpdateDocumentRequest { Document = document }, cancellationToken: cancellationToken);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            await _documentClient.CreateDocumentAsync(new CreateDocumentRequest
            {
                Parent = GetBranchName(dataStoreId),
                DocumentId = documentId,
                Document = document
            }, cancellationToken: cancellationToken);
        }
    }

    public async Task DeleteFeatureAsync(string tenantId, string featureType, string documentId, CancellationToken cancellationToken = default)
    {
        var dataStoreId = GetFeatureDataStoreId(tenantId, featureType);
        await GetOrCreateDataStoresAsync(tenantId, cancellationToken: cancellationToken);

        try
        {
            await _documentClient.DeleteDocumentAsync(new DeleteDocumentRequest
            {
                Name = GetDocumentName(dataStoreId, documentId)
            }, cancellationToken: cancellationToken);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.LogInformation("Document {DocumentId} in data store {DataStoreId} already deleted.", documentId, dataStoreId);
        }
    }

    public async Task ImportDocumentsAsync(string tenantId, IEnumerable<string> gcsUris, CancellationToken cancellationToken = default)
    {
        var dataStoreId = GetDocumentDataStoreId(tenantId);
        await GetOrCreateDataStoreAsync(tenantId, dataStoreId, "Unstructured Docs", DataStore.Types.ContentConfig.ContentRequired, null, null, false, cancellationToken);

        var request = new ImportDocumentsRequest
        {
            Parent = GetBranchName(dataStoreId),
            GcsSource = new GcsSource { InputUris = { gcsUris }, DataSchema = "content" }
        };

        var operation = await _documentClient.ImportDocumentsAsync(request, cancellationToken: cancellationToken);
        await operation.PollUntilCompletedAsync(cancellationToken);
    }

    public async Task DeleteDocumentsAsync(string tenantId, IEnumerable<string> documentIds, CancellationToken cancellationToken = default)
    {
        var dataStoreId = GetDocumentDataStoreId(tenantId);
        await GetOrCreateDataStoresAsync(tenantId, cancellationToken: cancellationToken);

        foreach (var id in documentIds)
        {
            try
            {
                await _documentClient.DeleteDocumentAsync(new DeleteDocumentRequest
                {
                    Name = GetDocumentName(dataStoreId, id)
                }, cancellationToken: cancellationToken);
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                _logger.LogInformation("Document {DocumentId} in data store {DataStoreId} already deleted.", id, dataStoreId);
            }
        }
    }

    private async Task<GoogleChatbotDataStores> GetOrCreateDataStoresAsync(string tenantId, bool ensureSchema = false, CancellationToken cancellationToken = default)
    {
        var structuredTypes = new[] { "document", "entity", "metric", "note", "portfolio", "proxy_vote", "questionnaire", "rfp_question", "task" };
        var structuredDataStoreIds = new List<(string FeatureType, string Id)>();

        foreach (var type in structuredTypes)
        {
            var dataStoreId = GetFeatureDataStoreId(tenantId, type);
            var dataStore = await GetOrCreateDataStoreAsync(tenantId, dataStoreId, $"{type} Structured JSON", DataStore.Types.ContentConfig.NoContent, type, null, ensureSchema, cancellationToken);
            structuredDataStoreIds.Add((type, GetResourceId(dataStore.Name)));
        }

        var unstructured = await GetOrCreateDataStoreAsync(tenantId, GetDocumentDataStoreId(tenantId), "Unstructured Docs", DataStore.Types.ContentConfig.ContentRequired, null, null, false, cancellationToken);

        return new GoogleChatbotDataStores(structuredDataStoreIds, GetResourceId(unstructured.Name));
    }

    private async Task<DataStore> GetOrCreateDataStoreAsync(string tenantId, string dataStoreId, string displayName, DataStore.Types.ContentConfig contentConfig, string? featureType, string? jsonSchema, bool ensureSchema, CancellationToken cancellationToken)
    {
        var dataStoreName = DataStoreName.FromProjectLocationDataStore(_options.ProjectId, _options.Location, dataStoreId).ToString();

        try
        {
            var existing = await _dataStoreClient.GetDataStoreAsync(new GetDataStoreRequest { Name = dataStoreName }, cancellationToken: cancellationToken);
            if (ensureSchema && jsonSchema != null && featureType != null)
            {
                await EnsureSchemaUpToDateAsync(dataStoreId, jsonSchema, cancellationToken);
            }

            return existing;
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            var newDataStore = new DataStore
            {
                DisplayName = $"{tenantId} ({displayName})",
                ContentConfig = contentConfig
            };
            newDataStore.SolutionTypes.Add(SolutionType.Search);
            newDataStore.IndustryVertical = IndustryVertical.Generic;

            if (featureType != null && jsonSchema != null)
            {
                newDataStore.StartingSchema = new Schema { JsonSchema = jsonSchema };
            }

            var request = new CreateDataStoreRequest
            {
                Parent = GetCollectionName(),
                DataStoreId = dataStoreId,
                DataStore = newDataStore
            };

            var created = await _dataStoreClient.CreateDataStoreAsync(request, cancellationToken: cancellationToken);
            if (ensureSchema && jsonSchema != null)
            {
                await EnsureSchemaUpToDateAsync(dataStoreId, jsonSchema, cancellationToken);
            }

            return created;
        }
    }

    private async Task EnsureSchemaUpToDateAsync(string dataStoreId, string jsonSchema, CancellationToken cancellationToken)
    {
        var parent = DataStoreName.FromProjectLocationDataStore(_options.ProjectId, _options.Location, dataStoreId).ToString();
        var existing = _schemaClient.ListSchemas(new ListSchemasRequest { Parent = parent }).FirstOrDefault();

        try
        {
            if (existing == null)
            {
                await _schemaClient.CreateSchemaAsync(new CreateSchemaRequest
                {
                    Parent = parent,
                    SchemaId = "default_schema",
                    Schema = new Schema { JsonSchema = jsonSchema }
                }, cancellationToken: cancellationToken);
            }
            else
            {
                var updated = existing.Clone();
                updated.JsonSchema = jsonSchema;
                await _schemaClient.UpdateSchemaAsync(new UpdateSchemaRequest { Schema = updated }, cancellationToken: cancellationToken);
            }
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.FailedPrecondition)
        {
            _logger.LogWarning("Schema update already in progress for datastore {DataStoreId}", dataStoreId);
        }
    }

    private async Task<Engine> GetOrCreateEngineAsync(string tenantId, GoogleChatbotDataStores dataStores, CancellationToken cancellationToken)
    {
        var engineId = GetEngineId(tenantId);
        var engineName = EngineName.FromProjectLocationCollectionEngine(_options.ProjectId, _options.Location, _options.Collection, engineId).ToString();

        try
        {
            var existing = await _engineClient.GetEngineAsync(new GetEngineRequest { Name = engineName }, cancellationToken: cancellationToken);
            await EnsureEngineDataStoresAsync(existing, dataStores, cancellationToken);
            return existing;
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            var engine = new Engine
            {
                DisplayName = $"{tenantId} Chatbot",
                IndustryVertical = IndustryVertical.Generic,
                SolutionType = SolutionType.Search
            };
            engine.DataStoreIds.AddRange(dataStores.StructuredDataStoreIds.Select(x => x.Id));
            engine.DataStoreIds.Add(dataStores.UnstructuredDataStoreId);
            engine.SearchEngineConfig = new Engine.Types.SearchEngineConfig
            {
                SearchTier = SearchTier.Enterprise
            };
            engine.SearchEngineConfig.SearchAddOns.Add(SearchAddOn.Llm);

            var request = new CreateEngineRequest
            {
                Parent = GetLocationName(),
                EngineId = engineId,
                Engine = engine
            };

            return await _engineClient.CreateEngineAsync(request, cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureEngineDataStoresAsync(Engine engine, GoogleChatbotDataStores dataStores, CancellationToken cancellationToken)
    {
        var expected = dataStores.StructuredDataStoreIds.Select(x => x.Id)
            .Concat(new[] { dataStores.UnstructuredDataStoreId })
            .ToHashSet();

        if (engine.DataStoreIds.ToHashSet().SetEquals(expected)) return;

        var updated = engine.Clone();
        updated.DataStoreIds.Clear();
        updated.DataStoreIds.AddRange(expected);

        await _engineClient.UpdateEngineAsync(new UpdateEngineRequest
        {
            Engine = updated,
            UpdateMask = new FieldMask { Paths = { "data_store_ids" } }
        }, cancellationToken: cancellationToken);
    }

    private GoogleCredential BuildCredential(VertexAiOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CredentialsFile))
        {
            return GoogleCredential.FromFile(options.CredentialsFile);
        }

        if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
        {
            return GoogleCredential.FromJson(options.CredentialsJson);
        }

        return GoogleCredential.GetApplicationDefault();
    }

    private string GetLocationName() => LocationName.FromProjectLocation(_options.ProjectId, _options.Location).ToString();
    private string GetCollectionName() => CollectionName.FromProjectLocationCollection(_options.ProjectId, _options.Location, _options.Collection).ToString();
    private string GetBranchName(string dataStoreId) => BranchName.FromProjectLocationCollectionDataStoreBranch(_options.ProjectId, _options.Location, _options.Collection, dataStoreId, _options.Branch).ToString();
    private string GetDocumentName(string dataStoreId, string documentId) => DocumentName.FromProjectLocationDataStoreBranchDocument(_options.ProjectId, _options.Location, dataStoreId, _options.Branch, documentId).ToString();
    private string GetEngineId(string tenantId) => $"{tenantId}-blended-search";
    private string GetFeatureDataStoreId(string tenantId, string featureType) => $"{tenantId}_{featureType}_structured";
    private string GetDocumentDataStoreId(string tenantId) => $"{tenantId}_unstructured";

    private static string GetResourceId(string name) => name.Split("/").Last();

    private static Struct MapToStruct<T>(T value)
    {
        var element = JsonSerializer.SerializeToElement(value ?? new { });
        return Struct.Parser.ParseJson(element.GetRawText());
    }
}

public record GoogleChatbotDataStores(IReadOnlyList<(string FeatureType, string Id)> StructuredDataStoreIds, string UnstructuredDataStoreId);
