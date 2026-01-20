namespace BenhaScooters.Application.Common;

public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; } = [];
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public bool HasNextPage { get; }

    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        HasNextPage = (pageNumber * pageSize) < totalCount;
    }
}

public static class PaginatedListExtensions
{
    public static PaginatedList<T> PaginateAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        var totalCount = source.Count();
        var items = source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }
}
