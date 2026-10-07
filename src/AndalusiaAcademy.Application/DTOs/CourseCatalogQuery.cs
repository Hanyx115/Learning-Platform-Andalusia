using System.ComponentModel.DataAnnotations;

namespace AndalusiaAcademy.Application.DTOs;

public sealed class CourseCatalogQuery
{
    public const string TitleSort = "title";
    public const string TitleDescendingSort = "title-desc";
    public const string NewestSort = "newest";
    public const string OldestSort = "oldest";

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(200)]
    public string? Search { get; init; }

    [StringLength(200)]
    public string? Category { get; init; }

    [StringLength(50)]
    public string? Status { get; init; }

    [StringLength(50)]
    public string? Type { get; init; }

    [Required]
    [RegularExpression(
        "^(?i:title|title-desc|newest|oldest)$",
        ErrorMessage = "Sort must be one of: title, title-desc, newest, oldest.")]
    public string Sort { get; init; } = TitleSort;
}
