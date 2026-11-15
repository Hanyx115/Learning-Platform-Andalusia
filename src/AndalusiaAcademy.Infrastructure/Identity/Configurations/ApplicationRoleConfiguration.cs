using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Identity.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.HasData(
            CreateRole("2fc90ac5-6457-4ea8-ad7b-54019cf1b694", RoleNames.Learner),
            CreateRole("98f19c47-27af-4690-94b0-d9362f5b02aa", RoleNames.Instructor),
            CreateRole("5a82aed4-d3d9-414f-936a-aa599adc8692", RoleNames.Manager),
            CreateRole("f3f7903a-ecf2-4c5d-8f44-5440a2714202", RoleNames.Admin),
            CreateRole("901245a4-147b-40c1-b479-81804854a9a9", RoleNames.ContentManager));
    }

    private static ApplicationRole CreateRole(string id, string name)
    {
        return new ApplicationRole
        {
            Id = Guid.Parse(id),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = id,
        };
    }
}
