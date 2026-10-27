namespace AndalusiaAcademy.Application.DTOs.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool IsActive,
    IReadOnlyList<string> Roles);
