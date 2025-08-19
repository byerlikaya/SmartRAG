using Microsoft.AspNetCore.Mvc;
using SmartRAG.Interfaces;

namespace SmartRAG.API.Controllers;

/// <summary>
/// AI-powered search controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SearchController(IDocumentSearchService documentSearchService) : ControllerBase
{
    /// <summary>
    /// Search documents using RAG (Retrieval-Augmented Generation)
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Search([FromBody] Contracts.SearchRequest request)
    {
        string? query = request?.Query;
        int maxResults = request?.MaxResults ?? 5;

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        try
        {
            var response = await documentSearchService.GenerateRagAnswerAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
