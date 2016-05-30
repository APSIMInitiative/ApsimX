// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using Glade;
    using Gtk;

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
    public class AxisView : ViewBase, IAxisView
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

        [Widget]
        private Table table1 = null;
        [Widget]
        private Entry entryMin = null;
        [Widget]
        private Entry entryMax = null;
        [Widget]
        private Entry entryInterval = null;
        [Widget]
        private Entry entryTitle = null;
        [Widget]
        private CheckButton checkbutton1 = null;

        /// <summary>
        /// Construtor
        /// </summary>
        public AxisView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.AxisView.glade", "table1");
            gxml.Autoconnect(this);
            _mainWidget = table1;
            entryTitle.Changed += TitleTextBox_TextChanged;
            entryMin.Changed += OnMinimumChanged;
            entryMax.Changed += OnMaximumChanged;
            entryInterval.Changed += OnIntervalChanged;
            checkbutton1.Toggled += OnCheckedChanged;
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get
            {
                return entryTitle.Text;
            }

            set
            {
                entryTitle.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is inverted.
        /// </summary>
        public bool Inverted
        {
            get
            {
                return checkbutton1.Active;
            }

            set
            {
                checkbutton1.Active = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum axis scale. double.Nan for auto scale
        /// </summary>
        public double Minimum
        { 
            get
            {
                if (String.IsNullOrEmpty(entryMin.Text))
                    return double.NaN;
                else
                    return Convert.ToDouble(entryMin.Text);
            }
            
            set
            {
                if (double.IsNaN(value))
                    entryMin.Text = "";
                else
                    entryMin.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the maximum axis scale. double.Nan for auto scale
        /// </summary>
        public double Maximum
        {
            get
            {
                if (String.IsNullOrEmpty(entryMax.Text))
                    return double.NaN;
                else
                    return Convert.ToDouble(entryMax.Text);
            }
            
            set
            {
                if (double.IsNaN(value))
                    entryMax.Text = "";
                else
                    entryMax.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the axis scale interval. double.Nan for auto scale
        /// </summary>
        public double Interval
        {
            get
            {
                if (String.IsNullOrEmpty(entryInterval.Text))
                    return double.NaN;
                else
                    return Convert.ToDouble(entryInterval.Text);
            }

            set
            {
                if (double.IsNaN(value))
                    entryInterval.Text = "";
                else
                    entryInterval.Text = value.ToString();
            }
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
