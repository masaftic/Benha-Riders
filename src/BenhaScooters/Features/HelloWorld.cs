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
        Put("/hello");
        Claims(JwtClaims.Sub);
        // AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Hello World Endpoint";
            s.Description = "A simple endpoint that returns a hello world message.";
        });
    }


    public override async Task<HelloResponse> ExecuteAsync(HelloRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        return new HelloResponse($"Hello, {userId}, {req.Message}!");
    }
}
