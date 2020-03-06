namespace UserInterface.Interfaces
{
    using System;

    /// <summary>An interface for a button</summary>
    public interface IButtonView
    {
        /// <summary>Invoked when the user clicks the button.</summary>
        event EventHandler Clicked;

        /// <summary>
        /// Gets or sets a value of the text of the button.
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is visible.
        /// </summary>
        bool IsVisible { get; set; }
    }
}