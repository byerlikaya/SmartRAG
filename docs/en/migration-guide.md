---
layout: default
title: Migration Guide
description: Upgrade from previous versions and migrate existing implementations
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Overview Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Overview</h2>
                    <p>SmartRAG 2.0.0 introduces a major framework change from .NET 9.0 to .NET Standard 2.0/2.1. This migration provides maximum compatibility with legacy and enterprise .NET applications while maintaining all existing functionality.</p>
                    
                    <div class="alert alert-info">
                        <h4>🎯 Key Benefits</h4>
                        <ul>
                            <li><strong>Maximum Compatibility</strong>: Support for .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+</li>
                            <li><strong>Enterprise Integration</strong>: Seamless integration with existing enterprise solutions</li>
                            <li><strong>Legacy Support</strong>: Full compatibility with older .NET applications</li>
                            <li><strong>No Breaking Changes</strong>: All existing APIs remain unchanged</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Breaking Changes Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Breaking Changes</h2>
                    <p>While the public API remains unchanged, there are some framework-level changes to be aware of:</p>
                    
                    <h3>Target Framework</h3>
                    <ul>
                        <li><strong>Before</strong>: <code>net9.0</code></li>
                        <li><strong>After</strong>: <code>netstandard2.0;netstandard2.1</code></li>
                    </ul>
                    
                    <h3>Language Version</h3>
                    <ul>
                        <li><strong>Before</strong>: C# 12 features (file-scoped namespaces, collection expressions, etc.)</li>
                        <li><strong>After</strong>: C# 7.3 compatibility (traditional syntax)</li>
                    </ul>
                    
                    <h3>Package Versions</h3>
                    <ul>
                        <li><strong>Microsoft.Extensions.*</strong>: Downgraded from 9.0.8 to 7.0.0</li>
                        <li><strong>System.Text.Json</strong>: Updated to 8.0.0 for security</li>
                        <li><strong>Qdrant.Client</strong>: Downgraded from 1.15.1 to 1.7.0</li>
                        <li><strong>StackExchange.Redis</strong>: Downgraded from 2.9.11 to 2.6.111</li>
                        <li><strong>Microsoft.Data.Sqlite</strong>: Downgraded from 9.0.8 to 7.0.0</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Migration Steps Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Migration Steps</h2>
                    
                    <h3>Step 1: Update Package Reference</h3>
                    <p>Update your SmartRAG package reference to version 2.0.0:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;!-- Before --&gt;
&lt;PackageReference Include="SmartRAG" Version="1.1.0" /&gt;

&lt;!-- After --&gt;
&lt;PackageReference Include="SmartRAG" Version="2.0.0" /&gt;</code></pre>
                    </div>
                    
                    <h3>Step 2: Verify Framework Compatibility</h3>
                    <p>Ensure your application targets a compatible framework:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;!-- Compatible Target Frameworks --&gt;
&lt;TargetFramework&gt;net48&lt;/TargetFramework&gt;           &lt;!-- .NET Framework 4.8 --&gt;
&lt;TargetFramework&gt;netcoreapp2.0&lt;/TargetFramework&gt;    &lt;!-- .NET Core 2.0 --&gt;
&lt;TargetFramework&gt;net5.0&lt;/TargetFramework&gt;           &lt;!-- .NET 5 --&gt;
&lt;TargetFramework&gt;net6.0&lt;/TargetFramework&gt;           &lt;!-- .NET 6 --&gt;
&lt;TargetFramework&gt;net7.0&lt;/TargetFramework&gt;           &lt;!-- .NET 7 --&gt;
&lt;TargetFramework&gt;net8.0&lt;/TargetFramework&gt;           &lt;!-- .NET 8 --&gt;
&lt;TargetFramework&gt;net9.0&lt;/TargetFramework&gt;           &lt;!-- .NET 9 --&gt;</code></pre>
                    </div>
                    
                    <h3>Step 3: Test Your Application</h3>
                    <p>After updating, test your application to ensure everything works correctly:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-bash"># Build your project
dotnet build

# Run your tests
dotnet test

# Test your application
dotnet run</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Compatibility Matrix Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Framework Compatibility Matrix</h2>
                    
                    <div class="table-responsive">
                        <table class="table table-striped">
                            <thead>
                                <tr>
                                    <th>Framework</th>
                                    <th>Version</th>
                                    <th>Compatibility</th>
                                    <th>Notes</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>.NET Framework</td>
                                    <td>4.6.1+</td>
                                    <td>✅ Full</td>
                                    <td>Recommended: 4.7.2+</td>
                                </tr>
                                <tr>
                                    <td>.NET Core</td>
                                    <td>2.0+</td>
                                    <td>✅ Full</td>
                                    <td>Recommended: 3.1+</td>
                                </tr>
                                <tr>
                                    <td>.NET</td>
                                    <td>5.0+</td>
                                    <td>✅ Full</td>
                                    <td>All versions supported</td>
                                </tr>
                                <tr>
                                    <td>.NET Standard</td>
                                    <td>2.0/2.1</td>
                                    <td>✅ Full</td>
                                    <td>Direct support</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </section>

        <!-- Troubleshooting Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Troubleshooting</h2>
                    
                    <h3>Common Issues</h3>
                    
                    <h4>Package Version Conflicts</h4>
                    <p>If you encounter package version conflicts, ensure your project uses compatible package versions:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;!-- Recommended package versions for .NET Standard compatibility --&gt;
&lt;PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="7.0.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="7.0.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="7.0.0" /&gt;
&lt;PackageReference Include="System.Text.Json" Version="8.0.0" /&gt;</code></pre>
                    </div>
                    
                    <h4>Build Errors</h4>
                    <p>If you encounter build errors, ensure your project targets a compatible framework version.</p>
                    
                    <h4>Runtime Errors</h4>
                    <p>All runtime behavior remains the same. If you encounter issues, check your framework version compatibility.</p>
                </div>
            </div>
        </section>

        <!-- Support Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Need Help?</h2>
                    <p>If you encounter any issues during migration:</p>
                    
                    <ul>
                        <li>📖 <strong>Documentation</strong>: Check our comprehensive documentation</li>
                        <li>🐛 <strong>Issues</strong>: Report bugs on <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub</a></li>
                        <li>💬 <strong>Discussions</strong>: Join community discussions on <a href="https://github.com/byerlikaya/SmartRAG/discussions" target="_blank">GitHub Discussions</a></li>
                        <li>⭐ <strong>Star</strong>: Show your support by starring the repository</li>
                    </ul>
                </div>
            </div>
        </section>
    </div>
</div>
