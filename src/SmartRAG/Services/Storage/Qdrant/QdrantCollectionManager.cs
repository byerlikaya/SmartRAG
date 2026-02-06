using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartRAG.Interfaces.Storage.Qdrant;
using SmartRAG.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Storage.Qdrant;


/// <summary>
/// Service for managing Qdrant collections and document storage
/// </summary>
public class QdrantCollectionManager : IQdrantCollectionManager, IDisposable
{
    private const int DefaultGrpcTimeoutMinutes = 5;
    private const int DefaultVectorDimension = 768;

    private readonly QdrantClient _client;
    private readonly QdrantConfig _config;
    private readonly string _collectionName;
    private readonly ILogger<QdrantCollectionManager> _logger;
    private static readonly SemaphoreSlim _collectionInitLock = new SemaphoreSlim(1, 1);
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
                _logger.LogError(ex, "Failed to initialize Qdrant collection");
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

            _logger.LogInformation("Creating Qdrant collection: {CollectionName} with dimension: {Dimension}",
                collectionName, vectorDimension);

            await _client.CreateCollectionAsync(collectionName, vectorParams);
            await _client.CreatePayloadIndexAsync(collectionName, "content", global::Qdrant.Client.Grpc.PayloadSchemaType.Text);
            _logger.LogInformation("Created text index for 'content' field in collection: {CollectionName}", collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Qdrant collection: {CollectionName}", collectionName);
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
                var collections = await _client.ListCollectionsAsync();
                if (collections.Contains(collectionName))
                    return;
            }

            int vectorDimension;
            
            if (document?.Chunks != null && document.Chunks.Count > 0)
            {
                var firstChunkWithEmbedding = document.Chunks.FirstOrDefault(c => c.Embedding != null && c.Embedding.Count > 0);
                if (firstChunkWithEmbedding != null)
                {
                    vectorDimension = firstChunkWithEmbedding.Embedding.Count;
                    _logger.LogDebug("Using embedding dimension {Dimension} from document chunk for collection {CollectionName}", vectorDimension, collectionName);
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
            _logger.LogError(ex, "Failed to ensure document collection exists: {CollectionName}", collectionName);
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

            var collections = await _client.ListCollectionsAsync();
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

            if (documentCollections.Count == 0 && collections.Contains(_collectionName))
            {
                documentCollections.Add(_collectionName);
            }

            if (documentCollections.Count > 0)
            {
                var firstCollection = documentCollections.Count > 0 ? documentCollections.First() : _collectionName;
                var collectionInfo = await _client.GetCollectionInfoAsync(firstCollection);

                if (collectionInfo.Config?.Params?.VectorsConfig != null)
                {
                    var config = collectionInfo.Config.Params.VectorsConfig;

                    var sizeProperty = config.GetType().GetProperty("Size");
                    if (sizeProperty != null)
                    {
                        var sizeValue = sizeProperty.GetValue(config);
                        if (sizeValue is ulong size)
                        {
                            return (int)size;
                        }
                    }
                }
            }

            return DefaultVectorDimension;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect vector dimension, using default");
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

        await _collectionInitLock.WaitAsync(cancellationToken);

        try
        {
            if (_collectionReady)
                return;

            var collections = await _client.ListCollectionsAsync();

            if (!collections.Contains(_collectionName))
            {
                var vectorDimension = await GetVectorDimensionAsync(cancellationToken);
                await CreateCollectionAsync(_collectionName, vectorDimension, cancellationToken);
            }
            else
            {
                try
                {
                    await _client.CreatePayloadIndexAsync(_collectionName, "content", global::Qdrant.Client.Grpc.PayloadSchemaType.Text);
                }
                catch
                {
                }
            }

            _collectionReady = true;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _collectionInitLock.Release();
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
        try
        {
            await _client.DeleteCollectionAsync(collectionName);
            _logger.LogInformation("Deleted Qdrant collection: {CollectionName}", collectionName);

            if (collectionName == _collectionName)
            {
                _collectionReady = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection: {CollectionName}", collectionName);
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

            _logger.LogInformation("Recreated Qdrant collection: {CollectionName}", collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recreate collection: {CollectionName}", collectionName);
            throw;
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            _client?.Dispose();
            _isDisposed = true;
        }
    }
}

