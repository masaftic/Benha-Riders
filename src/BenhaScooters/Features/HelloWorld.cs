using FastEndpoints;

namespace BenhaScooters.Features;

public class HelloWorld : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/hello");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Hello World Endpoint";
            s.Description = "A simple endpoint that returns a hello world message.";
        });
    }
    

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendAsync(new { Message = "Hello, World!" }, cancellation: ct);
        return;
    }
}
