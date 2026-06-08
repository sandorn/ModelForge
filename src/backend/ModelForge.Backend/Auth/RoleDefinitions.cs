namespace ModelForge.Backend.Auth;

public static class RoleDefinitions
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string Auditor = "Auditor";

    private static readonly Dictionary<string, string[]> _rolePermissions = new()
    {
        [Admin] = new[] { "users.manage", "audit.view", "config.write", "commands.execute", "links.manage", "aiwa.use" },
        [Analyst] = new[] { "commands.execute", "links.manage", "aiwa.use" },
        [Auditor] = new[] { "audit.view", "commands.execute" },
    };

    public static IReadOnlyDictionary<string, string[]> RolePermissions => _rolePermissions;
    public static string[] AllRoles => _rolePermissions.Keys.ToArray();
    public static string[] BuiltInRoles => [Admin, Analyst, Auditor];

    public static bool AddCustomRole(string role, string[] permissions)
    {
        if (BuiltInRoles.Contains(role)) return false;
        _rolePermissions[role] = permissions;
        return true;
    }

    public static bool RemoveCustomRole(string role)
    {
        if (BuiltInRoles.Contains(role)) return false;
        return _rolePermissions.Remove(role);
    }

    public static bool HasPermission(string role, string permission) =>
        _rolePermissions.TryGetValue(role, out var perms) && perms.Contains(permission);

    public static IReadOnlyCollection<string> GetPermissions(string role) =>
        _rolePermissions.TryGetValue(role, out var perms)
            ? perms.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();
}
