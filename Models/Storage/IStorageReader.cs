﻿using System;
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
        /// <param name="simulationNames">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Optional column names to retrieve from storage</param>
        /// <param name="filter">Optional filter</param>
        /// <param name="from">Optional start index. Only used when 'count' specified. The record number to offset.</param>
        /// <param name="count">Optional number of records to return or all if 0.</param>
        /// <param name="orderByFieldNames">Optional column names to order by</param>
        /// <param name="distinct">Only return distinct values for field?</param>
        DataTable GetData(string tableName, string checkpointName = "Current", IEnumerable<string> simulationNames = null, IEnumerable<string> fieldNames = null,
                        string filter = null,
                        int from = 0, int count = 0,
                        IEnumerable<string> orderByFieldNames = null,
                        bool distinct = false);

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        DataTable GetDataUsingSql(string sql);

        /// <summary>Execute sql.</summary>
        /// <param name="sql">The SQL.</param>
        void ExecuteSql(string sql);

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
        /// <param name="simulationID">The simulation ID (if it exists).</param>
        bool TryGetSimulationID(string simulationName, out int simulationID);

        /// <summary>
        /// Convert a collection of simulation names to ids.
        /// </summary>
        /// <param name="simulationNames">The simulation names to convert to Ids.</param>
        /// <returns></returns>
        IEnumerable<int> ToSimulationIDs(IEnumerable<string> simulationNames);

        /// <summary>Refresh this instance to reflect the database connection.</summary>
        void Refresh();
    }
}