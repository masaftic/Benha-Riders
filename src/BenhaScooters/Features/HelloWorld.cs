using BenhaScooters.Domain;
using BenhaScooters.Shared.Security;
using FastEndpoints;

namespace BenhaScooters.Features;

public record HelloRequest(string Message);

public record HelloResponse(string Message);

public class HelloWorld : Endpoint<HelloRequest, HelloResponse>
{
    public override void Configure()
    {
        Put("hello/{Message}");
        Claims(JwtClaims.Sub);
        Description(x => x
            .Accepts<HelloRequest>()
            .WithSummary("Hello World Endpoint")
            .Produces<HelloResponse>()
            .Produces(400)
            .Produces(401));

        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Hello World Endpoint";
            s.Description = "A simple endpoint that returns a hello world message.";
        });
    }


    public override Task<HelloResponse> ExecuteAsync(HelloRequest req, CancellationToken ct)
    {
        return Task.FromResult(new HelloResponse($"Hello {req.Message}"));
    }
}
