// -----------------------------------------------------------------------
// <copyright file="IDataStoreView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// The interface for a data store view
    /// </summary>
    public interface IDataStoreView
    {
        /// <summary>
        /// Invoked when a table is selected.
        /// </summary>
        event EventHandler OnTableSelected;

        /// <summary>
        /// Invoked when a table is selected.
        /// </summary>
        event EventHandler OnSimulationSelected;

        /// <summary>
        /// Invoked when the create now button is clicked.
        /// </summary>
        event EventHandler CreateNowClicked;

        /// <summary>
        /// Invoked when the run child models button is clicked.
        /// </summary>
        event EventHandler RunChildModelsClicked;

        /// <summary>
        /// Gets or sets the list of tables.
        /// </summary>
        string[] TableNames { get; set; }

        /// <summary>
        /// Gets the currently selected table name.
        /// </summary>
        string SelectedTableName { get; }

        /// <summary>
        /// Gets the main data grid.
        /// </summary>
        Interfaces.IGridView Grid { get; }

        /// <summary>
        /// Gets or sets the list of simulation names.
        /// </summary>
        string[] SimulationNames { get; set; }

        /// <summary>
        /// Gets or sets the selected simulation name.
        /// </summary>
        string SelectedSimulationName { get; set; }

        /// <summary>
        /// Show the summary content.
        /// </summary>
        /// <param name="content">The html content to show.</param>
        void ShowSummaryContent(string content);
    }
}