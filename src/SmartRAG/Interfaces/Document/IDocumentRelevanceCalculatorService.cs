using SmartRAG.Entities;
using SmartRAG.Models;
using System;
using System.Collections.Generic;

namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for calculating document-level relevance scores
/// </summary>
public interface IDocumentRelevanceCalculatorService
{
    /// <summary>
    /// Calculates relevance scores for documents based on query words and chunk scores
    /// </summary>
    /// <param name="documents">List of documents to score</param>
    /// <param name="scoredChunks">List of scored document chunks</param>
    /// <param name="queryWords">Tokenized query words</param>
    /// <param name="wordDocumentMap">Map of query words to documents that contain them</param>
    /// <param name="topChunksPerDocument">Number of top chunks to consider per document</param>
    /// <param name="query">Original query for fileName phrase extraction (optional)</param>
    /// <param name="potentialNames">Entity names from query for fileName matching (optional)</param>
    /// <returns>List of document scores ordered by relevance</returns>
    List<DocumentScoreResult> CalculateDocumentScores(
        List<Entities.Document> documents,
        List<DocumentChunk> scoredChunks,
        List<string> queryWords,
        Dictionary<string, HashSet<Guid>> wordDocumentMap,
        int topChunksPerDocument,
        string query = null,
        List<string> potentialNames = null);

    /// <summary>
    /// Identifies relevant documents based on calculated scores
    /// </summary>
    /// <param name="documentScores">List of document scores</param>
    /// <param name="scoreThreshold">Threshold ratio for including second document (e.g., 0.8 = 80% of top score)</param>
    /// <returns>List of relevant documents</returns>
    List<Entities.Document> IdentifyRelevantDocuments(
        List<DocumentScoreResult> documentScores,
        double scoreThreshold);
}


