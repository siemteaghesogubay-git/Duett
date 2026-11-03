using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Duett.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<LoginResponseDto> CreateTokenAsync(
        ApplicationUser user)
    {
        var key = GetRequiredSetting("Jwt:Key");
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");

        // Nyckeln i User Secrets är Base64-kodad.
        var keyBytes = Convert.FromBase64String(key);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT-nyckeln måste innehålla minst 32 slumpmässiga byte.");
        }

        var minutes = _configuration
            .GetValue<int>("Jwt:AccessTokenMinutes");

        if (minutes < 1 || minutes > 15)
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes måste vara mellan 1 och 15.");
        }

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        // Roller hämtas från databasen, inte från klienten.
        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new LoginResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),

            ExpiresAtUtc = expiresAtUtc
        };
    }

    private string GetRequiredSetting(string name)
    {
        var value = _configuration[name];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{name} saknas i konfigurationen.");
        }

        return value;
    }
}