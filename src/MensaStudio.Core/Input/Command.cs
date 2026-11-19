namespace MensaStudio.Core;

/// <summary>A command produced by <see cref="KeyCommandMapper"/> and executed by the view model.</summary>
public abstract record Command
{
    // Only the nested records below may derive from Command.
    private Command()
    {
    }

    /// <summary>Move the Current_Position (Req 6).</summary>
    public sealed record Navigate(NavigationKind Kind) : Command;

    /// <summary>Assign the folder mapped to numpad slot 1..9 (Req 7.1).</summary>
    public sealed record AssignSlot(int Slot) : Command;

    /// <summary>Clear the Folder_Assignment of the current Photo (numpad 0, Req 7.3).</summary>
    public sealed record ClearFolder : Command;

    /// <summary>Set the Rating of the current Photo; <c>null</c> clears it (Req 8.1, 8.2).</summary>
    public sealed record Rate(int? Rating) : Command;

    /// <summary>Ctrl+O (Req 1.1).</summary>
    public sealed record OpenFolder : Command;

    /// <summary>Ctrl+U (Req 1.3).</summary>
    public sealed record OpenUnsure : Command;

    /// <summary>Ctrl+, (Req 19.12).</summary>
    public sealed record OpenSettings : Command;

    /// <summary>Ctrl+Enter (Req 14.1).</summary>
    public sealed record StartApply : Command;

    /// <summary>F1 (Req 18.2).</summary>
    public sealed record ToggleHelp : Command;

    /// <summary>Escape while the Help_Overlay is visible (Req 18.5).</summary>
    public sealed record HideHelp : Command;

    /// <summary>Enter while the Apply_Preview is shown (Req 14.6).</summary>
    public sealed record ConfirmPreview : Command;

    /// <summary>Escape while the Apply_Preview is shown (Req 14.6).</summary>
    public sealed record CancelPreview : Command;

    /// <summary>Ignored key; the key press is left unhandled.</summary>
    public sealed record None : Command;
}
