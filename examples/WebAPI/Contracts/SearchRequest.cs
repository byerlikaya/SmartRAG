using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace SmartRAG.API.Contracts;

public class SearchRequest
{
    [Required(ErrorMessage = "Query is required")]
    [Description("The search query to find relevant documents")]
    public string Query { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "MaxResults must be between 1 and 20")]
    [DefaultValue(5)]
    [Description("Maximum number of document chunks to return (1-20, default: 5)")]
    public int MaxResults { get; set; } = 5;
}


