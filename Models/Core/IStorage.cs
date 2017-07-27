using System.Collections.Generic;
using System.Data;

namespace Models.Core
{
    /// <summary>
    /// Interface for reading and writing data to/from permanent storage.
    /// </summary>
    public interface IStorage
    {
        /// <summary>Returns the file name of the .db file</summary>
        string FileName { get; }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite);

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

        /// <summary>Create a table in the database based on the specified one.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="table">The table.</param>
        void WriteTable(string tableName, DataTable table);

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

        /// <summary>Store the list of factor names and values for the specified simulation.</summary>
        void StoreFactors(string experimentName, string simulationName, string folderName, List<string> names, List<string> values);

        /// <summary>Delete all tables</summary>
        /// <param name="cleanSlate">If true, all tables are deleted; otherwise Simulations and Messages tables are retained</param>
        void DeleteAllTables(bool cleanSlate = false);
    }
}