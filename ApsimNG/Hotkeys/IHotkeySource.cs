using System.Collections.Generic;

namespace UserInterface.Hotkeys
{
    /// <summary>
    /// An interface for a class which can retrieve hotkey information for an
    /// application domain or context.
    /// </summary>
    public interface IHotkeySource
    {
        /// <summary>
        /// Get hotkey metadata for this context.
        /// </summary>
        IEnumerable<IHotkey> GetHotkeys();
    }
}
