using System.Collections.Generic;
using System.Data;

namespace Models.Core
{
    /// <summary>
    /// Interface for reading and writing data to/from permanent storage.
    /// </summary>
    public interface IStorageReader
    {
        /// <summary>Returns the file name of the .db file</summary>
        string FileName { get; }

        /// <summary>
        /// Return list of checkpoints.
        /// </summary>
        List<string> Checkpoints();

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
        DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null,
                          string filter = null,
                          int from = 0, int count = 0,
                          string orderBy = null);

        /// <summary>Get a simulation ID for the specified simulation name</summary>
        /// <param name="simulationName">The simulation name to look for</param>
        /// <returns>The database ID or -1 if not found</returns>
        int GetSimulationID(string simulationName);

        /// <summary>Get a checkpoint ID for the specified checkpoint name</summary>
        /// <param name="checkpointName">The simulation name to look for</param>
        /// <returns>The database ID or -1 if not found</returns>
        int GetCheckpointID(string checkpointName);

        /// <summary>
        /// Obtain the units for a column of data
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnHeading">Name of the data column</param>
        /// <returns>The units (with surrounding parentheses), or null if not available</returns>
        string GetUnits(string tableName, string columnHeading);

        /// <summary>
        /// Create a table in the database based on the specified data. If a 'SimulationName'
        /// column is found a corresponding 'SimulationID' column will be created.
        /// </summary>
        /// <param name="data">The data to write</param>
        void WriteTable(DataTable data);

        /// <summary>Delete the specified table.</summary>
        /// <param name="tableName">Name of the table.</param>
        void DeleteDataInTable(string tableName);

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        DataTable RunQuery(string sql);

        /// <summary>Return a list of simulations names or empty string[]. Never returns null.</summary>
        string[] SimulationNames { get; }

        /// <summary>Returns a list of table names</summary>
        IEnumerable<string> TableNames { get; } 

        /// <summary>Return a list of simulations names or empty string[]. Never returns null.</summary>
        IEnumerable<string> ColumnNames(string tableName);

        /// <summary>Delete all tables</summary>
        void EmptyDataStore();

        /// <summary>
        /// Add units to table. Removes old units first.
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnNames">The column names to add</param>
        /// <param name="columnUnits">The column units to add</param>
        void AddUnitsForTable(string tableName, List<string> columnNames, List<string> columnUnits);

        /// <summary>
        /// Get a list of the table columns
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <returns></returns>
        List<string> GetTableColumns(string tableName);
    }
}