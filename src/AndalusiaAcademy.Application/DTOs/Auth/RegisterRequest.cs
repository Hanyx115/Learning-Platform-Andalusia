namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CountryCode,
    string PreferredLanguage);
