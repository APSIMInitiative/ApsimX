namespace UserInterface.Views
{
    using Gtk;
    using Interfaces;
    using System;
    using System.Drawing;

    /// <summary>
    /// View for a small intellisense window which displays the 
    /// completion options for a method.
    /// </summary>
    class MethodCompletionView : ViewBase, IMethodCompletionView
    {
        /// <summary>
        /// The main window for this view.
        /// </summary>
        private Window mainWindow;

        /// <summary>
        /// Label which holds the method signature.
        /// </summary>
        private Label lblMethodSignature;

        /// <summary>
        /// Label which holds the method summary.
        /// </summary>
        private Label lblMethodSummary;

        private Label lblArgumentSummaries;

        /// <summary>
        /// When the user is finished typing their method call (e.g. when they press the ')' key),
        /// this popup must be hidden, but what if one of their arguments contains a set of brackets?
        /// This field is used to count the number of bracket characters they type. Typing a (
        /// increments this number, and typing a ) decrements this number.
        /// </summary>
        private int bracketIndex = 0;

        /// <summary>
        /// Whenever the popup window is hidden and then displayed again, its location resets to the default
        /// (0, 0). This field acts as a workaround, storing the previous location of the popup.
        /// </summary>
        private Point previousLocation;

        /// <summary>
        /// Prepares the view for use, but doesn't show it. 
        /// After calling this constructor, set <see cref="Visible"/> to true to display the popup.
        /// </summary>
        /// <param name="owner">Owner widget.</param>
        public MethodCompletionView(ViewBase owner) : base(owner)
        {
            mainWindow = new Window(WindowType.Popup)
            {
                Decorated = false,
                SkipPagerHint = true,
                SkipTaskbarHint = true,
                TransientFor = owner.MainWidget.Toplevel as Window
            };

            lblMethodSignature = new Label();
            lblMethodSummary = new Label();
            lblArgumentSummaries = new Label();

            // Left-align text
            lblMethodSignature.Xalign = 0f;
            lblMethodSummary.Xalign = 0f;
            lblArgumentSummaries.Xalign = 0f;

            VBox container = new VBox();
            container.PackStart(lblMethodSignature, false, false, 0);
            container.PackStart(lblMethodSummary, false, false, 0);
            container.PackStart(lblArgumentSummaries, false, false, 0);

            mainWindow.Add(container);
            Window masterWindow = owner.MainWidget.Toplevel as Window;
            masterWindow.KeyPressEvent += OnKeyPress;
            masterWindow.FocusOutEvent += OnFocusOut;
            Visible = false;
        }

        /// <summary>
        /// Gets or sets the method signature.
        /// </summary>
        public string MethodSignature
        {
            get
            {
                return lblMethodSignature.Text;
            }
            set
            {
                lblMethodSignature.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the method summary.
        /// </summary>
        public string MethodSummary
        {
            get
            {
                return lblMethodSummary.Text;
            }
            set
            {
                lblMethodSummary.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the argument summaries.
        /// </summary>
        public string ArgumentSummaries
        {
            get
            {
                return lblArgumentSummaries.Text;
            }
            set
            {
                lblArgumentSummaries.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the visibility of the window.
        /// </summary>
        public bool Visible
        {
            get
            {
                return mainWindow.Visible;
            }
            set
            {
                if (value)
                {
                    mainWindow.ShowAll();
                    Location = previousLocation;
                }
                else
                {
                    previousLocation = Location;
                    mainWindow.HideAll();
                }
            }
        }

        /// <summary>
        /// Gets or sets the location (top-left corner) of the popup window.
        /// </summary>
        public Point Location
        {
            get
            {
                int x, y;
                mainWindow.GetPosition(out x, out y);
                return new Point(x, y);
            }
            set
            {
                previousLocation = value;
                if (Visible)
                {
                    mainWindow.Move(value.X, value.Y);
                    mainWindow.Resize(mainWindow.WidthRequest, mainWindow.HeightRequest);
                }
            }
        }

        /// <summary>
        /// Invoked when the user presses a key.
        /// Closes the popup if the key is enter.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            try
            {
                if (Visible)
                {
                    if (args.Event.Key == Gdk.Key.Escape)
                        Visible = false;
                    else if (args.Event.Key == Gdk.Key.parenleft)
                        bracketIndex++;
                    else if (args.Event.Key == Gdk.Key.parenright)
                    {
                        bracketIndex--;
                        if (bracketIndex <= 0)
                            Visible = false;
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the main Apsim window loses focus.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnFocusOut(object sender, EventArgs args)
        {
            try
            {
                Visible = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
