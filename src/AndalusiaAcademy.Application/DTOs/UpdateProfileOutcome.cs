namespace AndalusiaAcademy.Application.DTOs;

public sealed record UpdateProfileOutcome(bool Succeeded, ProfileDto? Profile, IReadOnlyList<string> Errors)
{
    public static UpdateProfileOutcome Success(ProfileDto profile) => new(true, profile, []);

    public static UpdateProfileOutcome Failure(IReadOnlyList<string> errors) => new(false, null, errors);
}
