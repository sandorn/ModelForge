using System.Security.Claims;
using ModelForge.Backend.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ModelForge.Backend.Tests.Auth;

public class JwtServiceTests
{
    private readonly JwtOptions _options = new()
    {
        SecretKey = "TestSecretKey-MustBeAtLeast32CharactersLong!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        TokenLifetimeHours = 1
    };

    [Fact]
    public void IssueToken_ProducesValidToken()
    {
        var service = new JwtService(_options, NullLogger<JwtService>.Instance);
        var token = service.IssueToken("user-1", "alice", "Admin");

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        var service = new JwtService(_options, NullLogger<JwtService>.Instance);
        var token = service.IssueToken("user-1", "alice", "Admin");

        var principal = service.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.Equal("user-1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("alice", principal.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var service = new JwtService(_options, NullLogger<JwtService>.Instance);

        var principal = service.ValidateToken("invalid-token");

        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        var shortLivedOptions = new JwtOptions
        {
            SecretKey = "TestSecretKey-MustBeAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            TokenLifetimeHours = -1
        };
        var service = new JwtService(shortLivedOptions, NullLogger<JwtService>.Instance);
        var token = service.IssueToken("user-1", "alice", "Admin");

        var principal = service.ValidateToken(token);

        Assert.Null(principal);
    }

    [Fact]
    public void IssueToken_ContainsExpectedClaims()
    {
        var service = new JwtService(_options, NullLogger<JwtService>.Instance);
        var token = service.IssueToken("user-2", "bob", "Auditor");

        var principal = service.ValidateToken(token);
        Assert.NotNull(principal);
        Assert.Equal("user-2", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("bob", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Auditor", principal.FindFirst(ClaimTypes.Role)?.Value);
    }
}
