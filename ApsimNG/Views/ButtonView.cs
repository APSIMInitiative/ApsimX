// -----------------------------------------------------------------------
// <copyright file="ButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using Gtk;

    /// <summary>An interface for a button</summary>
    public interface IButtonView
    {
        /// <summary>Invoked when the user clicks the button.</summary>
        event EventHandler Clicked;

        /// <summary>Get or sets the text of the button.</summary>
        string Value { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }
    }


    /// <summary>A button view.</summary>
    public class ButtonView : ViewBase, IButtonView
    {
        /// <summary>Invoked when the user clicks the button.</summary>
        public event EventHandler Clicked;

        private Button button;

        /// <summary>Constructor</summary>
        public ButtonView(ViewBase owner) : base(owner)
        {
            button = new Button();
            _mainWidget = button;
            button.Clicked += OnButtonClick;
            button.SetSizeRequest(80, 36);
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            button.Clicked -= OnButtonClick;
        }

        /// <summary>Get or sets the text of the button.</summary>
        public string Value
        {
            get { return button.Label; }
            set { button.Label = value; }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return button.Visible; }
            set { button.Visible = value; }
        }

        /// <summary>User has clicked the button.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonClick(object sender, EventArgs e)
        {
            PerformClick();
        }

        /// <summary>Click the button.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PerformClick()
        {
            if (Clicked != null)
                Clicked.Invoke(this, new EventArgs());
        }
    }
}
