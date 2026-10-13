namespace AndalusiaAcademy.Application.Identity;

public sealed record TokenClaimsUser(Guid UserId, string Email, IReadOnlyList<string> Roles);
