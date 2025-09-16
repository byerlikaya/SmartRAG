using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for managing Qdrant collections and document storage
    /// </summary>
    public class QdrantCollectionManager : IQdrantCollectionManager, IDisposable
    {
        #region Constants

        private const int DefaultGrpcTimeoutMinutes = 5;
        private const int DefaultVectorDimension = 768;

        #endregion

        #region Fields

        private readonly QdrantClient _client;
        private readonly QdrantConfig _config;
        private readonly string _collectionName;
        private readonly ILogger<QdrantCollectionManager> _logger;
        private static readonly SemaphoreSlim _collectionInitLock = new SemaphoreSlim(1, 1);
        private bool _collectionReady;
        private bool _isDisposed;

        #endregion

        #region Constructor

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
                    await InitializeCollectionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Qdrant collection");
                }
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensures the main collection exists and is ready for operations
        /// </summary>
        public async Task EnsureCollectionExistsAsync()
        {
            if (_collectionReady)
                return;

            await InitializeCollectionAsync();
        }

        /// <summary>
        /// Creates a new collection with specified vector parameters
        /// </summary>
        /// <param name="collectionName">Name of the collection to create</param>
        /// <param name="vectorDimension">Dimension of vectors to store</param>
        public async Task CreateCollectionAsync(string collectionName, int vectorDimension)
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
        public async Task EnsureDocumentCollectionExistsAsync(string collectionName, SmartRAG.Entities.Document document)
        {
            try
            {
                // Check if collection already exists (fast check)
                if (_collectionReady)
                {
                    var collections = await _client.ListCollectionsAsync();
                    if (collections.Contains(collectionName))
                        return;
                }

                // Get vector dimension dynamically
                var vectorDimension = await GetVectorDimensionAsync();

                await CreateCollectionAsync(collectionName, vectorDimension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure document collection exists: {CollectionName}", collectionName);
                throw;
            }
        }

        /// <summary>
        /// Gets the vector dimension for collections
        /// </summary>
        public async Task<int> GetVectorDimensionAsync()
        {
            try
            {
                // First try to get from config
                if (_config.VectorSize > 0)
                {
                    return _config.VectorSize;
                }

                // If config doesn't have it, detect from existing collections
                var collections = await _client.ListCollectionsAsync();
                var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

                // If no document collections, check main collection
                if (documentCollections.Count == 0 && collections.Contains(_collectionName))
                {
                    documentCollections.Add(_collectionName);
                }

                if (documentCollections.Count > 0)
                {
                    // Get dimension from first available collection
                    var firstCollection = documentCollections.Count > 0 ? documentCollections.First() : _collectionName;
                    var collectionInfo = await _client.GetCollectionInfoAsync(firstCollection);

                    // Try to get dimension from collection info
                    if (collectionInfo.Config?.Params?.VectorsConfig != null)
                    {
                        var config = collectionInfo.Config.Params.VectorsConfig;

                        // Try to access size property (might be named differently)
                        var sizeProperty = config.GetType().GetProperty("Size");
                        if (sizeProperty != null)
                        {
                            var sizeValue = sizeProperty.GetValue(config);
                            if (sizeValue is ulong size)
                            {
                                _logger.LogDebug("Detected vector dimension: {Dimension} from collection: {Collection}", 
                                    (int)size, firstCollection);
                                return (int)size;
                            }
                        }
                    }
                }

                // Default fallback
                _logger.LogDebug("Using default vector dimension: {Dimension}", DefaultVectorDimension);
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

        #endregion

        #region Private Methods

        private async Task InitializeCollectionAsync()
        {
            if (_collectionReady)
                return;

            await _collectionInitLock.WaitAsync();

            try
            {
                if (_collectionReady)
                    return;

                var collections = await _client.ListCollectionsAsync();

                if (!collections.Contains(_collectionName))
                {
                    var vectorDimension = await GetVectorDimensionAsync();
                    await CreateCollectionAsync(_collectionName, vectorDimension);
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
            switch (lowerMetric)
            {
                case "cosine":
                    return Distance.Cosine;
                case "dot":
                    return Distance.Dot;
                case "euclidean":
                    return Distance.Euclid;
                default:
                    return Distance.Cosine;
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

        #endregion
    }
}
