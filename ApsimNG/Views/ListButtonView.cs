// -----------------------------------------------------------------------
// <copyright file="ListButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using Gtk;

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
    public class ListButtonView : ViewBase, IListButtonView
    {
        private VBox vbox;
        private ListBoxView listboxView;
        private ScrolledWindow scrolledwindow1;
        private HButtonBox buttonPanel;

        /// <summary>Constructor</summary>
        public ListButtonView(ViewBase owner) : base(owner)
        {
            vbox = new VBox(false, 0);
            _mainWidget = vbox;
            buttonPanel = new HButtonBox();
            buttonPanel.Layout = ButtonBoxStyle.Start;
            listboxView = new ListBoxView(this);
            scrolledwindow1 = new ScrolledWindow();
            scrolledwindow1.Add(listboxView.MainWidget);
            vbox.PackStart(buttonPanel, false, true, 0);
            vbox.PackStart(scrolledwindow1, true, true, 0);
            _mainWidget.ShowAll();
        }

        /// <summary>The list.</summary>
        public IListBoxView List { get { return listboxView; } }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        public void AddButton(string text, Image image, EventHandler handler)
        {
            Button button = new Button(text);
            button.Image = image;
            button.Clicked += handler;
            button.BorderWidth = 5;
            if (image != null)
                button.ImagePosition = PositionType.Top;

            buttonPanel.PackStart(button, false, false, 0);
            buttonPanel.ShowAll();
        }

    }
}
