using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CecoChat.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Server.Identity
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IJwtOptions jwtOptions)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwt =>
                {
                    JwtSecurityTokenHandler jwtHandler = new() { MapInboundClaims = false };
                    jwt.SecurityTokenValidators.Clear();
                    jwt.SecurityTokenValidators.Add(jwtHandler);

                    byte[] issuerSigningKey = Encoding.UTF8.GetBytes(jwtOptions.Secret);

                    jwt.RequireHttpsMetadata = true;
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(issuerSigningKey),
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(5)
                    };
                });

            return services;
        }
    }
}
