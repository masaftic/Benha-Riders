using BenhaScooters.Application.Common;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record ListDriversWithFilters(
    OnboardingStatus? OnboardingStatus,
    int Page,
    int PageSize) : IRequest<PaginatedList<DriverSummaryDto>>;

public class ListDriversWithFiltersHandler(AppDbContext db) : IRequestHandler<ListDriversWithFilters, PaginatedList<DriverSummaryDto>>
{
    private readonly AppDbContext _db = db;

    public async Task<PaginatedList<DriverSummaryDto>> Handle(ListDriversWithFilters request, CancellationToken cancellationToken)
    {
        var query = _db.Drivers
            .AsNoTracking();

        if (request.OnboardingStatus.HasValue)
        {
            query = query.Where(d => d.OnboardingState.Status == request.OnboardingStatus.Value);
        }

        query = query.OrderBy(d => d.OnboardingState.CreatedAt);

        var count = await query.CountAsync(cancellationToken);

        var drivers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new
            {
                Id = d.Id,
                FullName = d.Info == null ? null : (string?)d.Info.FullName,
                NationalId = d.Info == null ? null : (NationalId?)d.Info.NationalId,
                Brand = d.Vehicle == null ? null : (string?)d.Vehicle.Brand,
                Year = d.Vehicle == null ? null : (int?)d.Vehicle.Year,
                Status = d.OnboardingState.Status,
                CreatedAt = d.OnboardingState.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var driverDtos = drivers.Select(d => new DriverSummaryDto(
            d.Id,
            d.FullName,
            d.NationalId,
            d.Brand,
            d.Year,
            d.Status,
            (int)d.Status * 20, // Now this calculation happens in C#, not SQL
            d.CreatedAt))
            .ToList();

        return new PaginatedList<DriverSummaryDto>(driverDtos, count, request.Page, request.PageSize);
    }
}
