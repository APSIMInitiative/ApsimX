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


        public string HTML
        {
            get
            {
                return HtmlControl.DocumentText;
                }
            set
            {
                HtmlControl.DocumentText = value;
            }
        }
    }
}
