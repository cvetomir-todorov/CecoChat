namespace Common.Jwt;

public sealed class JwtOptions
{
    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public TimeSpan AccessTokenExpiration { get; init; }
}
