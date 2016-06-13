using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using Glade;

/// <summary>
///  May still require:
///  Popup menu for tabs? Not really needed. Close button on tab is better.
///  Allowing selection with Enter key (is this needed?)
/// </summary>

namespace UserInterface.Views
{
    interface ITabbedExplorerView
    {
        event EventHandler<PopulateStartPageArgs> PopulateStartPage;

        event EventHandler MruFileClick;

        event EventHandler<TabClosingArgs> TabClosing;

        /// <summary>
        /// Add a tab form to the tab control. Optionally select the tab if SelectTab is true.
        /// </summary>
        void AddTab(string TabText, string ImageResource, Widget Contents, bool SelectTab);

        /// <summary>
        /// Ask user for a filename.
        /// </summary>
        string AskUserForFileName(string initialDir, string fileSpec);

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// Fill the recent files group with file names
        /// </summary>
        /// <param name="files"></param>
        void FillMruList(List<string> files);

        /// <summary>
        /// Return the selected filename from a double click on 
        /// a recent file item
        /// </summary>
        /// <returns></returns>
        string SelectedMruFileName();

        Int32 TabWidth { get; }

        /// <summary>
        /// Gets the current tab index.
        /// </summary>
        int CurrentTabIndex { get; }

        bool WaitCursor { get; set; }

        void Close(bool askToSave = true);
    }


    /// <summary>
    /// TabbedExplorerView maintains multiple explorer views in a tabbed interface. It also
    /// has a StartPageView that is shown to the use when they open a new tab.
    /// </summary>
    public class TabbedExplorerView : ViewBase, ITabbedExplorerView
    {

        public event EventHandler<PopulateStartPageArgs> PopulateStartPage;
        public event EventHandler MruFileClick;
        public event EventHandler<TabClosingArgs> TabClosing;

