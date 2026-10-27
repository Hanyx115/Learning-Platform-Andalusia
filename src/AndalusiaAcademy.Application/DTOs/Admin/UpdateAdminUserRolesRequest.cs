namespace AndalusiaAcademy.Application.DTOs.Admin;

public sealed record UpdateAdminUserRolesRequest(IReadOnlyList<string> Roles);
