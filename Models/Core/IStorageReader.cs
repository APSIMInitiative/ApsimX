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
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Optional column names to retrieve from storage</param>
        /// <param name="filter">Optional filter</param>
        /// <param name="from">Optional start index. Only used when 'count' specified. The record number to offset.</param>
        /// <param name="count">Optional number of records to return or all if 0.</param>
        DataTable GetData(string tableName, string simulationName = null, IEnumerable<string> fieldNames = null,
                                 string filter = null,
                                 int from = 0, int count = 0);

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
        void DeleteTable(string tableName);

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
        void DeleteAllTables();

        /// <summary>Begin writing to DB file</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file. If null no cleanup will be performed.</param>
        /// <param name="simulationNamesBeingRun">Collection of simulation names being run. If null no cleanup will be performed.</param>
        void BeginWriting(IEnumerable<string> knownSimulationNames = null, IEnumerable<string> simulationNamesBeingRun = null);

        /// <summary>Finish writing to DB file</summary>
        void EndWriting();
    }
}