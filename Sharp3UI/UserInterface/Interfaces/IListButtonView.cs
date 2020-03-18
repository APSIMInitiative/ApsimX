using System;
using System.Collections.Generic;
using System.Text;
using Gtk;

namespace UserInterface.Interfaces
{
    /// <summary>An interface for a list with a button bar</summary>
    public interface IListButtonView
    {
        /// <summary>The list.</summary>
        IListBoxView List { get; }

        /// <summary>
        /// Filter to be applied to displayed items.
        /// </summary>
        string Filter { get; }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        void AddButton(string text, Image image, EventHandler handler);

        /// <summary>
        /// Adds a button with a submenu.
        /// </summary>
        /// <param name="text">Text for button.</param>
        /// <param name="image">Image for button.</param>
        void AddButtonWithMenu(string text, Image image);

        /// <summary>
        /// Adds a button to a sub-menu.
        /// </summary>
        /// <param name="menuId">Text on the menu button.</param>
        /// <param name="text">Text on the button.</param>
        /// <param name="image">Image on the button.</param>
        /// <param name="handler">Handler to call when button is clicked.</param>
        void AddButtonToMenu(string menuId, string text, Image image, EventHandler handler);

        /// <summary>
        /// Invoked when the filter is changed.
        /// </summary>
        event EventHandler FilterChanged;
    }
}
