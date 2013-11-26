using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{
    public partial class HtmlView : UserControl
    {
        public HtmlView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the summary contents.
        /// </summary>
        public void SetSummary(string contents, bool html)
        {
            if (html)
            {
                TextBox.Visible = false;
                HtmlControl.DocumentText = contents;
            }
            else
            {
                HtmlControl.Visible = false;
                TextBox.Dock = DockStyle.Fill;
                TextBox.Visible = true;
                TextBox.Text = contents;
            }
        }
    }
}
