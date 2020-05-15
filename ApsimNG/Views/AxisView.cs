namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;
    using OxyPlot.Axes;

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
        /// Crosses at checkbox.
        /// </summary>
        private CheckButton checkbutton2 = null;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="owner">The owning view</param>
        public AxisView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.AxisView.glade");
            table1 = (Table)builder.GetObject("table1");
            entryMin = (Entry)builder.GetObject("entryMin");
            entryMax = (Entry)builder.GetObject("entryMax");
            entryInterval = (Entry)builder.GetObject("entryInterval");
            entryTitle = (Entry)builder.GetObject("entryTitle");
            checkbutton1 = (CheckButton)builder.GetObject("checkbutton1");
            checkbutton2 = (CheckButton)builder.GetObject("checkbutton2");
            mainWidget = table1;
            entryTitle.Changed += TitleTextBox_TextChanged;
            entryMin.Changed += OnMinimumChanged;
            entryMax.Changed += OnMaximumChanged;
            entryInterval.Changed += OnIntervalChanged;
            checkbutton1.Toggled += OnCheckedChanged;
            checkbutton2.Toggled += OnCrossesAtZeroChanged;
            mainWidget.Destroyed += _mainWidget_Destroyed;
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
        /// Invoked when the user has changed the crosses at zero field
        /// </summary>
        public event EventHandler CrossesAtZeroChanged;
       
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
        /// Gets or sets a value indicating whether the axis crosses the other axis at zero.
        /// </summary>
        public bool CrossesAtZero
        {
            get
            {
                return checkbutton2.Active;
            }

            set
            {
                checkbutton2.Active = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is a date time axis.
        /// This is not editable by the user.
        /// </summary>
        public bool IsDateAxis { get; set; }

        /// <summary>
        /// Gets or sets the minimum axis scale. double.Nan for auto scale
        /// </summary>
        public double Minimum
        { 
            get
            {
                DateTime date;
                if (string.IsNullOrEmpty(entryMin.Text))
                    return double.NaN;
                else if (IsDateAxis && DateTime.TryParse(entryMin.Text, out date))
                    return DateTimeAxis.ToDouble(date);
                else
                    return Convert.ToDouble(
                                            entryMin.Text,
                                            System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets or sets the maximum axis scale. double.Nan for auto scale
        /// </summary>
        public double Maximum
        {
            get
            {
                DateTime date;
                if (string.IsNullOrEmpty(entryMax.Text))
                    return double.NaN;
                else if (IsDateAxis && DateTime.TryParse(entryMax.Text, out date))
                    return DateTimeAxis.ToDouble(date);
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
                DateTimeIntervalType intervalType;
                if (string.IsNullOrEmpty(entryInterval.Text))
                    return double.NaN;
                else if (Enum.TryParse(entryInterval.Text, out intervalType))
                    return (double)intervalType;
                else
                    return Convert.ToDouble(
                                            entryInterval.Text,
                                            System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Sets the text in the minimum textbox.
        /// </summary>
        /// <param name="value">Value to display.</param>
        /// <param name="isDate">If true, the value will be interpreted as a DateTime.</param>
        public void SetMinimum(double value, bool isDate)
        {
            if (!entryMin.HasFocus)
            {
                if (double.IsNaN(value))
                    entryMin.Text = string.Empty;
                else
                    entryMin.Text = isDate ? DateTimeAxis.ToDateTime(value).ToShortDateString() : value.ToString();
            }
        }

        /// <summary>
        /// Sets the text in the minimum textbox based on a DateTime stored as a double.
        /// </summary>
        /// <param name="value">Value to display.</param>
        /// <param name="isDate">If true, the value will be interpreted as a DateTime.</param>
        public void SetMaximum(double value, bool isDate)
        {
            if (!entryMax.HasFocus)
            {
                if (double.IsNaN(value))
                    entryMax.Text = string.Empty;
                else
                    entryMax.Text = isDate ? DateTimeAxis.ToDateTime(value).ToShortDateString() : value.ToString();
            }
        }

        /// <summary>
        /// Sets the text in the interval textbox.
        /// </summary>
        /// <param name="value">Value to display.</param>
        /// <param name="isDate">If true, the value will be interpreted as a DateTime interval.</param>
        public void SetInterval(double value, bool isDate)
        {
            if (!entryInterval.HasFocus)
            {
                if (double.IsNaN(value))
                    entryInterval.Text = string.Empty;
                else
                    entryInterval.Text = isDate ? ((DateTimeIntervalType)((int)value)).ToString() : value.ToString();
            }
        }

        /// <summary>
        /// Destroying the main widget
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The event arguments</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                entryTitle.Changed -= TitleTextBox_TextChanged;
                entryMin.FocusOutEvent -= OnMinimumChanged;
                entryMax.FocusOutEvent -= OnMaximumChanged;
                entryInterval.FocusOutEvent -= OnIntervalChanged;
                checkbutton1.Toggled -= OnCheckedChanged;
                checkbutton2.Toggled -= OnCheckedChanged;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the title text box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void TitleTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (TitleChanged != null)
                    TitleChanged.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the inverted box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (InvertedChanged != null)
                    InvertedChanged(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the crosses at zero check box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCrossesAtZeroChanged(object sender, EventArgs e)
        {
            try
            {
                if (CrossesAtZeroChanged != null)
                    CrossesAtZeroChanged(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the minimum box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnMinimumChanged(object sender, EventArgs e)
        {
            try
            {
                if (MinimumChanged != null)
                    MinimumChanged(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the maximum box.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnMaximumChanged(object sender, EventArgs e)
        {
            try
            {
                if (MaximumChanged != null)
                    MaximumChanged(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the maximum box.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnIntervalChanged(object sender, EventArgs e)
        {
            try
            {
                if (IntervalChanged != null)
                    IntervalChanged(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
