using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartRAG.API.Filters;

public class MultipartFileUploadFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this is the upload-multiple endpoint
        if (context.MethodInfo.Name == "UploadDocuments" && 
            context.MethodInfo.DeclaringType?.Name == "DocumentsController")
        {
            // Remove any existing request body
            operation.RequestBody = null;

            // Add the multipart form data request body with multiple files
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["files"] = new OpenApiSchema
                                {
                                    Type = "array",
                                    Items = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                }
                            },
                            Required = new HashSet<string> { "files" }
                        }
                    }
                }
            };

            // Add external documentation link for simple upload page
            if (operation.ExternalDocs == null)
                operation.ExternalDocs = new OpenApiExternalDocs();
            
            operation.ExternalDocs.Description = "Simple multiple file upload page";
            operation.ExternalDocs.Url = new Uri("/upload.html", UriKind.Relative);
        }
    }
}
