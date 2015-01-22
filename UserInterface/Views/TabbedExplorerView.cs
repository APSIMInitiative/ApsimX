using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using UserInterface.Commands;
using System.Reflection;

namespace UserInterface.Views
{
    interface ITabbedExplorerView
    {
        event EventHandler<PopulateStartPageArgs> PopulateStartPage;

        event EventHandler MruFileClick;
       
        event EventHandler TabClosing;

        /// <summary>
        /// Add a tab form to the tab control. Optionally select the tab if SelectTab is true.
        /// </summary>
        void AddTab(string TabText, Image TabImage, UserControl Contents, bool SelectTab);

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
    }


    /// <summary>
    /// TabbedExplorerView maintains multiple explorer views in a tabbed interface. It also
    /// has a StartPageView that is shown to the use when they open a new tab.
    /// </summary>
    public partial class TabbedExplorerView : UserControl, ITabbedExplorerView
    {
        private ListViewGroup recentFilesGroup;

        public event EventHandler<PopulateStartPageArgs> PopulateStartPage;
        public event EventHandler MruFileClick;
        public event EventHandler TabClosing;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public TabbedExplorerView()
        {
            InitializeComponent();
            
            recentFilesGroup = new ListViewGroup("Recent files", HorizontalAlignment.Left);
        }

        private const string indexTabText = "+";

        /// <summary>
        /// Gets the current tab index.
        /// </summary>
        public int CurrentTabIndex
        {
            get
            {
                return TabControl.SelectedIndex;
            }
        }

        /// <summary>
        /// View has loaded
        /// </summary>
        private void OnLoad(object sender, EventArgs e)
        {
            if (PopulateStartPage != null)
                PopulateStartPageList();
        }

        /// <summary>
        /// Populate the start page.
        /// </summary>
        private void PopulateStartPageList()
        {
            listViewMain.Clear();
            listViewMain.Groups.Insert(0, new ListViewGroup("Standard", HorizontalAlignment.Left));
            PopulateStartPageArgs Args = new PopulateStartPageArgs();
            listViewMain.Items.Clear();
            PopulateStartPage(this, Args);
            foreach (PopulateStartPageArgs.Description Description in Args.Descriptions)
            {
                ListViewItem Item = new ListViewItem();
                Item.Text = Description.Name;
                Item.Tag = Description.OnClick;
                Item.ToolTipText = "Double click to open";

                // Load image
                int ImageIndex = ListViewImages.Images.IndexOfKey(Description.ResourceNameForImage);
                if (ImageIndex == -1)
                {
                    Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(Description.ResourceNameForImage);
                    Bitmap Icon = new Bitmap(s); // Properties.Resources.ResourceManager.GetObject(Description.ResourceNameForImage) as Bitmap;
                    if (Icon != null)
                    {
                        ListViewImages.Images.Add(Description.ResourceNameForImage, Icon);
                        ImageIndex = ListViewImages.Images.Count - 1;
                    }
                }
                Item.ImageIndex = ImageIndex;
                listViewMain.Items.Add(Item);
                Item.Group = listViewMain.Groups[0];
            }
         
        }

        /// <summary>
        /// File the most recently used files group
        /// </summary>
        /// <param name="files"></param>
        public void FillMruList(List<string> files)
        {
            // cleanup the list so it can be reshown in the correct order
            foreach (ListViewItem item in listViewMain.Items)
            {
                item.Selected = false;
                if (item.Group == recentFilesGroup)
                {
                    
                    item.Remove();
                }
            }

            if (!listViewMain.Groups.Contains(recentFilesGroup))
                listViewMain.Groups.Add(recentFilesGroup);
            // now add each item from the list of files
            foreach (string xfile in files)
            {
                ListViewItem Item = new ListViewItem();
                Item.Text = Path.GetFileNameWithoutExtension(xfile);
                Item.Tag = MruFileClick;
                Item.ToolTipText = xfile;

                // Load image
                int ImageIndex = ListViewImages.Images.IndexOfKey("apsim_logo32");
                if (ImageIndex == -1)
                {
                    Bitmap Icon = Properties.Resources.ResourceManager.GetObject("apsim_logo32") as Bitmap;
                    if (Icon != null)
                    {
                        ListViewImages.Images.Add("apsim_logo32", Icon);
                        ImageIndex = ListViewImages.Images.Count - 1;
                    }
                }
                Item.ImageIndex = ImageIndex;
                listViewMain.Items.Add(Item);
                Item.Group = recentFilesGroup;
            }
        }
        
