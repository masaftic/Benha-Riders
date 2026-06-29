using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BenhaScooters.Presentation.Swagger;

public class AppTypeHeaderTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var parameter = new OpenApiParameter
        {
            Name = "X-APP-TYPE",
            In = ParameterLocation.Header,
            Description = "Application type.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = JsonValue.Create("RiderApp"),
                Enum = [
                    JsonValue.Create("DriverApp"),
                    JsonValue.Create("RiderApp"),
                ]
            }
        };
        
        operation.Parameters ??= [];
        operation.Parameters.Add(parameter);

        return Task.CompletedTask;
    }
}
