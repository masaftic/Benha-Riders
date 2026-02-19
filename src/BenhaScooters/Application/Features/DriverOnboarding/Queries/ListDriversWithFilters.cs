using BenhaScooters.Application.Common;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record ListDriversWithFilters(
    DriverOnboardingStatus? OnboardingStatus,
    int Page,
    int PageSize) : IRequest<PaginatedList<DriverSummaryDto>>;

public class ListDriversWithFiltersHandler(AppDbContext db) : IRequestHandler<ListDriversWithFilters, PaginatedList<DriverSummaryDto>>
{
    private readonly AppDbContext _db = db;

    public async Task<PaginatedList<DriverSummaryDto>> Handle(ListDriversWithFilters request, CancellationToken cancellationToken)
    {
        var query = _db.DriverProfiles
            .AsNoTracking();

        if (request.OnboardingStatus.HasValue)
        {
            query = query.Where(d => d.OnboardingStatus == request.OnboardingStatus.Value);
        }

        query = query.OrderBy(d => d.CreatedAt);

        var count = await query.CountAsync(cancellationToken);

        var drivers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new
            {
                Id = d.UserId,
                FullName = d.PersonalInfo == null ? null : (string?)d.PersonalInfo.FullName,
                PhoneNumber = d.User.PhoneNumber,
                NationalId = d.PersonalInfo == null ? null : (NationalId?)d.PersonalInfo.NationalId,
                Brand = d.Vehicle == null ? null : (string?)d.Vehicle.Brand,
                Year = d.Vehicle == null ? null : (int?)d.Vehicle.Year,
                Status = d.OnboardingStatus,
                CreatedAt = d.CreatedAt,

                // For progress calculation, we need to know which steps are completed. Instead of loading the entire profile, we can determine this based on the presence of related data.
                HasPersonalInfo = d.PersonalInfo != null,
                HasVehicleInfo = d.Vehicle != null,
                DocumentsCount = d.Documents.Count,
                IsApproved = d.OnboardingStatus == DriverOnboardingStatus.Approved
            })
            .ToListAsync(cancellationToken);

        var driverDtos = drivers.Select(d => new DriverSummaryDto(
            d.Id,
            d.FullName,
            d.NationalId,
            d.PhoneNumber!,
            d.Brand,
            d.Year,
            d.Status,
            CalculateProgress(d.HasPersonalInfo, d.HasVehicleInfo, d.DocumentsCount, d.IsApproved),
            d.CreatedAt))
            .ToList();

        return new PaginatedList<DriverSummaryDto>(driverDtos, count, request.Page, request.PageSize);
    }

    private static int CalculateProgress(
        bool hasPersonalInfo,
        bool hasVehicleInfo,
        int documentsCount,
        bool isApproved)
    {
        int steps = 0;
        if (hasPersonalInfo) steps++;
        if (hasVehicleInfo) steps++;
        if (documentsCount >= 3) steps++;
        if (isApproved) steps++;
        return steps * 25; // 25% per step
    }
}
