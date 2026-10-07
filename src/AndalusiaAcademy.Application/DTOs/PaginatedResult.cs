namespace AndalusiaAcademy.Application.DTOs;

public sealed class PaginatedResult<T>(
    IReadOnlyList<T> items,
    int page,
    int pageSize,
    int totalCount)
{
    public IReadOnlyList<T> Items { get; } = items;

    public int Page { get; } = page;

    public int PageSize { get; } = pageSize;

    public int TotalCount { get; } = totalCount;

    public int TotalPages { get; } = totalCount == 0
        ? 0
        : (int)Math.Ceiling(totalCount / (double)pageSize);
}
