using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace SmartRAG.API.Filters
{
    /// <summary>
    /// Schema filter to add example values to Swagger documentation
    /// </summary>
    public class ExampleSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// Apply example values to schema
        /// </summary>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == null) return;

            // Add examples for our custom types
            if (context.Type.Name.Contains("DatabaseConnectionApiRequest"))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["connectionString"] = new Microsoft.OpenApi.Any.OpenApiString("Server=localhost;Database=Northwind;Trusted_Connection=true;"),
                    ["databaseType"] = new Microsoft.OpenApi.Any.OpenApiString("SqlServer"),
                    ["includedTables"] = new Microsoft.OpenApi.Any.OpenApiArray
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("Customers"),
                        new Microsoft.OpenApi.Any.OpenApiString("Orders"),
                        new Microsoft.OpenApi.Any.OpenApiString("Products")
                    },
                    ["maxRows"] = new Microsoft.OpenApi.Any.OpenApiInteger(1000),
                    ["includeSchema"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                    ["sanitizeSensitiveData"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true)
                };
            }
            else if (context.Type.Name.Contains("QueryExecutionApiRequest"))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["connectionString"] = new Microsoft.OpenApi.Any.OpenApiString("Server=localhost;Database=Northwind;Trusted_Connection=true;"),
                    ["query"] = new Microsoft.OpenApi.Any.OpenApiString("SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = 'USA'"),
                    ["databaseType"] = new Microsoft.OpenApi.Any.OpenApiString("SqlServer"),
                    ["maxRows"] = new Microsoft.OpenApi.Any.OpenApiInteger(10)
                };
            }
            else if (context.Type.Name.Contains("TableSchemaApiRequest"))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["connectionString"] = new Microsoft.OpenApi.Any.OpenApiString("Server=localhost;Database=Northwind;Trusted_Connection=true;"),
                    ["tableName"] = new Microsoft.OpenApi.Any.OpenApiString("Customers"),
                    ["databaseType"] = new Microsoft.OpenApi.Any.OpenApiString("SqlServer")
                };
            }
            else if (context.Type.Name.Contains("SearchRequest"))
            {
                schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["query"] = new Microsoft.OpenApi.Any.OpenApiString("What are the main benefits mentioned in the contract?"),
                    ["maxResults"] = new Microsoft.OpenApi.Any.OpenApiInteger(5)
                };
            }
        }
    }
}
