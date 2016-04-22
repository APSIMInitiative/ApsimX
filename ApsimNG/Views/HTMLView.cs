using System;
using System.Diagnostics;
using System.Threading;
using Glade;
using Gtk;
using WebKit;

namespace UserInterface.Views
{
    /// <summary>
    /// An interface for a HTML view.
    /// </summary>
    interface IHTMLView
    {
        /// <summary>
        /// Path to find images on.
        /// </summary>
        string ImagePath { get; set; }

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        void SetContents(string contents, bool allowModification);

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        string GetMarkdown();

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        void UseMonoSpacedFont();
    }

    interface IBrowserWidget
    {
        void Navigate(string uri);
        void LoadHTML(string html);
    }


    public class TWWebBrowserIE : IBrowserWidget
    {
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll",
            EntryPoint = "SetParent")]
        internal static extern System.IntPtr
        SetParent([System.Runtime.InteropServices.InAttribute()] System.IntPtr
            hWndChild, [System.Runtime.InteropServices.InAttribute()] System.IntPtr
            hWndNewParent);

        public System.Windows.Forms.WebBrowser wb = null;
        public Gtk.Socket socket = new Gtk.Socket();

        public void InitIE(Gtk.Box w)
        {
            wb = new System.Windows.Forms.WebBrowser();
            w.SetSizeRequest(500, 500);
            wb.Height = 500; // w.GdkWindow.FrameExtents.Height;
            wb.Width = 500; // w.GdkWindow.FrameExtents.Width;

            socket.SetSizeRequest(wb.Width, wb.Height);
            w.Add(socket);
            socket.Realize();
            socket.Show();

            IntPtr browser_handle = wb.Handle;
            IntPtr window_handle = (IntPtr)socket.Id;
            SetParent(browser_handle, window_handle);
        }

        public void Navigate(string uri)
        {
            wb.Navigate(uri);
        }

        public void LoadHTML(string html)
        {
            wb.Navigate("about:blank");
            wb.Document.Write(String.Empty);
            wb.DocumentText = html;
        }

        public TWWebBrowserIE(Gtk.Box w)
        {
            InitIE(w);
        }
    }

    public class TWWebBrowserWK : IBrowserWidget
    {
        public WebView wb = null;
        public ScrolledWindow scrollWindow = new ScrolledWindow();

        public void InitWebKit(Gtk.Box w)
        {
            wb = new WebView();
            scrollWindow.Add(wb);
            // Hack to work around webkit bug; webkit will crash the app if a size is not provided
            // See https://bugs.eclipse.org/bugs/show_bug.cgi?id=466360 for a related bug report
            wb.SetSizeRequest(2, 2);
            w.PackStart(scrollWindow, true, true, 0);
            w.ShowAll();
        }

        public void Navigate(string uri)
        {
            wb.Open(uri);
        }

        public void LoadHTML(string html)
        {
            wb.LoadHtmlString(html, "about:blank");
        }

        public TWWebBrowserWK(Gtk.Box w)
        {
            InitWebKit(w);
        }
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// </summary>
    public class HTMLView : ViewBase, IHTMLView
    {
        /// <summary>
        /// Path to find images on.
        /// </summary>
        public string ImagePath { get; set; }

        [Widget]
        private VBox vbox1;
        [Widget]
        private TextView textview1;
        [Widget]
        private VBox vbox2;
        [Widget]
        private Frame frame1;

        private IBrowserWidget browser = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public HTMLView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.HTMLView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;
            vbox1.ShowAll();
            frame1.ExposeEvent += OnWidgetExpose;
        }

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        public void SetContents(string contents, bool allowModification)
        {
            bool editingEnabled = false;
            if (contents != null)
            {
                textview1.Buffer.Text = contents;
                editingEnabled = PopulateView(contents, editingEnabled);
            }

            if (!editingEnabled)
            {
                /// TBI richTextBox1.ContextMenuStrip = null;
                /// TBI textBox1.ContextMenuStrip = null;
            }
            TurnEditorOn(false);
        }

        /// <summary>
        /// Populate the view given the specified text.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="editingEnabled"></param>
        /// <returns></returns>
        private bool PopulateView(string contents, bool editingEnabled)
        {
            if (browser == null)
            {
                if (Environment.OSVersion.Platform.ToString().StartsWith("Win"))
                {
                    browser = new TWWebBrowserIE(vbox2);
                }
                else
                    browser = new TWWebBrowserWK(vbox2);
            }
            browser.LoadHTML(contents);
            //browser.Navigate("http://blend-bp.nexus.csiro.au/wiki/index.php");
            return editingEnabled;
        }

        // Allow this isn't the obvious way to do the window resizing,
        // I couldn't find any better technique. 
        public void OnWidgetExpose(object o, ExposeEventArgs args)
        {
            int height, width;
            frame1.GdkWindow.GetSize(out width, out height);
            vbox2.SetSizeRequest(width, height);
            if (browser is TWWebBrowserIE)
            {
                (browser as TWWebBrowserIE).socket.SetSizeRequest(width, height);
                (browser as TWWebBrowserIE).wb.Height = height;
                (browser as TWWebBrowserIE).wb.Width = width;
            }
            //else
            //    (browser as TWWebBrowserWK).wb.SetSizeRequest(width, height);
        }

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        public string GetMarkdown()
        {
            return textview1.Buffer.Text;
        }

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        public void UseMonoSpacedFont() 
        {
            /// TBI richTextBox1.Font = new Font("Consolas", 10F);   
        }

        /// <summary>
        /// Turn the editor on or off.
        /// </summary>
        /// <param name="turnOn"></param>
        private void TurnEditorOn(bool turnOn)
        {
            vbox2.Visible = !turnOn;
            textview1.Visible = turnOn;

            /// TBI menuItem1.Visible = !turnOn;
            /// TBI menuItem2.Visible = turnOn;
            /// TBIif (textview1.Visible)
            /// TBI    textview1.Focus();

            if (!turnOn)
                PopulateView(textview1.Buffer.Text, true);               
        }

        /// <summary>
        /// Toggle edit / preview mode.
        /// </summary>
        private void ToggleEditMode()
        {
            bool editorIsOn = false;  /// TBI !richTextBox1.Visible;
            TurnEditorOn(!editorIsOn);   // toggle preview / edit mode.
        }

        #region Event Handlers

        /// <summary>
        /// User has clicked a link.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments</param>
        private void OnLinkClicked(object sender, /* TBI LinkClicked */ EventArgs e)
        {
            /// TBI Process.Start(e.LinkText);
        }

        /// <summary>
        /// User has clicked 'edit'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEditClick(object sender, EventArgs e)
        {
            TurnEditorOn(true);
        }

        /// <summary>
        /// User has clicked 'preview'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewClick(object sender, EventArgs e)
        {
            TurnEditorOn(false);
            PopulateView(textview1.Buffer.Text, true);
        }

        /// <summary>
        /// User has pressed a key. Look for toggle character.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDown(object sender, /* TBI Key */ EventArgs e)
        {
            /*
            if (e.KeyCode == Keys.F12)
                ToggleEditMode();
            */
        }

        #endregion

        private void OnHelpClick(object sender, EventArgs e)
        {
            Process.Start("https://www.apsim.info/Documentation/APSIM(nextgeneration)/Memo.aspx");
        }

    }
}
