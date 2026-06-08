using ModelForge.Backend.Auth;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests;

public class EnterpriseApiTests
{
    [Fact]
    public void RoleDefinitions_HasThreeBuiltInRoles()
    {
        Assert.Equal(3, RoleDefinitions.BuiltInRoles.Length);
        Assert.Contains("Admin", RoleDefinitions.BuiltInRoles);
        Assert.Contains("Analyst", RoleDefinitions.BuiltInRoles);
        Assert.Contains("Auditor", RoleDefinitions.BuiltInRoles);
    }

    [Fact]
    public void RoleDefinitions_AddCustomRole_Succeeds()
    {
        Assert.True(RoleDefinitions.AddCustomRole("TestRole", new[] { "commands.execute" }));
        Assert.Contains("TestRole", RoleDefinitions.AllRoles);
        Assert.Contains("commands.execute", RoleDefinitions.GetPermissions("TestRole"));
        RoleDefinitions.RemoveCustomRole("TestRole");
    }

    [Fact]
    public void RoleDefinitions_CannotRemoveBuiltInRole()
    {
        Assert.False(RoleDefinitions.RemoveCustomRole("Admin"));
        Assert.Contains("Admin", RoleDefinitions.AllRoles);
    }

    [Fact]
    public void RoleDefinitions_HasPermission_Works()
    {
        Assert.True(RoleDefinitions.HasPermission("Admin", "users.manage"));
        Assert.True(RoleDefinitions.HasPermission("Analyst", "commands.execute"));
        Assert.False(RoleDefinitions.HasPermission("Analyst", "users.manage"));
    }

    [Fact]
    public void EnterprisePolicy_Dto_RoundTrips()
    {
        var req = new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["forceShortcuts"] = "true" },
            UpdatedBy = "admin"
        };
        Assert.Equal("true", req.Values["forceShortcuts"]);
        Assert.Equal("admin", req.UpdatedBy);
    }

    [Fact]
    public void AdminRoleCreateRequest_Validates()
    {
        var req = new AdminRoleCreateRequest
        {
            Role = "CustomRole",
            Permissions = new[] { "commands.execute", "aiwa.use" }
        };
        Assert.Equal("CustomRole", req.Role);
        Assert.Equal(2, req.Permissions.Length);
    }

    [Fact]
    public void DashboardSummaryResponse_HasAllFields()
    {
        var resp = new DashboardSummaryResponse
        {
            TotalEvents = 100,
            ActiveUserCount = 5,
            WindowHours = 168,
            TopCommands = new List<DashboardTopCommand> { new() { CommandId = "test.cmd", Count = 10 } },
            ByHost = new List<DashboardHostBucket> { new() { Host = "Excel", Count = 80 } },
            Timeline = new List<DashboardTimelineBucket> { new() { Label = "06-07", Count = 10 } }
        };
        Assert.Equal(100, resp.TotalEvents);
        Assert.Single(resp.TopCommands);
        Assert.Single(resp.ByHost);
        Assert.Single(resp.Timeline);
    }

    [Fact]
    public void AiwaChatRequest_Validates()
    {
        var req = new AiwaChatRequest { Message = "Test", Mode = "explain" };
        Assert.Equal("Test", req.Message);
        Assert.Equal("explain", req.Mode);
    }

    [Fact]
    public void AiwaChatResponse_HasFallbackFlag()
    {
        var resp = new AiwaChatResponse
        {
            Response = "OK",
            Mode = "chat",
            Model = "agnes-2.0-flash",
            FallbackMock = false
        };
        Assert.False(resp.FallbackMock);
        Assert.Equal("agnes-2.0-flash", resp.Model);
    }

    [Fact]
    public void UserEntry_Defaults()
    {
        var entry = new Data.UserEntry
        {
            Id = "user1",
            Username = "test",
            Role = "Analyst",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        };
        Assert.True(entry.IsActive);
        Assert.Equal("Analyst", entry.Role);
    }

    [Fact]
    public void UserEntry_CanSetAllProperties()
    {
        var entry = new Data.UserEntry
        {
            Id = "u1",
            Username = "test@corp.com",
            Role = "Admin",
            IsActive = false,
            PasswordHash = "abc123",
            PasswordSalt = "salt123",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        Assert.Equal("u1", entry.Id);
        Assert.False(entry.IsActive);
        Assert.Equal(2026, entry.CreatedAtUtc.Year);
    }

    [Fact]
    public void AdminRolesResponse_ReturnsCorrectType()
    {
        var resp = new AdminRolesResponse
        {
            Roles = new[]
            {
                new AdminRolePermissionResponse { Role = "Admin", BuiltIn = true, Permissions = new[] { "users.manage" } },
                new AdminRolePermissionResponse { Role = "CustomRole", BuiltIn = false, Permissions = new[] { "commands.execute" } }
            }
        };
        Assert.Equal(2, resp.Roles.Count);
        Assert.True(resp.Roles.First().BuiltIn);
        Assert.False(resp.Roles.Last().BuiltIn);
    }
}
