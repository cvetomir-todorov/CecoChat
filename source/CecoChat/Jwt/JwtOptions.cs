using System;

namespace CecoChat.Jwt
{
    public interface IJwtOptions
    {
        string Secret { get; }

        string Issuer { get; }

        string Audience { get; }

        TimeSpan AccessTokenExpiration { get; }
    }

    public sealed class JwtOptions : IJwtOptions
    {
        public string Secret { get; set; }

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public TimeSpan AccessTokenExpiration { get; set; }
    }
}
