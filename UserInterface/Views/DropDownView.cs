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
    /// <summary>An interface for a drop down</summary>
    public interface IDropDownView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Get or sets the list of valid values.</summary>
        string[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        string SelectedValue { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }

        /// <summary>Gets or sets whether the control should be editable.</summary>
        bool IsEditable { get; set; }
    }

    /// <summary>A drop down view.</summary>
    public partial class DropDownView : UserControl, IDropDownView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Constructor</summary>
        public DropDownView()
        {
            InitializeComponent();
            comboBox1.Visible = true;
        }

        /// <summary>Get or sets the list of valid values.</summary>
        public string[] Values
        {
            get
            {
                List<string> items = new List<string>();
                foreach (string item in comboBox1.Items)
                    items.Add(item);
                return items.ToArray();
            }
            set
            {
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(value);
            }
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                if (comboBox1.SelectedItem == null)
                    return null;
                return comboBox1.SelectedItem.ToString();
            }
            set
            {
                if (comboBox1.Text != value)
                    comboBox1.Text = value;
            }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return comboBox1.Visible; }
            set { comboBox1.Visible = value; }
        }

        /// <summary>Gets or sets whether the control should be editable.</summary>
        public bool IsEditable
        {
            get
            {
                return comboBox1.DropDownStyle == ComboBoxStyle.DropDown; 
            }
            set
            {
                if (value)
                    comboBox1.DropDownStyle = ComboBoxStyle.DropDown;
                else
                    comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }

    }
}
