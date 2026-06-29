using System.Text.Json.Nodes;
using BenhaScooters.Shared.Localization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BenhaScooters.Presentation.Swagger;

public class AcceptLanguageHeaderParameter : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var parameter = new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Description = "Language preference for the response.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = JsonValue.Create(AppLanguages.English),
                Enum = [
                    JsonValue.Create(AppLanguages.English),
                    JsonValue.Create(AppLanguages.Arabic),
                ]
            }
        };
        
        operation.Parameters ??= [];
        operation.Parameters.Add(parameter);

        return Task.CompletedTask;
    }
}
