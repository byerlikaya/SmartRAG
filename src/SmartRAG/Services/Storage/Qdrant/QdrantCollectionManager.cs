using Qdrant.Client;
using Qdrant.Client.Grpc;

using SmartRAG.Services.Shared;

namespace SmartRAG.Services.Storage.Qdrant;


/// <summary>
/// Service for managing Qdrant collections and document storage
/// </summary>
public class QdrantCollectionManager : IQdrantCollectionManager
{
    private const int DefaultGrpcTimeoutMinutes = 5;
    private const int DefaultVectorDimension = 768;

    private readonly QdrantClient _client;
    private readonly QdrantConfig _config;
    private readonly string _collectionName;
    private readonly ILogger<QdrantCollectionManager> _logger;
    private static readonly SemaphoreSlim CollectionInitLock = new(1, 1);
    private bool _collectionReady;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the QdrantCollectionManager
    /// </summary>
    /// <param name="config">Qdrant configuration options</param>
    /// <param name="logger">Logger instance for this service</param>
    public QdrantCollectionManager(IOptions<QdrantConfig> config, ILogger<QdrantCollectionManager> logger)
    {
        _config = config.Value;
        _collectionName = _config.CollectionName;
        _logger = logger;

        string host;
        bool useHttps;

        if (config.Value.Host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || config.Value.Host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(config.Value.Host);
            host = uri.Host;
            useHttps = uri.Scheme == "https";
        }
        else
        {
            host = config.Value.Host;
            useHttps = config.Value.UseHttps;
        }

        _client = new QdrantClient(
            host,
            https: useHttps,
            apiKey: config.Value.ApiKey,
            grpcTimeout: TimeSpan.FromMinutes(DefaultGrpcTimeoutMinutes)
        );

        Task.Run(async () =>
        {
            try
            {
                await InitializeCollectionAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogQdrantCollectionInitFailed(_logger, ex);
            }
        });
    }

    /// <summary>
    /// Ensures the main collection exists and is ready for operations
    /// </summary>
    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        if (_collectionReady)
            return;

