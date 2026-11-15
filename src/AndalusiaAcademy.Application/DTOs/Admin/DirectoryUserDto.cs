namespace AndalusiaAcademy.Application.DTOs.Admin;

public sealed record DirectoryUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool IsActive);
