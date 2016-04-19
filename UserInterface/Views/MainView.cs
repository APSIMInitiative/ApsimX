// -----------------------------------------------------------------------
// <copyright file="TabbedExplorerView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    /// <summary>An enum type for the AskQuestion method.</summary>
    public enum QuestionResponseEnum { Yes, No, Cancel }

    public interface IMainView
    {
        /// <summary>Get the start page 1 view</summary>
        IListButtonView StartPage1 { get; }

        /// <summary>Get the start page 2 view</summary>
        IListButtonView StartPage2 { get; }

        /// <summary>Add a tab form to the tab control. Optionally select the tab if SelectTab is true.</summary>
        /// <param name="text">Text for tab.</param>
        /// <param name="image">Image for tab.</param>
        /// <param name="control">Control for tab.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        void AddTab(string text, Image image, UserControl control, bool onLeftTabControl);

        /// <summary>Change the text of a tab.</summary>
        /// <param name="currentTabName">Current tab text.</param>
        /// <param name="newTabName">New text of the tab.</param>
        void ChangeTabText(string currentTabName, string newTabName);

        /// <summary>Gets or set the main window position.</summary>
        Point WindowLocation { get; set; }

        /// <summary>Gets or set the main window size.</summary>
        Size WindowSize { get; set; }

        /// <summary>Gets or set the main window size.</summary>
        FormWindowState WindowMinimisedOrMaximised { get; set; }

        /// <summary>Gets or set the main window size.</summary>
        string WindowCaption { get; set; }

        /// <summary>Turn split window on/off</summary>
        bool SplitWindowOn { get; set; }

        /// <summary>Ask user for a filename to open.</summary>
        /// <param name="fileSpec">The file specification to use to filter the files.</param>
        /// <param name="initialDirectory">Optional Initial starting directory</param>
        string AskUserForOpenFileName(string fileSpec, string initialDirectory = "");

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <param name="fileSpec">The file specification to filter the files.</param>
        /// <param name="OldFilename">The current file name.</param>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        string AskUserForSaveFileName(string fileSpec, string OldFilename);

        /// <summary>Ask the user a question</summary>
        /// <param name="message">The message to show the user.</param>
        QuestionResponseEnum AskQuestion(string message);

        /// <summary>
        /// Add a status message. A message of null will clear the status message.
        /// </summary>
        /// <param name="Message"></param>
        void ShowMessage(string Message, Models.DataStore.ErrorLevel errorLevel);

        /// <summary>
        /// Show progress bar with the specified percent.
        /// </summary>
        /// <param name="percent"></param>
        void ShowProgress(int percent);

        /// <summary>Set the wait cursor (or not)/</summary>
        /// <param name="wait">Shows wait cursor if true, normal cursor if false.</param>
        void ShowWaitCursor(bool wait);

        /// <summary>Close the application.</summary>
        /// <param name="askToSave">If true, will ask user whether they want to save.</param>
        void Close(bool askToSave = true);

        /// <summary>Invoked when application tries to close</summary>
        event EventHandler<AllowCloseArgs> AllowClose;

        /// <summary>Invoked when a tab is closing.</summary>
        event EventHandler<TabEventArgs> TabClosing;
    }

    /// <summary>
    /// TabbedExplorerView maintains multiple explorer views in a tabbed interface. It also
    /// has a StartPageView that is shown to the use when they open a new tab.
    /// </summary>
    public partial class MainView : Form, IMainView
    {
        private static string indexTabText = "+";
        private List<EventHandler<TabEventArgs>> tabClosingEvents = new List<EventHandler<TabEventArgs>>();
        Point tabControlRightClickLocation;

        /// <summary>Get the list and button view</summary>
        public IListButtonView StartPage1 { get { return listButtonView1; } }

        /// <summary>Get the list and button view</summary>
        public IListButtonView StartPage2 { get { return listButtonView2; } }

        /// <summary>Invoked when application tries to close</summary>
        public event EventHandler<AllowCloseArgs> AllowClose;

        /// <summary>Invoked when a tab is closing.</summary>
        public event EventHandler<TabEventArgs> TabClosing;

        /// <summary>Constructor</summary>
        public MainView()
        {
            InitializeComponent();

            // Adjust font size for MONO.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT &&
                Environment.OSVersion.Platform != PlatformID.Win32Windows)
            {
                this.Font = new Font(this.Font.FontFamily, 10.2F);
            }

            StatusWindow.Visible = false;
        }

        /// <summary>Add a tab form to the tab control. Optionally select the tab if SelectTab is true.</summary>
        /// <param name="text">Text for tab.</param>
        /// <param name="image">Image for tab.</param>
        /// <param name="control">Control for tab.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        public void AddTab(string text, Image image, UserControl control, bool onLeftTabControl)
        {
            // Determine the tab image index.
            int imageIndex = 1;
            if (image != null)
            {
                for (imageIndex = 0; imageIndex < TabImageList.Images.Count; imageIndex++)
                    if (imageIndex.Equals(image))
                        break;
                if (imageIndex == TabImageList.Images.Count)
                    TabImageList.Images.Add(image);
            }

            // Add the tab page.
            TabControl tabControl;
            if (onLeftTabControl)
                tabControl = tabControl1;
            else
                tabControl = tabControl2;
            tabControl.TabPages.Add(text, text, imageIndex);

            // Insert the control on the page.
            TabPage page = tabControl.TabPages[tabControl.TabPages.Count - 1];
            page.Controls.Clear();
            page.Controls.Add(control);
            control.Dock = DockStyle.Fill;

            // On MONO OSX: The screen doesn't redraw properly when a tab is 'inserted'.
            tabControl.SelectedTab = page;
        }

        /// <summary>Change the text of a tab.</summary>
        /// <param name="currentTabName">Current tab text.</param>
        /// <param name="newTabName">New text of the tab.</param>
        public void ChangeTabText(string currentTabName, string newTabName)
        {
            TabPage page = null;
            int index = tabControl1.TabPages.IndexOfKey(currentTabName);
            if (index == -1)
            {
                index = tabControl2.TabPages.IndexOfKey(currentTabName);
                if (index == -1)
                    ShowMessage("Cannot find tab: " + currentTabName + ". Cannot rename tab", Models.DataStore.ErrorLevel.Error);
                else
                    page = tabControl2.TabPages[index];
            }
            else
                page = tabControl1.TabPages[index];
            
            if (page != null)
                page.Text = newTabName;
        }

        /// <summary>Set the wait cursor (or not)/</summary>
        /// <param name="wait">Shows wait cursor if true, normal cursor if false.</param>
        public void ShowWaitCursor(bool wait)
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

        /// <summary>Gets or set the main window position.</summary>
        public Point WindowLocation { get { if (WindowState == FormWindowState.Normal) return Location; else return RestoreBounds.Location; } set { Location = value; } }

        /// <summary>Gets or set the main window size.</summary>
        public Size WindowSize { get { if (WindowState == FormWindowState.Normal) return Size; else return RestoreBounds.Size; } set { Size = value; } }

        /// <summary>Gets or set the main window size.</summary>
        public FormWindowState WindowMinimisedOrMaximised { get { return WindowState; } set { WindowState = value; } }

        /// <summary>Gets or set the main window size.</summary>
        public string WindowCaption { get { return Text; } set { Text = value; } }

        /// <summary>Turn split window on/off</summary>
        public bool SplitWindowOn
        {
            get { return !splitContainer.Panel2Collapsed; }
            set { splitContainer.Panel2Collapsed = !value; }
        }

        /// <summary>Ask user for a filename to open.</summary>
        /// <param name="fileSpec">The file specification to use to filter the files.</param>
        /// <param name="initialDirectory">Optional Initial starting directory</param>
        public string AskUserForOpenFileName(string fileSpec, string initialDirectory = "")
        {
            string fileName = null;
            OpenFileDialog.Filter = fileSpec;
            if (initialDirectory.Length > 0)
                OpenFileDialog.InitialDirectory = initialDirectory;
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
        /// <param name="fileSpec">The file specification to filter the files.</param>
        /// <param name="OldFilename">The current file name.</param>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        public string AskUserForSaveFileName(string fileSpec, string OldFilename)
        {
            SaveFileDialog.FileName = Path.GetFileName(OldFilename);
            SaveFileDialog.InitialDirectory = Utility.Configuration.Settings.PreviousFolder;
            SaveFileDialog.Filter = fileSpec + "|" + fileSpec;

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

        /// <summary>Ask the user a question</summary>
        /// <param name="message">The message to show the user.</param>
        public QuestionResponseEnum AskQuestion(string message)
        {
            TabPage page = Parent as TabPage;
            DialogResult result = MessageBox.Show("Do you want to save changes for " + page.Text + " ?", "", MessageBoxButtons.YesNoCancel);
            switch (result)
            {
                case DialogResult.Cancel: return QuestionResponseEnum.Cancel;
                case DialogResult.Yes: return QuestionResponseEnum.Yes;
                case DialogResult.No: return QuestionResponseEnum.No;
            }

            return QuestionResponseEnum.Cancel;
        }



        /// <summary>Add a status message to the explorer window</summary>
        /// <param name="message">The message.</param>
        /// <param name="errorLevel">The error level.</param>
        public void ShowMessage(string message, Models.DataStore.ErrorLevel errorLevel)
        {
            MethodInvoker messageUpdate = delegate
            {
                StatusWindow.Visible = message != null;

                // Output the message
                if (errorLevel == Models.DataStore.ErrorLevel.Error)
                {
                    StatusWindow.ForeColor = Color.Red;
                }
                else if (errorLevel == Models.DataStore.ErrorLevel.Warning)
                {
                    StatusWindow.ForeColor = Color.Brown;
                }
                else
                {
                    StatusWindow.ForeColor = Color.Blue;
                }
                message = message.TrimEnd("\n".ToCharArray());
                message = message.Replace("\n", "\n                      ");
                message += "\n";
                StatusWindow.Text = message;
                this.toolTip1.SetToolTip(this.StatusWindow, message);
                progressBar.Visible = false;
                Application.DoEvents();
            };

            if (InvokeRequired)
                this.BeginInvoke(new Action(messageUpdate));
            else
                messageUpdate();
        }

        /// <summary>
        /// Show progress bar with the specified percent.
        /// </summary>
        /// <param name="percent"></param>
        public void ShowProgress(int percent)
        {
            // We need to use "Invoke" if the timer is running in a
            // different thread. That means we can use either
            // System.Timers.Timer or Windows.Forms.Timer in 
            // RunCommand.cs
            MethodInvoker progressBarUpdate = delegate
            {
                progressBar.Visible = true;
                progressBar.Value = percent;
            };

            if (InvokeRequired)
                this.BeginInvoke(new Action(progressBarUpdate));
            else
                progressBarUpdate();
        }

        /// <summary>User is trying to close the application - allow that to happen?</summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (AllowClose != null)
            {
                AllowCloseArgs args = new AllowCloseArgs();
                AllowClose.Invoke(this, args);
                e.Cancel = !args.AllowClose;
            }
                e.Cancel = false;
        }

        /// <summary>User has right clicked a tab control. Save the location for the popup menu opening event invokation.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnTabControlMouseDown(object sender, MouseEventArgs e)
        {
            tabControlRightClickLocation = e.Location;
        }

        /// <summary>A popup menu is opening. Do we let it open.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnPopupMenuOpening(object sender, CancelEventArgs e)
        {
            TabControl tabControl;
            if ((sender as Control).Name == "tabPopupMenu1")
                tabControl = tabControl1;
            else
                tabControl = tabControl2;

            e.Cancel = true;
            for (int i = 0; i < tabControl.TabCount; ++i)
            {
                Rectangle r = tabControl.GetTabRect(i);
                if (r.Contains(tabControlRightClickLocation) /* && it is the header that was clicked*/)
                {
                    e.Cancel = false;
                    CloseTabMenuItem.Enabled = tabControl.SelectedTab.Text != indexTabText;
                }
            }
            tabControlRightClickLocation = new Point(0, 0);
        }

        /// <summary>User is closing a tab.</summary>
        private void OnCloseTabClick1(object sender, EventArgs e)
        {
            TabEventArgs args = new TabEventArgs();
            args.LeftTabControl = true;

            if (TabClosing != null)
            {
                args.Name = tabControl1.SelectedTab.Text;
                args.Index = tabControl1.SelectedIndex;
                TabClosing.Invoke(this, args);
            }

            if (tabControl1.SelectedTab.Text != indexTabText)
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
        }

        /// <summary>User is closing a tab.</summary>
        private void OnCloseTabClick2(object sender, EventArgs e)
        {
            TabEventArgs args = new TabEventArgs();
            args.LeftTabControl = false;

            if (TabClosing != null)
            {
                args.Name = tabControl2.SelectedTab.Text;
                args.Index = tabControl2.SelectedIndex;
                TabClosing.Invoke(this, args);
            }

            if (tabControl2.SelectedTab.Text != indexTabText)
                tabControl2.TabPages.Remove(tabControl2.SelectedTab);
        }

    }

    /// <summary>An event argument structure with a string.</summary>
    public class TabEventArgs : EventArgs
    {
        public bool LeftTabControl;
        public string Name;
        public int Index;
    }

    /// <summary>An event argument structure with a field for allow to close.</summary>
    public class AllowCloseArgs : EventArgs
    {
        public bool AllowClose;
    }

}
