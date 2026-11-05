using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace VapeShopInventoryAPI.Api;

public sealed class TokenService
{
    private const int TokenLifetimeMinutes = 60;
    private readonly JwtSettings _settings;

    public TokenService(JwtSettings settings)
    {
        _settings = settings;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var expiresAt = DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.Username),
                new Claim("role", user.Role.ToString())
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                _settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
