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
        public string Shortcut { get; }

        /// <summary>
        /// A description of the hotkey's effects.
        /// </summary>
        public string Description { get; }
    }
}
