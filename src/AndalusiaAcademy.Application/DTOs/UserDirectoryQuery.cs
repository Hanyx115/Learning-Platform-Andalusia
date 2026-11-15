using System.ComponentModel.DataAnnotations;

namespace AndalusiaAcademy.Application.DTOs;

public sealed class UserDirectoryQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(200)]
    public string? Search { get; init; }

    [RegularExpression("^(?i:active|inactive)$")]
    public string? Status { get; init; }
}
