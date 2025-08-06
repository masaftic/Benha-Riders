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
