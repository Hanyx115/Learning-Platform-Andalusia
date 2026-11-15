using AndalusiaAcademy.Application.DTOs.Attendance;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Attendance;

public sealed class MarkSessionAttendanceRequestValidator : AbstractValidator<MarkSessionAttendanceRequest>
{
    private static readonly string[] ValidStatuses =
    [
        AttendanceStatuses.Present,
        AttendanceStatuses.Absent,
        AttendanceStatuses.Late,
        AttendanceStatuses.Excused,
    ];

    public MarkSessionAttendanceRequestValidator()
    {
        RuleFor(request => request.Entries)
            .NotEmpty()
            .WithMessage("At least one attendance entry is required.");

        RuleForEach(request => request.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.UserId).NotEmpty();
            entry.RuleFor(e => e.Status)
                .Must(status => string.IsNullOrWhiteSpace(status) || ValidStatuses.Contains(status))
                .WithMessage($"Status must be empty to clear attendance, or one of: {string.Join(", ", ValidStatuses)}.");
            entry.RuleFor(e => e.Note).MaximumLength(500);
            entry.RuleFor(e => e.Note)
                .Empty()
                .When(e => string.IsNullOrWhiteSpace(e.Status))
                .WithMessage("A note cannot be saved without an attendance status.");
        });

        RuleFor(request => request.Entries)
            .Must(entries => entries is not null
                && entries.Select(e => e.UserId).Distinct().Count() == entries.Count)
            .WithMessage("Each learner can only appear once per attendance submission.");
    }
}
