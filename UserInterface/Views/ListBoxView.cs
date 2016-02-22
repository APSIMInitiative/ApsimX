// -----------------------------------------------------------------------
// <copyright file="ListBoxView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    /// <summary>An interface for a list box</summary>
    public interface IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        event EventHandler DoubleClicked;

        /// <summary>Get or sets the list of valid values.</summary>
        string[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        string SelectedValue { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }
    }

    /// <summary>A list view.</summary>
    public partial class ListBoxView : UserControl, IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        public event EventHandler DoubleClicked;

        /// <summary>Constructor</summary>
        public ListBoxView()
        {
            InitializeComponent();
            listBox1.Visible = true;
        }

        /// <summary>Get or sets the list of valid values.</summary>
        public string[] Values
        {
            get
            {
                List<string> items = new List<string>();
                if (listBox1.Items != null)
                    foreach (string item in listBox1.Items)
                        items.Add(item);
                return items.ToArray();
            }
            set
            {
                listBox1.Items.Clear();
                listBox1.Items.AddRange(value);
            }
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                if (listBox1.SelectedItem == null)
                    return null;
                return listBox1.SelectedItem.ToString();
            }
            set
            {
                if (listBox1.Text != value)
                    listBox1.Text = value;
            }
        }

        /// <summary>Return true if dropdown is visible.</summary>
        public bool IsVisible
        {
            get { return listBox1.Visible; }
            set { listBox1.Visible = value; }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }

        /// <summary>User has double clicked the list box.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDoubleClick(object sender, EventArgs e)
        {
            if (DoubleClicked != null)
                DoubleClicked.Invoke(this, e);
        }
    }
}
