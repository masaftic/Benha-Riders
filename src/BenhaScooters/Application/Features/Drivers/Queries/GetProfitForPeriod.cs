using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Queries;

public record GetProfitForPeriodQuery(
    UserId DriverId,
    DateTime From,
    DateTime To) : IRequest<ErrorOr<ProfitPeriodSummary>>;

public class GetProfitForPeriodQueryValidator : AbstractValidator<GetProfitForPeriodQuery>
{
    public GetProfitForPeriodQueryValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .WithMessage("'From' date must be earlier than or equal to 'To' date.");
    }
}

public class GetProfitForPeriodQueryHandler(AppDbContext db)
    : IRequestHandler<GetProfitForPeriodQuery, ErrorOr<ProfitPeriodSummary>>
{
    public async Task<ErrorOr<ProfitPeriodSummary>> Handle(GetProfitForPeriodQuery request, CancellationToken cancellationToken)
    {
        var wallet = await db.DriverWallets
            .AsNoTracking()
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.DriverUserId == request.DriverId, cancellationToken);

        if (wallet is null)
            return AppErrors.Driver.Wallet.NotFound();

        var fromUtc = request.From.ToUniversalTime();
        var toUtc = request.To.ToUniversalTime();

        var completedTrips = await DriverProfitSummaryCalculator.GetCompletedTripsAsync(
            db.Trips.Where(t => t.DriverId == request.DriverId),
            cancellationToken);

        var transactionImpactByTrip = DriverProfitSummaryCalculator.GetTransactionImpactByTrip(wallet.Transactions);

        return DriverProfitSummaryCalculator.BuildProfitPeriodSummary(
            completedTrips,
            transactionImpactByTrip,
            fromUtc,
            toUtc);
    }
}
