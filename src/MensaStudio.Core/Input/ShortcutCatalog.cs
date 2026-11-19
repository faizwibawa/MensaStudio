namespace MensaStudio.Core;

/// <summary>
/// One keyboard shortcut handled by the Key_Router in Normal mode, as shown in the Help_Overlay
/// (Req 18.3). <see cref="Key"/> and <see cref="Modifiers"/> are a representative key press that
/// <see cref="KeyCommandMapper.Map"/> turns into a non-<see cref="Command.None"/> command.
/// </summary>
/// <param name="KeyText">The key or key combination as displayed to the user.</param>
/// <param name="Description">The command the shortcut performs.</param>
/// <param name="Key">The representative key for this shortcut.</param>
/// <param name="Modifiers">The representative modifiers for this shortcut.</param>
public sealed record ShortcutInfo(string KeyText, string Description, InputKey Key, Modifiers Modifiers);

/// <summary>
/// The single source of truth for the shortcuts shown in the Help_Overlay. Every entry corresponds
/// to a Normal-mode key press that <see cref="KeyCommandMapper.Map"/> handles, and the Help_Overlay
/// lists no key the Key_Router does not handle (Req 18.3). Property 14 asserts this list matches the
/// keys <see cref="KeyCommandMapper.Map"/> accepts in Normal mode, so keep it in sync with
/// <see cref="KeyCommandMapper"/>.
/// </summary>
public static class ShortcutCatalog
{
    /// <summary>All Normal-mode shortcuts, in Help_Overlay display order.</summary>
    public static IReadOnlyList<ShortcutInfo> All { get; } = new[]
    {
        // Navigation (Req 6.1–6.5).
        new ShortcutInfo("\u2192", "Next photo", InputKey.Right, Modifiers.None),
        new ShortcutInfo("\u2190", "Previous photo", InputKey.Left, Modifiers.None),
        new ShortcutInfo("Home", "First photo", InputKey.Home, Modifiers.None),
        new ShortcutInfo("End", "Last photo", InputKey.End, Modifiers.None),
        new ShortcutInfo("N", "Next unassigned photo", InputKey.N, Modifiers.None),

        // Folder assignment on the numpad (Req 7.1, 7.3), NumLock on.
        new ShortcutInfo("NumPad 1", "Assign folder 1", InputKey.NumPad1, Modifiers.None),
        new ShortcutInfo("NumPad 2", "Assign folder 2", InputKey.NumPad2, Modifiers.None),
        new ShortcutInfo("NumPad 3", "Assign folder 3", InputKey.NumPad3, Modifiers.None),
        new ShortcutInfo("NumPad 4", "Assign folder 4", InputKey.NumPad4, Modifiers.None),
        new ShortcutInfo("NumPad 5", "Assign folder 5", InputKey.NumPad5, Modifiers.None),
        new ShortcutInfo("NumPad 6", "Assign folder 6", InputKey.NumPad6, Modifiers.None),
        new ShortcutInfo("NumPad 7", "Assign folder 7", InputKey.NumPad7, Modifiers.None),
        new ShortcutInfo("NumPad 8", "Assign folder 8", InputKey.NumPad8, Modifiers.None),
        new ShortcutInfo("NumPad 9", "Assign folder 9", InputKey.NumPad9, Modifiers.None),
        new ShortcutInfo("NumPad 0", "Clear folder assignment", InputKey.NumPad0, Modifiers.None),

        // Rating on the top-row digits (Req 8.1, 8.2).
        new ShortcutInfo("1", "Rate 1 star", InputKey.D1, Modifiers.None),
        new ShortcutInfo("2", "Rate 2 stars", InputKey.D2, Modifiers.None),
        new ShortcutInfo("3", "Rate 3 stars", InputKey.D3, Modifiers.None),
        new ShortcutInfo("4", "Rate 4 stars", InputKey.D4, Modifiers.None),
        new ShortcutInfo("5", "Rate 5 stars", InputKey.D5, Modifiers.None),
        new ShortcutInfo("0", "Clear rating", InputKey.D0, Modifiers.None),

        // Ctrl shortcuts (Req 1.1, 1.3, 14.1, 19.12).
        new ShortcutInfo("Ctrl+O", "Open folder", InputKey.O, Modifiers.Ctrl),
        new ShortcutInfo("Ctrl+U", "Open unsure folder", InputKey.U, Modifiers.Ctrl),
        new ShortcutInfo("Ctrl+Enter", "Apply changes", InputKey.Enter, Modifiers.Ctrl),
        new ShortcutInfo("Ctrl+,", "Open settings", InputKey.OemComma, Modifiers.Ctrl),

        // Help (Req 18.2).
        new ShortcutInfo("F1", "Toggle help", InputKey.F1, Modifiers.None),
    };
}
