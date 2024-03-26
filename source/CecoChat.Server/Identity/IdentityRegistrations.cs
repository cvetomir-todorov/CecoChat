using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Server.Identity;

public static class IdentityRegistrations
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtOptions jwtOptions)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                JwtSecurityTokenHandler jwtHandler = new() { MapInboundClaims = false };
                jwt.TokenHandlers.Clear();
                jwt.TokenHandlers.Add(jwtHandler);

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

    public static IServiceCollection AddUserPolicyAuthorization(this IServiceCollection services)
    {
        return services.AddAuthorization(authorization =>
        {
            authorization.AddPolicy("user", policy =>
            {
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole("user")
                    .RequireClaim(ClaimTypes.Name);
            });
        });
    }
}
