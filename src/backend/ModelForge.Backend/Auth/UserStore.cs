using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ModelForge.Backend.Auth;

/// <summary>
/// 用户身份模型。
/// </summary>
public sealed class UserIdentity
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = "Analyst";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 用户登录请求。
/// </summary>
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应。
/// </summary>
public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 内存用户存储（阶段五 MVP）。生产环境替换为数据库 + ASP.NET Core Identity。
/// </summary>
public sealed class InMemoryUserStore
{
    private readonly ConcurrentDictionary<string, UserIdentity> _users = new();
    private readonly ConcurrentDictionary<string, (string Hash, string Salt)> _passwords = new();

    public InMemoryUserStore()
    {
        // 预置默认用户
        SeedUser("admin", "admin123", "Admin");
        SeedUser("analyst", "analyst123", "Analyst");
        SeedUser("auditor", "auditor123", "Auditor");
    }

    private void SeedUser(string username, string password, string role)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        _users[id] = new UserIdentity { Id = id, Username = username, Role = role };
        var salt = Guid.NewGuid().ToString("N")[..16];
        _passwords[username] = (HashPassword(password, salt), salt);
    }

    public UserIdentity? ValidateCredentials(string username, string password)
    {
        if (!_passwords.TryGetValue(username, out var stored))
            return null;

        if (HashPassword(password, stored.Salt) != stored.Hash)
            return null;

        return _users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.IsActive);
    }

    public UserIdentity? GetById(string id) =>
        _users.TryGetValue(id, out var user) ? user : null;

    public IReadOnlyList<UserIdentity> GetAll() => _users.Values.ToArray();

    public UserIdentity AddUser(string username, string password, string role)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var salt = Guid.NewGuid().ToString("N")[..16];
        var user = new UserIdentity { Id = id, Username = username, Role = role };

        _users[id] = user;
        _passwords[username] = (HashPassword(password, salt), salt);
        return user;
    }

    public bool SetActive(string userId, bool active)
    {
        if (!_users.TryGetValue(userId, out var user)) return false;
        user.IsActive = active;
        return true;
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToHexStringLower(hash);
    }
}
