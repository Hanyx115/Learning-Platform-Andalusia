namespace AndalusiaAcademy.Domain.Models;

public sealed class TrainingLocation
{
    public Guid Id { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? MapUrl { get; set; }

    public bool IsActive { get; set; }
}
