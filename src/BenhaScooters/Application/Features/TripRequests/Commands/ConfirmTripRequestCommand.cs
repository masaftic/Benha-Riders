using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Application.Features.TripRequests.Commands;

public record ConfirmTripRequestCommand(TripRequestId TripRequestId, RiderId RiderId) : IRequest<ErrorOr<Success>>;


public class ConfirmTripRequestCommandHandler(AppDbContext db) : IRequestHandler<ConfirmTripRequestCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ConfirmTripRequestCommand request, CancellationToken cancellationToken)
    {
        var tripRequest = await db.TripRequests.FindAsync([request.TripRequestId], cancellationToken);

        if (tripRequest is null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        if (tripRequest.RiderId != request.RiderId)
        {
            return TripErrors.TripRequest.Forbidden;
        }

        var result = tripRequest.Confirm();

        if (result.IsError)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
