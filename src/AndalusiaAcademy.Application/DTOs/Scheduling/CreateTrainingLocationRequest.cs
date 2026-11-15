namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record CreateTrainingLocationRequest(
    string BranchName,
    string RoomName,
    string City,
    string Address,
    string? MapUrl,
    int Capacity);
