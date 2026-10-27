using AndalusiaAcademy.Application.DTOs.Admin;

namespace AndalusiaAcademy.Application.Identity;

public enum AdminUserUpdateFailure
{
    NotFound,
    InvalidRequest,
    Conflict,
}

public sealed record AdminUserUpdateOutcome(
    AdminUserDto? User,
    AdminUserUpdateFailure? Failure,
    string? Error)
{
    public bool Succeeded => User is not null;

    public static AdminUserUpdateOutcome Success(AdminUserDto user) => new(user, null, null);

    public static AdminUserUpdateOutcome Fail(AdminUserUpdateFailure failure, string error) =>
        new(null, failure, error);
}
