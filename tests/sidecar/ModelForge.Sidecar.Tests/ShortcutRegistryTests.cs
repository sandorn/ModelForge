using ModelForge.Sidecar.Commands;
using Xunit;

namespace ModelForge.Sidecar.Tests;

public class ShortcutRegistryTests
{
    [Fact]
    public void RegisterDefaults_ReturnsAllImplementedCommandShortcuts()
    {
        var registry = new ShortcutRegistry();
        registry.RegisterDefaults();
        var all = registry.GetAll();
        Assert.Equal(39, all.Count);
        Assert.Contains(all, item => item.CommandId == PptCommandIds.DeckCheck);
        Assert.Contains(all, item => item.CommandId == WordCommandIds.BuildDueDiligence);
    }

    [Fact]
    public void Register_SingleShortcut_FindableByChord()
    {
        var registry = new ShortcutRegistry();
        var def = new ShortcutDefinition("test.cmd", "Test Command", "Ctrl+T");
        registry.Register(def);

        var found = registry.FindByChord("Ctrl+T");
        Assert.NotNull(found);
        Assert.Equal("test.cmd", found!.CommandId);
        Assert.Equal("Test Command", found.DisplayName);
    }

    [Fact]
    public void FindByChord_CaseInsensitive()
    {
        var registry = new ShortcutRegistry();
        registry.Register(new ShortcutDefinition("test.cmd", "Test", "Ctrl+Alt+R"));

        Assert.NotNull(registry.FindByChord("ctrl+alt+r"));
        Assert.NotNull(registry.FindByChord("CTRL+ALT+R"));
        Assert.NotNull(registry.FindByChord("Ctrl+Alt+r"));
    }

    [Fact]
    public void Register_DuplicateChord_Throws()
    {
        var registry = new ShortcutRegistry();
        registry.Register(new ShortcutDefinition("cmd.a", "A", "Ctrl+X"));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new ShortcutDefinition("cmd.b", "B", "Ctrl+X")));
    }

    [Fact]
    public void FindByChord_NotFound_ReturnsNull()
    {
        var registry = new ShortcutRegistry();
        Assert.Null(registry.FindByChord("Ctrl+Alt+Z"));
    }

    [Fact]
    public void ReplaceAll_ReplacesShortcuts()
    {
        var registry = new ShortcutRegistry();
        registry.RegisterDefaults();
        Assert.Equal(39, registry.GetAll().Count);

        var newShortcuts = new[]
        {
            new ShortcutDefinition("custom.1", "Custom 1", "Ctrl+1"),
            new ShortcutDefinition("custom.2", "Custom 2", "Ctrl+2"),
        };
        registry.ReplaceAll(newShortcuts);

        Assert.Equal(2, registry.GetAll().Count);
        Assert.NotNull(registry.FindByChord("Ctrl+1"));
        Assert.Null(registry.FindByChord("Ctrl+Alt+R"));
    }

    [Fact]
    public void ReplaceAll_DuplicateChord_DoesNotClearExistingShortcuts()
    {
        var registry = new ShortcutRegistry();
        registry.RegisterDefaults();

        Assert.Throws<InvalidOperationException>(() =>
            registry.ReplaceAll(new[]
            {
                new ShortcutDefinition("custom.1", "Custom 1", "Ctrl+1"),
                new ShortcutDefinition("custom.2", "Custom 2", "Ctrl+1"),
            }));

        Assert.Equal(39, registry.GetAll().Count);
        Assert.NotNull(registry.FindByChord("Ctrl+Alt+R"));
    }

    [Fact]
    public void RegisterDefaults_IsIdempotent()
    {
        var registry = new ShortcutRegistry();
        registry.RegisterDefaults();
        registry.RegisterDefaults();

        Assert.Equal(39, registry.GetAll().Count);
    }

    [Fact]
    public void RegisterDefaults_HasNoDuplicateChordsOrCommandIds()
    {
        var shortcuts = DefaultShortcutMap.Create();

        Assert.Equal(shortcuts.Count, shortcuts.Select(item => item.Shortcut).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(shortcuts.Count, shortcuts.Select(item => item.CommandId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void GetAll_ReturnsImmutableCopy()
    {
        var registry = new ShortcutRegistry();
        registry.Register(new ShortcutDefinition("test", "Test", "Ctrl+T"));

        var all1 = registry.GetAll();
        var all2 = registry.GetAll();
        Assert.NotSame(all1, all2);
        Assert.Single(all1);
    }
}
