namespace UserInterface.Hotkeys
{
    /// <summary>
    /// An interface for a hotkey (keyboard shortcut).
    /// </summary>
    public interface IHotkey
    {
        /// <summary>
        /// The keyboard shortcut.
        /// </summary>
        string Shortcut { get; }

        /// <summary>
        /// A description of the hotkey's effects.
        /// </summary>
        string Description { get; }
    }
}
