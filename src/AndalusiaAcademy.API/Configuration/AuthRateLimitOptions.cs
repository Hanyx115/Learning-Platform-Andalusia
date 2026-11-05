namespace AndalusiaAcademy.API.Configuration;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Authentication";

    public int LoginPermitLimit { get; set; } = 5;

    public int LoginWindowSeconds { get; set; } = 60;

    public int RegistrationPermitLimit { get; set; } = 5;

    public int RegistrationWindowSeconds { get; set; } = 600;

    public int RecoveryPermitLimit { get; set; } = 3;

    public int RecoveryWindowSeconds { get; set; } = 900;

    public int SessionPermitLimit { get; set; } = 30;

    public int SessionWindowSeconds { get; set; } = 60;
}
