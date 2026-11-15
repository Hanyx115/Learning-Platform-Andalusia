using Microsoft.AspNetCore.Identity;

namespace AndalusiaAcademy.Infrastructure.Identity.Models;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = string.Empty;

    public bool LearnerPetEnabled { get; set; }

    public string LearnerPetType { get; set; } = "cat";

    public DateTimeOffset ConsentedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
