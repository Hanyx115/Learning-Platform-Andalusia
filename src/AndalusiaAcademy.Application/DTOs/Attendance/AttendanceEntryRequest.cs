namespace AndalusiaAcademy.Application.DTOs.Attendance;

public sealed record AttendanceEntryRequest(Guid UserId, string? Status, string? Note);
