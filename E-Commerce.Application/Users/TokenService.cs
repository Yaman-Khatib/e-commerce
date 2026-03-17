using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using E_Commerce.Application.Users.Models;
using E_Commerce.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce.Application.Users;

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AuthResponse GenerateToken(User user)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username)
        };

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(token);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            AccessToken = tokenString,
            ExpiresAtUtc = expires
        };
    }
}

