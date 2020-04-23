using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gtk;
using WebKit;
using MonoMac.AppKit;
using APSIM.Shared.Utilities;
using UserInterface.EventArguments;
using HtmlAgilityPack;
using UserInterface.Classes;
using System.IO;
using System.Drawing;
using Utility;
using System.Globalization;

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


    public class TWWebBrowserIE : IBrowserWidget
    {
        internal class NativeMethods
        {
            [System.Runtime.InteropServices.DllImportAttribute("user32.dll",
                EntryPoint = "SetParent")]
            internal static extern System.IntPtr
            SetParent([System.Runtime.InteropServices.InAttribute()] System.IntPtr
                hWndChild, [System.Runtime.InteropServices.InAttribute()] System.IntPtr
                hWndNewParent);
        }

        public System.Windows.Forms.WebBrowser Browser { get; set; } = null;
        public Socket WebSocket { get; set; } = null;
        public bool Unmapped { get; set; } = false;
        public Widget HoldingWidget { get; set; }

        public void InitIE(Gtk.Box w)
        {
            HoldingWidget = w;
            Browser = new System.Windows.Forms.WebBrowser();
            w.SetSizeRequest(500, 500);
            Browser.Height = 500; // w.GdkWindow.FrameExtents.Height;
            Browser.Width = 500; // w.GdkWindow.FrameExtents.Width;
            Browser.Navigate("about:blank");
            Browser.Document.Write(String.Empty);

            WebSocket = new Gtk.Socket();
            WebSocket.SetSizeRequest(Browser.Width, Browser.Height);
            w.Add(WebSocket);
            WebSocket.Realize();
            WebSocket.Show();
            WebSocket.UnmapEvent += Socket_UnmapEvent;
            IntPtr browser_handle = Browser.Handle;
            IntPtr window_handle = (IntPtr)WebSocket.Id;
            NativeMethods.SetParent(browser_handle, window_handle);

            /// Another interesting issue is that on Windows, the WebBrowser control by default is
            /// effectively an IE7 browser, and I don't think you can easily change that without
            /// changing registry settings. The lack of JSON parsing in IE7 triggers errors in google maps.
            /// See https://code.google.com/p/gmaps-api-issues/issues/detail?id=9004 for the details.
            /// Including the meta tag of <meta http-equiv="X-UA-Compatible" content="IE=edge"/>
            /// fixes the problem, but we can't do that in the HTML that we set as InnerHtml in the
            /// LoadHTML function, as the meta tag triggers a restart of the browser, so it 
            /// just reloads "about:blank", without the new innerHTML, and we get a blank browser.
            /// Hence we set the browser type here.
            /// Another way to get around this problem is to add JSON.Parse support available from
            /// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/JSON
            /// into the HTML Script added when loading Google Maps
            /// I am taking the belts-and-braces approach of doing both, primarily because the 
            /// meta tag, while probably the technically better" solution, sometimes doesn't work.
            /// 10/8/17 - I've added yet another "fix" for this problem: the installer now writes a 
            /// registry key requesting that IE 11 be used for ApsimNG.exe (and for ApsimNG.vshost.exe,
            /// so it also works when run from Visual Studio).

            Browser.DocumentText = @"<!DOCTYPE html>
                   <html>
                   <head>
                   <meta http-equiv=""X-UA-Compatible"" content=""IE=edge,10""/>
                   </head>
                   </html>";
        }

        /// <summary>
        /// Gets the text selected by the user.
        /// </summary>
        public string GetSelectedText()
        {
            Browser.Document.ExecCommand("Copy", false, null);
            dynamic document = Browser.Document.DomDocument;
            dynamic selection = document.selection;
            dynamic text = selection.createRange().text;
            return (string)text;
        }

        /// <summary>
        /// Selects all text in the document.
        /// </summary>
        public void SelectAll()
        {
            Browser.Document.ExecCommand("selectAll", false, null);
        }

        public void Remap()
        {
            // There are some quirks in the use of GTK sockets. I don't know why, but
            // once the socket has been "unmapped", we seem to lose it and its content.
            // In the GUI, this unmapping can occur either by switching to another tab, 
            // or by shrinking the window dimensions down to 0.
            // This explict remapping seems to correct the problem.
            if (WebSocket != null)
            {
                WebSocket.Unmap();
                WebSocket.Map();
            }
            Unmapped = false;
        }

        internal void Socket_UnmapEvent(object o, UnmapEventArgs args)
        {
            Unmapped = true;
        }

        public void Navigate(string uri)
        {
            Browser.Navigate(uri);
        }

        public void LoadHTML(string html)
        {
            if (Browser.Document.Body != null && !html.Contains("<script"))
                // If we already have a document body, this is the more efficient
                // way to update its contents. It doesn't affect the scroll position
                // and doesn't make a little clicky sound.
                // lie112: we do need a full update if the html code includes java <script> used for Chart.js otherwise it is not run and updated
                Browser.Document.Body.InnerHtml = html;
            else
            {
                // When the browser loads and creates its body for the first time,
                // its BackColor and ForeColor properties are reset to their default
                // values (white/black). This causes a flicker when in dark mode.
                // To work around this, we embed some css into the markup when we
                // first load the document. This is a pretty gnarly workaround so
                // may need to be tweaked if there's some strange html passed into here.

                string bgColour = Colour.ToHex(Colour.FromGtk(HoldingWidget.Style.Background(StateType.Normal)));
                string fgColour = Colour.ToHex(Colour.FromGtk(HoldingWidget.Style.Foreground(StateType.Normal)));
                Pango.FontDescription font = HoldingWidget.Style.FontDescription;
                // Don't want any peksy commas showing up during the conversion to string.
                string fontSize = GetHtmlFontSize(font).ToString(CultureInfo.InvariantCulture);
                html = $"<style>body {{ background-color: {bgColour}; color: {fgColour}; font-family: {font.Family}; font-size: {fontSize}; }}</style>" + html;
                Browser.DocumentText = html;
            }
            // Probably should make this conditional.
            // We use a timeout so we don't sit here forever if a document fails to load.

            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (Browser != null && Browser.ReadyState != WebBrowserReadyState.Complete && watch.ElapsedMilliseconds < 10000)
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
        }

        private double GetHtmlFontSize(Pango.FontDescription font)
        {
            return 1.5 * font.Size / Pango.Scale.PangoScale;
        }

        public System.Drawing.Color BackgroundColour
        {
            get
            {
                if (Browser == null || Browser.Document == null)
                    return Color.Empty;
                return Browser.Document.BackColor;
            }
            set
            {
                if (Browser != null && Browser.Document != null)
                    Browser.Document.BackColor = value;
            }
        }

        public System.Drawing.Color ForegroundColour
        {
            get
            {
                if (Browser == null || Browser.Document == null)
                    return Color.Empty;
                return Browser.Document.ForeColor;
            }
            set
            {
                if (Browser != null && Browser.Document != null)
                    Browser.Document.ForeColor = value;
            }
        }

        public Pango.FontDescription Font
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                if (Browser == null || Browser.Document == null || Browser.Document.Body == null)
                    return;

                if (Browser.Document.Body.Style == null)
                    Browser.Document.Body.Style = "";
                string fontSize = GetHtmlFontSize(value).ToString(CultureInfo.InvariantCulture);
                Browser.Document.Body.Style += $"font-family: {value.Family}; font-size: {fontSize}px;";
            }
        }

        public TWWebBrowserIE(Gtk.Box w)
        {
            InitIE(w);
        }

        public string GetTitle()
        {
            if (Browser.Document != null)
                return Browser.Document.Title;
            else
                return String.Empty;
        }

        public bool Search(string forString, bool forward, bool caseSensitive, bool wrap)
        {
            // The Windows.Forms.WebBrowser doesn't provide this as part of the basic interface
            // It can be done using COM interfaces, but that involves pulling in the Windows-specific Microsoft.MSHTML assembly
            // and I don't think this will play well on Mono.
            return false; 
        }

        public void ExecJavaScript(string command, object[] args)
        {
            Browser.Document.InvokeScript(command, args);
        }

        public void ExecJavaScript(string script)
        {
            Browser.Document.InvokeScript(script);
        }

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Browser.Dispose();
                Browser = null;
                WebSocket.Dispose();
                WebSocket = null;
            }

            // Free any unmanaged objects here. 
            //
            disposed = true;
        }
    }

    public class TWWebBrowserSafari : IBrowserWidget
    {
        internal class NativeMethods
        {
            const string LIBQUARTZ = "libgtk-quartz-2.0.dylib";

            [DllImport(LIBQUARTZ)]
            internal static extern IntPtr gdk_quartz_window_get_nsview(IntPtr window);

            [DllImport(LIBQUARTZ)]
            internal static extern IntPtr gdk_quartz_window_get_nswindow(IntPtr window);

            [DllImport(LIBQUARTZ, CallingConvention = CallingConvention.Cdecl)]
            internal static extern bool gdk_window_supports_nsview_embedding();

            [DllImport(LIBQUARTZ)]
            internal extern static IntPtr gtk_ns_view_new(IntPtr nsview);
        }
        
		public class NSEventArgs : EventArgs
		{
			public NSEvent Event;
		}

        public class Safari : MonoMac.WebKit.WebView
        {
			public event EventHandler<NSEventArgs> OnKeyDown;

            public override void KeyDown(NSEvent theEvent)
            {
                base.KeyDown(theEvent);
				if (OnKeyDown != null)
					OnKeyDown.Invoke(this, new NSEventArgs { Event = theEvent });
            }   

            public Safari(System.Drawing.RectangleF frame, string frameName, string groupName)
                : base(frame, frameName, groupName) {}

        }

        public static Gtk.Widget NSViewToGtkWidget(NSView view)
        {
            return new Gtk.Widget(NativeMethods.gtk_ns_view_new((IntPtr)view.Handle));
        }

        public static NSWindow GetWindow(Gtk.Window window)
        {
            if (window.GdkWindow == null)
                return null;
            var ptr = NativeMethods.gdk_quartz_window_get_nswindow(window.GdkWindow.Handle);
            if (ptr == IntPtr.Zero)
                return null;
            return (NSWindow)MonoMac.ObjCRuntime.Runtime.GetNSObject(ptr);
        }

        public static NSView GetView(Gtk.Widget widget)
        {
            var ptr = NativeMethods.gdk_quartz_window_get_nsview(widget.GdkWindow.Handle);
            if (ptr == IntPtr.Zero)
                return null;
            return (NSView)MonoMac.ObjCRuntime.Runtime.GetNSObject(ptr);
        }

        public Safari Browser { get; set; } = null;
        public Gtk.Socket WebSocket { get; set; } = new Gtk.Socket();
        public ScrolledWindow ScrollWindow { get; set; } = new ScrolledWindow();
        public Widget HoldingWidget { get; set; }
        public Color ForegroundColour
        {
            get
            {
                return Color.Empty; // TODO
            }
            set
            {
                string colour = Utility.Colour.ToHex(value);
                Browser.StringByEvaluatingJavaScriptFromString($"document.body.style.color = \"{colour}\";");
            }
        }


        public Color BackgroundColour
        {
            get
            {
                return Color.Empty; // TODO
            }
            set
            {
                string colour = Utility.Colour.ToHex(value);
                Browser.StringByEvaluatingJavaScriptFromString($"document.body.style.backgroundColor = \"{colour}\";");
            }
        }

        public Pango.FontDescription Font
        {
            get => throw new NotImplementedException();
            set
            {
                Browser.StringByEvaluatingJavaScriptFromString($"document.body.style.fontFamily = \"{value.Family}\";");
                Browser.StringByEvaluatingJavaScriptFromString($"document.body.style.fontSize = {1.5 * value.Size / Pango.Scale.PangoScale}");
            }
        }

        /// <summary>
        /// The find form
        /// </summary>
        private Utility.FindInBrowserForm findForm = new Utility.FindInBrowserForm();

        public void InitWebKit(Gtk.Box w)
        {
            HoldingWidget = w;
            Browser = new Safari(new System.Drawing.RectangleF(10, 10, 200, 200), "foo", "bar");
			Browser.OnKeyDown += OnKeyDown;
            ScrollWindow.AddWithViewport(NSViewToGtkWidget(Browser));
            w.PackStart(ScrollWindow, true, true, 0);
            w.ShowAll();
            Browser.ShouldCloseWithWindow = true;
        }

		private void OnKeyDown(object sender, NSEventArgs args)
		{
			if ((args.Event.ModifierFlags & NSEventModifierMask.CommandKeyMask) == NSEventModifierMask.CommandKeyMask)
            {
                if (args.Event.Characters.ToLower() == "a")
                {
                    MonoMac.WebKit.DomRange range = Browser.MainFrameDocument.CreateRange();
                    range.SelectNodeContents(Browser.MainFrameDocument);
                    // Ugh! This is what we need to call, but it's not in the "official" MonoMac.WebKit stuff
                    // It requires a modified version. Be grateful for open source!
                    Browser.SetSelectedDomRange(range, NSSelectionAffinity.Downstream);
                }
                else if (args.Event.Characters.ToLower() == "c")
                {
					Browser.Copy(Browser);
                }
				else if (args.Event.Characters.ToLower() == "f")
                {
					findForm.ShowFor(this);
				}
                else if (args.Event.Characters.ToLower() == "g")
                {
                    findForm.FindNext((args.Event.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != NSEventModifierMask.ShiftKeyMask, null);
                }
            }
        }

        public void Navigate(string uri)
        {
            Browser.MainFrame.LoadRequest(new MonoMac.Foundation.NSUrlRequest(new MonoMac.Foundation.NSUrl(uri)));
        }

        public void LoadHTML(string html)
        {
            Browser.MainFrame.LoadHtmlString(html, new MonoMac.Foundation.NSUrl("file://"));
            // Probably should make this conditional.
            // We use a timeout so we don't sit here forever if a document fails to load.
			Stopwatch watch = new Stopwatch();
			watch.Start();
			while (Browser != null && Browser.IsLoading && watch.ElapsedMilliseconds < 10000)
				while (Gtk.Application.EventsPending())
					Gtk.Application.RunIteration();
        }

        public TWWebBrowserSafari(Gtk.Box w)
        {
            InitWebKit(w);
        }

        public string GetTitle()
        {
            return Browser.MainFrameTitle;
        }

        public bool Search(string forString, bool forward, bool caseSensitive, bool wrap)
        {
            return Browser.Search(forString, forward, caseSensitive, wrap);
        }

        public void ExecJavaScript(string command, object[] args)
        {
            string argString = "";
            bool first = true;
            foreach (object obj in args)
            {
                if (!first)
                    argString += ", ";
                first = false;
                argString += obj.ToString();
            }
            Browser.StringByEvaluatingJavaScriptFromString(command + "(" + argString + ");");
        }

        public void ExecJavaScript(string script)
        {
            Browser.StringByEvaluatingJavaScriptFromString(script);
        }

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
			Browser.OnKeyDown -= OnKeyDown;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Browser.Dispose();
                Browser = null;
                WebSocket.Dispose();
                WebSocket = null;
                ScrollWindow.Destroy();
            }

            // Free any unmanaged objects here. 
            //
            disposed = true;
        }
    }

    public class TWWebBrowserWK : IBrowserWidget
    {

		public WebView Browser = null;
        public ScrolledWindow ScrollWindow { get; set; } = new ScrolledWindow();
        public Widget HoldingWidget { get; set; }

		/// <summary>
        /// The find form
        /// </summary>
        private Utility.FindInBrowserForm findForm = new Utility.FindInBrowserForm();

        public void InitWebKit(Gtk.Box w)
        {
            HoldingWidget = w;
            Browser = new WebKit.WebView();
            ScrollWindow.Add(Browser);
            // Hack to work around webkit bug; webkit will crash the app if a size is not provided
            // See https://bugs.eclipse.org/bugs/show_bug.cgi?id=466360 for a related bug report
            Browser.SetSizeRequest(2, 2);
			Browser.KeyPressEvent += Wb_KeyPressEvent;
            w.PackStart(ScrollWindow, true, true, 0);
            w.ShowAll();
        }

        public void Navigate(string uri)
        {
            Browser.Open(uri);
        }

        public void LoadHTML(string html)
        {
            Browser.LoadHtmlString(html, "file://");
            // Probably should make this conditional.
            // We use a timeout so we don't sit here forever if a document fails to load.

            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (Browser != null && Browser.LoadStatus != LoadStatus.Finished && watch.ElapsedMilliseconds < 10000)
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
        }

        public TWWebBrowserWK(Gtk.Box w)
        {
            InitWebKit(w);
        }

        public string GetTitle()
        {
            return Browser.Title;
        }

        public bool Search(string forString, bool forward, bool caseSensitive, bool wrap)
        {
            return Browser.SearchText(forString, caseSensitive, forward, wrap);
        }

		public void Highlight(string text, bool caseSenstive, bool doHighlight)
        {
			// Doesn't seem to work as well as expected....
 			Browser.SelectAll();
			Browser.UnmarkTextMatches();
			if (doHighlight)
			{
			    Browser.MarkTextMatches(text, caseSenstive, 0);
			    Browser.HighlightTextMatches = true;
			}
        }

        public void ExecJavaScript(string command, object[] args)
        {
            string argString = "";
            bool first = true;
            foreach (object obj in args)
            {
                if (!first)
                    argString += ", ";
                first = false;
                argString += obj.ToString();
            }
            Browser.ExecuteScript(command + "(" + argString + ")");
        }

        public void ExecJavaScript(string script)
        {
            Browser.ExecuteScript(script);
        }

        // Flag: Has Dispose already been called? 
        bool disposed = false;

		[GLib.ConnectBefore]
		void Wb_KeyPressEvent(object o, Gtk.KeyPressEventArgs args)
        {
			args.RetVal = false;
			if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
			{
			    if (args.Event.Key == Gdk.Key.f || args.Event.Key == Gdk.Key.F)
				{
				    findForm.ShowFor(this);
			    }
				else if (args.Event.Key == Gdk.Key.g || args.Event.Key == Gdk.Key.G)
				{	
					findForm.FindNext((args.Event.State & Gdk.ModifierType.ShiftMask) != Gdk.ModifierType.ShiftMask, null);
				}
			}
			else if (args.Event.Key == Gdk.Key.F3)
				findForm.FindNext((args.Event.State & Gdk.ModifierType.ShiftMask) != Gdk.ModifierType.ShiftMask, null);
        }

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
			Browser.KeyPressEvent -= Wb_KeyPressEvent;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Browser.Dispose();
                Browser = null;
                ScrollWindow.Destroy();
            }

            // Free any unmanaged objects here. 
            //
            disposed = true;
        }

        /// <summary>
        /// Sets the background colour of the document.
        /// </summary>
        /// <value></value>

        public System.Drawing.Color BackgroundColour
        {
            get
            {
                return System.Drawing.Color.Empty;
            }
            set
            {
                string colour = Utility.Colour.ToHex(value);
                Browser.ExecuteScript($"document.body.style.backgroundColor = \"{colour}\";");
            }
        }

        /// <summary>
        /// Sets the foreground colour of the document.
        /// </summary>
        /// <value></value>
        public System.Drawing.Color ForegroundColour
        {
            get
            {
                return System.Drawing.Color.Empty;
            }
            set
            {
                string colour = Utility.Colour.ToHex(value);
                Browser.ExecuteScript($"document.body.style.color = \"{colour}\";");
            }
        }

        public Pango.FontDescription Font
        {
            get => throw new NotImplementedException();
            set
            {
                Browser.ExecuteScript($"document.body.style.fontFamily = \"{value.Family}\";");
                Browser.ExecuteScript($"document.body.style.fontSize = \"{1.5 * value.Size / Pango.Scale.PangoScale}\";");
            }
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

        /// <summary>
        /// Memo view used to display markdown content.
        /// </summary>
        private MemoView memo;

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
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.HTMLView.glade");
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
            memo = new MemoView(this);
            hbox1.PackStart(memo.MainWidget, true, true, 0);
            vpaned1.PositionSet = true;
            vpaned1.Position = 0;
            hbox1.Visible = false;
            hbox1.NoShowAll = true;
            memo.ReadOnly = false;
            memo.WordWrap = true;
            memo.MemoChange += this.TextUpdate;
            memo.StartEdit += this.ToggleEditing;
            vpaned1.ShowAll();
            frame1.ExposeEvent += OnWidgetExpose;
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
                if (allowModification)
                    memo.MemoText = contents;
                else
                    PopulateView(contents, isURI);
            }
        }

        // Although this isn't the obvious way to handle window resizing,
        // I couldn't find any better technique. 
        public void OnWidgetExpose(object o, ExposeEventArgs args)
        {
            try
            {
            int height, width;
            frame1.GdkWindow.GetSize(out width, out height);
            frame1.SetSizeRequest(width, height);
            if (browser is TWWebBrowserIE)
            {
                TWWebBrowserIE brow = browser as TWWebBrowserIE;
                if (brow.Unmapped)
                {
                    brow.Remap();
                }

                if (brow.Browser.Height != height || brow.Browser.Width != width)
                {
                    brow.WebSocket.SetSizeRequest(width, height);
                    brow.Browser.Height = height;
                    brow.Browser.Width = width;
                }
                }
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
            return memo.MemoText;
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
            if (browser is TWWebBrowserIE)
                (browser as TWWebBrowserIE).Browser.Parent.Enabled = state;
        }

        protected void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                memo.MemoChange -= this.TextUpdate;
                vbox2.SizeAllocated -= OnBrowserSizeAlloc;
                if (keyPressObject != null)
                    (keyPressObject as HtmlElement).KeyPress -= OnKeyPress;
                frame1.ExposeEvent -= OnWidgetExpose;
                hbox1.Realized -= Hbox1_Realized;
                hbox1.SizeAllocated -= Hbox1_SizeAllocated;
                if ((browser as TWWebBrowserIE) != null)
                {
                    if (vbox2.Toplevel is Window)
                        (vbox2.Toplevel as Window).SetFocus -= MainWindow_SetFocus;
                    frame1.Unrealized -= Frame1_Unrealized;
                    (browser as TWWebBrowserIE).WebSocket.UnmapEvent -= (browser as TWWebBrowserIE).Socket_UnmapEvent;
                }
                if (browser != null)
                    browser.Dispose();
                if (popupWindow != null)
                {
                    popupWindow.Destroy();
                }
                memo.StartEdit -= this.ToggleEditing;
                memo.MainWidget.Destroy();
                memo = null;
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
                memo.LabelText = "Edit text";
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
                if (!this.editing)
                    vpaned1.Position = memo.HeaderHeight();
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
                if ((browser as TWWebBrowserIE) != null)
                    (vbox2.Toplevel as Window).SetFocus -= MainWindow_SetFocus;
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
                if (ProcessUtilities.CurrentOS.IsWindows)
                {
                    browser = CreateIEBrowser(vbox2);
                }
                else if (ProcessUtilities.CurrentOS.IsMac)
                {
                    browser = CreateSafariBrowser(vbox2);
                }
                else
                {
                    browser = CreateWebKitBrowser(vbox2);
                }
            }
            if (isURI)
                browser.Navigate(contents);
            else
               browser.LoadHTML(contents);

            if (MasterView != null)
                browser.Font = (MasterView as ViewBase).MainWidget.Style.FontDescription;

            if (browser is TWWebBrowserIE && (browser as TWWebBrowserIE).Browser != null)
            {
                TWWebBrowserIE ieBrowser = browser as TWWebBrowserIE;
                keyPressObject = ieBrowser.Browser.Document.ActiveElement;
                if (keyPressObject != null)
                    (keyPressObject as HtmlElement).KeyPress += OnKeyPress;

                /// UGH! Once the browser control gets keyboard focus, it doesn't relinquish it to 
                /// other controls. It's actually a COM object, I guess, and running
                /// with a different message loop, and probably in a different thread. 
                /// 
                /// Well, this hack works, more or less.
                if (vbox2.Toplevel is Window)
                    (vbox2.Toplevel as Window).SetFocus += MainWindow_SetFocus;
                frame1.Unrealized += Frame1_Unrealized;
                if (this is MapView) // If we're only displaying a map, remove the unneeded scrollbar
                    ieBrowser.Browser.ScrollBarsEnabled = false;
            }

            browser.BackgroundColour = Utility.Colour.FromGtk(MainWidget.Style.Background(StateType.Normal));
            browser.ForegroundColour = Utility.Colour.FromGtk(MainWidget.Style.Foreground(StateType.Normal));

            //browser.Navigate("http://blend-bp.nexus.csiro.au/wiki/index.php");
        }

        /// <summary>
        /// Handle's the Windows IE browser's key press events.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnKeyPress(object sender, HtmlElementEventArgs e)
        {
            try
            {
                if (browser is TWWebBrowserIE)
                {
                    TWWebBrowserIE ieBrowser = browser as TWWebBrowserIE;

                    // By default, we assume that the key press is not significant, so we set the
                    // event args' return value to false, so event propagation continues.
                    e.ReturnValue = false;

                    int keyCode = e.KeyPressedCode;
                    if (e.CtrlKeyPressed)
                    {
                        keyCode += 96;
                        if (keyCode == 'c')
                            Copy?.Invoke(this, new CopyEventArgs() { Text = ieBrowser.GetSelectedText() });
                        else if (keyCode == 'a')
                            ieBrowser.SelectAll();
                        else if (keyCode == 'f')
                            // We just send the appropriate keypress event to the WebBrowser. This doesn't 
                            // seem to work well for ctrl + a, and doesn't work at all for ctrl + c. 
                            SendKeys.SendWait("^f");
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private IBrowserWidget CreateIEBrowser(Gtk.Box box)
        {
            return new TWWebBrowserIE(box);
        }

        private IBrowserWidget CreateSafariBrowser(Gtk.Box box)
        {
            return new TWWebBrowserSafari(box);
        }

        private IBrowserWidget CreateWebKitBrowser(Gtk.Box box)
        {
            return new TWWebBrowserWK(box);
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
                MarkdownDeep.Markdown markDown = new MarkdownDeep.Markdown();
                markDown.ExtraMode = true;
                string html = markDown.Transform(memo.MemoText);
                html = ParseHtmlImages(html);
                PopulateView(html);
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
                if (editing)
                    vpaned1.Position = memo.HeaderHeight();
                else
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
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            // Find images via xpath.
            var images = doc.DocumentNode.SelectNodes(@"//img");
            if (images != null)
            {
                foreach (HtmlNode image in images)
                {
                    string src = image.GetAttributeValue("src", null);
                    if (!File.Exists(src) && !string.IsNullOrEmpty(src))
                    {
                        string tempFileName = HtmlToMigraDoc.GetImagePath(src, Path.GetTempPath());
                        if (!string.IsNullOrEmpty(tempFileName))
                            image.SetAttributeValue("src", tempFileName);
                    }
                }
            }
            return doc.DocumentNode.OuterHtml;
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
