using System.ComponentModel.DataAnnotations;

namespace AndalusiaAcademy.Application.DTOs;

public sealed class CatalogListQuery
{
    public const string TitleSort = "title";
    public const string TitleDescendingSort = "title-desc";

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(50)]
    public string? Status { get; init; }

    [StringLength(200)]
    public string? Search { get; init; }

    [Required]
    [RegularExpression(
        "^(?i:title|title-desc)$",
        ErrorMessage = "Sort must be one of: title, title-desc.")]
    public string Sort { get; init; } = TitleSort;
}
