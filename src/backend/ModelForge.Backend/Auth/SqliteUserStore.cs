using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ModelForge.Backend.Data;

namespace ModelForge.Backend.Auth;

/// <summary>
/// EF Core-based user store. Replaces InMemoryUserStore when DatabaseProvider is not inmemory.
/// </summary>
public sealed class SqliteUserStore
{
    private readonly IDbContextFactory<ModelForgeDbContext> _dbFactory;

    public SqliteUserStore(IDbContextFactory<ModelForgeDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        SeedDefaultUsers();
    }

    private void SeedDefaultUsers()
    {
        using var db = _dbFactory.CreateDbContext();
        if (!db.Users.Any())
        {
            SeedUser(db, "admin", "admin123", "Admin");
            SeedUser(db, "analyst", "analyst123", "Analyst");
            SeedUser(db, "auditor", "auditor123", "Auditor");
            db.SaveChanges();
        }
    }

    public UserIdentity? ValidateCredentials(string username, string password)
    {
        username = NormalizeUsername(username);
        using var db = _dbFactory.CreateDbContext();
        var entry = db.Users.FirstOrDefault(u =>
            u.Username == username && u.IsActive);
        if (entry == null) return null;

        if (HashPassword(password, entry.PasswordSalt) != entry.PasswordHash)
            return null;

        return ToIdentity(entry);
    }

    public UserIdentity? GetById(string id)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = db.Users.Find(id);
        return entry == null ? null : ToIdentity(entry);
    }

    public IReadOnlyList<UserIdentity> GetAll()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Users.Where(u => u.IsActive).AsEnumerable().Select(ToIdentity).ToArray();
    }

    public UserIdentity AddUser(string username, string password, string role)
    {
        username = NormalizeUsername(username);
        using var db = _dbFactory.CreateDbContext();
        if (db.Users.Any(u => u.Username == username))
            throw new InvalidOperationException($"User '{username}' already exists.");

        var salt = Guid.NewGuid().ToString("N")[..16];
        var entry = new UserEntry
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Username = username,
            Role = role,
            PasswordHash = HashPassword(password, salt),
            PasswordSalt = salt,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(entry);
        db.SaveChanges();
        return ToIdentity(entry);
    }

    public bool SetActive(string userId, bool active)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = db.Users.Find(userId);
        if (entry == null) return false;
        entry.IsActive = active;
        db.SaveChanges();
        return true;
    }

    public bool DeleteUser(string userId)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = db.Users.Find(userId);
        if (entry == null) return false;
        db.Users.Remove(entry);
        db.SaveChanges();
        return true;
    }

    public UserIdentity? UpdateUser(string userId, string? newPassword, string? newRole)
    {
        using var db = _dbFactory.CreateDbContext();
        var entry = db.Users.Find(userId);
        if (entry == null) return null;

        if (!string.IsNullOrWhiteSpace(newRole))
        {
            if (!RoleDefinitions.AllRoles.Contains(newRole))
                throw new ArgumentException($"Invalid role: {newRole}");
            entry.Role = newRole;
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var salt = Guid.NewGuid().ToString("N")[..16];
            entry.PasswordHash = HashPassword(newPassword, salt);
            entry.PasswordSalt = salt;
        }

        db.SaveChanges();
        return ToIdentity(entry);
    }

    private static void SeedUser(ModelForgeDbContext db, string username, string password, string role)
    {
        var salt = Guid.NewGuid().ToString("N")[..16];
        db.Users.Add(new UserEntry
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Username = NormalizeUsername(username),
            Role = role,
            PasswordHash = HashPassword(password, salt),
            PasswordSalt = salt
        });
    }

    private static UserIdentity ToIdentity(UserEntry entry) => new()
    {
        Id = entry.Id,
        Username = entry.Username,
        Role = entry.Role,
        IsActive = entry.IsActive,
        CreatedAt = entry.CreatedAtUtc
    };

    private static string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexStringLower(sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
    }

    private static string NormalizeUsername(string? username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();
}
