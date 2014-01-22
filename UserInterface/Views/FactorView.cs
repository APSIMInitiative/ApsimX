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
    public interface IFactorView
    {
        Utility.IEditor Editor { get; }

    }


    public partial class FactorView : UserControl, IFactorView
    {
        public FactorView()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Provide access to the editor view.
        /// </summary>
        public Utility.IEditor Editor { get { return editor; } }


    }
}
