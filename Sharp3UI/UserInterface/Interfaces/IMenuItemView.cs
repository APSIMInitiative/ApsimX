using System;

namespace UserInterface.Interfaces
{
    /// <summary>Interface for a menu item.</summary>
    public interface IMenuItemView
    {
        /// <summary>Menu clicked event.</summary>
        event EventHandler Clicked;

        /// <summary>Gets or set the checked status of the menu item.</summary>
        bool Checked { get; set; }
    }
}
