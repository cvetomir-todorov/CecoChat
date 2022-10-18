namespace CecoChat.Jwt;

public sealed class JwtOptions
{
    public string Secret { get; set; }

    public string Issuer { get; set; }

    public string Audience { get; set; }

    public TimeSpan AccessTokenExpiration { get; set; }
}