using System;
using Gtk;

namespace UserInterface.Views
{
    /// <summary>
    /// Displays another view in a popup window.
    /// </summary>
    public class WindowView : ViewBase
    {
        /// <summary>
        /// The view to be displayed in the window.
        /// </summary>
        private ViewBase view;

        /// <summary>
        /// The popup window.
        /// </summary>
        private Window popupWindow;

        /// <summary>
        /// Constructor - requires a reference to the main view.
        /// Technically, we could use a reference to any view
        /// whose main widget is anchored, but the main view provides
        /// convenient access to the top-level window.
        /// </summary>
        /// <param name="mainView">Main view.</param>
        /// <param name="viewToDisplay">View to be displayed in a popup window.</param>
        /// <param name="title">Window title.</param>
        public WindowView(MainView mainView, ViewBase viewToDisplay, string title)
        {
            view = viewToDisplay;

            popupWindow = new Window(title);
            popupWindow.TransientFor = mainView.MainWidget.Toplevel as Window;
            popupWindow.Decorated = true;
            popupWindow.SkipPagerHint = false;
            popupWindow.SkipTaskbarHint = false;
            popupWindow.WindowPosition = WindowPosition.CenterOnParent;
            popupWindow.DestroyWithParent = true;

            popupWindow.Add(view.MainWidget);

            popupWindow.Destroyed += OnWindowClosed;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            try
            {
                Closed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the window is closed.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Window width. -1 for auto width.
        /// </summary>
        public int Width
        {
            get
            {
                return popupWindow.WidthRequest;
            }
            set
            {
                popupWindow.WidthRequest = value;
            }
        }

        /// <summary>
        /// Window height. -1 for auto width.
        /// </summary>
        public int Height
        {
            get
            {
                return popupWindow.HeightRequest;
            }
            set
            {
                popupWindow.HeightRequest = value;
            }
        }

        /// <summary>
        /// Is the window resizable?
        /// </summary>
        public bool Resizable
        {
            get
            {
                return popupWindow.Resizable;
            }
            set
            {
                popupWindow.Resizable = value;
            }
        }

        public bool Visible
        {
            get
            {
                return popupWindow.Visible;
            }
            set
            {
                if (value)
                    popupWindow.ShowAll();
                else
                    popupWindow.Hide();
            }
        }
    }
}
