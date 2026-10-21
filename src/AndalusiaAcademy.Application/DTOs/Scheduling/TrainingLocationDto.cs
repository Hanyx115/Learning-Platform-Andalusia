namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record TrainingLocationDto(
    Guid Id,
    string BranchName,
    string RoomName,
    string City,
    string Address,
    string? MapUrl,
    int Capacity,
    bool IsActive);
