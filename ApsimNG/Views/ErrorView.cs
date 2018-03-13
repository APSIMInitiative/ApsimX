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
        private Button BtnClose;

        /// <summary>
        /// This button copies the contents of the error message to the clipboard.
        /// </summary>
        private Button BtnCopy;

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
                WindowPosition = WindowPosition.CenterAlways
            };
            errorWindow.KeyPressEvent += OnKeyPress;
            
            textArea = new TextView()
            {
                Editable = false,
                BorderWidth = 20
            };

            Error = information;

            BtnClose = new Button("Close");
            BtnClose.Clicked += Close;
            Alignment alignCloseButton = new Alignment(1, 1, 0, 0)
            {
                BtnClose
            };
            
            BtnCopy = new Button("Copy");
            BtnCopy.Clicked += Copy;
            Alignment alignCopyButton = new Alignment(0, 1, 0, 0)
            {
                BtnCopy
            };

            HBox buttonContainer = new HBox();
            buttonContainer.PackStart(alignCopyButton, false, false, 0);
            buttonContainer.PackEnd(alignCloseButton, false, false, 0);

            VBox primaryContainer = new VBox()
            {
                Name = "primaryContainer"
            };
            primaryContainer.PackStart(textArea, true, true, 0);
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
            BtnCopy.Clicked -= Copy;
            BtnClose.Clicked -= Close;
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
            errorWindow.Destroy();
        }

        /// <summary>
        /// Copies all text in the text area to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Copy(object sender, EventArgs e)
        {
            Gdk.Atom modelClipboard = Gdk.Atom.Intern("CLIPBOARD", false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            cb.Text = Error;
        }

        [GLib.ConnectBefore]
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.Event.Key == Gdk.Key.Escape)
                Destroy();
        }
    }
}
