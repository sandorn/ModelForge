namespace ModelForge.Backend.Auth;

/// <summary>
/// RBAC 角色定义与授权策略常量。
/// </summary>
public static class RoleDefinitions
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string Auditor = "Auditor";

    public static readonly IReadOnlyDictionary<string, string[]> RolePermissions = new Dictionary<string, string[]>
    {
        [Admin] = new[] { "users.manage", "audit.view", "config.write", "commands.execute", "links.manage", "aiwa.use" },
        [Analyst] = new[] { "commands.execute", "links.manage", "aiwa.use" },
        [Auditor] = new[] { "audit.view", "commands.execute" },
    };

    public static readonly string[] AllRoles = { Admin, Analyst, Auditor };

    public static bool HasPermission(string role, string permission) =>
        RolePermissions.TryGetValue(role, out var perms) && perms.Contains(permission);
}
