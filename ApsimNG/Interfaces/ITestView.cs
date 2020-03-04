namespace UserInterface.Interfaces
{
    using System.Data;
    using Views;
using System;

    /// <summary>
    /// An interface for a test view
    /// </summary>
    public interface ITestView
    {
        /// <summary>
        /// The table name has changed.
        /// </summary>
        event EventHandler TableNameChanged;

        /// <summary>
        /// Gets or sets a list of table names
        /// </summary>
        string[] TableNames { get; set; }

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        string TableName { get; set; }

        /// <summary>
        /// Gets or sets the data for the grid
        /// </summary>
        DataTable Data { get; set; }

        /// <summary>
        /// Gets the editor.
        /// </summary>
        IEditorView Editor { get; }
    }
}
