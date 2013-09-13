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
        /// Construcotr
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Application.EnableVisualStyles();

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
    }
}
