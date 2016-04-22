// -----------------------------------------------------------------------
// <copyright file="ListButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>An interface for a list with a button bar</summary>
    public interface IListButtonView
    {
        /// <summary>The list.</summary>
        IListBoxView List { get; }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        void AddButton(string text, Image image, EventHandler handler);
    }

    /// <summary>A view for a list with a button bar</summary>
    public partial class ListButtonView : UserControl, IListButtonView
    {
        /// <summary>Constructor</summary>
        public ListButtonView()
        {
            InitializeComponent();
        }

        /// <summary>The list.</summary>
        public IListBoxView List { get { return listBoxView1; } }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        public void AddButton(string text, Image image, EventHandler handler)
        {
            Button button = new Button();
            button.Text = text;
            button.Image = image;
            button.Click += handler;
            button.AutoSize = true;
            if (image != null)
                button.TextImageRelation = TextImageRelation.ImageAboveText;

            buttonPanel.Controls.Add(button);
        }
    }
}
