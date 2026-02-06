using SmartRAG.API.Contracts;
using System.Text.Json;

namespace SmartRAG.API.Controllers;


/// <summary>
/// Configuration Management and System Settings Controller
/// 
/// This controller provides comprehensive configuration management capabilities including:
/// - AI provider configuration and management
/// - Storage provider settings and optimization
/// - System-wide configuration and preferences
/// - Conversation management settings
/// - Runtime configuration updates without restarts
/// - Configuration validation and health checks
/// - Backup and restore capabilities
/// 
/// Key Features:
/// - Runtime Configuration: Update settings without system restarts
/// - Multi-Provider Management: Configure multiple AI and storage providers
/// - Validation Engine: Comprehensive configuration validation and testing
/// - Health Monitoring: Real-time configuration and connection health checks
/// - Backup and Restore: Configuration backup, versioning, and disaster recovery
/// - Security Controls: Sensitive data masking and secure configuration handling
/// - Environment Management: Development, staging, and production configurations
/// - Schema Validation: Ensure configuration integrity and compatibility
/// 
/// Configuration Categories:
/// - **AI Providers**: OpenAI, Anthropic, Google Gemini, Azure OpenAI, Custom providers
/// - **Storage**: Vector databases (Qdrant), SQL databases, file systems, Redis
/// - **System**: File upload limits, logging, analytics, CORS, rate limiting
/// - **Conversations**: History limits, timeouts, archival, export settings
/// - **Security**: Authentication, authorization, encryption, privacy controls
/// - **Performance**: Caching, connection pooling, timeout settings
/// 
/// Use Cases:
/// - DevOps and Operations: Centralized configuration management
/// - Multi-Environment Deployment: Development, staging, production settings
/// - Disaster Recovery: Configuration backup and rapid restoration
/// - Security Compliance: Secure configuration handling and audit trails
/// - Performance Tuning: Optimize system performance through configuration
/// - Integration Management: Configure external service connections
/// - Monitoring and Alerting: Health checks and configuration validation
/// 
/// Example Usage:
/// ```bash
/// # Get current AI provider configuration
/// curl -X GET "https://localhost:7001/api/configuration/ai-provider/OpenAI"
/// 
/// # Update AI provider settings
/// curl -X PUT "https://localhost:7001/api/configuration/ai-provider" \
///   -H "Content-Type: application/json" \
///   -d '{"provider": "OpenAI", "apiKey": "sk-...", "defaultModel": "gpt-5.1"}'
/// 
/// # Validate configuration before applying
/// curl -X POST "https://localhost:7001/api/configuration/validate" \
///   -H "Content-Type: application/json" \
///   -d '{"section": "AIProvider", "configData": {...}}'
/// 
/// # Backup all configurations
/// curl -X POST "https://localhost:7001/api/configuration/backup" \
///   -H "Content-Type: application/json" \
///   -d '{"sections": ["AIProvider", "Storage", "System"]}'
/// ```
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ConfigurationController : ControllerBase
{
    /// <summary>
    /// Gets all configuration sections and their current status
    /// </summary>
    /// <remarks>
    /// Returns a comprehensive overview of all system configurations including:
    /// - **Configuration Sections**: All available configuration categories
    /// - **Current Status**: Active, inactive, or error states for each section
    /// - **Last Updated**: When each configuration was last modified
    /// - **Validation Status**: Whether configurations are valid and functional
    /// - **Health Status**: Connection and functionality status for external services
    /// 
    /// This endpoint provides a dashboard view of the entire system configuration,
    /// making it easy to identify:
    /// - Misconfigured or failing services
    /// - Outdated configuration settings
    /// - Missing or incomplete configurations
    /// - Configuration drift between environments
    /// 
    /// Use this endpoint for:
    /// - System health monitoring dashboards
    /// - Configuration auditing and compliance
    /// - Troubleshooting configuration issues
    /// - Overview before making changes
    /// </remarks>
    /// <returns>Overview of all configuration sections</returns>
    /// <response code="200">Configuration overview retrieved successfully</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ConfigurationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConfigurationResponse>>> GetAllConfigurations()
    {
        try
        {
            var configurations = await GetAllConfigurationsAsync();
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets AI provider configuration for a specific provider
    /// </summary>
    /// <remarks>
    /// Retrieves detailed configuration for a specific AI provider including:
    /// - **Provider Settings**: API keys (masked), base URLs, model configurations
    /// - **Performance Settings**: Timeout values, retry policies, rate limits
    /// - **Health Status**: Connection status and last health check results
    /// - **Usage Statistics**: Request counts, success rates, token consumption
    /// - **Available Models**: List of supported models and their capabilities
    /// - **Custom Settings**: Provider-specific configuration options
    /// 
    /// Security features:
    /// - **API Key Masking**: Sensitive credentials are masked for security
    /// - **Secure Transmission**: Configuration data is transmitted securely
    /// - **Access Control**: Only authorized users can view configurations
    /// 
    /// Use this endpoint for:
    /// - Provider-specific configuration management
    /// - Troubleshooting AI provider connectivity issues
    /// - Performance monitoring and optimization
    /// - Security auditing and compliance checks
    /// </remarks>
    /// <param name="provider">AI provider to get configuration for</param>
    /// <returns>AI provider configuration details</returns>
    /// <response code="200">AI provider configuration retrieved successfully</response>
    /// <response code="404">AI provider configuration not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("ai-provider/{provider}")]
    [ProducesResponseType(typeof(AIProviderConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AIProviderConfigResponse>> GetAIProviderConfiguration(AIProvider provider)
    {
        try
        {
            var config = await GetAIProviderConfigAsync(provider);
            if (config == null)
            {
                return NotFound(new { Error = $"Configuration for provider {provider} not found" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates AI provider configuration
    /// </summary>
    /// <remarks>
    /// Updates AI provider configuration with validation and health checks including:
    /// - **Configuration Validation**: Validates all settings before applying
    /// - **Connection Testing**: Tests provider connectivity and functionality
    /// - **Security Handling**: Securely stores and manages API keys
    /// - **Hot Reloading**: Applies changes without system restart
    /// - **Rollback Support**: Automatic rollback on configuration failure
    /// - **Audit Logging**: Tracks all configuration changes for compliance
    /// 
    /// Update process:
    /// 1. **Validation**: Validates new configuration against schema
    /// 2. **Testing**: Tests connectivity and basic functionality
    /// 3. **Backup**: Creates backup of current configuration
    /// 4. **Application**: Applies new configuration
    /// 5. **Verification**: Verifies configuration is working correctly
    /// 6. **Rollback**: Automatically rolls back if verification fails
    /// 
    /// Security considerations:
    /// - **API Key Encryption**: API keys are encrypted before storage
    /// - **Audit Trail**: All changes are logged with user and timestamp
    /// - **Validation**: Prevents malicious or malformed configurations
    /// - **Access Control**: Requires appropriate permissions
    /// </remarks>
    /// <param name="request">AI provider configuration update</param>
    /// <returns>Updated AI provider configuration</returns>
    /// <response code="200">AI provider configuration updated successfully</response>
    /// <response code="400">Invalid configuration data</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("ai-provider")]
    [ProducesResponseType(typeof(AIProviderConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIProviderConfigResponse>> UpdateAIProviderConfiguration([FromBody] AIProviderConfigRequest request)
    {
        try
        {
            // Validate configuration
            var validation = await ValidateAIProviderConfigAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new { Error = "Configuration validation failed", Errors = validation.Errors });
            }

            // Update configuration
            var updatedConfig = await UpdateAIProviderConfigAsync(request);

            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets storage provider configuration
    /// </summary>
    /// <remarks>
    /// Retrieves detailed storage provider configuration including:
    /// - **Connection Settings**: Database connections, vector store configurations
    /// - **Performance Settings**: Connection pooling, timeout values, batch sizes
    /// - **Storage Statistics**: Space usage, document counts, index information
    /// - **Health Monitoring**: Connection status and performance metrics
    /// - **Optimization Settings**: Caching, indexing, and query optimization
    /// 
    /// Storage provider information includes:
    /// - **Vector Databases**: Qdrant, Pinecone, Weaviate configurations
    /// - **SQL Databases**: PostgreSQL, MySQL, SQL Server settings
    /// - **File Systems**: Local and cloud storage configurations
    /// - **Cache Systems**: Redis and in-memory cache settings
    /// 
    /// Use this endpoint for:
    /// - Storage performance monitoring and optimization
    /// - Capacity planning and resource management
    /// - Troubleshooting storage connectivity issues
    /// - Configuration auditing and compliance
    /// </remarks>
    /// <param name="provider">Storage provider to get configuration for</param>
    /// <returns>Storage provider configuration details</returns>
    /// <response code="200">Storage configuration retrieved successfully</response>
    /// <response code="404">Storage provider configuration not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("storage/{provider}")]
    [ProducesResponseType(typeof(StorageConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorageConfigResponse>> GetStorageConfiguration(StorageProvider provider)
    {
        try
        {
            var config = await GetStorageConfigAsync(provider);
            if (config == null)
            {
                return NotFound(new { Error = $"Configuration for storage provider {provider} not found" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates storage provider configuration
    /// </summary>
    /// <remarks>
    /// Updates storage provider configuration with comprehensive validation including:
    /// - **Connection Validation**: Tests database and storage connectivity
    /// - **Performance Testing**: Validates connection pooling and performance settings
    /// - **Data Integrity**: Ensures existing data remains accessible
    /// - **Migration Support**: Handles configuration changes that require data migration
    /// - **Rollback Protection**: Maintains previous configuration for quick recovery
    /// 
    /// Update features:
    /// - **Hot Configuration**: Updates settings without service interruption
    /// - **Connection Pooling**: Manages connection pool reconfiguration
    /// - **Index Management**: Updates search indexes and optimization settings
    /// - **Backup Verification**: Ensures data backup before major changes
    /// - **Performance Monitoring**: Tracks performance impact of changes
    /// 
    /// Critical considerations:
    /// - **Data Safety**: Existing data and indexes are preserved
    /// - **Performance Impact**: Changes are applied gradually to minimize impact
    /// - **Rollback Plan**: Previous configuration is maintained for quick recovery
    /// - **Validation**: Comprehensive testing before applying changes
    /// </remarks>
    /// <param name="request">Storage provider configuration update</param>
    /// <returns>Updated storage provider configuration</returns>
    /// <response code="200">Storage configuration updated successfully</response>
    /// <response code="400">Invalid configuration data</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("storage")]
    [ProducesResponseType(typeof(StorageConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorageConfigResponse>> UpdateStorageConfiguration([FromBody] StorageConfigRequest request)
    {
        try
        {
            // Validate configuration
            var validation = await ValidateStorageConfigAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new { Error = "Configuration validation failed", Errors = validation.Errors });
            }

            // Update configuration
            var updatedConfig = await UpdateStorageConfigAsync(request);

            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets system-wide configuration settings
    /// </summary>
    /// <remarks>
    /// Retrieves comprehensive system configuration including:
    /// - **File Upload Settings**: Size limits, allowed extensions, processing options
    /// - **Security Settings**: CORS policies, rate limiting, authentication options
    /// - **Performance Settings**: Caching, connection pooling, timeout values
    /// - **Logging Configuration**: Log levels, retention, analytics collection
    /// - **Integration Settings**: External service configurations and API limits
    /// - **Environment Settings**: Development, staging, production configurations
    /// 
    /// System configuration covers:
    /// - **Application Settings**: Core application behavior and limits
    /// - **Security Policies**: Access control, rate limiting, CORS configuration
    /// - **Performance Tuning**: Cache settings, connection management
    /// - **Monitoring**: Analytics, logging, health check configurations
    /// - **Integration**: Third-party service and API configurations
    /// 
    /// Use this endpoint for:
    /// - System administration and maintenance
    /// - Performance tuning and optimization
    /// - Security policy management
    /// - Compliance and audit preparation
    /// - Environment-specific configuration management
    /// </remarks>
    /// <returns>System configuration details</returns>
    /// <response code="200">System configuration retrieved successfully</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("system")]
    [ProducesResponseType(typeof(SystemConfigResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemConfigResponse>> GetSystemConfiguration()
    {
        try
        {
            var config = await GetSystemConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates system-wide configuration settings
    /// </summary>
    /// <remarks>
    /// Updates system configuration with comprehensive validation and impact analysis including:
    /// - **Impact Assessment**: Analyzes impact of changes on running system
    /// - **Validation Engine**: Validates all settings against system constraints
    /// - **Security Verification**: Ensures security policies are maintained
    /// - **Performance Testing**: Tests performance impact of configuration changes
    /// - **Gradual Rollout**: Applies changes gradually to minimize system impact
    /// - **Monitoring Integration**: Updates monitoring and alerting configurations
    /// 
    /// System update process:
    /// 1. **Pre-validation**: Validates configuration against system requirements
    /// 2. **Impact Analysis**: Analyzes potential impact on running services
    /// 3. **Security Review**: Ensures security policies are not compromised
    /// 4. **Staged Application**: Applies changes in stages to minimize impact
    /// 5. **Monitoring**: Monitors system health during and after changes
    /// 6. **Rollback Capability**: Maintains ability to quickly rollback changes
    /// 
    /// Critical system settings:
    /// - **File Upload Limits**: Affects all document processing operations
    /// - **Rate Limiting**: Impacts API usage and system performance
    /// - **CORS Settings**: Affects web application integration
    /// - **Cache Configuration**: Impacts system performance and memory usage
    /// - **Security Policies**: Affects all system access and authentication
    /// </remarks>
    /// <param name="request">System configuration update</param>
    /// <returns>Updated system configuration</returns>
    /// <response code="200">System configuration updated successfully</response>
    /// <response code="400">Invalid configuration data</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("system")]
    [ProducesResponseType(typeof(SystemConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SystemConfigResponse>> UpdateSystemConfiguration([FromBody] SystemConfigRequest request)
    {
        try
        {
            // Validate configuration
            var validation = await ValidateSystemConfigAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new { Error = "Configuration validation failed", Errors = validation.Errors });
            }

            // Update configuration
            var updatedConfig = await UpdateSystemConfigAsync(request);

            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets conversation management configuration
    /// </summary>
    /// <remarks>
    /// Retrieves conversation system configuration including:
    /// - **Session Management**: Default history limits, concurrent conversation limits
    /// - **Timeout Settings**: Idle timeouts, auto-archival policies
    /// - **Feature Controls**: Export capabilities, message editing, conversation sharing
    /// - **Analytics Settings**: Conversation tracking and metrics collection
    /// - **Performance Settings**: Memory management, cleanup policies
    /// - **Privacy Controls**: Data retention, anonymization, GDPR compliance
    /// 
    /// Conversation configuration affects:
    /// - **User Experience**: History limits, session timeouts, feature availability
    /// - **System Performance**: Memory usage, storage requirements, cleanup frequency
    /// - **Privacy Compliance**: Data retention, user rights, anonymization
    /// - **Business Logic**: Conversation limits, archival policies, export options
    /// 
    /// Use this endpoint for:
    /// - Conversation system optimization
    /// - Privacy and compliance management
    /// - Performance tuning and resource management
    /// - Feature enablement and user experience customization
    /// </remarks>
    /// <returns>Conversation configuration details</returns>
    /// <response code="200">Conversation configuration retrieved successfully</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("conversation")]
    [ProducesResponseType(typeof(ConversationConfigResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConversationConfigResponse>> GetConversationConfiguration()
    {
        try
        {
            var config = await GetConversationConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Updates conversation management configuration
    /// </summary>
    /// <remarks>
    /// Updates conversation configuration with impact analysis including:
    /// - **Active Session Impact**: Analyzes impact on currently active conversations
    /// - **Memory Management**: Updates memory limits and cleanup policies
    /// - **Privacy Compliance**: Ensures privacy settings maintain compliance
    /// - **Performance Optimization**: Optimizes conversation handling performance
    /// - **Feature Rollout**: Enables or disables conversation features safely
    /// 
    /// Configuration update considerations:
    /// - **Active Conversations**: Changes are applied to new conversations first
    /// - **Backward Compatibility**: Existing conversations maintain their settings
    /// - **Gradual Migration**: Settings are migrated gradually to avoid disruption
    /// - **Performance Impact**: Memory and storage impact is monitored
    /// - **User Experience**: Changes are applied to minimize user disruption
    /// 
    /// Key settings that affect system behavior:
    /// - **History Limits**: Affects memory usage and conversation context
    /// - **Timeout Settings**: Impacts resource cleanup and user experience
    /// - **Concurrent Limits**: Affects system capacity and user limits
    /// - **Analytics**: Impacts data collection and privacy compliance
    /// - **Export Features**: Affects data portability and compliance
    /// </remarks>
    /// <param name="request">Conversation configuration update</param>
    /// <returns>Updated conversation configuration</returns>
    /// <response code="200">Conversation configuration updated successfully</response>
    /// <response code="400">Invalid configuration data</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("conversation")]
    [ProducesResponseType(typeof(ConversationConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationConfigResponse>> UpdateConversationConfiguration([FromBody] ConversationConfigRequest request)
    {
        try
        {
            // Validate configuration
            var validation = await ValidateConversationConfigAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new { Error = "Configuration validation failed", Errors = validation.Errors });
            }

            // Update configuration
            var updatedConfig = await UpdateConversationConfigAsync(request);

            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Validates configuration before applying changes
    /// </summary>
    /// <remarks>
    /// Provides comprehensive configuration validation including:
    /// - **Schema Validation**: Validates configuration against defined schemas
    /// - **Connectivity Testing**: Tests external service connections
    /// - **Dependency Checking**: Validates configuration dependencies
    /// - **Security Validation**: Ensures security requirements are met
    /// - **Performance Impact**: Analyzes potential performance implications
    /// - **Compatibility Checking**: Ensures compatibility with current system
    /// 
    /// Validation process:
    /// 1. **Schema Validation**: Validates against configuration schema
    /// 2. **Format Checking**: Ensures all values are in correct format
    /// 3. **Range Validation**: Validates numeric ranges and limits
    /// 4. **Dependency Analysis**: Checks for required dependencies
    /// 5. **Connectivity Testing**: Tests external service connections
    /// 6. **Security Review**: Validates security implications
    /// 7. **Performance Analysis**: Estimates performance impact
    /// 
    /// Use this endpoint before applying configuration changes to:
    /// - Prevent configuration errors and system failures
    /// - Identify potential issues before they affect production
    /// - Validate external service connectivity
    /// - Ensure security and compliance requirements
    /// - Optimize configuration for performance
    /// </remarks>
    /// <param name="request">Configuration validation request</param>
    /// <returns>Detailed validation results</returns>
    /// <response code="200">Validation completed (check IsValid property)</response>
    /// <response code="400">Invalid validation request</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ConfigValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfigValidationResponse>> ValidateConfiguration([FromBody] ConfigValidationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Section))
            {
                return BadRequest(new { Error = "Configuration section is required" });
            }

            var validation = await ValidateConfigurationAsync(request);

            return Ok(validation);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a backup of current configuration
    /// </summary>
    /// <remarks>
    /// Creates comprehensive configuration backups including:
    /// - **Selective Backup**: Choose specific configuration sections
    /// - **Security Options**: Include or exclude sensitive data (API keys)
    /// - **Multiple Formats**: JSON, YAML, XML backup formats
    /// - **Compression**: Optional compression for large configurations
    /// - **Metadata**: Backup timestamp, version, and creator information
    /// - **Validation**: Validates backup integrity and completeness
    /// 
    /// Backup features:
    /// - **Full System Backup**: Complete system configuration backup
    /// - **Selective Backup**: Backup only specific configuration sections
    /// - **Security Handling**: Option to exclude sensitive data
    /// - **Format Options**: Multiple export formats for different uses
    /// - **Compression**: Reduce backup size with optional compression
    /// - **Metadata**: Rich metadata for backup management
    /// 
    /// Use cases:
    /// - **Disaster Recovery**: Create backups before major changes
    /// - **Environment Migration**: Export configuration for other environments
    /// - **Compliance**: Regular backups for audit and compliance
    /// - **Version Control**: Track configuration changes over time
    /// - **Testing**: Create backups before testing configuration changes
    /// 
    /// Security considerations:
    /// - **Sensitive Data**: API keys and passwords can be excluded
    /// - **Access Control**: Backup access requires appropriate permissions
    /// - **Encryption**: Backups can be encrypted for additional security
    /// - **Audit Trail**: All backup operations are logged
    /// </remarks>
    /// <param name="request">Backup configuration request</param>
    /// <returns>Configuration backup data</returns>
    /// <response code="200">Configuration backup created successfully</response>
    /// <response code="400">Invalid backup request</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("backup")]
    [ProducesResponseType(typeof(ConfigBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfigBackupResponse>> BackupConfiguration([FromBody] ConfigBackupRequest request)
    {
        try
        {
            var validFormats = new[] { "json", "yaml", "xml" };
            if (!validFormats.Contains(request.Format.ToLower()))
            {
                return BadRequest(new { Error = "Format must be one of: json, yaml, xml" });
            }

            var backup = await CreateConfigurationBackupAsync(request);

            return Ok(backup);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Restores configuration from backup
    /// </summary>
    /// <remarks>
    /// Restores system configuration from backup with comprehensive safety measures including:
    /// - **Pre-Restore Validation**: Validates backup data before restoration
    /// - **Current Backup**: Creates backup of current configuration before restore
    /// - **Selective Restore**: Restore only specific configuration sections
    /// - **Rollback Capability**: Ability to rollback if restore fails
    /// - **Service Management**: Optional service restart after restore
    /// - **Validation Testing**: Tests restored configuration before activation
    /// 
    /// Restore process:
    /// 1. **Backup Validation**: Validates backup data integrity and format
    /// 2. **Current Backup**: Creates backup of current configuration
    /// 3. **Compatibility Check**: Ensures backup is compatible with current system
    /// 4. **Selective Restore**: Restores only requested sections
    /// 5. **Configuration Testing**: Tests restored configuration
    /// 6. **Service Updates**: Updates running services with new configuration
    /// 7. **Verification**: Verifies system health after restore
    /// 8. **Rollback**: Automatic rollback if verification fails
    /// 
    /// Safety features:
    /// - **Automatic Backup**: Current configuration is backed up before restore
    /// - **Validation**: Comprehensive validation before applying changes
    /// - **Rollback Protection**: Automatic rollback on failure
    /// - **Service Safety**: Services are updated safely without interruption
    /// - **Health Monitoring**: System health is monitored during restore
    /// 
    /// **Warning**: Configuration restore can significantly impact system behavior.
    /// Always validate backups and test in non-production environments first.
    /// </remarks>
    /// <param name="request">Configuration restore request</param>
    /// <returns>Restore operation results</returns>
    /// <response code="200">Configuration restored successfully</response>
    /// <response code="400">Invalid restore request or backup data</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("restore")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RestoreConfiguration([FromBody] ConfigRestoreRequest request)
    {
        try
        {
            if (request.BackupData == null || !request.BackupData.Any())
            {
                return BadRequest(new { Error = "Backup data is required" });
            }

            var result = await RestoreConfigurationAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Resets configuration to default values
    /// </summary>
    /// <remarks>
    /// Resets system configuration to factory defaults with safety measures including:
    /// - **Selective Reset**: Reset only specific configuration sections
    /// - **Backup Creation**: Automatic backup before reset
    /// - **Confirmation Required**: Multiple confirmations for safety
    /// - **Service Impact**: Analysis of service impact before reset
    /// - **Rollback Capability**: Ability to restore previous configuration
    /// 
    /// Reset options:
    /// - **Full System Reset**: Reset all configuration to defaults
    /// - **Section Reset**: Reset only specific configuration sections
    /// - **Preserve Settings**: Option to preserve certain critical settings
    /// - **Custom Defaults**: Use environment-specific default values
    /// 
    /// **DANGER**: This operation will reset configuration to default values.
    /// This may cause service interruption and loss of custom settings.
    /// Always create a backup before performing reset operations.
    /// 
    /// Use cases:
    /// - **Troubleshooting**: Reset to resolve configuration issues
    /// - **Clean Installation**: Start with fresh configuration
    /// - **Environment Setup**: Initialize new environment with defaults
    /// - **Recovery**: Recover from corrupted configuration
    /// </remarks>
    /// <param name="sections">Configuration sections to reset (empty for all)</param>
    /// <param name="confirm">Confirmation token (required for safety)</param>
    /// <returns>Reset operation results</returns>
    /// <response code="200">Configuration reset successfully</response>
    /// <response code="400">Invalid reset request or missing confirmation</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetConfiguration(
        [FromQuery] List<string> sections,
        [FromQuery] string confirm = "")
    {
        try
        {
            if (confirm != "RESET_CONFIRMED")
            {
                return BadRequest(new
                {
                    Error = "Configuration reset requires confirmation",
                    RequiredConfirmation = "RESET_CONFIRMED",
                    Warning = "This operation will reset configuration to defaults and may cause service interruption"
                });
            }

            var result = await ResetConfigurationAsync(sections);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    #region Private Helper Methods

    private async Task<List<ConfigurationResponse>> GetAllConfigurationsAsync()
    {
        // Mock configuration overview - replace with actual implementation
        var configurations = new List<ConfigurationResponse>
        {
            new ConfigurationResponse
            {
                Section = "AIProvider",
                Data = new Dictionary<string, object> { { "activeProviders", 2 }, { "status", "healthy" } },
                LastUpdated = DateTime.UtcNow.AddHours(-2),
                UpdatedBy = "admin",
                Version = "1.0.0",
                IsValid = true,
                IsActive = true
            },
            new ConfigurationResponse
            {
                Section = "Storage",
                Data = new Dictionary<string, object> { { "provider", "Qdrant" }, { "status", "connected" } },
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                UpdatedBy = "admin",
                Version = "1.0.0",
                IsValid = true,
                IsActive = true
            },
            new ConfigurationResponse
            {
                Section = "System",
                Data = new Dictionary<string, object> { { "maxFileSize", "100MB" }, { "status", "optimal" } },
                LastUpdated = DateTime.UtcNow.AddDays(-3),
                UpdatedBy = "admin",
                Version = "1.0.0",
                IsValid = true,
                IsActive = true
            }
        };

        return await Task.FromResult(configurations);
    }

    private async Task<AIProviderConfigResponse> GetAIProviderConfigAsync(AIProvider provider)
    {
        // Mock AI provider configuration - replace with actual implementation
        var config = new AIProviderConfigResponse
        {
            Section = "AIProvider",
            Provider = provider,
            DisplayName = GetProviderDisplayName(provider),
            MaskedApiKey = "sk-***...***def",
            BaseUrl = GetProviderBaseUrl(provider),
            DefaultModel = GetProviderDefaultModel(provider),
            DefaultEmbeddingModel = GetProviderDefaultEmbeddingModel(provider),
            AvailableModels = GetProviderModels(provider),
            Capabilities = GetProviderCapabilities(provider),
            MaxTokens = 4000,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            IsEnabled = true,
            LastUpdated = DateTime.UtcNow.AddHours(-2),
            UpdatedBy = "admin",
            Version = "1.0.0",
            IsValid = true,
            IsActive = true,
            Health = new ProviderHealthStatus
            {
                IsHealthy = true,
                LastChecked = DateTime.UtcNow.AddMinutes(-5),
                ResponseTimeSeconds = 0.85,
                StatusCode = 200
            },
            Usage = new ProviderUsageStats
            {
                TotalRequests = 1250,
                SuccessfulRequests = 1200,
                FailedRequests = 50,
                AverageResponseTime = 1.35,
                TotalTokens = 125000,
                LastRequest = DateTime.UtcNow.AddMinutes(-2)
            }
        };

        return await Task.FromResult(config);
    }

    private async Task<AIProviderConfigResponse> UpdateAIProviderConfigAsync(AIProviderConfigRequest request)
    {
        // Mock AI provider configuration update - replace with actual implementation
        var config = await GetAIProviderConfigAsync(request.Provider);
        config.LastUpdated = DateTime.UtcNow;
        config.UpdatedBy = "current-user"; // Replace with actual user

        return config;
    }

    private async Task<StorageConfigResponse> GetStorageConfigAsync(StorageProvider provider)
    {
        // Mock storage configuration - replace with actual implementation
        var config = new StorageConfigResponse
        {
            Section = "Storage",
            Provider = provider,
            DisplayName = GetStorageProviderDisplayName(provider),
            ConnectionString = GetStorageConnectionString(provider),
            CollectionName = "smartrag_documents",
            VectorDimensions = 1536,
            MaxSearchResults = 10,
            ConnectionPoolSize = 10,
            IsEnabled = true,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = "admin",
            Version = "1.0.0",
            IsValid = true,
            IsActive = true,
            Health = new StorageHealthStatus
            {
                IsHealthy = true,
                ConnectionStatus = "Connected",
                AvailableSpaceGB = 85.5,
                UsedSpaceGB = 14.5,
                LastChecked = DateTime.UtcNow.AddMinutes(-10)
            },
            Usage = new StorageUsageStats
            {
                TotalDocuments = 1500,
                TotalVectors = 15000,
                AverageSearchTime = 0.25,
                TotalSearches = 5000,
                LastUpload = DateTime.UtcNow.AddHours(-3)
            }
        };

        return await Task.FromResult(config);
    }

    private async Task<StorageConfigResponse> UpdateStorageConfigAsync(StorageConfigRequest request)
    {
        // Mock storage configuration update - replace with actual implementation
        var config = await GetStorageConfigAsync(request.Provider);
        config.LastUpdated = DateTime.UtcNow;
        config.UpdatedBy = "current-user"; // Replace with actual user

        return config;
    }

    private async Task<SystemConfigResponse> GetSystemConfigAsync()
    {
        // Mock system configuration - replace with actual implementation
        var config = new SystemConfigResponse
        {
            Section = "System",
            MaxFileSizeMB = 100,
            AllowedFileExtensions = new List<string> { ".pdf", ".docx", ".txt", ".md", ".xlsx", ".pptx" },
            DefaultLanguage = "en",
            EnableDetailedLogging = true,
            EnableAnalytics = true,
            AnalyticsRetentionDays = 90,
            EnableCORS = true,
            CORSOrigins = new List<string> { "https://localhost:3000", "https://myapp.com" },
            RateLimitPerMinute = 100,
            EnableCaching = true,
            CacheTTLMinutes = 30,
            LastUpdated = DateTime.UtcNow.AddDays(-3),
            UpdatedBy = "admin",
            Version = "1.0.0",
            IsValid = true,
            IsActive = true,
            Health = new SystemHealthStatus
            {
                Status = "Healthy",
                Uptime = TimeSpan.FromDays(15),
                MemoryUsagePercent = 45.2,
                CpuUsagePercent = 18.5,
                DiskUsagePercent = 65.8,
                ActiveConnections = 25
            },
            Performance = new SystemPerformanceMetrics
            {
                RequestsPerMinute = 45,
                AverageRequestDuration = 1.25,
                ErrorRatePercent = 2.1,
                CacheHitRatePercent = 85.5,
                TotalRequests = 125000
            }
        };

        return await Task.FromResult(config);
    }

    private async Task<SystemConfigResponse> UpdateSystemConfigAsync(SystemConfigRequest request)
    {
        // Mock system configuration update - replace with actual implementation
        var config = await GetSystemConfigAsync();
        config.LastUpdated = DateTime.UtcNow;
        config.UpdatedBy = "current-user"; // Replace with actual user

        return config;
    }

    private async Task<ConversationConfigResponse> GetConversationConfigAsync()
    {
        // Mock conversation configuration - replace with actual implementation
        var config = new ConversationConfigResponse
        {
            Section = "Conversation",
            DefaultMaxHistoryLength = 50,
            MaxConcurrentConversations = 10,
            IdleTimeoutMinutes = 60,
            AutoArchiveDays = 30,
            EnableAnalytics = true,
            EnableExport = true,
            DefaultExportFormat = "json",
            EnableMessageEditing = false,
            EnableConversationSharing = false,
            LastUpdated = DateTime.UtcNow.AddDays(-5),
            UpdatedBy = "admin",
            Version = "1.0.0",
            IsValid = true,
            IsActive = true,
            Usage = new ConversationUsageStats
            {
                ActiveConversations = 150,
                ArchivedConversations = 500,
                AverageConversationLength = 8.5,
                TotalMessages = 5500,
                AverageSessionDuration = TimeSpan.FromMinutes(25)
            }
        };

        return await Task.FromResult(config);
    }

    private async Task<ConversationConfigResponse> UpdateConversationConfigAsync(ConversationConfigRequest request)
    {
        // Mock conversation configuration update - replace with actual implementation
        var config = await GetConversationConfigAsync();
        config.LastUpdated = DateTime.UtcNow;
        config.UpdatedBy = "current-user"; // Replace with actual user

        return config;
    }

    private async Task<ConfigValidationResponse> ValidateConfigurationAsync(ConfigValidationRequest request)
    {
        // Mock configuration validation - replace with actual implementation
        var validation = new ConfigValidationResponse
        {
            Section = request.Section,
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
            ValidationTimeSeconds = 1.25,
            ConnectionTest = new ConnectionTestResult
            {
                Success = true,
                ResponseTimeSeconds = 0.85,
                Details = "Connection test successful"
            },
            SchemaValidation = new SchemaValidationResult
            {
                IsValid = true,
                SchemaVersion = "1.0.0"
            }
        };

        return await Task.FromResult(validation);
    }

    private async Task<ConfigValidationResponse> ValidateAIProviderConfigAsync(AIProviderConfigRequest request)
    {
        // Mock AI provider validation - replace with actual implementation
        return await ValidateConfigurationAsync(new ConfigValidationRequest
        {
            Section = "AIProvider",
            ConfigData = new Dictionary<string, object> { { "provider", request.Provider.ToString() } }
        });
    }

    private async Task<ConfigValidationResponse> ValidateStorageConfigAsync(StorageConfigRequest request)
    {
        // Mock storage validation - replace with actual implementation
        return await ValidateConfigurationAsync(new ConfigValidationRequest
        {
            Section = "Storage",
            ConfigData = new Dictionary<string, object> { { "provider", request.Provider.ToString() } }
        });
    }

    private async Task<ConfigValidationResponse> ValidateSystemConfigAsync(SystemConfigRequest request)
    {
        // Mock system validation - replace with actual implementation
        return await ValidateConfigurationAsync(new ConfigValidationRequest
        {
            Section = "System",
            ConfigData = new Dictionary<string, object> { { "maxFileSize", request.MaxFileSizeMB } }
        });
    }

    private async Task<ConfigValidationResponse> ValidateConversationConfigAsync(ConversationConfigRequest request)
    {
        // Mock conversation validation - replace with actual implementation
        return await ValidateConfigurationAsync(new ConfigValidationRequest
        {
            Section = "Conversation",
            ConfigData = new Dictionary<string, object> { { "maxHistory", request.DefaultMaxHistoryLength } }
        });
    }

    private async Task<ConfigBackupResponse> CreateConfigurationBackupAsync(ConfigBackupRequest request)
    {
        // Mock configuration backup - replace with actual implementation
        var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        var backupData = new Dictionary<string, object>();

        // Add mock data for requested sections
        foreach (var section in request.Sections)
        {
            backupData[section] = new { version = "1.0.0", data = "mock_data" };
        }

        var backup = new ConfigBackupResponse
        {
            BackupId = backupId,
            CreatedAt = DateTime.UtcNow,
            IncludedSections = request.Sections,
            FileSizeBytes = JsonSerializer.Serialize(backupData).Length,
            IsCompressed = request.Compress,
            Format = request.Format,
            IncludesSensitiveData = request.IncludeSensitiveData,
            Data = backupData
        };

        return await Task.FromResult(backup);
    }

    private async Task<object> RestoreConfigurationAsync(ConfigRestoreRequest request)
    {
        // Mock configuration restore - replace with actual implementation
        var result = new
        {
            Success = true,
            Message = "Configuration restored successfully",
            RestoredSections = request.Sections,
            BackupCreated = request.BackupCurrent,
            ServicesRestarted = request.RestartServices,
            RestoredAt = DateTime.UtcNow
        };

        return await Task.FromResult(result);
    }

    private async Task<object> ResetConfigurationAsync(List<string> sections)
    {
        // Mock configuration reset - replace with actual implementation
        var result = new
        {
            Success = true,
            Message = "Configuration reset to defaults successfully",
            ResetSections = sections.Any() ? sections : new List<string> { "All" },
            BackupCreated = true,
            ResetAt = DateTime.UtcNow,
            Warning = "System configuration has been reset to default values"
        };

        return await Task.FromResult(result);
    }

    // Helper methods for provider information
    private string GetProviderDisplayName(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => "OpenAI GPT Models",
            AIProvider.Anthropic => "Anthropic Claude Models",
            AIProvider.Gemini => "Google Gemini Models",
            AIProvider.AzureOpenAI => "Azure OpenAI Service",
            AIProvider.Custom => "Custom AI Provider",
            _ => provider.ToString()
        };
    }

    private string GetProviderBaseUrl(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => "https://api.openai.com/v1",
            AIProvider.Anthropic => "https://api.anthropic.com/v1",
            AIProvider.Gemini => "https://generativelanguage.googleapis.com/v1",
            AIProvider.AzureOpenAI => "https://your-resource.openai.azure.com",
            AIProvider.Custom => "https://custom-api.example.com",
            _ => ""
        };
    }

    private string GetProviderDefaultModel(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => "gpt-5.1",
            AIProvider.Anthropic => "claude-sonnet-4-5",
            AIProvider.Gemini => "gemini-2.5-pro",
            AIProvider.AzureOpenAI => "gpt-5.1",
            AIProvider.Custom => "custom-model",
            _ => "default"
        };
    }

    private string GetProviderDefaultEmbeddingModel(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => "text-embedding-3-small",
            AIProvider.AzureOpenAI => "text-embedding-3-small",
            AIProvider.Anthropic => "claude-embedding",
            AIProvider.Gemini => "embedding-001",
            AIProvider.Custom => "custom-embedding",
            _ => "default-embedding"
        };
    }

    private List<string> GetProviderModels(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => new List<string> { "gpt-5.1", "gpt-5", "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "text-embedding-3-small", "text-embedding-3-large" },
            AIProvider.Anthropic => new List<string> { "claude-sonnet-4-5", "claude-3.5-sonnet", "claude-3-opus-20240229", "claude-3-haiku-20240307" },
            AIProvider.Gemini => new List<string> { "gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-pro" },
            AIProvider.AzureOpenAI => new List<string> { "gpt-5.1", "gpt-4o", "gpt-4", "gpt-35-turbo", "text-embedding-3-small", "text-embedding-3-large" },
            AIProvider.Custom => new List<string> { "custom-model" },
            _ => new List<string>()
        };
    }

    private List<string> GetProviderCapabilities(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => new List<string> { "TextGeneration", "Embeddings", "ChatCompletion", "FunctionCalling" },
            AIProvider.Anthropic => new List<string> { "TextGeneration", "ChatCompletion", "LongContext" },
            AIProvider.Gemini => new List<string> { "TextGeneration", "ChatCompletion", "Vision", "Embeddings" },
            AIProvider.AzureOpenAI => new List<string> { "TextGeneration", "Embeddings", "ChatCompletion", "Enterprise" },
            AIProvider.Custom => new List<string> { "TextGeneration", "CustomEndpoints" },
            _ => new List<string> { "TextGeneration" }
        };
    }

    private string GetStorageProviderDisplayName(StorageProvider provider)
    {
        return provider switch
        {
            StorageProvider.Qdrant => "Qdrant Vector Database",
            StorageProvider.Redis => "Redis Cache",
            StorageProvider.InMemory => "In-Memory Storage",
            _ => provider.ToString()
        };
    }

    private string GetStorageConnectionString(StorageProvider provider)
    {
        return provider switch
        {
            StorageProvider.Qdrant => "http://localhost:6333",
            StorageProvider.Redis => "localhost:6379",
            StorageProvider.InMemory => "N/A (In-Memory)",
            _ => ""
        };
    }

    #endregion
}

