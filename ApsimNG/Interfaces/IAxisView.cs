namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    public interface IAxisView
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
        /// Invoked when the user has changed the crosses at zero field
        /// </summary>
        event EventHandler CrossesAtZeroChanged;

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
        double Minimum { get; }

        /// <summary>
        /// Gets or sets the maximum axis scale. double.Nan for auto scale
        /// </summary>
        double Maximum { get; }

        /// <summary>
        /// Gets or sets the axis scale interval. double.Nan for auto scale
        /// </summary>
        double Interval { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the axis crosses the other axis at zero.
        /// </summary>
        bool CrossesAtZero { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is a date time axis.
        /// This is not editable by the user.
        /// </summary>
        bool IsDateAxis { get; set; }

        /// <summary>
        /// Sets the text in the minimum textbox.
        /// </summary>
        /// <param name="value">Value to display.</param>
        /// <param name="isDate">If true, the value will be interpreted as a DateTime.</param>
        void SetMinimum(double value, bool isDate);

        /// <summary>
        /// Sets the text in the minimum textbox based on a DateTime stored as a double.
        /// </summary>
        /// <param name="value">Value to display.</param>
        /// <param name="isDate">If true, the value will be interpreted as a DateTime.</param>
        void SetMaximum(double value, bool isDate);

        /// <summary>
        /// Sets the text in the interval textbox.
        /// </summary>
        /// <param name="value">Value to display.</param>
        /// <param name="isDate">If true, the value will be interpreted as a DateTime interval.</param>
        void SetInterval(double value, bool isDate);
    }
}