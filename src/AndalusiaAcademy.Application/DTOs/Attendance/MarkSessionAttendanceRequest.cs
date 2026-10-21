namespace AndalusiaAcademy.Application.DTOs.Attendance;

public sealed record MarkSessionAttendanceRequest(IReadOnlyList<AttendanceEntryRequest> Entries);
