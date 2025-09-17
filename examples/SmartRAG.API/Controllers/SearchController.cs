using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartRAG.Interfaces;
using System;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers
{
    /// <summary>
    /// AI-powered search and conversation controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SearchController : ControllerBase
    {
        private readonly IDocumentSearchService _documentSearchService;

        public SearchController(IDocumentSearchService documentSearchService)
        {
            _documentSearchService = documentSearchService;
        }

        /// <summary>
        /// Intelligent AI search with automatic query routing
        /// </summary>
        /// <param name="request">Search request with query and parameters</param>
        /// <returns>AI-generated response</returns>
        [HttpPost("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<object>> Search([FromBody] Contracts.SearchRequest request)
        {
            string? query = request?.Query;
            int maxResults = request?.MaxResults ?? 5;

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            try
            {
                var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}