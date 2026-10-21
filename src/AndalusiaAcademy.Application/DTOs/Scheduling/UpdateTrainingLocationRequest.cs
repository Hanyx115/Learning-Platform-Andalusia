namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record UpdateTrainingLocationRequest(
    string BranchName,
    string RoomName,
    string City,
    string Address,
    string? MapUrl,
    bool IsActive);
