using SmartRAG.Entities;
using System;
using System.Linq;

namespace SmartRAG.Services.Helpers
{
    public static class DocumentValidator
    {
        #region Constants
        private const int MaxChunkContentLength = 1000000; // 1MB limit for chunk content
        private const int MaxChunkIndex = 10000; // Reasonable limit for chunk index
        private const int MinRelevanceScore = -1;
        private const int MaxRelevanceScore = 1;
        private const int MaxEmbeddingVectorSize = 10000; // Reasonable limit for embedding vector size
        private const int MinEmbeddingValue = -1000;
        private const int MaxEmbeddingValue = 1000;
        private const int MaxAbsoluteValue = 100;
        private const int MinValueThreshold = 0;
        private const int OpenAIVectorDimension = 1536;
        private const int SentenceTransformersDimension = 768;
        #endregion

        public static void ValidateDocument(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (string.IsNullOrEmpty(document.FileName))
                throw new ArgumentException("FileName cannot be null or empty", nameof(document));

            if (string.IsNullOrEmpty(document.Content))
                throw new ArgumentException("Content cannot be null or empty", nameof(document));

            if (document.Chunks == null || document.Chunks.Count == 0)
                throw new ArgumentException("Document must have at least one chunk", nameof(document));
        }

        public static void ValidateChunks(Document document)
        {
            foreach (var chunk in document.Chunks)
            {
                if (chunk == null)
                    throw new ArgumentException($"Chunk cannot be null for document {document.FileName} (ID: {document.Id})");

                if (string.IsNullOrEmpty(chunk.Content))
                    throw new ArgumentException($"Chunk content cannot be null or empty for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.ChunkIndex < 0)
                    throw new ArgumentException($"Chunk index cannot be negative for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Id == Guid.Empty)
                    throw new ArgumentException($"Chunk ID cannot be empty for chunk in document {document.FileName} (ID: {document.Id})");

                if (chunk.DocumentId != document.Id)
                    throw new ArgumentException($"Chunk DocumentId mismatch: chunk has {chunk.DocumentId}, document has {document.Id}");

                if (chunk.CreatedAt == default)
                    throw new ArgumentException($"Chunk CreatedAt cannot be default for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.Content.Length > MaxChunkContentLength)
                    throw new ArgumentException($"Chunk content too large ({chunk.Content.Length} characters) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.ChunkIndex > MaxChunkIndex)
                    throw new ArgumentException($"Chunk index too large ({chunk.ChunkIndex}) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                if (chunk.RelevanceScore < MinRelevanceScore || chunk.RelevanceScore > MaxRelevanceScore)
                    throw new ArgumentException($"Chunk RelevanceScore must be between {MinRelevanceScore} and {MaxRelevanceScore} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

                ValidateChunkEmbedding(chunk, document);
            }
        }

        private static void ValidateChunkEmbedding(DocumentChunk chunk, Document document)
        {
            if (chunk.Embedding == null) return;

            if (chunk.Embedding.Count > MaxEmbeddingVectorSize)
                throw new ArgumentException($"Chunk embedding vector too large ({chunk.Embedding.Count} dimensions) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Any(f => float.IsNaN(f) || float.IsInfinity(f)))
                throw new ArgumentException($"Chunk embedding contains invalid values (NaN or Infinity) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Any(f => f < MinEmbeddingValue || f > MaxEmbeddingValue))
                throw new ArgumentException($"Chunk embedding contains values outside reasonable range [{MinEmbeddingValue}, {MaxEmbeddingValue}] for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count != 0 && chunk.Embedding.Count != SentenceTransformersDimension && chunk.Embedding.Count != OpenAIVectorDimension)
                throw new ArgumentException($"Chunk embedding vector size must be 0, {SentenceTransformersDimension}, or {OpenAIVectorDimension} dimensions, got {chunk.Embedding.Count} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.All(f => f == 0))
                throw new ArgumentException($"Chunk embedding vector contains only zeros for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.All(f => Math.Abs(f) < MinValueThreshold))
                throw new ArgumentException($"Chunk embedding vector contains only very small values (< {MinValueThreshold}) for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => Math.Abs(f) > MaxAbsoluteValue))
                throw new ArgumentException($"Chunk embedding vector contains values with absolute value > {MaxAbsoluteValue} for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => f != 0 && Math.Abs(f) < float.Epsilon))
                throw new ArgumentException($"Chunk embedding vector contains subnormal values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");

            if (chunk.Embedding.Count > 0 && chunk.Embedding.Any(f => float.IsNegativeInfinity(f) || float.IsPositiveInfinity(f)))
                throw new ArgumentException($"Chunk embedding vector contains infinity values for chunk {chunk.Id} in document {document.FileName} (ID: {document.Id})");
        }
    }
}
