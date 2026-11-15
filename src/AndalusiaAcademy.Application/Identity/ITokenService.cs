namespace AndalusiaAcademy.Application.Identity;

public interface ITokenService
{
    AccessToken GenerateAccessToken(TokenClaimsUser user);

    GeneratedRefreshToken GenerateRefreshToken();

    string Hash(string plainTextToken);
}
