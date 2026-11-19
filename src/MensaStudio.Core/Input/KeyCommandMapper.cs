namespace MensaStudio.Core;

/// <summary>
/// Pure mapping from a key press to a <see cref="Command"/> (design "Key mapping" table).
/// Numpad digits map only to folder commands and top-row digits only to rating commands (Req 7.5);
/// modal states restrict the handled keys (Req 1.10, 14.6, 15.9, 18.6).
/// </summary>
public static class KeyCommandMapper
{
    private const Modifiers AllModifiers = Modifiers.Ctrl | Modifiers.Alt | Modifiers.Shift;

    private static readonly Command NoneCommand = new Command.None();
    private static readonly Command NextCommand = new Command.Navigate(NavigationKind.Next);
    private static readonly Command PreviousCommand = new Command.Navigate(NavigationKind.Previous);
    private static readonly Command FirstCommand = new Command.Navigate(NavigationKind.First);
    private static readonly Command LastCommand = new Command.Navigate(NavigationKind.Last);
    private static readonly Command NextUnassignedCommand = new Command.Navigate(NavigationKind.NextUnassigned);
    private static readonly Command ClearFolderCommand = new Command.ClearFolder();
    private static readonly Command OpenFolderCommand = new Command.OpenFolder();
    private static readonly Command OpenUnsureCommand = new Command.OpenUnsure();
    private static readonly Command OpenSettingsCommand = new Command.OpenSettings();
    private static readonly Command StartApplyCommand = new Command.StartApply();
    private static readonly Command ToggleHelpCommand = new Command.ToggleHelp();
    private static readonly Command HideHelpCommand = new Command.HideHelp();
    private static readonly Command ConfirmPreviewCommand = new Command.ConfirmPreview();
    private static readonly Command CancelPreviewCommand = new Command.CancelPreview();

    /// <summary>Maps a key-down (including auto-repeat) to the command to execute.</summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="mods">Modifiers held during the key press.</param>
    /// <param name="numLockOn">Current NumLock state.</param>
    /// <param name="mode">Current modal state of the main window.</param>
    /// <returns>The command, or <see cref="Command.None"/> when the key is not handled.</returns>
    public static Command Map(InputKey key, Modifiers mods, bool numLockOn, UiMode mode)
    {
        mods &= AllModifiers;

        return mode switch
        {
            UiMode.Normal => MapNormal(key, mods, numLockOn),
            UiMode.HelpVisible => MapHelpVisible(key, mods),
            UiMode.ApplyPreview => MapApplyPreview(key, mods),
            UiMode.Applying => NoneCommand,          // Req 1.10, 15.9
            UiMode.Busy => MapBusy(key, mods),
            _ => NoneCommand,
        };
    }

    private static Command MapNormal(InputKey key, Modifiers mods, bool numLockOn)
    {
        bool alt = (mods & Modifiers.Alt) != 0;

        // Numpad digits: folder channel only (Req 7.1, 7.3, 7.5, 7.11). Alt+NumPad is left to
        // Windows alt-code entry.
        if (key is >= InputKey.NumPad0 and <= InputKey.NumPad9)
        {
            if (!numLockOn || alt)
            {
                return NoneCommand;
            }

            int slot = key - InputKey.NumPad0;
            return slot == 0 ? ClearFolderCommand : new Command.AssignSlot(slot);
        }

        // Top-row digits: rating channel only, without any modifier (Req 8.1, 8.2, 8.5, 8.6).
        if (key is >= InputKey.D0 and <= InputKey.D9)
        {
            if (mods != Modifiers.None)
            {
                return NoneCommand;
            }

            int digit = key - InputKey.D0;
            return digit switch
            {
                0 => new Command.Rate(null),
                <= 5 => new Command.Rate(digit),
                _ => NoneCommand,
            };
        }

        return key switch
        {
            // Navigation (Req 6.1–6.5): any modifiers except Alt.
            InputKey.Right when !alt => NextCommand,
            InputKey.Left when !alt => PreviousCommand,
            InputKey.Home when !alt => FirstCommand,
            InputKey.End when !alt => LastCommand,
            InputKey.N when mods == Modifiers.None => NextUnassignedCommand,

            // Ctrl shortcuts (Req 1.1, 1.3, 14.1, 19.12).
            InputKey.O when mods == Modifiers.Ctrl => OpenFolderCommand,
            InputKey.U when mods == Modifiers.Ctrl => OpenUnsureCommand,
            InputKey.Enter when mods == Modifiers.Ctrl => StartApplyCommand,
            InputKey.OemComma when mods == Modifiers.Ctrl => OpenSettingsCommand,

            // Help (Req 18.2).
            InputKey.F1 when mods == Modifiers.None => ToggleHelpCommand,

            _ => NoneCommand,
        };
    }

    // Req 18.2, 18.5, 18.6: only F1 and Escape are handled while the Help_Overlay is visible.
    private static Command MapHelpVisible(InputKey key, Modifiers mods) => (key, mods) switch
    {
        (InputKey.F1, Modifiers.None) => ToggleHelpCommand,
        (InputKey.Escape, Modifiers.None) => HideHelpCommand,
        _ => NoneCommand,
    };

    // Req 14.6: only Enter (confirm) and Escape (cancel) are handled while the Apply_Preview is shown.
    private static Command MapApplyPreview(InputKey key, Modifiers mods) => (key, mods) switch
    {
        (InputKey.Enter, Modifiers.None) => ConfirmPreviewCommand,
        (InputKey.Escape, Modifiers.None) => CancelPreviewCommand,
        _ => NoneCommand,
    };

    // Folder scan/load in progress: only F1 and Ctrl+, are handled (Req 18.2, 19.12).
    private static Command MapBusy(InputKey key, Modifiers mods) => (key, mods) switch
    {
        (InputKey.F1, Modifiers.None) => ToggleHelpCommand,
        (InputKey.OemComma, Modifiers.Ctrl) => OpenSettingsCommand,
        _ => NoneCommand,
    };
}
