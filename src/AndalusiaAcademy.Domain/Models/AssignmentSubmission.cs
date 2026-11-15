namespace AndalusiaAcademy.Domain.Models;

public sealed class AssignmentSubmission
{
    public Guid Id { get; set; }

    public Guid AssignmentId { get; set; }

    public Assignment? Assignment { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string SubmissionType { get; set; } = AssignmentSubmissionTypes.Text;

    public string? LinkUrl { get; set; }

    public string? OriginalFileName { get; set; }

    public string? StoredFileName { get; set; }

    public string? FileContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public DateTimeOffset SubmittedAtUtc { get; set; }

    public decimal? Score { get; set; }

    public string? Feedback { get; set; }

    public Guid? GradedByUserId { get; set; }

    public DateTimeOffset? GradedAtUtc { get; set; }
}
