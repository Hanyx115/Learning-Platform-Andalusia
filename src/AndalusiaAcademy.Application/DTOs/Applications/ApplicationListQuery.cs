using System.ComponentModel.DataAnnotations;

namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed class ApplicationListQuery : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(50)]
    public string? Status { get; init; }

    [StringLength(100)]
    public string? LearnerName { get; init; }

    [StringLength(200)]
    public string? OfferingTitle { get; init; }

    public DateOnly? IntakeFrom { get; init; }

    public DateOnly? IntakeTo { get; init; }

    [RegularExpression("^(submitted_desc|learner_asc|learner_desc|offering_asc|offering_desc|intake_asc|intake_desc)$")]
    public string Sort { get; init; } = "submitted_desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IntakeFrom.HasValue && IntakeTo.HasValue && IntakeFrom > IntakeTo)
        {
            yield return new ValidationResult(
                "Intake from date must be on or before the intake to date.",
                [nameof(IntakeFrom), nameof(IntakeTo)]);
        }
    }
}
