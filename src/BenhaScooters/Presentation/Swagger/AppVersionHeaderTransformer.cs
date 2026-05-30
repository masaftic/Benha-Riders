using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BenhaScooters.Presentation.Swagger;

public class AppVersionHeaderTransformer : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameter = new OpenApiParameter
        {
            Name = "X-APP-VERSION",
            In = ParameterLocation.Header,
            Description = "Application version.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString("0.1.0+14"),
                Enum = [
                    new OpenApiString("0.0.0+0"),
                    new OpenApiString("0.1.0+14"),
                ]
            }
        };
        operation.Parameters.Add(parameter);
    }
}
