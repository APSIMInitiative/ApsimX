using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using UserInterface.Commands;
using UserInterface.Presenters;

namespace UserInterface
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Public access to the settings
        /// </summary>
  		public Utility.SessionSettings APSIMSession;    
        private Utility.ConfigManager SessionConfiguration;
        /// <summary>
        /// Construcotr
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Application.EnableVisualStyles();

            SessionConfiguration = new Utility.ConfigManager();
            APSIMSession = SessionConfiguration.Session;
            
            TabbedExplorerPresenter Presenter1 = new TabbedExplorerPresenter();
            Presenter1.Attach(tabbedExplorerView1);

            TabbedExplorerPresenter Presenter2 = new TabbedExplorerPresenter();
            Presenter2.Attach(tabbedExplorerView2);

            SplitContainer.Panel2Collapsed = true;
        }

        public void ToggleSecondExplorerViewVisible()
        {
            SplitContainer.Panel2Collapsed = !SplitContainer.Panel2Collapsed;
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (APSIMSession != null)
            {
                APSIMSession.mainformwidth = this.Width;
                APSIMSession.mainformtop = Top;
                APSIMSession.mainformleft = Left;
                APSIMSession.mainformheight = this.Height;
                if (!(this.WindowState == (FormWindowState)1))
                {
                    APSIMSession.windowstate = (int)this.WindowState;
                }
                //store settings on closure
                SessionConfiguration.StoreConfig();
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Read the previous main form sizing values from the configuration file.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            SuspendLayout();
            if (APSIMSession != null)
            {
                try
                {
                    WindowState = (FormWindowState)Convert.ToInt32(APSIMSession.windowstate);
                    Top = APSIMSession.mainformtop;
                    Left = APSIMSession.mainformleft;
                    this.Width = APSIMSession.mainformwidth;
                    this.Height = APSIMSession.mainformheight;
                }
                catch (System.Exception)
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            ResumeLayout();
        }
    }
     
}
