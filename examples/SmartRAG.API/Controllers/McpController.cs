using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartRAG.Interfaces.Mcp;
using SmartRAG.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers;


/// <summary>
/// Controller for interacting with MCP (Model Context Protocol) servers and tools
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class McpController : ControllerBase
{
    private readonly IMcpIntegrationService _mcpIntegrationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpController"/> class.
    /// </summary>
    /// <param name="mcpIntegrationService">Service for integrating MCP servers with SmartRAG.</param>
    public McpController(IMcpIntegrationService mcpIntegrationService)
    {
        _mcpIntegrationService = mcpIntegrationService;
    }

    /// <summary>
    /// Gets all available tools from all connected MCP servers.
    /// </summary>
    /// <returns>List of available MCP tools grouped by server.</returns>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAvailableTools()
    {
        var tools = await _mcpIntegrationService.GetAvailableToolsAsync();

        var grouped = tools
            .GroupBy(t => string.IsNullOrWhiteSpace(t.ServerId) ? "Unknown" : t.ServerId)
            .Select(g => new
            {
                ServerId = g.Key,
                Tools = g.Select(t => new
                {
                    t.Name,
                    t.Description
                }).ToList()
            })
            .ToList();

        return Ok(new
        {
            TotalServers = grouped.Count,
            TotalTools = tools.Count,
            Servers = grouped
        });
    }

    /// <summary>
    /// Calls a specific MCP tool on a given server with the provided parameters.
    /// </summary>
    /// <param name="serverId">MCP server identifier.</param>
    /// <param name="toolName">Tool name to call.</param>
    /// <param name="parameters">Tool parameters as key-value pairs.</param>
    /// <returns>Result of the MCP tool execution.</returns>
    [HttpPost("tools/{serverId}/{toolName}")]
    [ProducesResponseType(typeof(McpToolResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<McpToolResult>> CallTool(
        string serverId,
        string toolName,
        [FromBody] Dictionary<string, object> parameters)
    {
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return BadRequest("ServerId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return BadRequest("ToolName cannot be empty.");
        }

        parameters ??= new Dictionary<string, object>();

        var result = await _mcpIntegrationService.CallToolAsync(serverId, toolName, parameters);
        return Ok(result);
    }
}




