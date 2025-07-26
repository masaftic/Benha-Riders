using System;
using FluentValidation;

namespace BenhaScooters.Presentation;

public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
        {
            return Results.BadRequest();
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>() ?? throw new InvalidOperationException($"No validator registered for type {typeof(T).Name}");

        var validationResult = await validator.ValidateAsync(model, context.HttpContext.RequestAborted);

        if (!validationResult.IsValid)
        {
            var errorDict = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Results.ValidationProblem(errorDict);
        }

        return await next(context);
    }
}