        private ListStore standardList = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(EventHandler));
        private ListStore recentFileList = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(EventHandler));
        [Widget]
        private Notebook notebook1 = null;
        [Widget]
        private IconView standardView = null;
        [Widget]
        private IconView recentFilesView = null;
        [Widget]
        private Label label4 = null;
        [Widget]
        private EventBox eventbox1 = null;
        [Widget]
        private VBox vbox1 = null;

        private const string indexTabText = "Home";

        public TabbedExplorerView(ViewBase owner = null) : base(owner)
        {
            _owner = owner;
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.TabbedExplorerView.glade", "notebook1");
            gxml.Autoconnect(this);
            _mainWidget = notebook1;

            label4.Text = indexTabText;
            notebook1.SetMenuLabel(vbox1, new Label(indexTabText));
            notebook1.ShowAll();

            standardView.Model = standardList;
            standardView.PixbufColumn = 0;
            standardView.TextColumn = 1;
            standardView.TooltipColumn = 2;
            Gtk.CellRendererText labelText = standardView.Cells[1] as Gtk.CellRendererText;
            if (labelText != null)
            {
                labelText.WrapMode = Pango.WrapMode.Word;
                labelText.SizePoints = 8;  // Works in GTKSharp 2, but not in 3
            }

            recentFilesView.Model = recentFileList;
            recentFilesView.PixbufColumn = 0;
            recentFilesView.TextColumn = 1;
            recentFilesView.TooltipColumn = 2;

            labelText = recentFilesView.Cells[1] as Gtk.CellRendererText;
            if (labelText != null)
            {
                labelText.WrapMode = Pango.WrapMode.Word;
                labelText.SizePoints = 8;  // Works in GTKSharp 2, but not in 3
            }
            eventbox1.ButtonPressEvent += on_eventbox1_button_press_event;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            eventbox1.ButtonPressEvent -= on_eventbox1_button_press_event;
        }

        /// <summary>
        /// Gets the current tab index.
        /// </summary>
        public int CurrentTabIndex
        {
            get
            {
                return notebook1.CurrentPage;
            }
        }

        /// <summary>
        /// Close the application.
        /// </summary>
        /// <param name="askToSave">Flag to turn on the request to save</param>
        public void Close(bool askToSave = true)
        {
            if (!askToSave)
            {
                if (Owner is UserInterface.MainForm)
                    (Owner as UserInterface.MainForm).queryClose = false;
            }
            mainWindow.Destroy();
        }

        /// <summary>
        /// View has loaded
        /// </summary>
        public void OnRealize(object o, EventArgs e)
        {
            if (PopulateStartPage != null)
                PopulateStartPageList();
        }

        /// <summary>
        /// Populate the start page.
        /// </summary>
        private void PopulateStartPageList()
        {

            standardList.Clear();
            PopulateStartPageArgs Args = new PopulateStartPageArgs();
            PopulateStartPage(this, Args);
            foreach (PopulateStartPageArgs.Description Description in Args.Descriptions)
            {
                Gdk.Pixbuf itemPixbuf = new Gdk.Pixbuf(null, Description.ResourceNameForImage, 48, 48);
                standardList.AppendValues(itemPixbuf, Description.Name, "Double click to open", Description.OnClick);
            }
        }

        /// <summary>
        /// File the most recently used files list
        /// </summary>
        /// <param name="files"></param>
        public void FillMruList(List<string> files)
        {
            recentFileList.Clear();
            Gdk.Pixbuf logoPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.apsim logo32.png", 48, 48);
            foreach (string xfile in files)
            {
                string name = Path.GetFileNameWithoutExtension(xfile);
                recentFileList.AppendValues(logoPixbuf, name, xfile, MruFileClick);
            }
        }

        public void on_eventbox1_button_press_event(object o, ButtonPressEventArgs e)
        {
            if (e.Event.Button == 2)
            {
                int tabPage = GetTabOfWidget(o);
                notebook1.CurrentPage = tabPage;
                if (tabPage > 0)
                {
                    if (TabClosing != null)
                    {
                        TabClosingArgs args = new TabClosingArgs();
                        args.tabIndex = tabPage;
                        TabClosing.Invoke(this, args);
                    }
                    notebook1.RemovePage(tabPage);
                }
            }
        }

        /// <summary>
        /// Helper function for dealing with clicks on the tab labels, or whatever
        /// widgets the tab label might control. Tests to see which tab the 
        /// indicated objects is on. This lets us identify the tabs associated
        /// with click events, for example.
        /// </summary>
        /// <param name="o">The widget that we are seaching for</param>
        /// <returns>Page number of the tab, or -1 if not found</returns>
        private int GetTabOfWidget(object o) // Is there a better way?
        {
            Widget widg = o as Widget;
            if (widg == null)
                return -1;
            for (int i = 0; i < notebook1.NPages; i++)
            {
                Widget testParent = notebook1.GetTabLabel(notebook1.GetNthPage(i));
                if (testParent == widg || widg.IsAncestor(testParent))
                    return i;
            }
            return -1;
        }

        public void OnCloseBtnClick(object o, EventArgs e)
        {
            int tabPage = GetTabOfWidget(o);
            if (tabPage > 0)
            {
                if (TabClosing != null)
                {
                    TabClosingArgs args = new TabClosingArgs();
                    args.tabIndex = tabPage;
                    TabClosing.Invoke(this, args);
                }
                notebook1.RemovePage(tabPage);
            }
        }

        public void OnStandardIconActivated(object o, ItemActivatedArgs e)
        {
            TreeIter iter;
            if (standardList.GetIter(out iter, e.Path))
            {
                EventHandler OnClick = (EventHandler)standardList.GetValue(iter, 3);
                if (OnClick != null)
                    OnClick(this, e);
            }
        }

        public void OnRecentFileIconActivated(object o, ItemActivatedArgs e)
        {
            TreeIter iter;
            if (recentFileList.GetIter(out iter, e.Path))
            {
                EventHandler OnClick = (EventHandler)recentFileList.GetValue(iter, 3);
                if (OnClick != null)
                    OnClick(this, e);
            }
        }

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowError(string message)
        {
            MessageDialog md = new MessageDialog(notebook1.Toplevel as Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, message);
            md.Run();
            md.Destroy();
        }

        public Int32 TabWidth
        {
            get
            {
                return notebook1.Allocation.Width;
            }
        }

        public string SelectedMruFileName()
        {
            string result = "";
            TreePath[] selected = recentFilesView.SelectedItems;
            if (selected.Length > 0)
            {
                TreeIter iter;
                if (recentFileList.GetIter(out iter, selected[0]))
                {
                    result = (string)recentFileList.GetValue(iter, 2);
                }
            }
            return result;
        }


        /// <summary>
        /// Add a tab form to the tab control. Optionally select the tab if SelectTab is true.
        /// </summary>
        public void AddTab(string TabText, string ImageResource, Widget Contents, bool SelectTab)
        {
            Label tabLabel = new Label();
            // If the tab text passed in is a filename then only show the filename (no path)
            // on the tab. The ToolTipText will still have the full path and name.
            if (TabText.Contains(Path.DirectorySeparatorChar.ToString()))
                tabLabel.Text = Path.GetFileNameWithoutExtension(TabText);
            else
                tabLabel.Text = TabText;
            HBox headerBox = new HBox();
            Button closeBtn = new Button();
            Image closeImg = new Image(new Gdk.Pixbuf(null, "ApsimNG.Resources.Close.png", 12, 12));

            closeBtn.Image = closeImg;
            closeBtn.Relief = ReliefStyle.None;
            closeBtn.Clicked += OnCloseBtnClick;

            headerBox.PackStart(tabLabel);
            headerBox.PackEnd(closeBtn);
            headerBox.ButtonPressEvent += on_eventbox1_button_press_event;
            //headerBox.ShowAll();

            // Wrap the whole thing inside an event box, so we can respond to a right-button click
            EventBox eventbox = new EventBox();
            eventbox.ButtonPressEvent += on_eventbox1_button_press_event;
            eventbox.Add(headerBox);
            eventbox.ShowAll();
            notebook1.CurrentPage = notebook1.AppendPageMenu(Contents, eventbox, new Label(tabLabel.Text));
        }

        public void SetTabLabelText(Widget tab, string newText)
        {
            // The top level of the "label" is an EventBox
            EventBox ebox = (EventBox)notebook1.GetTabLabel(tab);
            // The EventBox holds an HBox
            HBox hbox = (HBox)ebox.Child;
            // And the HBox has the actual label as its first child
            Label tabLabel = (Label)hbox.Children[0];
            tabLabel.Text = newText;
        }

        /// <summary>
        /// Ask user for a filename.
        /// </summary>
        public string AskUserForFileName(string initialDir, string fileSpec)
        {
            string fileName = null;

            FileChooserDialog fileChooser = new FileChooserDialog("Choose a file to open", null, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            if (!String.IsNullOrEmpty(fileSpec))
            {
                string[] specParts = fileSpec.Split(new Char[] { '|' });
                for (int i = 0; i < specParts.Length; i += 2)
                {
                    FileFilter fileFilter = new FileFilter();
                    fileFilter.Name = specParts[i];
                    fileFilter.AddPattern(specParts[i + 1]);
                    fileChooser.AddFilter(fileFilter);
                }
            }

            FileFilter allFilter = new FileFilter();
            allFilter.AddPattern("*");
            allFilter.Name = "All files";
            fileChooser.AddFilter(allFilter);

            if (initialDir.Length > 0)
                fileChooser.SetCurrentFolder(initialDir);
            else
                fileChooser.SetCurrentFolder(Utility.Configuration.Settings.PreviousFolder);
            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                fileName = fileChooser.Filename;
                string dir = Path.GetDirectoryName(fileName);
                if (!dir.Contains(@"ApsimX\Examples"))
                    Utility.Configuration.Settings.PreviousFolder = dir;
            }
            fileChooser.Destroy();
            return fileName;
        }

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        public string AskUserForSaveFileName(string OldFilename)
        {
            string result = null;
            FileChooserDialog fileChooser = new FileChooserDialog("Choose a file name for saving", null, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
            fileChooser.CurrentName = Path.GetFileName(OldFilename);
            fileChooser.SetCurrentFolder(Utility.Configuration.Settings.PreviousFolder);

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                string dir = Path.GetDirectoryName(fileChooser.Filename);
                if (!dir.Contains(@"ApsimX\Examples"))
                    Utility.Configuration.Settings.PreviousFolder = dir;
                result = fileChooser.Filename;
            }
            fileChooser.Destroy();
            return result;
        }
    }

    public class TabClosingArgs : EventArgs
    {
        public int tabIndex;
    }

    public class PopulateStartPageArgs : EventArgs
    {
        public struct Description
        {
            public string Name;
            public string ResourceNameForImage;
            public EventHandler OnClick;
        }

        public List<Description> Descriptions = new List<Description>();
    }

}