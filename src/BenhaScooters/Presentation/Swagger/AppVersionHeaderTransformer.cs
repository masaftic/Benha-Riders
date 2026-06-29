using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BenhaScooters.Presentation.Swagger;

public class AppVersionHeaderTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var parameter = new OpenApiParameter
        {
            Name = "X-APP-VERSION",
            In = ParameterLocation.Header,
            Description = "Application version.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = JsonValue.Create("0.1.0+14"),
                Enum = [
                    JsonValue.Create("0.0.0+0"),
                    JsonValue.Create("0.1.0+14"),
                ]
            }
        };
        
        operation.Parameters ??= [];
        operation.Parameters.Add(parameter);

        return Task.CompletedTask;
    }
}