        /// <summary>
        /// User has double clicked. Open. Open a .apsim file.
        /// </summary>
        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            EventHandler OnClick = listViewMain.SelectedItems[0].Tag as EventHandler;
            if (OnClick != null)
                OnClick(this, e);
        }
        private void ListView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ListView_DoubleClick(sender, e);
        }
        /// <summary>
        /// Tab popup menu is about to open. Enable/disable the close menu item.
        /// </summary>
        private void OnTabControlMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                for (int i = 0; i < TabControl.TabCount; ++i)
                {
                    Rectangle r = TabControl.GetTabRect(i);
                    if (r.Contains(e.Location) /* && it is the header that was clicked*/)
                    {
                        TabControl.SelectedTab = TabControl.TabPages[i];
                        CloseTabMenuItem.Enabled = TabControl.SelectedTab.Text != indexTabText;
                        TabPopupMenu.Show(this, e.Location);

                    }
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                for (int i = 0; i < TabControl.TabCount; ++i)
                {
                    Rectangle r = TabControl.GetTabRect(i);
                    if (r.Contains(e.Location) /* && it is the header that was clicked*/)
                    {
                        TabControl.SelectedTab = TabControl.TabPages[i];
                        if (TabControl.SelectedTab.Text != indexTabText)
                           OnCloseTabClick(sender, e);
                    }
                }
            }
        }

        /// <summary>
        /// User is closing this tab.
        /// </summary>
        private void OnCloseTabClick(object sender, EventArgs e)
        {
            if (TabClosing != null)
                TabClosing.Invoke(this, e);
            
            if (TabControl.SelectedTab.Text != indexTabText)
                TabControl.TabPages.Remove(TabControl.SelectedTab);
        }

        /// <summary>
        /// Add a tab form to the tab control. Optionally select the tab if SelectTab is true.
        /// </summary>
        public void AddTab(string TabText, Image TabImage, UserControl Contents, bool SelectTab)
        {
            listViewMain.Clear();
            //foreach (ListViewItem item in listViewMain.Items)
            //    item.Selected = false;

            TabPage OriginalTab = TabControl.SelectedTab;
            TabPage NewTabPage = new TabPage();
            int i = TabControl.TabPages.Count - 1;
            TabControl.TabPages.Add(NewTabPage);
            SetupTab(NewTabPage, TabText, TabImage, Contents);

            // On MONO OSX: The screen doesn't redraw properly when a tab is 'inserted'.
            TabControl.SelectedTab = NewTabPage;
            //NewTabPage.Invalidate();
        }

        /// <summary>
        /// Replace the current focused tab with the specified text, image and contents.
        /// </summary>
        public void ReplaceCurrentTab(string TabText, Image TabImage, UserControl Contents)
        {
            TabPage TabPage = TabControl.SelectedTab;
            SetupTab(TabPage, TabText, TabImage, Contents);
        }

        /// <summary>
        /// Remove a tab from the tab control.
        /// </summary>
        public void RemoveTab(string TabText)
        {
            TabControl.TabPages.RemoveByKey(TabText);
        }

        /// <summary>
        /// Setup a tab according to the specified parameters.
        /// </summary>
        private void SetupTab(TabPage TabPage, string TabText, Image TabImage, UserControl Contents)
        {
            // If the tab text passed in is a filename then only show the filename (no path)
            // on the tab. The ToolTipText will still have the f ull path and name.
            if (TabText.Contains(Path.DirectorySeparatorChar.ToString()))
                TabPage.Text = Path.GetFileNameWithoutExtension(TabText);
            else
                TabPage.Text = TabText;
            TabPage.Name = TabText;
            TabPage.ToolTipText = TabText;

            // Add the specified tab image to the image list if it doesn't already exist.
            if (TabImage != null)
            {
                int TabIndex;
                for (TabIndex = 0; TabIndex < TabImageList.Images.Count; TabIndex++)
                    if (TabIndex.Equals(TabImage))
                        break;
                if (TabIndex == TabImageList.Images.Count)
                    TabImageList.Images.Add(TabImage);
                TabPage.ImageIndex = TabIndex;
            }
            else
                TabPage.ImageIndex = -1;

            // Add the TabForm passed in to the new tab page.
            TabPage.Controls.Clear();
            TabPage.Controls.Add(Contents);
            Contents.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Ask user for a filename.
        /// </summary>
        public string AskUserForFileName(string initialDir, string fileSpec)
        {
            string fileName = null;
            OpenFileDialog.Filter = fileSpec;
            if (initialDir.Length > 0)
                OpenFileDialog.InitialDirectory = initialDir;
            else
                OpenFileDialog.InitialDirectory = Utility.Configuration.Settings.PreviousFolder;

            if (OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = OpenFileDialog.FileName;
                string dir = Path.GetDirectoryName(fileName);
                if (!dir.Contains(@"ApsimX\Examples"))
                    Utility.Configuration.Settings.PreviousFolder = dir;
            }

            return fileName;
        }

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        public string AskUserForSaveFileName(string OldFilename)
        {
            SaveFileDialog.FileName = Path.GetFileName(OldFilename);
            SaveFileDialog.InitialDirectory = Utility.Configuration.Settings.PreviousFolder;

            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string dir = Path.GetDirectoryName(SaveFileDialog.FileName);
                if (!dir.Contains(@"ApsimX\Examples"))
                    Utility.Configuration.Settings.PreviousFolder = dir;
                return SaveFileDialog.FileName;
            }
            else
                return null;
        }

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public Int32 TabWidth 
        {
            get { return TabControl.Width; }
            
        }

        /// <summary>
        /// Get the filename from a recent file item. 
        /// </summary>
        /// <returns>The full path name for the file</returns>
        public string SelectedMruFileName()
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                if (item.Group == recentFilesGroup)
                {
                    return item.ToolTipText;    // full path for the file
                }
            }

            return "";  //invalid item
        }

        /// <summary>
        /// When changing back to the main tab reload the mru list into the listview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Text == indexTabText)
                PopulateStartPageList();
        }
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
