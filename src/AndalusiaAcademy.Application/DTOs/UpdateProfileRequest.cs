namespace AndalusiaAcademy.Application.DTOs;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CountryCode,
    string PreferredLanguage);
