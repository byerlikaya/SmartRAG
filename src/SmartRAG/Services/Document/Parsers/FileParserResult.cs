using System.Collections.Generic;

namespace SmartRAG.Services.Document.Parsers
{
    public class FileParserResult
    {
        public string Content { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
