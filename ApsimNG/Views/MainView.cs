namespace UserInterface.Views
{
    using APSIM.Shared.Utilities;
    using Gtk;
    using Models.Core;
    using MonoMac.AppKit;
    using System;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Linq;
    using Interfaces;
    using EventArguments;

    /// <summary>An enum type for the AskQuestion method.</summary>
    public enum QuestionResponseEnum { Yes, No, Cancel }

    /// <summary>
    /// TabbedExplorerView maintains multiple explorer views in a tabbed interface. It also
    /// has a StartPageView that is shown to the use when they open a new tab.
    /// </summary>
    public class MainView : ViewBase, IMainView
    {
        /// <summary>
        /// List of resources embedded in this assembly.
        /// </summary>
        private static string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        /// <summary>
        /// 
        /// </summary>
        private static string indexTabText = "Home";

        /// <summary>
        /// Stores the size, in points, of the "default" base font
        /// </summary>
        private double defaultBaseSize;

        /// <summary>
        /// Keeps track of whether or not the waiting cursor is being used.
        /// </summary>
        private bool waiting = false;

        /// <summary>
        /// The size, in points, of our base font
        /// </summary>
        private double baseFontSize = 12.5;

        /// <summary>
        /// Step by which we do font size changes (in points)
        /// </summary>
        private double scrollSizeStep = 0.5;

        /// <summary>
        /// Number of buttons in the status panel.
        /// </summary>
        private int numberOfButtons;

        /// <summary>
        /// Button panel for the left hand view's start page.
        /// </summary>
        private ListButtonView listButtonView1;

        /// <summary>
        /// Button panel for the right hand view's start page.
        /// </summary>
        private ListButtonView listButtonView2;

        /// <summary>
        /// Main Gtk window.
        /// </summary>
        private Window window1 = null;

        /// <summary>
        /// Progress bar which displays simulation progress.
        /// </summary>
        private ProgressBar progressBar = null;

        /// <summary>
        /// Status window used to display error messages and other information.
        /// </summary>
        private TextView StatusWindow = null;

        /// <summary>
        /// Button to stop a simulation.
        /// </summary>
        private Button stopButton = null;

        /// <summary>
        /// Primary widget for tabs on the left side of the screen.
        /// </summary>
        private Notebook notebook1 = null;

        /// <summary>
        /// Primary widget for tabs on the right side of the screen.
        /// </summary>
        private Notebook notebook2 = null;

        /// <summary>
        /// Gtk box which holds <see cref="listButtonView1"/>.
        /// </summary>
        private VBox vbox1 = null;

        /// <summary>
        /// Gtk box which holds <see cref="listButtonView2"/>.
        /// </summary>
        private VBox vbox2 = null;

        /// <summary>
        /// Gtk widget which holds the two sets of tabs.
        /// </summary>
        private HPaned hpaned1 = null;

        /// <summary>
        /// Gtk widget which holds the status panel.
        /// </summary>
        private HBox hbox1 = null;

        /// <summary>
        /// Keeps track of the font size (and, in theory, other font attributes).
        /// </summary>
        private Pango.FontDescription baseFont;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainView(ViewBase owner = null) : base(owner)
        {
            MasterView = this;
            numberOfButtons = 0;
            if ((uint)Environment.OSVersion.Platform <= 3)
            {
                Rc.Parse(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                      ".gtkrc"));
            }
            baseFont = Rc.GetStyle(new Label()).FontDescription.Copy();
            defaultBaseSize = baseFont.Size / Pango.Scale.PangoScale;
            FontSize = Utility.Configuration.Settings.BaseFontSize;
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.MainView.glade");
            window1 = (Window)builder.GetObject("window1");
            progressBar = (ProgressBar)builder.GetObject("progressBar");
            StatusWindow = (TextView)builder.GetObject("StatusWindow");
            stopButton = (Button)builder.GetObject("stopButton");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            notebook2 = (Notebook)builder.GetObject("notebook2");
            vbox1 = (VBox)builder.GetObject("vbox1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            hpaned1 = (HPaned)builder.GetObject("hpaned1");
            hbox1 = (HBox)builder.GetObject("hbox1");
            _mainWidget = window1;
            window1.Icon = new Gdk.Pixbuf(null, "ApsimNG.Resources.apsim logo32.png");
            listButtonView1 = new ListButtonView(this);
            listButtonView1.ButtonsAreToolbar = true;
            vbox1.PackEnd(listButtonView1.MainWidget, true, true, 0);
            listButtonView2 = new ListButtonView(this);
            listButtonView2.ButtonsAreToolbar = true;
            vbox2.PackEnd(listButtonView2.MainWidget, true, true, 0);
            hpaned1.PositionSet = true;
            hpaned1.Child2.Hide();
            hpaned1.Child2.NoShowAll = true;
            notebook1.SetMenuLabel(vbox1, new Label(indexTabText));
            notebook2.SetMenuLabel(vbox2, new Label(indexTabText));
            hbox1.HeightRequest = 20;
            

            TextTag tag = new TextTag("error");
            tag.Foreground = "red";
            StatusWindow.Buffer.TagTable.Add(tag);
            tag = new TextTag("warning");
            tag.Foreground = "brown";
            StatusWindow.Buffer.TagTable.Add(tag);
            tag = new TextTag("normal");
            tag.Foreground = "blue";
            StatusWindow.ModifyBase(StateType.Normal, new Gdk.Color(0xff, 0xff, 0xf0));
            StatusWindow.Visible = false;
            stopButton.Image = new Gtk.Image(new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Delete.png", 12, 12));
            stopButton.ImagePosition = PositionType.Right;
            stopButton.Image.Visible = true;
            stopButton.Clicked += OnStopClicked;
            window1.DeleteEvent += OnClosing;
            listButtonView1.ListView.MainWidget.ScrollEvent += ListView_ScrollEvent;
            listButtonView2.ListView.MainWidget.ScrollEvent += ListView_ScrollEvent;
            listButtonView1.ListView.MainWidget.KeyPressEvent += ListView_KeyPressEvent;
            listButtonView2.ListView.MainWidget.KeyPressEvent += ListView_KeyPressEvent;
            //window1.ShowAll();
            if (ProcessUtilities.CurrentOS.IsMac)
                InitMac();
        }

        /// <summary>
        /// Invoked when an error has been thrown in a view.
        /// </summary>
        public event EventHandler<ErrorArgs> OnError;

        /// <summary>
        /// Invoked when application tries to close
        /// </summary>
        public event EventHandler<AllowCloseArgs> AllowClose;

        /// <summary>
        /// Invoked when a tab is closing.
        /// </summary>
        public event EventHandler<TabClosingEventArgs> TabClosing;

        /// <summary>
        /// Invoked when application tries to close
        /// </summary>
        public event EventHandler<EventArgs> StopSimulation;

        /// <summary>
        /// Show a detailed error message.
        /// </summary>
        public event EventHandler ShowDetailedError;

        /// <summary>
        /// Get the list and button view
        /// </summary>
        public IListButtonView StartPage1 { get { return listButtonView1; } }

        /// <summary>
        /// Get the list and button view
        /// </summary>
        public IListButtonView StartPage2 { get { return listButtonView2; } }

        /// <summary>
        /// Controls the height of the status panel.
        /// </summary>
        public int StatusPanelHeight
        {
            get
            {
                return hbox1.Allocation.Height;
            }
            set
            {
                hbox1.HeightRequest = value;
            }
        }

        /// <summary>
        /// The size, in pointer, of our base font
        /// </summary>
        public double FontSize
        {
            get
            {
                return baseFontSize;
            }
            set
            {
                double newSize = Math.Min(40.0, Math.Max(4.0, value));
                if (newSize != baseFontSize)
                {
                    baseFontSize = value;
                    SetFontSize(baseFontSize);
                }
            }
        }

        /// <summary>
        /// The main Gdk window. This is the window which is exposed to the window manager.
        /// </summary>
        public Gdk.Window MainWindow
        {
            get
            {
                return MainWidget == null ? null : MainWidget.Toplevel.GdkWindow;
            }
        }

        /// <summary>
        /// Display the window.
        /// </summary>
        public void Show()
        {
            window1.ShowAll();
        }


        /// <summary>Add a tab form to the tab control. Optionally select the tab if SelectTab is true.</summary>
        /// <param name="text">Text for tab.</param>
        /// <param name="image">Image for tab.</param>
        /// <param name="control">Control for tab.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        public void AddTab(string text, Gtk.Image image, Widget control, bool onLeftTabControl)
        {
            Label tabLabel = new Label();
            // If the tab text passed in is a filename then only show the filename (no path)
            // on the tab. The ToolTipText will still have the full path and name.
            if (text.Contains(Path.DirectorySeparatorChar.ToString()))
                tabLabel.Text = Path.GetFileNameWithoutExtension(text);
            else
                tabLabel.Text = text;
            HBox headerBox = new HBox();
            Button closeBtn = new Button();
            Gtk.Image closeImg = new Gtk.Image(new Gdk.Pixbuf(null, "ApsimNG.Resources.Close.png", 12, 12));

            closeBtn.Image = closeImg;
            closeBtn.Relief = ReliefStyle.None;
            closeBtn.Clicked += OnCloseBtnClick;

            headerBox.PackStart(tabLabel);
            headerBox.PackEnd(closeBtn);

            // Wrap the whole thing inside an event box, so we can respond to a right-button or center-button click
            EventBox eventbox = new EventBox();
            eventbox.HasTooltip = text.Contains(Path.DirectorySeparatorChar.ToString());
            eventbox.TooltipText = text;
            eventbox.ButtonPressEvent += on_eventbox1_button_press_event;
            eventbox.Add(headerBox);
            eventbox.ShowAll();
            Notebook notebook = onLeftTabControl ? notebook1 : notebook2;
            notebook.CurrentPage = notebook.AppendPageMenu(control, eventbox, new Label(tabLabel.Text));
        }

        /// <summary>
        /// Inits code to allow us to use AppKit on Mac OSX
        /// This lets us use the native browser and native file open/save dialogs
        /// Thia initialisation must be done once and only once
        /// Keeping this in a separate function allows the Microsoft .Net Frameworks
        /// to run even when MonoDoc.dll is not present (that is, with Microsoft,
        /// checking for referenced DLLs seems to occur on a "method", rather than "class" basis.
        /// The Mono runtime works differently.
        /// </summary>
        private void InitMac()
        {
            NSApplication.Init();
        }

        /// <summary>
        /// Checks if the current assembly contains a given resource.
        /// </summary>
        /// <param name="name">Name of the resource.</param>
        /// <returns>True if this assembly contains the resource. False otherwise.</returns>
        public bool HasResource(string name)
        {
            return resources.Contains(name);
        }
        /// <summary>
        /// Handles button press event on the "tab" part of a tabbed page.
        /// Currently responds by closing the tab if the middle button was pressed
        /// </summary>
        /// <param name="o">The object issuing the event</param>
        /// <param name="e">Button press event arguments</param>
        public void on_eventbox1_button_press_event(object o, ButtonPressEventArgs e)
        {
            if (e.Event.Button == 2) // Let a center-button click on a tab close that tab.
            {
                CloseTabContaining(o);
            }
        }

        /// <summary>Change the text of a tab.</summary>
        /// <param name="currentTabName">Current tab text.</param>
        /// <param name="newTabName">New text of the tab.</param>
        public void ChangeTabText(object ownerView, string newTabName, string tooltip)
        {
            if (ownerView is ExplorerView)
            {
                Widget tab = (ownerView as ExplorerView).MainWidget;
                Notebook notebook = tab.IsAncestor(notebook1) ? notebook1 : notebook2;
                // The top level of the "label" is an EventBox
                EventBox ebox = (EventBox)notebook.GetTabLabel(tab);
                ebox.TooltipText = tooltip;
                ebox.HasTooltip = !String.IsNullOrEmpty(tooltip);
                // The EventBox holds an HBox
                HBox hbox = (HBox)ebox.Child;
                // And the HBox has the actual label as its first child
                Label tabLabel = (Label)hbox.Children[0];
                tabLabel.Text = newTabName;
            }
        }

        /// <summary>Set the wait cursor (or not)/</summary>
        /// <param name="wait">Shows wait cursor if true, normal cursor if false.</param>
        public void ShowWaitCursor(bool wait)
        {
            WaitCursor = wait;
            while (GLib.MainContext.Iteration())
                ;
        }

        /// <summary>Close the application.</summary>
        /// <param name="askToSave">Flag to turn on the request to save</param>
        public void Close(bool askToSave)
        {
            if (askToSave && AllowClose != null)
            {
                AllowCloseArgs args = new AllowCloseArgs();
                AllowClose.Invoke(this, args);
                if (!args.AllowClose)
                    return;
            }
            stopButton.Clicked -= OnStopClicked;
            window1.DeleteEvent -= OnClosing;
            listButtonView1.ListView.MainWidget.ScrollEvent -= ListView_ScrollEvent;
            listButtonView2.ListView.MainWidget.ScrollEvent -= ListView_ScrollEvent;
            listButtonView1.ListView.MainWidget.KeyPressEvent -= ListView_KeyPressEvent;
            listButtonView2.ListView.MainWidget.KeyPressEvent -= ListView_KeyPressEvent;
            _mainWidget.Destroy();

            // Let all the destruction stuff be carried out, just in 
            // case we've got any unmanaged resources that should be 
            // cleaned up.
            while (GLib.MainContext.Iteration())
                ;

            // If we're running a script passed as a command line argument, 
            // we've never called Application.Run, so we don't want to call
            // Application.Quit. We test this by seeing whether the event 
            // loop is active. If we're not running the Application loop,
            // call Exit instead.
            if (Application.CurrentEvent != null)
                Application.Quit();
            else
                Environment.Exit(0);
        }

        /// <summary>
        /// Helper function for dealing with clicks on the tab labels, or whatever
        /// widgets the tab label might control. Tests to see which tab the 
        /// indicated objects is on. This lets us identify the tabs associated
        /// with click events, for example.
        /// </summary>
        /// <param name="o">The widget that we are seaching for</param>
        /// <returns>Page number of the tab, or -1 if not found</returns>
        private int GetTabOfWidget(object o, ref Notebook notebook, ref string tabName) // Is there a better way?
        {
            tabName = null;
            Widget widg = o as Widget;
            if (widg == null)
                return -1;
            notebook = IsControlOnLeft(o) ? notebook1 : notebook2;
            for (int i = 0; i < notebook.NPages; i++)
            {
                // First check the tab labels
                Widget testParent = notebook.GetTabLabel(notebook.GetNthPage(i));
                if (testParent == widg || widg.IsAncestor(testParent))
                {
                    tabName = notebook.GetTabLabelText(notebook.GetNthPage(i));
                    return i;
                }
                // If not found, check the tab contents
                testParent = notebook.GetNthPage(i);
                if (testParent == widg || widg.IsAncestor(testParent))
                {
                    tabName = notebook.GetTabLabelText(notebook.GetNthPage(i));
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Responds to presses of the "Close" button by closing the associated tab
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public void OnCloseBtnClick(object o, EventArgs e)
        {
            CloseTabContaining(o);
        }

        /// <summary>Close a tab.</summary>
        /// <param name="o">A widget appearing on the tab</param>
        public void CloseTabContaining(object o)
        {
            Notebook notebook = null;
            string tabText = null;
            int tabPage = GetTabOfWidget(o, ref notebook, ref tabText);
            if (tabPage > 0)
            {
                TabClosingEventArgs args = new TabClosingEventArgs();
                if (TabClosing != null)
                {
                    args.LeftTabControl = IsControlOnLeft(o);
                    args.Name = tabText;
                    args.Index = tabPage;
                    TabClosing.Invoke(this, args);
                }
            }
        }

        /// <summary>
        /// Looks for the tab holding the specified user interface object, and makes that the active tab
        /// </summary>
        /// <param name="o">The interface object being sought; normally will be a Gtk Widget</param>
        public void SelectTabContaining(object o)
        {
            Notebook notebook = null;
            string tabText = null;
            int tabPage = GetTabOfWidget(o, ref notebook, ref tabText);
            if (tabPage >= 0 && notebook != null)
                notebook.CurrentPage = tabPage;
        }

        /// <summary>Gets or set the main window position.</summary>
        public Point WindowLocation
        {
            get
            {
                int x, y;
                window1.GetPosition(out x, out y);
                return new Point(x, y);
            }
            set
            {
                window1.Move(value.X, value.Y);
            }
        }

        /// <summary>Gets or set the main window size.</summary>
        public Size WindowSize
        {
            get
            {
                int width, height;
                window1.GetSize(out width, out height);
                return new Size(width, height);
            }
            set
            {
                window1.Resize(value.Width, value.Height);
            }
        }

        /// <summary>Gets or set the main window size.</summary>
        public bool WindowMaximised
        {
            get
            {
                if (window1.GdkWindow != null)
                    return window1.GdkWindow.State == Gdk.WindowState.Maximized;
                else
                    return false;
            }
            set
            {
                if (value)
                    window1.Maximize();
                else
                    window1.Unmaximize();
            }
        }

        /// <summary>Gets or set the main window size.</summary>
        public string WindowCaption
        {
            get { return window1.Title; }
            set { window1.Title = value; }
        }

        /// <summary>Turn split window on/off</summary>
        public bool SplitWindowOn
        {
            get { return hpaned1.Child2.Visible; }
            set
            {
                if (value == hpaned1.Child2.Visible)
                    return;
                if (value)
                {
                    hpaned1.Child2.Show();
                    hpaned1.Position = hpaned1.Allocation.Width / 2;
                }
                else
                    hpaned1.Child2.Hide();
            }
        }

        /// <summary>
        /// Returns true if the object is a control on the left side
        /// </summary>
        public bool IsControlOnLeft(object control)
        {
            if (control is Widget)
            {
                if (control is MenuItem && (control as MenuItem).Parent is Menu)
                {
                    Widget menuOwner = ((control as MenuItem).Parent as Menu).AttachWidget;
                    if (menuOwner != null)
                        return menuOwner.IsAncestor(notebook1);
                }
                return (control as Widget).IsAncestor(notebook1);
            }
            return false;
        }

        /// <summary>
        /// Returns the file name associated with the currently selected object, given
        /// a menu item from the popup menu for the more-recently-used file list
        /// </summary>
        /// <param name="obj">A menu item</param>
        /// <returns></returns>
        public string GetMenuItemFileName(object obj)
        {
            if (obj is MenuItem && (obj as MenuItem).Parent is Menu)
            {
                Widget menuOwner = ((obj as MenuItem).Parent as Menu).AttachWidget;
                if (menuOwner.IsAncestor(notebook1))
                    return StartPage1.List.SelectedValue;
                else if (menuOwner.IsAncestor(notebook2))
                    return StartPage2.List.SelectedValue;
            }
            return null;
        }

        /// <summary>Ask the user a question</summary>
        /// <param name="message">The message to show the user.</param>
        public QuestionResponseEnum AskQuestion(string message)
        {
            MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, message);
            md.Title = "Save changes";
            int result = md.Run();
            md.Destroy();
            switch ((ResponseType)result)
            {
                case ResponseType.Yes: return QuestionResponseEnum.Yes;
                case ResponseType.No: return QuestionResponseEnum.No;
                default: return QuestionResponseEnum.Cancel;
            }
        }

        /// <summary>Add a status message to the explorer window</summary>
        /// <param name="message">The message.</param>
        /// <param name="errorLevel">The error level.</param>
        public void ShowMessage(string message, Simulation.ErrorLevel errorLevel, bool overwrite = true, bool addSeparator = false, bool withButton = true)
        {
            Application.Invoke(delegate
            {
                StatusWindow.Visible = message != null;
                if (overwrite || message == null)
                {
                    numberOfButtons = 0;
                    StatusWindow.Buffer.Clear();
                }

                if (message != null)
                {
                    string tagName;
                    // Output the message
                    if (errorLevel == Simulation.ErrorLevel.Error)
                    {
                        tagName = "error";
                    }
                    else if (errorLevel == Simulation.ErrorLevel.Warning)
                    {
                        tagName = "warning";
                    }
                    else
                    {
                        tagName = "normal";
                    }
                    message = message.TrimEnd(Environment.NewLine.ToCharArray());
                    //message = message.Replace("\n", "\n                      ");
                    message += Environment.NewLine;
                    TextIter insertIter;
                    if (overwrite)
                        insertIter = StatusWindow.Buffer.StartIter;
                    else
                        insertIter = StatusWindow.Buffer.EndIter;

                    StatusWindow.Buffer.InsertWithTagsByName(ref insertIter, message, tagName);
                    if (errorLevel == Simulation.ErrorLevel.Error && withButton)
                        AddButtonToStatusWindow("More Information", numberOfButtons++);
                    if (addSeparator)
                    {
                        insertIter = StatusWindow.Buffer.EndIter;
                        StatusWindow.Buffer.InsertWithTagsByName(ref insertIter, Environment.NewLine + "----------------------------------------------" + Environment.NewLine, tagName);
                    }
                }

                //this.toolTip1.SetToolTip(this.StatusWindow, message);
                progressBar.Visible = false;
                stopButton.Visible = false;
            });
            while (GLib.MainContext.Iteration()) ;
        }

        /// <summary>
        /// Displays an error message with a 'more info' button.
        /// </summary>
        /// <param name="err">Error for which we want to display information.</param>
        public new void ShowError(Exception err)
        {
            OnError?.Invoke(this, new ErrorArgs { Error = err });
        }

        private void AddButtonToStatusWindow(string buttonName, int buttonID)
        {
            TextIter iter = StatusWindow.Buffer.EndIter;
            TextChildAnchor anchor = StatusWindow.Buffer.CreateChildAnchor(ref iter);
            EventBox box = new EventBox();
            ApsimNG.Classes.CustomButton moreInfo = new ApsimNG.Classes.CustomButton(buttonName, buttonID);
            moreInfo.Clicked += ShowDetailedErrorMessage;
            box.Add(moreInfo);
            StatusWindow.AddChildAtAnchor(box, anchor);
            box.ShowAll();
            box.Realize();
            box.ShowAll();
            moreInfo.ParentWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
        }

        [GLib.ConnectBefore]
        private void ShowDetailedErrorMessage(object sender, EventArgs args)
        {
            ShowDetailedError?.Invoke(sender, args);
        }

        /// <summary>
        /// Show progress bar with the specified percent.
        /// </summary>
        /// <param name="percent"></param>
        public void ShowProgress(int percent, bool showStopButton = true)
        {
            // We need to use "Invoke" if the timer is running in a
            // different thread. That means we can use either
            // System.Timers.Timer or Windows.Forms.Timer in 
            // RunCommand.cs
            Application.Invoke(delegate
            {
                progressBar.Visible = true;
                progressBar.Fraction = percent / 100.0;
                if (showStopButton)
                    stopButton.Visible = true;
            });
        }

        /// <summary>User is trying to close the application - allow that to happen?</summary>
        /// <param name="e">Event arguments.</param>
        protected void OnClosing(object o, DeleteEventArgs e)
        {
            if (AllowClose != null)
            {
                AllowCloseArgs args = new AllowCloseArgs();
                AllowClose.Invoke(this, args);
                e.RetVal = !args.AllowClose;
            }
            else
                e.RetVal = false;
            if ((bool)e.RetVal == false)
            {
                Close(false);
            }
        }

        /// <summary>User is trying to stop all currently executing simulations.</summary>
        /// <param name="e">Event arguments.</param>
        protected void OnStopClicked(object o, EventArgs e)
        {
            if (StopSimulation != null)
            {
                EventArgs args = new EventArgs();
                StopSimulation.Invoke(this, args);
            }
        }

        /// <summary>
        /// Handler for mouse wheel events. We intercept it to allow Ctrl+wheel-up/down to adjust font size
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void ListView_ScrollEvent(object o, ScrollEventArgs args)
        {
            Gdk.ModifierType ctlModifier = !ProcessUtilities.CurrentOS.IsMac ? Gdk.ModifierType.ControlMask
                //Mac window manager already uses control-scroll, so use command
                //Command might be either meta or mod1, depending on GTK version
                : (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);

            if ((args.Event.State & ctlModifier) != 0)
            {
                if (args.Event.Direction == Gdk.ScrollDirection.Up)
                    FontSize += scrollSizeStep;
                else if (args.Event.Direction == Gdk.ScrollDirection.Down)
                    FontSize -= scrollSizeStep;
                args.RetVal = true;
            }
        }

        /// <summary>
        /// Handle key press events to allow ctrl +/-/0 to adjust font size
        /// </summary>
        /// <param name="o">Source of the event</param>
        /// <param name="args">Event arguments</param>
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void ListView_KeyPressEvent(object o, KeyPressEventArgs args)
        {
            args.RetVal = false;
            Gdk.ModifierType ctlModifier = !ProcessUtilities.CurrentOS.IsMac ? Gdk.ModifierType.ControlMask
                //Mac window manager already uses control-scroll, so use command
                //Command might be either meta or mod1, depending on GTK version
                : (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);

            if ((args.Event.State & ctlModifier) != 0)
            {
                switch (args.Event.Key)
                {
                    case Gdk.Key.Key_0: FontSize = defaultBaseSize; args.RetVal = true; break;
                    case Gdk.Key.KP_Add:
                    case Gdk.Key.plus: FontSize += scrollSizeStep; args.RetVal = true; break;
                    case Gdk.Key.KP_Subtract:
                    case Gdk.Key.minus: FontSize -= scrollSizeStep; args.RetVal = true; break;
                }
            }
        }

        /// <summary>
        /// Recursively applies a new FontDescription to all widgets
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="newFont"></param>
        private void SetWidgetFont(Widget widget, Pango.FontDescription newFont)
        {
            widget.ModifyFont(newFont);
            if (widget is Container)
            {
                foreach (Widget child in (widget as Container).Children)
                {
                    SetWidgetFont(child, newFont);
                }
                if (widget is Notebook)
                    for (int i = 0; i < (widget as Notebook).NPages; i++)
                        SetWidgetFont((widget as Notebook).GetTabLabel((widget as Notebook).GetNthPage(i)), newFont);
            }
        }

        /// <summary>
        /// Change the font size
        /// </summary>
        /// <param name="newSize">New base font size, in points</param>
        private void SetFontSize(double newSize)
        {
            newSize = Math.Min(40.0, Math.Max(4.0, newSize));
            // Convert the new size from points to Pango units
            int newVal = Convert.ToInt32(newSize * Pango.Scale.PangoScale);
            baseFont.Size = newVal;

            // Iterate through all existing controls, setting the new base font
            if (_mainWidget != null)
                SetWidgetFont(_mainWidget, baseFont);

            // Reset the style machinery to apply the new base font to all
            // newly created Widgets.
            Rc.ReparseAllForSettings(Settings.Default, true);
        }

        /// <summary>
        /// Used to modify the cursor. If set to true, the waiting cursor will be displayed.
        /// If set to false, the default cursor will be used.
        /// </summary>
        public bool WaitCursor
        {
            get
            {
                return waiting;
            }
            set
            {
                if (MainWindow != null)
                {
                    MainWindow.Cursor = value ? new Gdk.Cursor(Gdk.CursorType.Watch) : null;
                    waiting = value;
                }
            }
        }

        /// <summary>
        /// Returns a new Builder object generated by parsing the glade 
        /// text found in the indicated resource.
        /// </summary>
        /// <param name="resourceName">Name of the resouce.</param>
        /// <returns>A new Builder object, or null on failure.</returns>
        public Builder BuilderFromResource(string resourceName)
        {
            Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resStream == null)
                return null;
            StreamReader reader = new StreamReader(resStream);
            string gladeString = reader.ReadToEnd();
            Builder result = new Builder();
            result.AddFromString(gladeString);
            return result;
        }

        /// <summary>Show a message in a dialog box</summary>
        /// <param name="message">The message.</param>
        /// <param name="errorLevel">The error level.</param>
        public int ShowMsgDialog(string message, string title, Gtk.MessageType msgType, Gtk.ButtonsType buttonType, Window masterWindow)
        {
            MessageDialog md = new Gtk.MessageDialog(masterWindow, Gtk.DialogFlags.Modal,
                msgType, buttonType, message);
            md.Title = title;
            int result = md.Run();
            md.Destroy();
            return result;
        }

        /// <summary>
        /// Get whatever text is currently on a specific clipboard.
        /// </summary>
        /// <param name="clipboardName">Name of the clipboard.</param>
        /// <returns></returns>
        public string GetClipboardText(string clipboardName)
        {
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);

            return cb.WaitForText();
        }

        /// <summary>
        /// Place text on a specific clipboard.
        /// </summary>
        /// <param name="text">Text to place on the clipboard.</param>
        /// <param name="clipboardName">Name of the clipboard.</param>
        public void SetClipboardText(string text, string clipboardName)
        {
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            cb.Text = text;
        }
    }

    /// <summary>An event argument structure with a string.</summary>
    public class TabClosingEventArgs : EventArgs
    {
        public bool LeftTabControl;
        public string Name;
        public int Index;
        public bool AllowClose = true;
    }

    /// <summary>An event argument structure with a field for allow to close.</summary>
    public class AllowCloseArgs : EventArgs
    {
        public bool AllowClose;
    }

}
