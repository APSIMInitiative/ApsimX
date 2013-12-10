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
        private Utility.Configuration Configuration;
        private TabbedExplorerPresenter Presenter1;
        private TabbedExplorerPresenter Presenter2;

        /// <summary>
        /// Construcotr
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Application.EnableVisualStyles();

            Configuration = new Utility.Configuration();
            
            Presenter1 = new TabbedExplorerPresenter();
            Presenter1.Attach(tabbedExplorerView1);

            Presenter2 = new TabbedExplorerPresenter();
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
            Configuration.Settings.MainFormLocation = Location;
            Configuration.Settings.MainFormSize = Size;
            Configuration.Settings.MainFormWindowState = WindowState;
            //store settings on closure
            Configuration.Save();

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        /// <summary>
        /// Read the previous main form sizing values from the configuration file.
        /// </summary>
        private void OnLoad(object sender, EventArgs e)
        {
            SuspendLayout();

            try
            {
                Location = Configuration.Settings.MainFormLocation;
                Size = Configuration.Settings.MainFormSize;
                WindowState = Configuration.Settings.MainFormWindowState;
            }
            catch (System.Exception)
            {
                WindowState = FormWindowState.Maximized;
            }

            ResumeLayout();
        }

        /// <summary>
        /// User wants to close the form.
        /// </summary>
        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !Presenter1.AllowClose() || !Presenter2.AllowClose();
        }

    }
     
}
