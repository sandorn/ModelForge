using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ModelForge.Backend.Auth;

/// <summary>
/// JWT Token 签发与验证服务。
/// 开发/单机部署使用对称密钥 (HMAC-SHA256)；企业环境替换为证书或 OIDC。
/// </summary>
public sealed class JwtService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly ILogger<JwtService> _logger;

    public JwtService(JwtOptions options, ILogger<JwtService> logger)
    {
        _options = options;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        _logger = logger;
    }

    /// <summary>签发 JWT Token。</summary>
    public string IssueToken(string userId, string username, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.TokenLifetimeHours),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>验证 Token 并返回 ClaimsPrincipal。</summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = GetValidationParameters();

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT Token 验证失败");
            return null;
        }
    }

    public TokenValidationParameters GetValidationParameters() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _signingKey,
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
}

/// <summary>
/// JWT 配置选项。从 appsettings.json 的 "Jwt" 节绑定。
/// </summary>
public sealed class JwtOptions
{
    public string SecretKey { get; set; } = "ModelForge-Dev-SecretKey-ChangeInProduction-MinLength32Chars!";
    public string Issuer { get; set; } = "ModelForge.Backend";
    public string Audience { get; set; } = "ModelForge.Addins";
    public int TokenLifetimeHours { get; set; } = 8;
}
