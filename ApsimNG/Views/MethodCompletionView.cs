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

        /// <summary>
        /// Label which holds the summary of the argument which the user is typing.
        /// </summary>
        private Label lblArgumentSummary;

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
            lblArgumentSummary = new Label();

            // Left-align text
            lblMethodSignature.Xalign = 0f;
            lblMethodSummary.Xalign = 0f;
            lblArgumentSummary.Xalign = 0f;

            VBox container = new VBox();
            container.PackStart(lblMethodSignature, false, false, 0);
            container.PackStart(lblMethodSummary, false, false, 0);
            container.PackStart(lblArgumentSummary, false, false, 0);

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
        /// Gets or sets the argument summary.
        /// </summary>
        public string ArgumentSummary
        {
            get
            {
                return lblArgumentSummary.Text;
            }
            set
            {
                lblArgumentSummary.Text = value;
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
                else if (args.Event.Key == Gdk.Key.comma)
                {
                    // TODO - display summary of next argument
                }
            }
            else if ((args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask && (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask && args.Event.Key == Gdk.Key.space && !string.IsNullOrEmpty(MethodSummary))
                Visible = true;
        }

        /// <summary>
        /// Invoked when the main Apsim window loses focus.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnFocusOut(object sender, EventArgs args)
        {
            Visible = false;
        }
    }
}
