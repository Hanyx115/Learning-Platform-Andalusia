using AndalusiaAcademy.Application.Identity;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class RoleNamesTests
{
    [Fact]
    public void All_ContainsTheApprovedInitialRoles()
    {
        string[] expectedRoles =
        [
            RoleNames.Learner,
            RoleNames.Instructor,
            RoleNames.Manager,
            RoleNames.Admin,
            RoleNames.ContentManager,
        ];

        Assert.Equal(expectedRoles, RoleNames.All);
        Assert.Equal(RoleNames.All.Count, RoleNames.All.Distinct(StringComparer.Ordinal).Count());
    }
}
