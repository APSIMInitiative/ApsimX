using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UserInterface.Commands;
using System.Reflection;
using System.IO;
using System.Xml;

namespace UserInterface
{
    public partial class StartPageView : UserControl
    {
        ApplicationCommands ApplicationCommands;

        /// <summary>
        /// Constructor
        /// </summary>
        public StartPageView(ApplicationCommands ApplicationCommands)
        {
            InitializeComponent();
            this.ApplicationCommands = ApplicationCommands;
        }


        /// <summary>
        /// User has clicked open. Open a .apsim file.
        /// </summary>
        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListView.SelectedItems[0].Text.Contains("standard toolbox"))
            {
                byte[] b = Properties.Resources.ResourceManager.GetObject("StandardToolbox") as byte[];
                StreamReader SR = new StreamReader(new MemoryStream(b));
                ApplicationCommands.OpenApsimXFromMemoryInTab("Standard toolbox", SR.ReadToEnd());
            }
            else if (ListView.SelectedItems[0].Text.Contains("graph toolbox"))
            {
                byte[] b = Properties.Resources.ResourceManager.GetObject("GraphToolbox") as byte[];
                StreamReader SR = new StreamReader(new MemoryStream(b));
                ApplicationCommands.OpenApsimXFromMemoryInTab("Graph toolbox", SR.ReadToEnd());
            }
            else if (ListView.SelectedItems[0].Text.Contains("management toolbox"))
            {
                byte[] b = Properties.Resources.ResourceManager.GetObject("ManagementToolbox") as byte[];
                StreamReader SR = new StreamReader(new MemoryStream(b));
                ApplicationCommands.OpenApsimXFromMemoryInTab("Management toolbox", SR.ReadToEnd());
            }
            else if (OpenFileDialog.ShowDialog() == DialogResult.OK)
                ApplicationCommands.OpenApsimXFileInTab(OpenFileDialog.FileName);
            
            ApplicationCommands.AddStartTab();
        }


    }
}
