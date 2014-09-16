// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IAxisView
    {
        /// <summary>
        /// Invoked when the user has changed the title.
        /// </summary>
        event EventHandler TitleChanged;

        /// <summary>
        /// Invoked when the user has changed the inverted field
        /// </summary>
        event EventHandler InvertedChanged;

        /// <summary>
        /// Invoked when the user has changed the minimum field
        /// </summary>
        event EventHandler MinimumChanged;

        /// <summary>
        /// Invoked when the user has changed the maximum field
        /// </summary>
        event EventHandler MaximumChanged;

        /// <summary>
        /// Invoked when the user has changed the interval field
        /// </summary>
        event EventHandler IntervalChanged;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is inverted.
        /// </summary>
        bool Inverted { get; set; }

        /// <summary>
        /// Gets or sets the minimum axis scale. double.Nan for auto scale
        /// </summary>
        double Minimum { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum axis scale. double.Nan for auto scale
        /// </summary>
        double Maximum { get; set; }

        /// <summary>
        /// Gets or sets the axis scale interval. double.Nan for auto scale
        /// </summary>
        double Interval { get; set; }
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class AxisView : UserControl, IAxisView
    {
        /// <summary>
        /// Invoked when the user has changed the title.
        /// </summary>
        public event EventHandler TitleChanged;

        /// <summary>
        /// Invoked when the user has changed the inverted field
        /// </summary>
        public event EventHandler InvertedChanged;

        /// <summary>
        /// Invoked when the user has changed the minimum field
        /// </summary>
        public event EventHandler MinimumChanged;

        /// <summary>
        /// Invoked when the user has changed the maximum field
        /// </summary>
        public event EventHandler MaximumChanged;

        /// <summary>
        /// Invoked when the user has changed the interval field
        /// </summary>
        public event EventHandler IntervalChanged;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get
            {
                return TitleTextBox.Text;
            }

            set
            {
                TitleTextBox.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is inverted.
        /// </summary>
        public bool Inverted
        {
            get
            {
                return InvertedCheckBox.Checked;
            }

            set
            {
                InvertedCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum axis scale. double.Nan for auto scale
        /// </summary>
        public double Minimum
        { 
            get
            {
                if (textBox1.Text == string.Empty)
                    return double.NaN;
                else
                    return Convert.ToDouble(textBox1.Text);
            }
            
            set
            {
                if (double.IsNaN(value))
                    textBox1.Text = "";
                else
                    textBox1.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the maximum axis scale. double.Nan for auto scale
        /// </summary>
        public double Maximum
        {
            get
            {
                if (textBox2.Text == string.Empty)
                    return double.NaN;
                else
                    return Convert.ToDouble(textBox2.Text);
            }
            
            set
            {
                if (double.IsNaN(value))
                    textBox2.Text = "";
                else
                    textBox2.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the axis scale interval. double.Nan for auto scale
        /// </summary>
        public double Interval
        {
            get
            {
                if (textBox3.Text == string.Empty)
                    return double.NaN;
                else
                    return Convert.ToDouble(textBox3.Text);
            }

            set
            {
                if (double.IsNaN(value))
                    textBox3.Text = "";
                else
                    textBox3.Text = value.ToString();
            }
        }

        /// <summary>
        /// Construtor
        /// </summary>
        public AxisView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the user changes the title text box.
        /// </summary>
        private void TitleTextBox_TextChanged(object sender, EventArgs e)
        {
            if (TitleChanged != null)
                TitleChanged.Invoke(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the inverted box.
        /// </summary>
        private void OnCheckedChanged(object sender, EventArgs e)
        {
            if (InvertedChanged != null)
                InvertedChanged(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the minimum box.
        /// </summary>
        private void OnMinimumChanged(object sender, EventArgs e)
        {
            if (MinimumChanged != null)
                MinimumChanged(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the maximum box.
        /// </summary>
        private void OnMaximumChanged(object sender, EventArgs e)
        {
            if (MaximumChanged != null)
                MaximumChanged(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the maximum box.
        /// </summary>
        private void OnIntervalChanged(object sender, EventArgs e)
        {
            if (IntervalChanged != null)
                IntervalChanged(this, e);
        }
    }
}
