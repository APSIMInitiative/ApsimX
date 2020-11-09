using System;
using Gtk;

namespace UserInterface.Views
{
    /// <summary>
    /// A view to show detailed error information.
    /// </summary>
    class ErrorView : ViewBase
    {
        /// <summary>
        /// This button closes the view.
        /// </summary>
        private Button closeButton;

        /// <summary>
        /// This button copies the contents of the error message to the clipboard.
        /// </summary>
        private Button copyButton;

        /// <summary>
        /// Text area which the error information is written to.
        /// </summary>
        private TextView textArea;

        /// <summary>
        /// The window which holds all of the view's controls.
        /// </summary>
        private Window errorWindow;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="information">Error information to be displayed.</param>
        /// <param name="owner"></param>
        public ErrorView(string information, ViewBase owner = null) : base(owner)
        {
            errorWindow = new Window("Error Information")
            {
                TransientFor = owner.MainWidget.Toplevel as Window,
                WindowPosition = WindowPosition.CenterAlways,
                // Set a default size of 1280x480. 
                // Without a default size, the window will resize to fit all of the text in the TextView, which is 
                // a bad thing if the text is very long (the window may be bigger than the screen).
                // If we put the text in a ScrolledWindow, we get around this issue, but the window will be tiny.
                // The best solution seems to be a compromise: put the text in a ScrolledWindow, 
                // but also set a minimum size for the window.
                WidthRequest = 1280,
                HeightRequest = 480
            };
            // Capture Keypress events, so the user can close the form via the escape key
            errorWindow.KeyPressEvent += OnKeyPress;
            
            textArea = new TextView()
            {
                Editable = false,                
            };

            ScrolledWindow scroll = new ScrolledWindow()
            {
                HscrollbarPolicy = PolicyType.Automatic,
                VscrollbarPolicy = PolicyType.Automatic,
            };
            scroll.Add(textArea);
            Error = information;

            closeButton = new Button("Close");
            closeButton.Clicked += Close;
            Alignment alignCloseButton = new Alignment(1, 1, 0, 0)
            {
                closeButton
            };
            
            copyButton = new Button("Copy");
            copyButton.Clicked += Copy;
            Alignment alignCopyButton = new Alignment(0, 1, 0, 0)
            {
                copyButton
            };

            HBox buttonContainer = new HBox();
            buttonContainer.PackStart(alignCopyButton, false, false, 0);
            buttonContainer.PackEnd(alignCloseButton, false, false, 0);

            VBox primaryContainer = new VBox()
            {
                Name = "primaryContainer",
                BorderWidth = 20
            };
            primaryContainer.PackStart(scroll, true, true, 0);
            primaryContainer.PackStart(buttonContainer, false, false, 0);
            
            errorWindow.Add(primaryContainer);
        }

        /// <summary>
        /// The full error message.
        /// </summary>
        public string Error
        {
            get
            {
                return textArea.Buffer.Text;
            }
            set
            {
                textArea.Buffer.Text = value;
            }
        }

        /// <summary>
        /// Show the error window - make it visible.
        /// </summary>
        public void Show()
        {
            errorWindow.ShowAll();
            
        }

        /// <summary>
        /// Closes the cleans up the error window.
        /// </summary>
        public void Destroy()
        {
            copyButton.Clicked -= Copy;
            closeButton.Clicked -= Close;
            if (errorWindow != null)
                errorWindow.Destroy();
        }

        /// <summary>
        /// Close the error window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close(object sender, EventArgs e)
        {
            try
            {
                errorWindow.Destroy();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Copies all text in the text area to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Copy(object sender, EventArgs e)
        {
            try
            {
                Gdk.Atom modelClipboard = Gdk.Atom.Intern("CLIPBOARD", false);
                Clipboard cb = Clipboard.Get(modelClipboard);
                cb.Text = Error;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        [GLib.ConnectBefore]
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (e.Event.Key == Gdk.Key.Escape)
                    Destroy();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
