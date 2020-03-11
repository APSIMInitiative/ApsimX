using System;
using System.Collections.Generic;
using System.Data;

namespace Models.Storage
{
    /// <summary>
    /// Interface for reading and writing data to/from permanent storage.
    /// </summary>
    public interface IStorageReader
    {
        /// <summary>Return a list of checkpoint names or empty string[]. Never returns null.</summary>
        List<string> CheckpointNames { get; }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint.</param>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Optional column names to retrieve from storage</param>
        /// <param name="filter">Optional filter</param>
        /// <param name="from">Optional start index. Only used when 'count' specified. The record number to offset.</param>
        /// <param name="count">Optional number of records to return or all if 0.</param>
        /// <param name="orderBy">Optional column name to order by</param>
        /// <param name="distinct">Only return distinct values for field?</param>
        DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null,
                        string filter = null,
                        int from = 0, int count = 0,
                        string orderBy = null,
                        bool distinct = false);

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        DataTable GetDataUsingSql(string sql);

        /// <summary>
        /// Obtain the units for a column of data
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnHeading">Name of the data column</param>
        /// <returns>The units (with surrounding parentheses), or null if not available</returns>
        string Units(string tableName, string columnHeading);

        /// <summary>Return a list of simulations names or empty string[]. Never returns null.</summary>
        List<string> SimulationNames { get; }

        /// <summary>Returns a list of table names</summary>
        List<string> TableNames { get; }

        /// <summary>Returns a list of view names</summary>
        List<string> ViewNames { get; }

        /// <summary>Returns a list of table and view names</summary>
        List<string> TableAndViewNames { get; }

        /// <summary>Return a list of column names for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        List<string> ColumnNames(string tableName);

        /// <summary>Return a list of column names/column type tuples for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        List<Tuple<string, Type>> GetColumns(string tableName);

        /// <summary>
        /// Gets a "brief" column name for a column
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="fullColumnName">The "full" name of the column</param>
        /// <returns>The "brief" name of the column</returns>
        string BriefColumnName(string tablename, string fullColumnName);

        /// <summary>
        /// Gets the "full" column name for a column
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="queryColumnName"></param>
        /// <returns>The "full" name of the column</returns>
        string FullColumnName(string tablename, string queryColumnName);

        /// <summary>
        /// Return a checkpoint ID for the specified checkpoint name.
        /// </summary>
        /// <param name="checkpointName">The checkpoint name to look for.</param>
        /// <returns></returns>
        int GetCheckpointID(string checkpointName);

        /// <summary>
        /// Return true if checkpoint is to be shown on graphs.
        /// </summary>
        /// <param name="checkpointName">The checkpoint name to look for.</param>
        /// <returns></returns>
        bool GetCheckpointShowOnGraphs(string checkpointName);

        /// <summary>
        /// Return a simulation ID for the specified name.
        /// </summary>
        /// <param name="simulationName">The simulation name to look for.</param>
        /// <returns></returns>
        int GetSimulationID(string simulationName);

        /// <summary>Refresh this instance to reflect the database connection.</summary>
        void Refresh();
    }
}