namespace AndalusiaAcademy.Application.DTOs;

public sealed record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CountryCode,
    string PreferredLanguage,
    bool LearnerPetEnabled,
    string LearnerPetType,
    IReadOnlyList<string> Roles);
