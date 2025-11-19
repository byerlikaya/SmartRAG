using SmartRAG.Services.Document.Parsers;
using System.IO;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Parser.Strategies
{
    public interface IFileParser
    {
        Task<FileParserResult> ParseAsync(Stream fileStream, string fileName);
        bool CanParse(string fileName, string contentType);
    }
}
