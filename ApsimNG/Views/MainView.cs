using APSIM.Shared.Utilities;
using Gtk;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using Utility;
using MessageType = Models.Core.MessageType;

namespace UserInterface.Views
{

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
        /// Keeps track of whether or not the waiting cursor is being used.
        /// </summary>
        private bool waiting = false;

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
        /// The main Gtk Window.
        /// </summary>
        private Window window1 = null;

        /// <summary>
        /// Progress bar which displays simulation progress.
        /// </summary>
        private ProgressBar progressBar = null;

        /// <summary>
        /// Label adjacent to progress bar. Used to display
        /// progress status updates. The progress bar does
        /// support displaying text by itself, but vertical
        /// space here is limited so we display it in this
        /// label instead.
        /// </summary>
        private Label lblStatus = null;

        /// <summary>
        /// Status window used to display error messages and other information.
        /// </summary>
        private TextView statusWindow = null;

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
        private Widget hbox1 = null;

        /// <summary>
        /// Gtk vpane which holds two main parts of the viuw
        /// </summary>
        private VPaned vpaned1 = null;

        /// <summary>
        /// Dialog which allows the user to change fonts.
        /// </summary>

        private FontChooserDialog fontDialog;


        /// <summary>
        /// Constructor
        /// </summary>
        public MainView(ViewBase owner = null) : base(owner)
        {
            MasterView = (Interfaces.IMainView)this;
            numberOfButtons = 0;
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.MainView.glade");
            window1 = (Window)builder.GetObject("window1");
            progressBar = (ProgressBar)builder.GetObject("progressBar");
            lblStatus = (Label)builder.GetObject("lblStatus");
            statusWindow = (TextView)builder.GetObject("StatusWindow");
            stopButton = (Button)builder.GetObject("stopButton");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            notebook2 = (Notebook)builder.GetObject("notebook2");
            vbox1 = (VBox)builder.GetObject("vbox1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            hpaned1 = (HPaned)builder.GetObject("hpaned1");
            hbox1 = (Widget)builder.GetObject("vbox3");
            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            mainWidget = window1;
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
            hpaned1.AddNotification(OnDividerNotified);
            vpaned1.AddNotification(OnDividerNotified);

            notebook1.SetMenuLabel(vbox1, LabelWithIcon(indexTabText, "go-home"));
            notebook2.SetMenuLabel(vbox2, LabelWithIcon(indexTabText, "go-home"));

            notebook1.SwitchPage += OnChangeTab;
            notebook2.SwitchPage += OnChangeTab;

            notebook1.GetTabLabel(notebook1.Children[0]).Name = "selected-tab";

            hbox1.HeightRequest = 20;

            // Normally, one would specify the style class in the UI (.glade) file.
            // However, doing so breaks gtk2-compatibility, so for now, we will just
            // set the style class in code.
            progressBar.StyleContext.AddClass("fat-progress-bar");


            TextTag tag = new TextTag("error");
            // Make errors orange-ish in dark mode.
            if (Utility.Configuration.Settings.DarkTheme)
                tag.ForegroundGdk = Utility.Colour.ToGdk(ColourUtilities.ChooseColour(1));
            else
                tag.Foreground = "red";
            statusWindow.Buffer.TagTable.Add(tag);
            tag = new TextTag("warning");
            // Make warnings yellow in dark mode.
            if (Utility.Configuration.Settings.DarkTheme)
                tag.ForegroundGdk = Utility.Colour.ToGdk(ColourUtilities.ChooseColour(7));
            else
                tag.Foreground = "brown";
            statusWindow.Buffer.TagTable.Add(tag);
            tag = new TextTag("normal");
            tag.Foreground = "blue";
            statusWindow.Visible = false;
            stopButton.Image = new Gtk.Image(new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Delete.png", 12, 12));
            stopButton.ImagePosition = PositionType.Right;
            stopButton.Image.Visible = true;
            stopButton.Clicked += OnStopClicked;
            window1.DeleteEvent += OnClosing;

            // If font is null, or font family is null, or font size is 0, fallback
            // to the default font (on windows only).
            Pango.FontDescription f = null;
            if (!string.IsNullOrEmpty(Utility.Configuration.Settings.FontName))
                f = Pango.FontDescription.FromString(Utility.Configuration.Settings.FontName);
            if (ProcessUtilities.CurrentOS.IsWindows && (string.IsNullOrEmpty(Utility.Configuration.Settings.FontName) ||
                                                         f.Family == null ||
                                                         f.Size == 0))
            {
                // Default font on Windows is Segoe UI. Will fallback to sans if unavailable.
                Utility.Configuration.Settings.FontName = Pango.FontDescription.FromString("Segoe UI 11").ToString();
            }

            // Can't set font until widgets are initialised.
            if (!string.IsNullOrEmpty(Utility.Configuration.Settings.FontName))
            {
                try
                {
                    Pango.FontDescription font = Pango.FontDescription.FromString(Utility.Configuration.Settings.FontName);
                    ChangeFont(font);
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }

            //window1.ShowAll();
            if (ProcessUtilities.CurrentOS.IsMac)
            {
                InitMac();
                Utility.Configuration.Settings.DarkTheme = false;
                //Utility.Configuration.Settings.DarkTheme = Utility.MacUtilities.DarkThemeEnabled();
            }

            if (!ProcessUtilities.CurrentOS.IsLinux)
                RefreshTheme();


            LoadStylesheets();

        }


        private void LoadStylesheets()
        {
            LoadStylesheet("global");
            LoadStylesheet(Configuration.Settings.DarkTheme ? "dark" : "light");
        }

        private void LoadStylesheet(string cssName)
        {
            string css = ReflectionUtilities.GetResourceAsString($"ApsimNG.Resources.Style.{cssName}.css");
            CssProvider provider = new CssProvider();
            if (!provider.LoadFromData(css))
                throw new Exception($"Unable to parse {cssName}.css");
            StyleContext.AddProviderForScreen(window1.Screen, provider, StyleProviderPriority.Application);
        }


        /// <summary>
        /// Invoked when the user changes tabs.
        /// Gives the selected tab a special name so that its style is
        /// modified according to the rules in the .gtkrc file.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnChangeTab(object sender, SwitchPageArgs args)
        {
            try
            {
                if (sender is Notebook control)
                {
                    for (int i = 0; i < control.Children.Length; i++)
                    {
                        // The top-level widget in the tab label is always an event box.
                        Widget tabLabel = control.GetTabLabel(control.Children[i]);
                        tabLabel.Name = args.PageNum == i ? "selected-tab" : "unselected-tab";
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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

        /// <summary>Invoked when the divider position is changed</summary>
        public event EventHandler DividerChanged;

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
        public int StatusPanelPosition
        {
            get
            {
                return vpaned1.Position;
            }
            set
            {
                vpaned1.Position = value;
            }
        }

        /// <summary>
        /// Height of the VPaned that holds the view
        /// </summary>
        public int PanelHeight
        {
            get { return vpaned1.AllocatedHeight; }
        }

        /// <summary>
        /// Controls the width of the tree panel.
        /// </summary>
        public int TreePanelWidth
        {
            get
            {
                return vpaned1.Position;
            }
            set
            {
                vpaned1.Position = value;
            }
        }

        /// <summary>
        /// The main Gdk window. This is the window which is exposed to the window manager.
        /// </summary>
        public Gdk.Window MainWindow
        {
            get
            {
                return MainWidget == null ? null : MainWidget.Toplevel.Window;
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
            string imageName = Utility.Configuration.Settings.DarkTheme ? "Close.dark.svg" : "Close.light.svg";
            Gtk.Image closeImg = new Gtk.Image(new Gdk.Pixbuf(null, $"ApsimNG.Resources.TreeViewImages.{imageName}", 12, 12));

            closeBtn.Image = closeImg;
            closeBtn.Relief = ReliefStyle.None;
            closeBtn.Clicked += OnCloseBtnClick;

            headerBox.PackStart(tabLabel, true, true, 0);
            headerBox.PackEnd(closeBtn, true, true, 0);

            // Wrap the whole thing inside an event box, so we can respond to a right-button or center-button click
            EventBox eventbox = new EventBox();
            eventbox.HasTooltip = text.Contains(Path.DirectorySeparatorChar.ToString());
            eventbox.TooltipText = text;
            eventbox.ButtonPressEvent += OnEventbox1ButtonPress;
            eventbox.Add(headerBox);
            Notebook notebook = onLeftTabControl ? notebook1 : notebook2;
            // Attach an icon to the context menu
            Widget iconLabel = LabelWithIcon(tabLabel.Text, null);
            notebook.CurrentPage = notebook.AppendPageMenu(control, eventbox, iconLabel);
            // For reasons that I do not understand at all, with Release builds we must delay calling ShowAll until
            // after the page has been added. This is not the case with Debug builds.
            eventbox.ShowAll();
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
        public void OnEventbox1ButtonPress(object o, ButtonPressEventArgs e)
        {
            try
            {
                if (e.Event.Button == 2) // Let a center-button click on a tab close that tab.
                {
                    CloseTabContaining(o);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Change the text of a tab.</summary>
        /// <param name="ownerView">An <see cref="ExplorerView" /> instance whose tab text should be changed.</param>
        /// <param name="newTabName">New text of the tab.</param>
        /// <param name="tooltip">Optional tooltip text on the tab to be shown on mouseover.</param>
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
                // Update the context menu label
                Widget label = LabelWithIcon(newTabName, null);
                notebook.SetMenuLabel(tab, label);
            }
        }

        /// <summary>
        /// Creates a widget that contains a label with an icon on its left.
        /// </summary>
        /// <param name="text">The label text</param>
        /// <param name="icon">Icon path/Stock name</param>
        /// <remarks>
        /// If 'icon' is not a valid path, it is treated like a stock name.
        /// Invalid stock names default to an invalid file icon.
        /// </remarks>
        public Widget LabelWithIcon(string text, string icon)
        {
            Gtk.Image image;
            if (String.IsNullOrEmpty(icon)) // If no icon name provided, try using the text. 
            {
                string nameForImage = "ApsimNG.Resources.TreeViewImages." + text + ".png";
                if (HasResource(nameForImage))
                    icon = nameForImage;
                else
                    icon = "ApsimNG.Resources.apsim logo32.png";
            }

            // Are we looking for a resource?
            if (HasResource(icon))
            {
                image = new Gtk.Image(new Gdk.Pixbuf(null, icon, 12, 12));
            }

            // Or maybe a file?
            else if (File.Exists(icon))
            {
                image = new Gtk.Image(new Gdk.Pixbuf(icon, 12, 12));
            }
            else // OK, let's try the stock icons
            {
                image = new Gtk.Image();
                image.SetFromIconName(icon, IconSize.Menu);
            }
            image.Visible = true;

            // Make a label
            Label label = new Label(text);
            label.Visible = true;

            // Attach the label and icon together
            HBox box = new HBox(false, 4);
            box.PackStart(image, false, true, 0);
            box.PackStart(label, false, true, 0);
            box.Visible = true;
            return box;
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
            notebook1.SwitchPage -= OnChangeTab;
            notebook2.SwitchPage -= OnChangeTab;
            stopButton.Clicked -= OnStopClicked;
            window1.DeleteEvent -= OnClosing;
            mainWidget.Dispose();

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
        /// <param name="notebook">The notebook widget to which the object belongs.</param>
        /// <param name="tabName">This will be set to the name of the tab, if found.</param>
        /// <returns>Page number of the tab, or -1 if not found</returns>
        /// <remarks>Why is notebook passed by reference? Need to check if this is necessary and remove if not.</remarks>
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
            try
            {
                CloseTabContaining(o);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
        /// Returns the number of pages in the notebook
        /// </summary>
        /// <param name="onLeft">If true, use the left notebook; if false, use the right</param>
        /// <returns></returns>
        public int PageCount(bool onLeft)
        {
            Notebook notebook = onLeft ? notebook1 : notebook2;
            return notebook.NPages;
        }
        /// <summary>
        /// Close a tab.
        /// </summary>
        /// <param name="index">Index of the tab to be removed.</param>
        /// <param name="onLeft">Remove from the left (true) tab control or the right (false) tab control.</param>
        public void RemoveTab(int index, bool onLeft)
        {
            Notebook notebook = onLeft ? notebook1 : notebook2;
            if (index >= notebook.NPages)
                throw new InvalidOperationException($"Cannot remove tab {index} from {(onLeft ? "left" : "right")} tab control: only {notebook.NPages} tabs are open");
            if (index == 0)
                throw new InvalidOperationException($"Cannot remove home tab");
            notebook.RemovePage(index);
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
                if (window1.Window != null)
                    return (window1.Window.State & Gdk.WindowState.Maximized) == Gdk.WindowState.Maximized;
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

        /// <summary>Position of split screen divider.</summary>
        /// <remarks>Not sure what units this uses...might be pixels.</remarks>
        public int SplitScreenPosition
        {
            get { return hpaned1.Position; }
            set { hpaned1.Position = value; }
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
                    if (hpaned1.Position == 0)
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
            MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, Gtk.MessageType.Question, ButtonsType.YesNo, message);
            md.Title = "Save changes";
            int result = md.Run();
            md.Dispose();
            switch ((ResponseType)result)
            {
                case ResponseType.Yes:
                    return QuestionResponseEnum.Yes;
                case ResponseType.No:
                    return QuestionResponseEnum.No;
                default:
                    return QuestionResponseEnum.Cancel;
            }
        }

        /// <summary>
        /// Clear the status panel.
        /// </summary>
        public void ClearStatusPanel()
        {
            Application.Invoke(delegate
            {
                numberOfButtons = 0;
                statusWindow.Buffer.Clear();
            });
        }

        /// <summary>Add a status message to the explorer window</summary>
        /// <param name="message">The message.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="overwrite">Should any previous messages be overwritten?</param>
        /// <param name="addSeparator">Add a separator beneath the message?</param>
        /// <param name="withButton">Add a 'more info' button?</param>
        /// <remarks>This is kind of a cludge. This method could probably be extracted to its own class.</remarks>
        public void ShowMessage(string message, MessageType errorLevel, bool overwrite = true, bool addSeparator = false, bool withButton = true)
        {
            Application.Invoke(delegate
            {
                statusWindow.Visible = message != null;
                if (overwrite || message == null)
                {
                    numberOfButtons = 0;
                    statusWindow.Buffer.Clear();
                }

                if (message != null)
                {
                    string tagName;
                    // Output the message
                    if (errorLevel == MessageType.Error)
                    {
                        tagName = "error";
                    }
                    else if (errorLevel == MessageType.Warning)
                    {
                        tagName = "warning";
                    }
                    else
                    {
                        tagName = "normal";
                    }
                    message = message.TrimEnd(Environment.NewLine.ToCharArray());
                    message += Environment.NewLine;
                    TextIter insertIter;
                    if (overwrite)
                        insertIter = statusWindow.Buffer.StartIter;
                    else
                        insertIter = statusWindow.Buffer.EndIter;

                    statusWindow.Buffer.InsertWithTagsByName(ref insertIter, message, tagName);
                    if (errorLevel == MessageType.Error && withButton)
                        AddButtonToStatusWindow("More Information", numberOfButtons++);
                    if (addSeparator)
                    {
                        insertIter = statusWindow.Buffer.EndIter;
                        statusWindow.Buffer.InsertWithTagsByName(ref insertIter, Environment.NewLine + "----------------------------------------------" + Environment.NewLine, tagName);
                    }
                }

                //this.toolTip1.SetToolTip(this.StatusWindow, message);
            });
        }

        /// <summary>
        /// Displays an error message with a 'more info' button.
        /// </summary>
        /// <param name="err">Error for which we want to display information.</param>
        public new void ShowError(Exception err)
        {
            OnError?.Invoke(this, new ErrorArgs { Error = err });
        }

        /// <summary>
        /// Sets the Gtk theme based on the user's previous choice.
        /// </summary>
        public void RefreshTheme()
        {

            // tbi

        }

        private void AddButtonToStatusWindow(string buttonName, int buttonID)
        {
            TextIter iter = statusWindow.Buffer.EndIter;
            TextChildAnchor anchor = statusWindow.Buffer.CreateChildAnchor(ref iter);
            EventBox box = new EventBox();
            ApsimNG.Classes.CustomButton moreInfo = new ApsimNG.Classes.CustomButton(buttonName, buttonID);
            moreInfo.Clicked += ShowDetailedErrorMessage;
            box.Add(moreInfo);
            statusWindow.AddChildAtAnchor(box, anchor);
            box.ShowAll();
            box.Realize();
            box.ShowAll();
            moreInfo.ParentWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
        }

        [GLib.ConnectBefore]
        private void ShowDetailedErrorMessage(object sender, EventArgs args)
        {
            try
            {
                ShowDetailedError?.Invoke(sender, args);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Shows the font selection dialog.
        /// </summary>
        public void ShowFontChooser()
        {
            string title = "Select a font";

            fontDialog = new FontChooserDialog(title, window1);


            // Center the dialog on the main window.
            fontDialog.TransientFor = MainWidget as Window;
            fontDialog.WindowPosition = WindowPosition.CenterOnParent;

            // Select the current font.
            if (Utility.Configuration.Settings.FontName != null)
                fontDialog.Font = Utility.Configuration.Settings.FontName.ToString();


            //fontDialog.FontActivated += OnChangeFont;
            fontDialog.Response += OnChangeFont;


            // Show the dialog.
            fontDialog.ShowAll();
        }

        /// <summary>
        /// Invoked when the user clicks OK or Apply in the font selection
        /// dialog. Changes the font on all widgets and saves the new font
        /// in the config file.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnChangeFont(object sender, ResponseArgs args)
        {
            try
            {

                string fontName = fontDialog.Font;

                Pango.FontDescription newFont = Pango.FontDescription.FromString(fontName);
                Utility.Configuration.Settings.FontName = newFont.ToString();
                Configuration.Settings.Save();
                ChangeFont(newFont);
                if (args.ResponseId != ResponseType.Apply)
                    fontDialog.Dispose();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Show a message next to the progress bar.
        /// </summary>
        /// <param name="message">Message to be displayed.</param>
        public void ShowProgressMessage(string message)
        {
            Application.Invoke(delegate
            {
                lblStatus.Visible = !string.IsNullOrEmpty(message);
                lblStatus.Text = message ?? "";
            });
        }

        /// <summary>
        /// Show progress bar with the specified percent.
        /// </summary>
        /// <param name="progress">Progress (0 - 1).</param>
        /// <param name="showStopButton">Should a stop button be shown?</param>
        public void ShowProgress(double progress, bool showStopButton = true)
        {
            // We need to use "Invoke" if the timer is running in a
            // different thread. That means we can use either
            // System.Timers.Timer or Windows.Forms.Timer in 
            // RunCommand.cs
            Application.Invoke(delegate
            {
                progressBar.Visible = true;
                progressBar.Fraction = progress;
                if (showStopButton)
                    stopButton.Visible = true;
            });
        }

        /// <summary>
        /// Hide the progress bar.
        /// </summary>
        public void HideProgressBar()
        {
            Application.Invoke(delegate
            {
                progressBar.Visible = false;
                stopButton.Visible = false;
                lblStatus.Hide();
            });
        }

        /// <summary>User is trying to close the application - allow that to happen?</summary>
        /// <param name="o">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnClosing(object o, DeleteEventArgs e)
        {
            try
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
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>User is trying to stop all currently executing simulations.</summary>
        /// <param name="o">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnStopClicked(object o, EventArgs e)
        {
            try
            {
                if (StopSimulation != null)
                {
                    EventArgs args = new EventArgs();
                    StopSimulation.Invoke(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Listens to an event of the divider position changing</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDividerNotified(object sender, GLib.NotifyArgs args)
        {
            if (DividerChanged != null)
                DividerChanged.Invoke(sender, new EventArgs());
        }

        /// <summary>
        /// Change Apsim's default font, and apply the new font to all existing
        /// widgets.
        /// </summary>
        /// <param name="font">The new default font.</param>
        private void ChangeFont(Pango.FontDescription font)
        {
            SetWidgetFont(mainWidget, font);

            //Rc.ParseString($"gtk-font-name = \"{font}\"");
        }

        /// <summary>
        /// Recursively applies a new FontDescription to all widgets
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="newFont"></param>
        private void SetWidgetFont(Widget widget, Pango.FontDescription newFont)
        {

            int sizePt = newFont.SizeIsAbsolute ? newFont.Size : Convert.ToInt32(newFont.Size / Pango.Scale.PangoScale);
            CssProvider provider = new CssProvider();
            StringBuilder css = new StringBuilder();
            css.AppendLine("* {");
            css.AppendLine($"font-family: {newFont.Family};");
            css.AppendLine($"font-size: {sizePt}pt;");
            css.AppendLine($"font-style: {newFont.Style};");
            css.AppendLine($"font-variant: {newFont.Variant};");
            css.AppendLine($"font-weight: {newFont.Weight};");
            css.AppendLine($"font-stretch: {newFont.Stretch};");
            css.Append("}");
            provider.LoadFromData(css.ToString());
            window1.StyleContext.AddProvider(provider, StyleProviderPriority.Application);

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

        /// <summary>Show a message in a dialog box</summary>
        /// <param name="message">The message.</param>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="msgType">Message type (info, warning, error, ...).</param>
        /// <param name="buttonType">Type of buttons to be shown in the dialog.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="masterWindow">The main window.</param>
        public int ShowMsgDialog(string message, string title, Gtk.MessageType msgType, Gtk.ButtonsType buttonType, Window masterWindow)
        {
            MessageDialog md = new Gtk.MessageDialog(masterWindow, Gtk.DialogFlags.Modal,
                msgType, buttonType, message);
            md.Title = title;
            md.WindowPosition = WindowPosition.Center;
            int result = md.Run();
            md.Dispose();
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

        /// <inheritdoc />
        public (int, bool) GetCurrentTab()
        {
            Notebook notebook = GetCurrentNotebook();
            if (notebook == null)
                return (-1, false);

            bool onLeft = notebook.Name == notebook1.Name;
            return (notebook.CurrentPage, onLeft);
        }

        private Notebook GetCurrentNotebook()
        {
            if (!notebook2.Visible)
                return notebook1;
            return hpaned1.FocusChild as Notebook;
        }
    }

}
