// -----------------------------------------------------------------------
// <copyright file="ButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;

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
    public partial class ButtonView : UserControl, IButtonView
    {
        /// <summary>Invoked when the user clicks the button.</summary>
        public event EventHandler Clicked;

        /// <summary>Constructor</summary>
        public ButtonView()
        {
            InitializeComponent();
        }

        /// <summary>Get or sets the text of the button.</summary>
        public string Value
        {
            get { return button1.Text; }
            set { button1.Text = value; }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return button1.Visible; }
            set { button1.Visible = value; }
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
