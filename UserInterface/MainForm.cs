using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using UserInterface.Commands;

namespace UserInterface
{
    public partial class MainForm : Form
    {
        private ApplicationCommands AllCommands;

        /// <summary>
        /// Construcotr
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Application.EnableVisualStyles();
            AllCommands = new ApplicationCommands(this);
        }

        /// <summary>
        /// Form has been loaded - setup form.
        /// </summary>
        private void OnLoad(object sender, EventArgs e)
        {
            // Remove the dummy page on the tab control.
            TabControl.TabPages.Clear();

            StartPagePlugin Plugin2 = new StartPagePlugin();
            Plugin2.Setup(AllCommands);
        }


        /// <summary>
        /// Set the text caption of the main window.
        /// </summary>
        public void SetCaption(string CaptionText)
        {
            Text = CaptionText;
        }

        /// <summary>
        /// Add a tab form to the tab control. Optionally select the tab if SelectTab is true.
        /// </summary>
        public void AddTab(string TabText, Image TabImage, UserControl Contents, bool SelectTab)
        {
            TabPage NewTabPage = new TabPage();
            TabControl.TabPages.Add(NewTabPage);
            SetupTab(NewTabPage, TabText, TabImage, Contents);
            if (SelectTab)
                TabControl.SelectedTab = NewTabPage;
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
        public string CurrentTabText
        {
            get
            {
                if (TabControl.TabPages.Count > 0)
                    return TabControl.SelectedTab.Text;
                else
                    return "";
            }
            set
            {
                if (TabControl.TabPages.Count > 0)
                    TabControl.SelectedTab.Text = value;
            }
        }

        private void SetCurrentTab(string TabText)
        {
            int i = TabControl.TabPages.IndexOfKey(TabText);
            if (i != -1)
                TabControl.TabPages[i].Select();
        }
    }
}
