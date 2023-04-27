using System;

namespace Models.Core
{

    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MainMenuAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the main menu name.
        /// </summary>
        public string MenuName { get; private set; }

        /// <summary>
        /// The shortcut key to activate the menu item with the keyboard.
        /// </summary>
        public string Hotkey { get; private set; }

        /// <summary>
        /// Create a new <see cref="MainMenuAttribute"/> instance.
        /// </summary>
        /// <param name="menuName">Name of the menu item.</param>
        /// <param name="hotkey">Menu item keyboard shortcut.</param>
        public MainMenuAttribute(string menuName, string hotkey = null)
        {
            MenuName = menuName;
            Hotkey = hotkey;
        }
    } 
}
