namespace MensaStudio.Core;

/// <summary>
/// WPF-independent key identity produced by the App <c>KeyRouter</c>.
/// Numpad digits and top-row digits are distinct keys (Req 7.5). With NumLock off Windows reports
/// numpad 7/1/4/6 as Home/End/Left/Right, so those arrive as the navigation keys (Req 6.3, 6.4).
/// </summary>
public enum InputKey
{
    NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    Left, Right, Home, End,
    N, F1, Escape, Enter, O, U, OemComma,
    Other,
}

/// <summary>Modifier keys held during a key press.</summary>
[Flags]
public enum Modifiers
{
    None = 0,
    Ctrl = 1,
    Alt = 2,
    Shift = 4,
}

/// <summary>Modal state of the main window that decides which keys are handled.</summary>
public enum UiMode
{
    /// <summary>No overlay, dialog or long-running operation.</summary>
    Normal,

    /// <summary>The Help_Overlay is shown (Req 18.6).</summary>
    HelpVisible,

    /// <summary>The Apply_Preview is shown (Req 14.6).</summary>
    ApplyPreview,

    /// <summary>The Apply_Executor is running (Req 1.10, 15.9).</summary>
    Applying,

    /// <summary>A folder scan/load is in progress.</summary>
    Busy,
}

/// <summary>Kinds of navigation triggered by <see cref="Command.Navigate"/>.</summary>
public enum NavigationKind
{
    Next,
    Previous,
    First,
    Last,
    NextUnassigned,
}
