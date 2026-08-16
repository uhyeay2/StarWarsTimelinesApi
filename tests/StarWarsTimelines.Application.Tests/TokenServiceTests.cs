using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class TokenServiceTests
{
    private readonly JwtOptions _options = new(
        "StarWarsTimelinesApi",
        "StarWarsTimelinesClient",
        "StarWarsTimelines-Dev-Secret-Key-Change-Me-2026",
        120);

    [Fact]
    public void GenerateToken_CreatesReadableTokenWithUserClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "padme",
            DisplayName = "Padmé Amidala",
            Role = UserRole.Standard
        };
        var service = new TokenService(_options);

        var token = service.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey))
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.Username, principal.FindFirstValue(ClaimTypes.Name));
        Assert.True(principal.IsInRole(UserRole.Standard.ToString()));
    }
}
