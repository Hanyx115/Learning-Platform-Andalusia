using System.Security.Claims;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Services;
using Microsoft.Extensions.Options;

namespace AndalusiaAcademy.API.IntegrationTests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Issuer = "AndalusiaAcademy.Tests",
        Audience = "AndalusiaAcademy.Tests.Client",
        Key = Convert.ToBase64String(new byte[32]),
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30,
    }));

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaimsForEveryRole()
    {
        var service = CreateService();
        var user = new TokenClaimsUser(Guid.NewGuid(), "learner@example.com", ["Learner", "Instructor"]);

        var token = service.GenerateAccessToken(user);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Value);

        Assert.Equal(2, jwt.Claims.Count(claim => claim.Type == ClaimTypes.Role));
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Learner");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Instructor");
    }

    [Fact]
    public void GenerateRefreshToken_ProducesPlainTextWhoseHashMatchesStoredHash()
    {
        var service = CreateService();

        var refreshToken = service.GenerateRefreshToken();

        Assert.Equal(refreshToken.Hash, service.Hash(refreshToken.PlainText));
    }

    [Fact]
    public void GenerateRefreshToken_NeverReturnsTheSamePlainTextTwice()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first.PlainText, second.PlainText);
    }
}
