using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts;

public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    public int MaxResults { get; set; } = 5;
}