        await InitializeCollectionAsync(cancellationToken);
    }

    private async Task CreateCollectionAsync(string collectionName, int vectorDimension, CancellationToken cancellationToken = default)
    {
        try
        {
            var vectorParams = new VectorParams
            {
                Size = (ulong)vectorDimension,
                Distance = GetDistanceMetric(_config.DistanceMetric)
            };

            ServiceLogMessages.LogQdrantCollectionCreating(_logger, collectionName, vectorDimension, null);

            await _client.CreateCollectionAsync(collectionName, vectorParams, cancellationToken: cancellationToken);
            await _client.CreatePayloadIndexAsync(collectionName, "content", PayloadSchemaType.Text, cancellationToken: cancellationToken);
            ServiceLogMessages.LogQdrantCollectionCreatedTextIndex(_logger, collectionName, null);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogQdrantCollectionCreateFailed(_logger, collectionName, ex);
            throw;
        }
    }

    /// <summary>
    /// Ensures a document-specific collection exists
    /// </summary>
    /// <param name="collectionName">Name of the document collection</param>
    /// <param name="document">Document to store</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public async Task EnsureDocumentCollectionExistsAsync(string collectionName, SmartRAG.Entities.Document document, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_collectionReady)
            {
                var collections = await _client.ListCollectionsAsync(cancellationToken);
                if (collections.Contains(collectionName))
                    return;
            }

            int vectorDimension;

            if (document.Chunks is { Count: > 0 })
            {
                var firstChunkWithEmbedding = document.Chunks.FirstOrDefault(c => c.Embedding.Count > 0);
                if (firstChunkWithEmbedding != null)
                {
                    vectorDimension = firstChunkWithEmbedding.Embedding.Count;
                    ServiceLogMessages.LogQdrantCollectionUsingEmbeddingDimension(_logger, vectorDimension, collectionName, null);
                }
                else
                {
                    vectorDimension = await GetVectorDimensionAsync(cancellationToken);
                }
            }
            else
            {
                vectorDimension = await GetVectorDimensionAsync(cancellationToken);
            }

            await CreateCollectionAsync(collectionName, vectorDimension, cancellationToken);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogQdrantCollectionEnsureFailed(_logger, collectionName, ex);
            throw;
        }
    }

    private async Task<int> GetVectorDimensionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_config.VectorSize > 0)
            {
                return _config.VectorSize;
            }

            var collections = await _client.ListCollectionsAsync(cancellationToken);
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

            if (documentCollections.Count == 0 && collections.Contains(_collectionName))
            {
                documentCollections.Add(_collectionName);
            }

            if (documentCollections.Count <= 0)
                return DefaultVectorDimension;
            var firstCollection = documentCollections.Count > 0 ? documentCollections.First() : _collectionName;
            var collectionInfo = await _client.GetCollectionInfoAsync(firstCollection, cancellationToken);

            if (collectionInfo.Config?.Params?.VectorsConfig == null)
                return DefaultVectorDimension;
            var config = collectionInfo.Config.Params.VectorsConfig;

            var sizeProperty = config.GetType().GetProperty("Size");
            if (sizeProperty == null)
                return DefaultVectorDimension;
            var sizeValue = sizeProperty.GetValue(config);
            if (sizeValue is ulong size)
            {
                return (int)size;
            }

            return DefaultVectorDimension;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogQdrantCollectionDetectDimensionFailed(_logger, ex);
            return DefaultVectorDimension;
        }
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async Task InitializeCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (_collectionReady)
            return;

        await CollectionInitLock.WaitAsync(cancellationToken);

        try
        {
            if (_collectionReady)
                return;

            var collections = await _client.ListCollectionsAsync(cancellationToken);

            if (!collections.Contains(_collectionName))
            {
                var vectorDimension = await GetVectorDimensionAsync(cancellationToken);
                await CreateCollectionAsync(_collectionName, vectorDimension, cancellationToken);
            }
            else
            {
                try
                {
                    await _client.CreatePayloadIndexAsync(_collectionName, "content", PayloadSchemaType.Text, cancellationToken: cancellationToken);
                }
                catch
                {
                    // ignored
                }
            }

            _collectionReady = true;
        }
        finally
        {
            CollectionInitLock.Release();
        }
    }

    private static Distance GetDistanceMetric(string metric)
    {
        var lowerMetric = metric.ToLower(CultureInfo.InvariantCulture);
        return lowerMetric switch
        {
            "cosine" => Distance.Cosine,
            "dot" => Distance.Dot,
            "euclidean" => Distance.Euclid,
            _ => throw new ArgumentException($"Unknown distance metric: {metric}", nameof(metric)),
        };
    }

    /// <summary>
    /// Deletes a collection completely
    /// </summary>
    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _client.DeleteCollectionAsync(collectionName, cancellationToken: cancellationToken);
            ServiceLogMessages.LogQdrantCollectionDeleted(_logger, collectionName, null);

            if (collectionName == _collectionName)
            {
                _collectionReady = false;
            }
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogQdrantCollectionDeleteFailed(_logger, collectionName, ex);
            throw;
        }
    }

    /// <summary>
    /// Recreates a collection (deletes and creates anew)
    /// </summary>
    public async Task RecreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            await DeleteCollectionAsync(collectionName, cancellationToken);
            var vectorDimension = await GetVectorDimensionAsync(cancellationToken);
            await CreateCollectionAsync(collectionName, vectorDimension, cancellationToken);

            ServiceLogMessages.LogQdrantCollectionRecreated(_logger, collectionName, null);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogQdrantCollectionRecreateFailed(_logger, collectionName, ex);
            throw;
        }
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed || !disposing)
            return;
        _client.Dispose();
        _isDisposed = true;
    }
}

