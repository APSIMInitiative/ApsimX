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
    /// <summary>An interface for a check box.</summary>
    public interface ICheckBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Gets or sets whether the checkbox is checked.</summary>
        bool IsChecked { get; set; }
    }


    /// <summary>A checkbox view.</summary>
    public partial class CheckBoxView : UserControl, ICheckBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Constructor</summary>
        public CheckBoxView()
        {
            InitializeComponent();
        }

        /// <summary>Gets or sets whether the checkbox is checked.</summary>
        public bool IsChecked
        {
            get
            {
                return checkBox1.Checked;
            }
            set
            {
                if (IsChecked != value)
                    checkBox1.Checked = value;
            }
        }

        /// <summary>
        /// The checked status has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }

        /// <summary>Text property. Needed from designer.</summary>
        public string TextOfLabel
        {
            get { return checkBox1.Text; }
            set { checkBox1.Text = value; }
        }
    }
}
