using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for query word matching operations
    /// </summary>
    public class QueryWordMatcherService : IQueryWordMatcherService
    {
        private const int MinWordLengthForSubstringMatching = 4;
        private const int MaxSubstringLength = 8;

        /// <summary>
        /// Maps query words to documents that contain them
        /// </summary>
        public Dictionary<string, HashSet<Guid>> MapQueryWordsToDocuments(
            List<string> queryWords,
            List<Entities.Document> documents,
            List<DocumentChunk> scoredChunks,
            int chunksToCheckPerDocument)
        {
            if (queryWords == null)
                throw new ArgumentNullException(nameof(queryWords));
            if (documents == null)
                throw new ArgumentNullException(nameof(documents));
            if (scoredChunks == null)
                throw new ArgumentNullException(nameof(scoredChunks));

            var wordDocumentMap = new Dictionary<string, HashSet<Guid>>();
            foreach (var word in queryWords)
            {
                wordDocumentMap[word] = new HashSet<Guid>();
            }

            foreach (var doc in documents)
            {
                var docChunks = scoredChunks.Where(c => c.DocumentId == doc.Id).ToList();
                if (docChunks.Count == 0) continue;

                var chunksToCheck = docChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Take(chunksToCheckPerDocument)
                    .ToList();

                var docContent = string.Join(" ", chunksToCheck.Select(c => c.Content)).ToLowerInvariant();

                foreach (var word in queryWords)
                {
                    var wordLower = word.ToLowerInvariant();
                    if (IsWordInText(docContent, wordLower))
                    {
                        wordDocumentMap[word].Add(doc.Id);
                    }
                }
            }

            return wordDocumentMap;
        }

        /// <summary>
        /// Counts query word matches in content
        /// </summary>
        public (int MatchCount, int TotalOccurrences) CountQueryWordMatches(string content, List<string> queryWords)
        {
            if (string.IsNullOrEmpty(content) || queryWords == null || queryWords.Count == 0)
            {
                return (0, 0);
            }

            var queryWordMatches = 0;
            var totalQueryWordOccurrences = 0;

            foreach (var word in queryWords)
            {
                var wordLower = word.ToLowerInvariant();
                var wordFound = false;
                var occurrences = 0;

                var exactMatches = (content.Length - content.Replace(wordLower, "").Length) / wordLower.Length;
                if (exactMatches > 0)
                {
                    wordFound = true;
                    occurrences += exactMatches;
                }

                if (wordLower.Length >= MinWordLengthForSubstringMatching)
                {
                    for (int len = Math.Min(wordLower.Length, MaxSubstringLength); len >= MinWordLengthForSubstringMatching; len--)
                    {
                        for (int start = 0; start <= wordLower.Length - len; start++)
                        {
                            var substring = wordLower.Substring(start, len);
                            var substringMatches = (content.Length - content.Replace(substring, "").Length) / substring.Length;
                            if (substringMatches > 0)
                            {
                                wordFound = true;
                                occurrences += substringMatches;
                                break;
                            }
                        }
                        if (wordFound && exactMatches == 0) break;
                    }
                }

                if (wordFound)
                {
                    queryWordMatches++;
                    totalQueryWordOccurrences += occurrences;
                }
            }

            return (queryWordMatches, totalQueryWordOccurrences);
        }

        /// <summary>
        /// Finds unique keywords (keywords that appear only in one document)
        /// </summary>
        public int FindUniqueKeywords(Dictionary<string, HashSet<Guid>> wordDocumentMap, Guid documentId)
        {
            if (wordDocumentMap == null)
            {
                return 0;
            }

            var uniqueKeywordCount = 0;
            foreach (var kvp in wordDocumentMap)
            {
                if (kvp.Value.Count == 1 && kvp.Value.Contains(documentId))
                {
                    uniqueKeywordCount++;
                }
            }

            return uniqueKeywordCount;
        }

        /// <summary>
        /// Checks if a word exists in text with word boundaries (not as substring)
        /// </summary>
        public bool IsWordInText(string text, string word)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word))
            {
                return false;
            }

            var index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
            {
                var isStartBoundary = (index == 0) || !char.IsLetterOrDigit(text[index - 1]);
                var endIndex = index + word.Length;
                var isEndBoundary = (endIndex >= text.Length) || !char.IsLetterOrDigit(text[endIndex]);

                if (isStartBoundary && isEndBoundary)
                {
                    return true;
                }

                index++;
            }

            return false;
        }
    }
}

