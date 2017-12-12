// -----------------------------------------------------------------------
// <copyright file="ButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;

    /// <summary>A button view.</summary>
    public class ButtonView : ViewBase, IButtonView
    {
        /// <summary>
        /// The button object
        /// </summary>
        private Button button;

        /// <summary>The objects constructor</summary>
        /// <param name="owner">The owning view</param>
        public ButtonView(ViewBase owner) : base(owner)
        {
            button = new Button();
            _mainWidget = button;
            button.Clicked += OnButtonClick;
            button.SetSizeRequest(80, 36);
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>Invoked when the user clicks the button.</summary>
        public event EventHandler Clicked;

        /// <summary>Gets or sets the text of the button.</summary>
        public string Value
        {
            get { return button.Label; }
            set { button.Label = value; }
        }

        /// <summary>Gets or sets a value indicating whether the dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return button.Visible; }
            set { button.Visible = value; }
        }

        /// <summary>
        /// Cleanup objects
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            button.Clicked -= OnButtonClick;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>User has clicked the button.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        private void OnButtonClick(object sender, EventArgs e)
        {
            PerformClick();
        }

        /// <summary>Click the button.</summary>
        public void PerformClick()
        {
            if (Clicked != null)
                Clicked.Invoke(this, new EventArgs());
        }
    }
}
