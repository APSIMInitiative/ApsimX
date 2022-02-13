namespace UserInterface.Hotkeys
{
    /// <summary>
    /// Encapsulates metadata around a keyboard shortcut in the gui.
    /// </summary>
    public class Hotkey : IHotkey
    {
        /// <inheritdoc />
        public string Shortcut { get; private set; }

        /// <inheritdoc />
        public string Description { get; private set; }

        /// <summary>
        /// Initialises a new <see cref="Hotkey"/> instance.
        /// </summary>
        /// <param name="shortcut">The keyboard shortcut.</param>
        /// <param name="description"></param>
        public Hotkey(string shortcut, string description)
        {
            Shortcut = shortcut;
            Description = description;
        }
    }
}
