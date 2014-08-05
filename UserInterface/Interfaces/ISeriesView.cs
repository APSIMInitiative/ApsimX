// -----------------------------------------------------------------------
// <copyright file="ISeriesView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// Interface for a series view.
    /// </summary>
    public interface ISeriesView
    {
        /// <summary>
        /// Invoked when a series has been selected by user.
        /// </summary>
        event EventHandler SeriesSelected;

        /// <summary>
        /// Invoked when a new empty series is added.
        /// </summary>
        event EventHandler SeriesAdded;

        /// <summary>
        /// Invoked when a series is deleted.
        /// </summary>
        event EventHandler SeriesDeleted;

        /// <summary>
        /// Invoked when a series is deleted.
        /// </summary>
        event EventHandler AllSeriesCleared;

        /// <summary>
        /// Invoked when a series is renamed
        /// </summary>
        event EventHandler SeriesRenamed;

        /// <summary>
        /// Gets the series editor.
        /// </summary>
        ISeriesEditorView SeriesEditor { get; }

        /// <summary>
        /// Gets or sets the series names.
        /// </summary>
        string[] SeriesNames { get; set; }

        /// <summary>
        /// Gets or sets the selected series name.
        /// </summary>
        string SelectedSeriesName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the series editor is visible.
        /// </summary>
        bool EditorVisible { get; set; }
    }
}
