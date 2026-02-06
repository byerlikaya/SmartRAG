using SmartRAG.Models;
using System.IO;

namespace SmartRAG.Interfaces.Parser.Strategies;


public interface IFileParser
{
    Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string? language = null);
    bool CanParse(string fileName, string contentType);
}

