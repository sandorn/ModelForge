using ModelForge.Backend.Auth;
using Xunit;

namespace ModelForge.Backend.Tests.Auth;

public class UserStoreTests
{
    [Fact]
    public void ValidateCredentials_ReturnsDefaultAdmin()
    {
        var store = new InMemoryUserStore();

        var user = store.ValidateCredentials("admin", "admin123");

        Assert.NotNull(user);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public void SetActive_DisablesAndReEnablesUser()
    {
        var store = new InMemoryUserStore();
        var user = store.ValidateCredentials("analyst", "analyst123");
        Assert.NotNull(user);

        var disabled = store.SetActive(user.Id, false);
        var disabledLogin = store.ValidateCredentials("analyst", "analyst123");

        var enabled = store.SetActive(user.Id, true);
        var enabledLogin = store.ValidateCredentials("analyst", "analyst123");

        Assert.True(disabled);
        Assert.Null(disabledLogin);
        Assert.True(enabled);
        Assert.NotNull(enabledLogin);
    }

    [Fact]
    public void AddUser_NormalizesUsernameAndPreventsDuplicates()
    {
        var store = new InMemoryUserStore();

        var user = store.AddUser("  Alice  ", "ChangeMe123!", "Analyst");
        var login = store.ValidateCredentials("ALICE", "ChangeMe123!");

        Assert.Equal("alice", user.Username);
        Assert.NotNull(login);
        Assert.Throws<InvalidOperationException>(() => store.AddUser("alice", "ChangeMe123!", "Analyst"));
    }

    [Fact]
    public void AddUser_RejectsInvalidInputs()
    {
        var store = new InMemoryUserStore();

        Assert.Throws<ArgumentException>(() => store.AddUser("", "ChangeMe123!", "Analyst"));
        Assert.Throws<ArgumentException>(() => store.AddUser("bob", "", "Analyst"));
        Assert.Throws<ArgumentException>(() => store.AddUser("bob", "ChangeMe123!", "Owner"));
    }

    [Fact]
    public void RoleDefinitions_ReturnsSortedBuiltInPermissions()
    {
        var permissions = RoleDefinitions.GetPermissions(RoleDefinitions.Admin).ToArray();

        Assert.Contains("users.manage", permissions);
        Assert.Contains("audit.view", permissions);
        Assert.Equal(permissions.OrderBy(p => p, StringComparer.OrdinalIgnoreCase), permissions);
        Assert.Empty(RoleDefinitions.GetPermissions("Owner"));
    }
}
