namespace AndalusiaAcademy.Domain.Models;

public class LearnerApplicationStatusHistory
{
    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public LearnerApplication? Application { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; }

    public Guid ChangedByUserId { get; set; }

    public string? Note { get; set; }
}
