using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BenhaScooters.Presentation.Swagger;

public class AcceptLanguageHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameter = new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Description = "Language preference for the response.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString("en"),
                Enum = [
                    new OpenApiString("en"),
                    new OpenApiString("ar"),
                ]
            }
        };
        operation.Parameters.Add(parameter);
    }
}
