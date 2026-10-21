namespace AndalusiaAcademy.Domain.Models;

public sealed class ProgramSessionAttendance
{
    public Guid Id { get; set; }

    public Guid ProgramSessionId { get; set; }

    public ProgramSession? ProgramSession { get; set; }

    public Guid UserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Note { get; set; }

    public Guid RecordedByUserId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
