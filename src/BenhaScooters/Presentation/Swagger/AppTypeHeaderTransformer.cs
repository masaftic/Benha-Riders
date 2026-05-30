using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BenhaScooters.Presentation.Swagger;

public class AppTypeHeaderTransformer : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameter = new OpenApiParameter
        {
            Name = "X-APP-TYPE",
            In = ParameterLocation.Header,
            Description = "Application type.",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString("RiderApp"),
                Enum = [
                    new OpenApiString("DriverApp"),
                    new OpenApiString("RiderApp"),
                ]
            }
        };
        operation.Parameters.Add(parameter);
    }
}
