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
/// 内存用户存储（MVP）。生产环境应替换为数据库 + ASP.NET Core Identity。
/// </summary>
public sealed class InMemoryUserStore
{
    private readonly ConcurrentDictionary<string, UserIdentity> _users = new();
    private readonly ConcurrentDictionary<string, (string Hash, string Salt)> _passwords = new();

    public InMemoryUserStore()
    {
        SeedUser("admin", "admin123", "Admin");
        SeedUser("analyst", "analyst123", "Analyst");
        SeedUser("auditor", "auditor123", "Auditor");
    }

    public UserIdentity? ValidateCredentials(string username, string password)
    {
        username = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        if (!_passwords.TryGetValue(username, out var stored))
            return null;

        if (HashPassword(password, stored.Salt) != stored.Hash)
            return null;

        return _users.Values.FirstOrDefault(user =>
            user.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && user.IsActive);
    }

    public UserIdentity? GetById(string id) =>
        _users.TryGetValue(id, out var user) ? user : null;

    public IReadOnlyList<UserIdentity> GetAll() => _users.Values.ToArray();

    public UserIdentity AddUser(string username, string password, string role)
    {
        username = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("password is required.", nameof(password));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("role is required.", nameof(role));
        if (!RoleDefinitions.AllRoles.Contains(role))
            throw new ArgumentException($"role must be one of: {string.Join(", ", RoleDefinitions.AllRoles)}.", nameof(role));
        if (_passwords.ContainsKey(username))
            throw new InvalidOperationException($"User '{username}' already exists.");

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

    private void SeedUser(string username, string password, string role)
    {
        username = NormalizeUsername(username);
        var id = Guid.NewGuid().ToString("N")[..12];
        var salt = Guid.NewGuid().ToString("N")[..16];
        _users[id] = new UserIdentity { Id = id, Username = username, Role = role };
        _passwords[username] = (HashPassword(password, salt), salt);
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToHexStringLower(hash);
    }

    private static string NormalizeUsername(string? username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();
}
