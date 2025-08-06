using ErrorOr;
using MediatR;

namespace BenhaScooters.Application.Common.Behaviors;

public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : notnull
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling request: {RequestType} - {Request}", typeof(TRequest).Name, request);

        var response = await next();

        _logger.LogInformation("Handled request: {RequestType} - {Response}", typeof(TRequest).Name, response);

        if (response is IErrorOr)
        {
            var errorOrResponse = (IErrorOr)response;
            if (errorOrResponse.IsError)
            {
                _logger.LogError("Request {RequestType} failed with errors: {Errors}", typeof(TRequest).Name, errorOrResponse.Errors);
            }
        }

        return response;
    }
}
