---
layout: default
title: Getting Started
description: Install and configure SmartRAG in your .NET application in just a few minutes
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Installation Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Installation</h2>
                    <p>SmartRAG is available as a NuGet package and supports .NET Standard 2.0/2.1, making it compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+ applications. Choose your preferred installation method:</p>
                    
                    <div class="code-example">
                        <div class="code-tabs">
                            <button class="code-tab active" data-tab="cli">.NET CLI</button>
                            <button class="code-tab" data-tab="pm">Package Manager</button>
                            <button class="code-tab" data-tab="xml">Package Reference</button>
                        </div>
                        
                        <div class="code-panel active" data-tab="cli">
                            <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" data-tab="pm">
                            <pre><code class="language-bash">Install-Package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" data-tab="xml">
                            <pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="2.3.0" /&gt;</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Configuration</h2>
                    <p>Configure SmartRAG in your <code>Program.cs</code> or <code>Startup.cs</code>:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
using SmartRAG;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG services
builder.Services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

var app = builder.Build();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Example Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Quick Example</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Here's a simple example using the actual SmartRAG implementation with conversation history:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Inject the document search service
public class SearchController : ControllerBase
{
    private readonly IDocumentSearchService _documentSearchService;
    
    public SearchController(IDocumentSearchService documentSearchService)
    {
        _documentSearchService = documentSearchService;
    }
    
    [HttpPost("search")]
    public async Task<ActionResult<object>> Search([FromBody] SearchRequest request)
    {
        string query = request?.Query ?? string.Empty;
        int maxResults = request?.MaxResults ?? 5;

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        try
        {
            var response = await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    [DefaultValue(5)]
    public int MaxResults { get; set; } = 5;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Conversation History Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>💬 Conversation History</h2>
                    <p>SmartRAG automatically manages conversation history using intelligent session management. Each conversation maintains context across multiple questions and answers without requiring manual session handling.</p>
                    
                    <h3>How It Works</h3>
                    <ul>
                        <li><strong>Automatic Session Management</strong>: Session IDs are generated and managed automatically</li>
                        <li><strong>Automatic Context</strong>: Previous questions and answers are automatically included in context</li>
                        <li><strong>Intelligent Truncation</strong>: Conversation history is intelligently truncated to maintain optimal performance</li>
                        <li><strong>Storage Integration</strong>: Conversation data is stored using your configured storage provider</li>
                    </ul>

                    <h3>Usage Example</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// First question
var firstRequest = new SearchRequest
{
    Query = "What is machine learning?",
    MaxResults = 5
};

// Follow-up question (remembers previous context automatically)
var followUpRequest = new SearchRequest
{
    Query = "Can you explain supervised learning in more detail?",
    MaxResults = 5
};

// Another follow-up
var anotherRequest = new SearchRequest
{
    Query = "What are the advantages of deep learning?",
    MaxResults = 5
};</code></pre>
                    </div>

                    <div class="alert alert-success">
                        <h4><i class="fas fa-lightbulb me-2"></i>Pro Tip</h4>
                        <p class="mb-0">SmartRAG automatically manages session IDs and conversation context. No need to manually handle session management!</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Next Steps Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Next Steps</h2>
                    <p>Now that you have SmartRAG installed and configured, explore these features:</p>
                    
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-cog"></i>
                                </div>
                                <h3>Configuration</h3>
                                <p>Learn about advanced configuration options and best practices.</p>
                                <a href="{{ site.baseurl }}/en/configuration" class="btn btn-outline-primary btn-sm">Configure</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-code"></i>
                                </div>
                                <h3>API Reference</h3>
                                <p>Explore the complete API documentation with examples.</p>
                                <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm">View API</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-comments"></i>
                                </div>
                                <h3>Examples</h3>
                                <p>See practical examples including conversation history usage.</p>
                                <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm">View Examples</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-rocket"></i>
                                </div>
                                <h3>Features</h3>
                                <p>Discover all SmartRAG features including conversation management.</p>
                                <a href="{{ site.baseurl }}/en/features" class="btn btn-outline-primary btn-sm">Explore Features</a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section help-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Need Help?</h4>
                        <p class="mb-0">If you encounter any issues or need assistance:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Open an issue on GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Contact support via email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>