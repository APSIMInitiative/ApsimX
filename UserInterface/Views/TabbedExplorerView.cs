// -----------------------------------------------------------------------
// <copyright file="TabbedExplorerView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    public interface ITabbedExplorerView
    {
        /// <summary>Get the list and button view</summary>
        IListButtonView ListAndButtons { get; }

        /// <summary>Add a tab form to the tab control. Optionally select the tab if SelectTab is true.</summary>
        /// <param name="text">Text for tab.</param>
        /// <param name="image">Image for tab.</param>
        /// <param name="control">Control for tab.</param>
        /// <param name="onTabClosing">Event handler that will be called when the tab closes.</param>
        void AddTab(string text, Image image, UserControl control, EventHandler<TabEventArgs> onTabClosing);

        /// <summary>Ask user for a filename.</summary>
        /// <param name="initialDir">The initial directory to show the user.</param>
        /// <param name="fileSpec">The filespec to use to filter files.</param>
        string AskUserForFileName(string initialDir, string fileSpec);

        /// <summary>Show an error message to caller.</summary>
        void ShowError(string message);

        /// <summary>Set the wait cursor (or not)/</summary>
        /// <param name="wait">Shows wait cursor if true, normal cursor if false.</param>
        void SetWaitCursor(bool wait);

        /// <summary>Close the application.</summary>
        /// <param name="askToSave">If true, will ask user whether they want to save.</param>
        void Close(bool askToSave = true);
    }

    /// <summary>
    /// TabbedExplorerView maintains multiple explorer views in a tabbed interface. It also
    /// has a StartPageView that is shown to the use when they open a new tab.
    /// </summary>
    public partial class TabbedExplorerView : UserControl, ITabbedExplorerView
    {
        private static string indexTabText = "+";
        private List<EventHandler<TabEventArgs>> tabClosingEvents = new List<EventHandler<TabEventArgs>>();

        /// <summary>Get the list and button view</summary>
        public IListButtonView ListAndButtons { get { return listButtonView1; } }

        /// <summary>Constructor</summary>
        public TabbedExplorerView()
        {
            InitializeComponent();
        }

        /// <summary>Add a tab form to the tab control. Optionally select the tab if SelectTab is true.</summary>
        /// <param name="text">Text for tab.</param>
        /// <param name="image">Image for tab.</param>
        /// <param name="control">Control for tab.</param>
        /// <param name="onTabClosing">Event handler that will be called when the tab closes.</param>
        public void AddTab(string text, Image image, UserControl control, EventHandler<TabEventArgs> onTabClosing)
        {
            TabPage page = new TabPage(text);
            
            // Add the specified tab image to the image list if it doesn't already exist.
            if (image != null)
            {
                int imageIndex;
                for (imageIndex = 0; imageIndex < TabImageList.Images.Count; imageIndex++)
                    if (imageIndex.Equals(image))
                        break;
                if (imageIndex == TabImageList.Images.Count)
                    TabImageList.Images.Add(image);
                page.ImageIndex = imageIndex;
            }
            else
                page.ImageIndex = -1;

            page.Controls.Clear();
            page.Controls.Add(control);
            control.Dock = DockStyle.Fill;

            tabControl.TabPages.Add(page);
            tabClosingEvents.Add(onTabClosing);

            // On MONO OSX: The screen doesn't redraw properly when a tab is 'inserted'.
            tabControl.SelectedTab = page;
        }

        /// <summary>Set the wait cursor (or not)/</summary>
        /// <param name="wait">Shows wait cursor if true, normal cursor if false.</param>
        public void SetWaitCursor(bool wait)
        {
            if (wait == true)
                Cursor.Current = Cursors.WaitCursor;
            else
                Cursor.Current = Cursors.Default;
        }

        /// <summary>Close the application.</summary>
        /// <param name="askToSave">Flag to turn on the request to save</param>
        public void Close(bool askToSave = true)
        {
            Form mainForm = this.ParentForm;
            if (!askToSave)
                mainForm.DialogResult = DialogResult.Cancel;
            mainForm.Close();
        }

        /// <summary>Tab popup menu is about to open. Enable/disable the close menu item.</summary>
        private void OnTabControlMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                for (int i = 0; i < tabControl.TabCount; ++i)
                {
                    Rectangle r = tabControl.GetTabRect(i);
                    if (r.Contains(e.Location) /* && it is the header that was clicked*/)
                    {
                        tabControl.SelectedTab = tabControl.TabPages[i];
                        CloseTabMenuItem.Enabled = tabControl.SelectedTab.Text != indexTabText;
                        TabPopupMenu.Show(this, e.Location);

                    }
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                for (int i = 0; i < tabControl.TabCount; ++i)
                {
                    Rectangle r = tabControl.GetTabRect(i);
                    if (r.Contains(e.Location) /* && it is the header that was clicked*/)
                    {
                        tabControl.SelectedTab = tabControl.TabPages[i];
                        if (tabControl.SelectedTab.Text != indexTabText)
                           OnCloseTabClick(sender, e);
                    }
                }
            }
        }

        /// <summary>User is closing this tab.</summary>
        private void OnCloseTabClick(object sender, EventArgs e)
        {
            int tabIndex = tabControl.TabPages.IndexOf(tabControl.SelectedTab);
            tabClosingEvents[tabIndex - 1].Invoke(this, new TabEventArgs() { name = tabControl.SelectedTab.Text, index = tabControl.SelectedIndex });
            
            if (tabControl.SelectedTab.Text != indexTabText)
                tabControl.TabPages.Remove(tabControl.SelectedTab);
        }

        /// <summary>Ask user for a filename.</summary>
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

        /// <summary>Show an error message to caller.</summary>
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>An event argument structure with a string.</summary>
    public class TabEventArgs : EventArgs
    {
        public string name;
        public int index;
    }

}
