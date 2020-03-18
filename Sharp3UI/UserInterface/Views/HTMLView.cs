using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gtk;
using APSIM.Shared.Utilities;
using UserInterface.EventArguments;
using UserInterface.Classes;
using System.IO;
using System.Drawing;
using Pango;
using Webview;
using System.Threading.Tasks;

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
        void SetContents(string contents, bool allowModification, bool isURI);

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

    public interface IBrowserWidget : IDisposable
    {
        void Navigate(string uri);
        void LoadHTML(string html);

        /// <summary>
        /// Returns the Title of the current document
        /// </summary>
        /// <returns></returns>
        string GetTitle();

        Widget HoldingWidget { get; set; }

        /// <summary>
        /// Sets the foreground colour of the document.
        /// </summary>
        /// <value></value>
        System.Drawing.Color ForegroundColour { get; set; }

        /// <summary>
        /// Sets the foreground colour of the document.
        /// </summary>
        /// <value></value>
        System.Drawing.Color BackgroundColour { get; set; }

        /// <summary>
        /// Sets the font of the document.
        /// </summary>
        Pango.FontDescription Font { get; set; }

        void ExecJavaScript(string command, object[] args);

        void ExecJavaScript(string command);

        bool Search(string forString, bool forward, bool caseSensitive, bool wrap);
    }

    public class WebWindowWrapper : IBrowserWidget
    {
        private class NativeMethods
        {
            [DllImport("user32.dll", EntryPoint = "SetParent")]
            internal static extern IntPtr
            SetParent([In] IntPtr hWndChild, [In] IntPtr hWndNewParent);
        }

        private Webview.Webview browser;
        private Task runBrowser;

        public Widget HoldingWidget { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public System.Drawing.Color ForegroundColour { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public System.Drawing.Color BackgroundColour { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public FontDescription Font { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public WebWindowWrapper(VBox container)
        {
            //browser = new WebviewBuilder("Browser").WithInvokeCallback(OnConfigure).Debug().Build();
            //runBrowser = Task.Run(() => browser.Run());
            //webSocket = new Gtk.Plug(browser.Hwnd);
            //container.Add(webSocket);
            //browser.Width = browser.Height = 500;

            //NativeMethods.SetParent(browser.Hwnd, (IntPtr)webSocket.Id);
        }

        private void OnConfigure(Webview.Webview webview, string action)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            browser.Dispose();
        }

        public void ExecJavaScript(string command, object[] args)
        {
            throw new NotImplementedException();
        }

        public void ExecJavaScript(string command)
        {
            throw new NotImplementedException();
        }

        public string GetTitle()
        {
            throw new NotImplementedException();
        }

        public void LoadHTML(string html)
        {
            if (browser != null)
            {
                browser.Dispose();
                browser = null;
            }
            IContent content = Content.FromHtml(html);
            browser = new WebviewBuilder("Browser", content).WithInvokeCallback(OnConfigure).Debug().Build();
            runBrowser = Task.Run(() => browser.Run());
            //browser.NavigateToString(html);
        }

        public void Navigate(string uri)
        {
            throw new NotImplementedException();
            //browser.NavigateToUrl(uri);
        }

        public bool Search(string forString, bool forward, bool caseSensitive, bool wrap)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// </summary>
    public class HTMLView : ViewBase, IHTMLView
    {
        /// <summary>
        /// The VPaned object which holds the containers for the memo view and web browser.
        /// </summary>
        private VPaned vpaned1 = null;

        /// <summary>
        /// VBox obejct which holds the web browser.
        /// </summary>
        private VBox vbox2 = null;

        /// <summary>
        /// Frame object which holds and is used to position <see cref="vbox2"/>.
        /// </summary>
        private Frame frame1 = null;

        /// <summary>
        /// HBox which holds the memo view.
        /// </summary>
        private HBox hbox1 = null;

        /// <summary>
        /// Only used on Windows. Holds the HTML element which responds to key
        /// press events.
        /// </summary>
        private object keyPressObject = null;

        /// <summary>
        /// Web browser used to display HTML content.
        /// </summary>
        protected IBrowserWidget browser = null;

        ///// <summary>
        ///// Memo view used to display markdown content.
        ///// </summary>
        //private MemoView memo;

        /// <summary>
        /// In edit mode
        /// </summary>
        private bool editing = false;

        /// <summary>
        /// Used when exporting a map (e.g. autodocs).
        /// </summary>
        protected Gtk.Window popupWindow = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public HTMLView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("UserInterface.Resources.Glade.HTMLView.glade");
            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            frame1 = (Frame)builder.GetObject("frame1");
            hbox1 = (HBox)builder.GetObject("hbox1");
            mainWidget = vpaned1;
            // Handle a temporary browser created when we want to export a map.
            if (owner == null)
            {
                popupWindow = new Gtk.Window(Gtk.WindowType.Popup);
                popupWindow.SetSizeRequest(500, 500);
                // Move the window offscreen; the user doesn't need to see it.
                // This works with IE, but not with WebKit
                // Not yet tested on OSX
                if (ProcessUtilities.CurrentOS.IsWindows)
                    popupWindow.Move(-10000, -10000);
                popupWindow.Add(MainWidget);
                popupWindow.ShowAll();
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
            }
            //memo = new MemoView(this);
            //hbox1.PackStart(memo.MainWidget, true, true, 0);
            vpaned1.PositionSet = true;
            vpaned1.Position = 0;
            hbox1.Visible = false;
            hbox1.NoShowAll = true;
            //memo.ReadOnly = false;
            //memo.WordWrap = true;
            //memo.MemoChange += this.TextUpdate;
            //memo.StartEdit += this.ToggleEditing;
            vpaned1.ShowAll();
            frame1.Realized += OnWidgetExpose;
            hbox1.Realized += Hbox1_Realized;
            hbox1.SizeAllocated += Hbox1_SizeAllocated;
            vbox2.SizeAllocated += OnBrowserSizeAlloc;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// Path to find images on.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// Invoked when the user wishes to copy data out of the HTMLView.
        /// This is currently only used on Windows, as the other web 
        /// browsers are capable of handling the copy event themselves.
        /// </summary>
        public event EventHandler<CopyEventArgs> Copy;

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        public void SetContents(string contents, bool allowModification, bool isURI = false)
        {
            TurnEditorOn(allowModification);
            if (contents != null)
            {
                //if (allowModification)
                //    memo.MemoText = contents;
                //else
                    PopulateView(contents, isURI);
            }
        }

        // Although this isn't the obvious way to handle window resizing,
        // I couldn't find any better technique. 
        public void OnWidgetExpose(object o, EventArgs args)
        {
            try
            {
                int width = frame1.Window.Width;
                int height = frame1.Window.Height;

                frame1.SetSizeRequest(width, height);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        public string GetMarkdown()
        {
            throw new NotImplementedException();
            //return memo.MemoText;
        }

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        public void UseMonoSpacedFont()
        {
        }

        /// <summary>
        /// Enables or disables the Windows web browser.
        /// </summary>
        /// <param name="state">True to enable the browser, false to disable it.</param>
        public void EnableBrowser(bool state)
        {
            //if (browser is TWWebBrowserIE)
            //    (browser as TWWebBrowserIE).Browser.Parent.Enabled = state;
        }

        protected void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                //memo.MemoChange -= this.TextUpdate;
                vbox2.SizeAllocated -= OnBrowserSizeAlloc;
                //if (keyPressObject != null)
                //    (keyPressObject as HtmlElement).KeyPress -= OnKeyPress;
                frame1.Realized -= OnWidgetExpose;
                hbox1.Realized -= Hbox1_Realized;
                hbox1.SizeAllocated -= Hbox1_SizeAllocated;

                if (browser != null)
                    browser.Dispose();
                if (popupWindow != null)
                    popupWindow.Destroy();

                //memo.StartEdit -= this.ToggleEditing;
                //memo.MainWidget.Destroy();
                //memo = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        protected virtual void NewTitle(string title)
        {
        }

        private void Hbox1_Realized(object sender, EventArgs e)
        {
            try
            {
                vpaned1.Position = 30; 
                //memo.LabelText = "Edit text";
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Ok so for reasons I don't understand, the main widget's
        /// size request seems to be in some cases smaller than the
        /// browser's size request. As a result, the HTMLView will
        /// sometimes overlap with other widgets because the HTMLView's
        /// size request is actually smaller than the space used by the
        /// browser. In this scenario I would have expected the browser
        /// widget to be cut off at the limits of the main widget's
        /// gdk window, but this doesn't happen - perhaps due to a
        /// limitation or oversight in the gtk socket component which
        /// we use to wrap the browser widget.
        /// 
        /// Either way, this little hack seems to correct the problem.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnBrowserSizeAlloc(object sender, SizeAllocatedArgs args)
        {
            try
            {
                // Force the main widget to request enough space for
                // the browser.
                mainWidget.HeightRequest = args.Allocation.Height;
                mainWidget.WidthRequest = args.Allocation.Width;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// When the hbox changes size ensure that the panel below follows correctly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hbox1_SizeAllocated(object sender, EventArgs e)
        {
            try
            {
                //if (!this.editing)
                //    vpaned1.Position = memo.HeaderHeight();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void Frame1_Unrealized(object sender, EventArgs e)
        {
            try
            {
                //if ((browser as TWWebBrowserIE) != null)
                //    (vbox2.Toplevel as Window).SetFocus -= MainWindow_SetFocus;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void MainWindow_SetFocus(object o, SetFocusArgs args)
        {
            try
            {
                if (MasterView.MainWindow != null)
                    MasterView.MainWindow.Focus(0);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Populate the view given the specified text.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="editingEnabled"></param>
        /// <returns></returns>
        private void PopulateView(string contents, bool isURI= false)
        {
            if (browser == null)
            {
                browser = new WebWindowWrapper(vbox2);
            }
            if (isURI)
                browser.Navigate(contents);
            else
               browser.LoadHTML(contents);

            //if (MasterView != null)
            //    browser.Font = (MasterView as ViewBase).MainWidget.Style.FontDescription;

            //if (browser is TWWebBrowserIE && (browser as TWWebBrowserIE).Browser != null)
            //{
            //    TWWebBrowserIE ieBrowser = browser as TWWebBrowserIE;
            //    keyPressObject = ieBrowser.Browser.Document.ActiveElement;
            //    if (keyPressObject != null)
            //        (keyPressObject as HtmlElement).KeyPress += OnKeyPress;
            //}

            //browser.BackgroundColour = Utility.Colour.FromGtk(MainWidget.Style.Background(StateType.Normal));
            //browser.ForegroundColour = Utility.Colour.FromGtk(MainWidget.Style.Foreground(StateType.Normal));

            //browser.Navigate("http://blend-bp.nexus.csiro.au/wiki/index.php");
        }

        /// <summary>
        /// Turn the editor on or off.
        /// </summary>
        /// <param name="turnOn">Whether or not the editor should be turned on.</param>
        private void TurnEditorOn(bool turnOn)
        {
            hbox1.Visible = turnOn;
        }

        /// <summary>
        /// Toggle edit / preview mode.
        /// </summary>
        private void ToggleEditMode()
        {
            bool editorIsOn = hbox1.Visible;
            TurnEditorOn(!editorIsOn);   // toggle preview / edit mode.
        }

        /// <summary>
        /// User has clicked 'edit'
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event argument.</param>
        private void OnEditClick(object sender, EventArgs e)
        {
            try
            {
                TurnEditorOn(true);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Text has been changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event argument.</param>
        private void TextUpdate(object sender, EventArgs e)
        {
            try
            {
                //MarkdownDeep.Markdown markDown = new MarkdownDeep.Markdown();
                //markDown.ExtraMode = true;
                //string html = markDown.Transform(memo.MemoText);
                //html = ParseHtmlImages(html);
                //PopulateView(html);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Used to show or hide the editor panel. Used by the memo editing link label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleEditing(object sender, EventArgs e)
        {
            try
            {
                //if (editing)
                //    vpaned1.Position = memo.HeaderHeight();
                //else
                //    vpaned1.Position = (int)Math.Floor(vpaned1.Parent.Allocation.Height / 1.3);
                if (!editing)
                    vpaned1.Position = (int)Math.Floor(vpaned1.Parent.Allocation.Height / 1.3);
                editing = !editing;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Checks the src attribute for all images in the HTML, and attempts to
        /// find a resource of the same name. If the resource exists, it is
        /// written to a temporary file and the image's src is changed to point
        /// to the temp file.
        /// </summary>
        /// <param name="html">String containing valid HTML.</param>
        /// <returns>The modified HTML.</returns>
        private static string ParseHtmlImages(string html)
        {
            //fixme
            return "";
            //var doc = new HtmlAgilityPack.HtmlDocument();
            //doc.LoadHtml(html);
            //// Find images via xpath.
            //var images = doc.DocumentNode.SelectNodes(@"//img");
            //if (images != null)
            //{
            //    foreach (HtmlNode image in images)
            //    {
            //        string src = image.GetAttributeValue("src", null);
            //        if (!File.Exists(src) && !string.IsNullOrEmpty(src))
            //        {
            //            string tempFileName = HtmlToMigraDoc.GetImagePath(src, Path.GetTempPath());
            //            if (!string.IsNullOrEmpty(tempFileName))
            //                image.SetAttributeValue("src", tempFileName);
            //        }
            //    }
            //}
            //return doc.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// User has clicked the help button. 
        /// Opens a web browser (outside of APSIM) and navigates to a help page on the Next Gen site.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event argument.</param>
        private void OnHelpClick(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://apsimdev.apsim.info/Documentation/APSIM(nextgeneration)/Memo.aspx");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
