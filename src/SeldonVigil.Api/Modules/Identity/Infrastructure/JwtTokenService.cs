using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure;

internal sealed record TokenPair(string AccessToken, string RefreshToken, DateTime AccessExpiresAt, DateTime RefreshExpiresAt);

internal interface ITokenService
{
    TokenPair Issue(User user, TenantId tenantId, string? ipAddress, string? userAgent);
    ClaimsPrincipal? ValidateAccessToken(string accessToken);
    string HashToken(string token);
}

internal sealed class JwtTokenService(TokenOptions options) : ITokenService
{
    public TokenPair Issue(User user, TenantId tenantId, string? ipAddress, string? userAgent)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.Add(options.AccessTokenLifetime);
        var refreshExpires = now.Add(options.RefreshTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId.Value.ToString()),
            new(ClaimTypes.Role, MapRole(user.Role))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = CreateRefreshToken();

        return new TokenPair(accessToken, refreshToken, accessExpires, refreshExpires);
    }

    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));

        try
        {
            var principal = handler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string MapRole(UserRole role) => role switch
    {
        UserRole.SuperAdmin => "super_admin",
        UserRole.Admin => "admin",
        UserRole.Engineer => "engineer",
        UserRole.SecurityAnalyst => "security_analyst",
        UserRole.Viewer => "viewer",
        UserRole.Billing => "billing",
        _ => "viewer"
    };
}

internal sealed record TokenOptions
{
    public string Issuer { get; init; } = "seldonvigil";
    public string Audience { get; init; } = "seldonvigil-api";
    public string SigningKey { get; init; } = string.Empty;
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);
}
