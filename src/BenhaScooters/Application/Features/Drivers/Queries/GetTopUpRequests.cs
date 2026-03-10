using BenhaScooters.Application.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Queries;

public record GetTopUpRequestsQuery(
    int Page,
    int PageSize,
    TopUpRequestStatus? StatusFilter,
    UserId? DriverIdFilter) : IRequest<ErrorOr<PaginatedList<TopUpRequestDto>>>;

public record TopUpRequestDto(
    int Id,
    int DriverUserId,
    string DriverName,
    decimal Amount,
    string ReceiptUrl,
    string Status,
    string? ReviewNote,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public class GetTopUpRequestsQueryHandler
    : IRequestHandler<GetTopUpRequestsQuery, ErrorOr<PaginatedList<TopUpRequestDto>>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3;

    public GetTopUpRequestsQueryHandler(AppDbContext db, IS3Service s3)
    {
        _db = db;
        _s3 = s3;
    }

    public async Task<ErrorOr<PaginatedList<TopUpRequestDto>>> Handle(
        GetTopUpRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.WalletTopUpRequests
            .AsNoTracking()
            .Include(r => r.Driver)
            .AsQueryable();

        if (request.StatusFilter.HasValue)
            query = query.Where(r => r.Status == request.StatusFilter.Value);

        if (request.DriverIdFilter.HasValue)
            query = query.Where(r => r.DriverUserId == request.DriverIdFilter.Value);

        var page = query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.DriverUserId,
                DriverName = r.Driver.Name,
                r.Amount,
                r.ReceiptUrl,
                r.Status,
                r.ReviewNote,
                r.CreatedAt,
                r.ReviewedAt
            })
            .PaginateAsync(request.Page, request.PageSize);

        // Resolve pre-signed URLs for receipts
        var dtoTasks = page.Items.Select(async item =>
        {
            var url = await _s3.GetPreSignedUrlAsync(item.ReceiptUrl, TimeSpan.FromMinutes(15));
            return new TopUpRequestDto(
                item.Id,
                item.DriverUserId,
                item.DriverName,
                item.Amount,
                url,
                item.Status.ToString(),
                item.ReviewNote,
                item.CreatedAt,
                item.ReviewedAt);
        });

        var dtos = await Task.WhenAll(dtoTasks);

        return new PaginatedList<TopUpRequestDto>(dtos, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
