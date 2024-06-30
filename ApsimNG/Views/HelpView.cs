using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using APSIM.Shared.Utilities;
using Gtk;
using UserInterface.Hotkeys;

namespace UserInterface.Views
{
    /// <summary>
    /// This view displays some basic info about the application.
    /// </summary>
    /// <remarks>
    /// Pretty sure gtk3 has a built-in widget which could do this for us.
    /// </remarks>
    public class HelpView : ViewBase
    {
        private const string citationMarkup = @"<b>APSIM Next Generation citation:</b>

Holzworth, Dean, N.I.Huth, J.Fainges, H.Brown, E.Zurcher, R.Cichota, S.Verrall, N.I.Herrmann, B.Zheng, and V.Snow. “APSIM Next Generation: Overcoming Challenges in Modernising a Farming Systems Model.” Environmental Modelling &amp; Software 103(May 1, 2018): 43–51.https://doi.org/10.1016/j.envsoft.2018.02.002.

<b>APSIM Acknowledgement</b>

The APSIM Initiative would appreciate an acknowledgement in your research paper if you or your team have utilised APSIM in its development. For ease, we suggest the following wording:

<i>Acknowledgment is made to the APSIM Initiative which takes responsibility for quality assurance and a structured innovation programme for APSIM's modelling software, which is provided free for research and development use (see apsim.info for details)</i>";

        /// <summary>
        /// Window in which help info is displayed.
        /// </summary>
        private Window window;

        /// <summary>
        /// Label containing link to the next gen website.
        /// </summary>
        private Button website;

        /// <summary>
        /// Constructor. Initialises the view.
        /// </summary>
        /// <param name="owner"></param>
        public HelpView(MainView owner) : base(owner)
        {
            window = new Window("Help");
            window.TransientFor = owner.MainWidget as Window;
            window.Modal = true;
            window.DestroyWithParent = true;
            window.WindowPosition = WindowPosition.Center;
            window.Resizable = true;
            window.DeleteEvent += OnDelete;
            window.Destroyed += OnClose;
            window.Resizable = false;
            VBox container = new VBox(true, 10);
            container.Homogeneous = false;
            container.Margin = 10;

            website = new Button("Apsim Next Generation Website");
            website.ButtonPressEvent += OnWebsiteClicked;
            container.PackStart(website, false, false, 0);

            Button keyboardShortcuts = new Button("Keyboard Shortcuts");
            keyboardShortcuts.ButtonPressEvent += OnShowKeyboardShortcuts;
            container.PackStart(keyboardShortcuts, false, false, 0);

            Frame citationFrame = new Frame("Acknowledgement");
            Label citation = new Label(citationMarkup);
            citation.Selectable = true;
            citation.UseMarkup = true;

            // fixme - this is very crude. Unfortunately, if we turn on
            // word wrapping, the label's text will not resize if we
            // resize the window, and the default width will be very
            // narrow. This will force the label to be 640px wide.
            window.DefaultWidth = 640;
            citation.Wrap = true;

            citationFrame.Add(citation);
            container.PackStart(citationFrame, true, true, 0);

            ScrolledWindow scroller = new ScrolledWindow();
            scroller.PropagateNaturalHeight = true;
            scroller.Add(container);
            window.Add(scroller);
            mainWidget = window;
        }

        [GLib.ConnectBefore]
        private void OnShowKeyboardShortcuts(object o, ButtonPressEventArgs args)
        {
            try
            {
                window.Close();
                KeyboardShortcutsView shortcutsDialog = new KeyboardShortcutsView();
                shortcutsDialog.Populate(new MainMenuHotkeys().GetHotkeys());
                shortcutsDialog.Show();
            }
            catch (Exception error)
            {
                ShowError(error);
            }
        }

        /// <summary>
        /// Invoked when the user clicks on the link to the website.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event Arguments.</param>
        [GLib.ConnectBefore]
        private void OnWebsiteClicked(object sender, EventArgs e)
        {
            try
            {
                ProcessUtilities.ProcessStart("https://apsimnextgeneration.netlify.app");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Controls the visibility of the view.
        /// Settings this to true displays the view.
        /// </summary>
        public bool Visible
        {
            get
            {
                return window.Visible;
            }
            set
            {
                if (value)
                    window.ShowAll();
                else
                    window.Hide();
            }
        }

        /// <summary>
        /// Invoked when the user closes the window.
        /// This prevents the window from closing, but still hides
        /// the window. This means we don't have to re-initialise
        /// the window each time the user opens it.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnDelete(object sender, DeleteEventArgs args)
        {
            try
            {
                Visible = false;
                args.RetVal = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the window is closed for good, when Apsim closes.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="args">Sender object.</param>
        [GLib.ConnectBefore]
        private void OnClose(object sender, EventArgs args)
        {
            try
            {
                website.ButtonPressEvent += OnWebsiteClicked;
                window.DeleteEvent -= OnDelete;
                window.Destroyed -= OnClose;
                window.Dispose();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
