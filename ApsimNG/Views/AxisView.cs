// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public class AxisView : ViewBase, IAxisView
    {
        /// <summary>
        /// The table
        /// </summary>
        private Table table1 = null;

        /// <summary>
        /// The minumum value
        /// </summary>
        private Entry entryMin = null;

        /// <summary>
        /// The maximum value
        /// </summary>
        private Entry entryMax = null;

        /// <summary>
        /// The interval
        /// </summary>
        private Entry entryInterval = null;

        /// <summary>
        /// The title
        /// </summary>
        private Entry entryTitle = null;

        /// <summary>
        /// Check button object
        /// </summary>
        private CheckButton checkbutton1 = null;
        
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="owner">The owning view</param>
        public AxisView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.AxisView.glade");
            table1 = (Table)builder.GetObject("table1");
            entryMin = (Entry)builder.GetObject("entryMin");
            entryMax = (Entry)builder.GetObject("entryMax");
            entryInterval = (Entry)builder.GetObject("entryInterval");
            entryTitle = (Entry)builder.GetObject("entryTitle");
            checkbutton1 = (CheckButton)builder.GetObject("checkbutton1");
            _mainWidget = table1;
            entryTitle.Changed += TitleTextBox_TextChanged;
            entryMin.Changed += OnMinimumChanged;
            entryMax.Changed += OnMaximumChanged;
            entryInterval.Changed += OnIntervalChanged;
            checkbutton1.Toggled += OnCheckedChanged;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

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
                if (string.IsNullOrEmpty(entryMin.Text))
                    return double.NaN;
                else
                    return Convert.ToDouble(
                                            entryMin.Text, 
                                            System.Globalization.CultureInfo.InvariantCulture);
            }
            
            set
            {
                if (double.IsNaN(value))
                    entryMin.Text = string.Empty;
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
                if (string.IsNullOrEmpty(entryMax.Text))
                    return double.NaN;
                else
                    return Convert.ToDouble(
                                            entryMax.Text, 
                                            System.Globalization.CultureInfo.InvariantCulture);
            }
            
            set
            {
                if (double.IsNaN(value))
                    entryMax.Text = string.Empty;
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
                if (string.IsNullOrEmpty(entryInterval.Text))
                    return double.NaN;
                else
                    return Convert.ToDouble(
                                            entryInterval.Text, 
                                            System.Globalization.CultureInfo.InvariantCulture);
            }

            set
            {
                if (double.IsNaN(value))
                    entryInterval.Text = string.Empty;
                else
                    entryInterval.Text = value.ToString();
            }
        }

        /// <summary>
        /// Destroying the main widget
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The event arguments</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            entryTitle.Changed -= TitleTextBox_TextChanged;
            entryMin.Changed -= OnMinimumChanged;
            entryMax.Changed -= OnMaximumChanged;
            entryInterval.Changed -= OnIntervalChanged;
            checkbutton1.Toggled -= OnCheckedChanged;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>
        /// Invoked when the user changes the title text box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void TitleTextBox_TextChanged(object sender, EventArgs e)
        {
            if (TitleChanged != null)
                TitleChanged.Invoke(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the inverted box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCheckedChanged(object sender, EventArgs e)
        {
            if (InvertedChanged != null)
                InvertedChanged(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the minimum box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnMinimumChanged(object sender, EventArgs e)
        {
            if (MinimumChanged != null)
                MinimumChanged(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the maximum box.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnMaximumChanged(object sender, EventArgs e)
        {
            if (MaximumChanged != null)
                MaximumChanged(this, e);
        }

        /// <summary>
        /// Invoked when the user changes the maximum box.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnIntervalChanged(object sender, EventArgs e)
        {
            if (IntervalChanged != null)
                IntervalChanged(this, e);
        }
    }
}
