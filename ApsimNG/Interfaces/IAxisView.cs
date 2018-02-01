// -----------------------------------------------------------------------
// <copyright file="IAxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

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
}