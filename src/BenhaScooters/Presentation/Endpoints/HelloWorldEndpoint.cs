using System;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints;

public static class HelloWorldEndpoint
{
    public class Request
    {
        public string Name { get; set; } = null!;
    }

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MinimumLength(4)
                .WithMessage("Name must be at least 4 characters long.");
        }
    }

    public record Response(string Message);

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/hello", Handler)
                .AddEndpointFilter<ValidationFilter<Request>>()
                .WithName("HelloWorld")
                .WithTags("Hello World")
                .WithSummary("Returns a greeting message")
                .WithDescription("This endpoint returns a simple greeting message based on the provided name.")
                .Produces<Response>()
                .ProducesValidationProblem()
                .WithOpenApi();
        }

        public IResult Handler([FromBody] Request request)
        {
            var message = $"Hello, {request.Name}!";
            var response = new Response(message);
            return Results.Ok(response);
        }
    }
}

[Mapper]
public partial class HelloWorldEndpointMapper
{
    public HelloWorldEndpoint.Request MapToRequest(string name)
    {
        return new HelloWorldEndpoint.Request { Name = name };
    }

    public HelloWorldEndpoint.Response MapToResponse(string message)
    {
        return new HelloWorldEndpoint.Response(message);
    }
}