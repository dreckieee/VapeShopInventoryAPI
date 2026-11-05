namespace VapeShopInventoryAPI.Api;

public sealed class JwtSettings
{
    private const int MinSigningKeyLength = 32;

    public required string SigningKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        string signingKey = ReadRequired(configuration, "Jwt:SigningKey");
        string issuer = ReadRequired(configuration, "Jwt:Issuer");
        string audience = ReadRequired(configuration, "Jwt:Audience");

        if (signingKey.Length < MinSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinSigningKeyLength} characters.");
        }

        return new JwtSettings { 
            SigningKey = signingKey, 
            Issuer = issuer, 
            Audience = audience 
        };
    }

    private static string ReadRequired(IConfiguration configuration, string settingName)
    {
        string? value = configuration[settingName];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{settingName} is not configured.");
        }

        return value;
    }
}