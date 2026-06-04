namespace ModelForge.Excel.Commands
{
    public sealed class ShortcutDefinition
    {
        public ShortcutDefinition(string commandId, string displayName, string shortcut)
        {
            CommandId = commandId;
            DisplayName = displayName;
            Shortcut = shortcut;
        }

        public string CommandId { get; }

        public string DisplayName { get; }

        public string Shortcut { get; }
    }
}
