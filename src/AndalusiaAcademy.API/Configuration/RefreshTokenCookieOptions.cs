using Microsoft.AspNetCore.Http;

namespace AndalusiaAcademy.API.Configuration;

public sealed class RefreshTokenCookieOptions
{
    public const string SectionName = "Authentication:RefreshCookie";
    public const string DefaultName = "AndalusiaAcademy.RefreshToken";
    public const string CookiePath = "/api/v1/auth";

    public string Name { get; set; } = DefaultName;

    public bool Secure { get; set; } = true;

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
}
