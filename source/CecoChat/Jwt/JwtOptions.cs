namespace CecoChat.Jwt;

public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public TimeSpan AccessTokenExpiration { get; set; }
}
