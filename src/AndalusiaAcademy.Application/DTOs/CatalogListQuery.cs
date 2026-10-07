using System.ComponentModel.DataAnnotations;

namespace AndalusiaAcademy.Application.DTOs;

public sealed class CatalogListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(50)]
    public string? Status { get; init; }
}
