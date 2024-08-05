using System;
using System.Collections.Generic;
using System.Drawing;
using Gtk;
using UserInterface.Intellisense;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

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
        /// Label which holds summaries for the method's arguments.
        /// </summary>
        private Label lblArgumentSummaries;

        /// <summary>
        /// Label which shows the user the number of available overloads for this method.
        /// </summary>
        private Label lblOverloadIndex;

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
        /// Index of the visible method completion.
        /// </summary>
        private int visibleCompletionIndex = 0;

        /// <summary>
        /// List of method completions for all overloads of this method.
        /// </summary>
        private List<MethodCompletion> completions;

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
            lblOverloadIndex = new Label();

            // Left-align text
            lblMethodSignature.Xalign = 0f;
            lblMethodSummary.Xalign = 0f;
            lblArgumentSummaries.Xalign = 0f;
            lblOverloadIndex.Xalign = 1f;
            lblOverloadIndex.Yalign = 1f;
            lblArgumentSummaries.WidthChars = 100;
            //lblArgumentSummaries.Ellipsize = Pango.EllipsizeMode.Start;
            lblArgumentSummaries.LineWrap = true;

            Box bottomRow = new Box(Orientation.Horizontal, 0);
            bottomRow.PackStart(lblArgumentSummaries, true, true, 0);
            bottomRow.PackEnd(lblOverloadIndex, false, false, 0);

            Box container = new Box(Orientation.Vertical, 0);
            container.PackStart(lblMethodSignature, false, false, 0);
            container.PackStart(lblMethodSummary, false, false, 0);
            container.PackStart(bottomRow, false, false, 0);

            mainWindow.Add(container);
            mainWindow.Resizable = false;
            mainWindow.Destroyed += OnDestroyed;
            Window masterWindow = (MasterView as ViewBase)?.MainWidget?.Toplevel as Window;
            if (masterWindow != null)
            {
                masterWindow.KeyPressEvent += OnKeyPress;
                masterWindow.FocusOutEvent += OnFocusOut;
                masterWindow.ButtonPressEvent += OnFocusOut;
            }
            mainWidget = mainWindow;
            Visible = false;
        }

        /// <summary>
        /// List of method completions for all overloads of this method.
        /// </summary>
        public List<MethodCompletion> Completions
        {
            get
            {
                return completions;
            }
            set
            {
                completions = value;
                if (completions.Count > 0)
                    VisibleCompletionIndex = 0;
            }
        }

        /// <summary>
        /// Index of the visible method completion.
        /// </summary>
        public int VisibleCompletionIndex
        {
            get
            {
                return visibleCompletionIndex;
            }
            set
            {
                if (value >= completions.Count)
                    value = 0;
                else if (value < 0)
                    value = completions.Count - 1;
                visibleCompletionIndex = value;
                Refresh();
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
                    mainWindow.Hide();
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

                    if (mainWindow.WidthRequest > 0 && mainWindow.HeightRequest > 0)
                        mainWindow.Resize(mainWindow.WidthRequest, mainWindow.HeightRequest);
                }
            }
        }

        /// <summary>
        /// Invoked when this view is destroyed. Detaches event handlers.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                Window masterWindow = (MasterView as ViewBase)?.MainWidget?.Toplevel as Window;
                if (masterWindow != null)
                {
                    masterWindow.KeyPressEvent -= OnKeyPress;
                    masterWindow.FocusOutEvent -= OnFocusOut;
                    masterWindow.ButtonPressEvent -= OnFocusOut;
                }
                mainWindow.Destroyed -= OnDestroyed;
            }
            catch
            {
                // Swallow exceptions
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
                    else if (args.Event.Key == Gdk.Key.Up)
                    {
                        VisibleCompletionIndex--;
                        // If the user pressed an arrow key, we don't want them to move up or down a row in the text editor.
                        // To stop propagation of this keypress event, we set args.RetVal to true.
                        args.RetVal = true;
                    }
                    else if (args.Event.Key == Gdk.Key.Down)
                    {
                        VisibleCompletionIndex++;
                        // If the user pressed an arrow key, we don't want them to move up or down a row in the text editor.
                        // To stop propagation of this keypress event, we set args.RetVal to true.
                        args.RetVal = true;
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

        /// <summary>
        /// Updates the view. Typically called after the user cycles through the
        /// available method overloads.
        /// </summary>
        private void Refresh()
        {
            MethodCompletion completion = completions[visibleCompletionIndex];
            lblMethodSignature.Text = completion.Signature;
            lblMethodSummary.Text = completion.Summary;
            // For each line in the argument summaries, we need to make the argument name bold. 
            // We can do this by putting it inside html <b></b> tags. To get the name by itself,
            // we look for all text between the start of a line and the first colon : on that line.
            // This makes a lot of assumptions about how the data is formatted by the presenter,
            // but it's good enough for now.
            lblArgumentSummaries.Markup = System.Text.RegularExpressions.Regex.Replace(completion.ParameterDocumentation, @"^([^:]+:)", @"<b>$1</b>", System.Text.RegularExpressions.RegexOptions.Multiline);
            lblArgumentSummaries.WidthChars = Math.Max(completion.Signature.Length, completion.Summary.Length);
            lblOverloadIndex.Text = string.Format("{0} of {1}", visibleCompletionIndex + 1, completions.Count);
        }
    }
}
