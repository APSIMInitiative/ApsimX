// -----------------------------------------------------------------------
// <copyright file="ColourDropDownView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>An interface for a drop down</summary>
    public interface IColourDropDownView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Get or sets the list of valid values. Can be Color or string objects.</summary>
        object[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        object SelectedValue { get; set; }
    }

    /// <summary>A colour drop down capable of showing colours and/or strings.</summary>
    public partial class ColourDropDownView : UserControl, IColourDropDownView
    {
        /// <summary>Constructor</summary>
        public ColourDropDownView()
        {
            InitializeComponent();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Get or sets the list of valid values. Can be Color or string objects.</summary>
        public object[] Values
        {
            get
            {
                object[] items = new object[comboBox1.Items.Count];
                comboBox1.Items.CopyTo(items, 0);
                return items;
            }
            set
            {
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(value);
            }
        }

        /// <summary>Gets or sets the selected value. Can be colour or string.</summary>
        public object SelectedValue
        {
            get
            {
                return comboBox1.SelectedItem;
            }
            set
            {
                comboBox1.SelectedItem = value;
            }
        }

        /// <summary>
        /// Handles the DrawItem combo box event to display colours.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDrawColourCombo(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = e.Bounds;
            if (e.Index >= 0)
            {
                object item = ((ComboBox)sender).Items[e.Index];
                if (item is Color)
                {
                    Color c = (Color) item;
                    Brush b = new SolidBrush(c);
                    g.FillRectangle(b, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    g.DrawString(item.ToString(), comboBox1.Font, Brushes.Black, rect.X, rect.Top);
                }
            }
        }

        /// <summary>User has changed the selected colour.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, EventArgs e)
        {
            if (Changed != null)
                Changed.Invoke(this, e);
        }
    }
}
