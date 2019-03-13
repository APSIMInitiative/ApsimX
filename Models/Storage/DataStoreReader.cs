namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A class for reading from a database connection.
    /// </summary>
    public class DataStoreReader : IStorageReader
    {
        /// <summary>A database connection</summary>
        private IDatabaseConnection connection = null;

        /// <summary>A list of field names for each table.</summary>
        private Dictionary<string, List<string>> tables = new Dictionary<string, List<string>>();

        /// <summary>The IDS for all simulations</summary>
        private Dictionary<string, int> simulationIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The IDs for all checkpoints</summary>
        private Dictionary<string, int> checkpointIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A copy of the units table.
        /// </summary>
        private DataView units;

        /// <summary>Return a list of simulation names or empty string[]. Never returns null.</summary>
        public List<string> SimulationNames { get { return simulationIDs.Keys.ToList(); } }

        /// <summary>Return a list of checkpoint names or empty string[]. Never returns null.</summary>
        public List<string> CheckpointNames { get { return checkpointIDs.Keys.ToList(); } }

        /// <summary>Default constructor.</summary>
        public DataStoreReader()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="database">The database to read from.</param>
        public DataStoreReader(IDatabaseConnection database)
        {
            SetConnection(database);
        }

        /// <summary>
        /// Set the database connection.
        /// </summary>
        /// <param name="database">The database connection to read from.</param>
        public void SetConnection(IDatabaseConnection database)
        {
            connection = database;
            Refresh();
        }

        /// <summary>
        /// Obtain the units for a column of data
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnHeading">Name of the data column</param>
        /// <returns>The units (with surrounding parentheses), or null if not available</returns>
        public string Units(string tableName, string columnHeading)
        {
            if (units.Table != null)
            {
                units.RowFilter = string.Format("TableName='{0}' AND ColumnHeading='{1}'",
                                                tableName, columnHeading);
                if (units.Count == 1)
                    return units[0]["Units"].ToString();
                else if (units.Count > 1)
                    throw new Exception(string.Format("Found multiple units for column {0} in table {1}",
                                        columnHeading, tableName));
            }
            return null;
        }

        /// <summary>Return a list of column names for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        public List<string> ColumnNames(string tableName)
        {
            return connection.GetColumnNames(tableName);
        }

        /// <summary>Returns a list of table names</summary>
        public List<string> TableNames { get { return connection.GetTableNames().FindAll(t => !t.StartsWith("_")); } }

        /// <summary>Refresh this instance to reflect the database connection.</summary>
        public void Refresh()
        {
            simulationIDs.Clear();
            checkpointIDs.Clear();
            tables.Clear();

            // Read in simulation ids.
            if (connection.TableExists("_Simulations"))
            {
                var data = connection.ExecuteQuery("SELECT * FROM _Simulations");
                foreach (DataRow row in data.Rows)
                    simulationIDs.Add(row["Name"].ToString(), Convert.ToInt32(row["ID"]));
            }

            // Read in checkpoint ids.
            if (connection.TableExists("_Checkpoints"))
            {
                var data = connection.ExecuteQuery("SELECT * FROM _Checkpoints");
                foreach (DataRow row in data.Rows)
                    checkpointIDs.Add(row["Name"].ToString(), Convert.ToInt32(row["ID"]));
            }

            // For each table in the database, read in field names.
            foreach (var tableName in connection.GetTableNames())
                tables.Add(tableName, connection.GetTableColumns(tableName));

            // Get the units table.
            units = new DataView(GetData("_Units"));
        }

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
        /// <returns></returns>
        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null,
                                 string filter = null,
                                 int from = 0, int count = 0,
                                 string orderBy = null)
        {
            if (!connection.TableExists(tableName))
                return null;

            var fieldNamesInTable = connection.GetColumnNames(tableName);

            if (fieldNamesInTable == null || fieldNamesInTable.Count == 0)
                return null;

            StringBuilder sql = new StringBuilder();

            bool hasToday = false;

            // Write SELECT clause
            List<string> fieldList = null;
            if (fieldNames == null)
                fieldList = fieldNamesInTable;
            else
                fieldList = fieldNames.ToList();

            bool hasSimulationName = fieldList.Contains("SimulationID") || fieldList.Contains("SimulationName") || simulationName != null;

            sql.Append("SELECT C.[Name] AS CheckpointName, C.[ID] AS CheckpointID");
            if (hasSimulationName)
                sql.Append(", S.[Name] AS SimulationName, S.[ID] AS SimulationID");

            fieldList.Remove("CheckpointID");
            fieldList.Remove("SimulationName");
            fieldList.Remove("SimulationID");

            foreach (string fieldName in fieldList)
            {
                if (fieldNamesInTable.Contains(fieldName))
                {
                    sql.Append(",T.");
                    sql.Append("[");
                    sql.Append(fieldName);
                    sql.Append(']');
                    if (fieldName == "Clock.Today")
                        hasToday = true;
                }
            }

            // Write FROM clause
            sql.Append(" FROM [_Checkpoints] C");
            if (hasSimulationName)
                sql.Append(", [_Simulations] S");
            sql.Append(", [" + tableName);
            sql.Append("] T ");

            // Write WHERE clause
            sql.Append("WHERE [CheckpointID] = C.[ID]");
            if (hasSimulationName)
            {
                sql.Append(" AND [SimulationID] = S.[ID]");
                if (simulationName != null)
                {
                    sql.Append(" AND S.[Name] = '");
                    sql.Append(simulationName);
                    sql.Append('\'');
                }
            }

            // Write checkpoint name
            if (checkpointName == null)
                sql.Append(" AND C.[Name] = 'Current'");
            else
                sql.Append(" AND C.[Name] = '" + checkpointName + "'");

            if (filter != null)
            {
                sql.Append(" AND (");
                sql.Append(filter);
                sql.Append(")");
            }

            // Write ORDER BY clause
            if (orderBy == null)
            {
                if (hasSimulationName)
                {
                    sql.Append(" ORDER BY S.[ID]");
                    if (hasToday)
                        sql.Append(", T.[Clock.Today]");
                }
            }
            else
            {
                sql.Append(" ORDER BY " + orderBy);
            }

            // Write LIMIT/OFFSET clause
            if (count > 0)
            {
                sql.Append(" LIMIT ");
                sql.Append(count);
                sql.Append(" OFFSET ");
                sql.Append(from);
            }

            // It appears that the a where clause that has 'SimulationName in ('xxx, 'yyy') is
            // case sensitive despite having COLLATE NOCASE in the 'CREATE TABLE _Simulations'
            // statement. I don't know why this is. The replace below seems to fix the problem.
            var st = sql.ToString();
            //if (!useFirebird)
                st = st.Replace("SimulationName IN ", "SimulationName COLLATE NOCASE IN ");
            return connection.ExecuteQuery(st);
        }

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public DataTable GetDataUsingSql(string sql)
        {
            try
            {
                return connection.ExecuteQuery(sql);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Return a checkpoint ID for the specified checkpoint name.
        /// </summary>
        /// <param name="checkpointName">The checkpoint name to look for.</param>
        /// <returns></returns>
        public int GetCheckpointID(string checkpointName)
        {
            return checkpointIDs[checkpointName];
        }

        /// <summary>
        /// Return a simulation ID for the specified name.
        /// </summary>
        /// <param name="simulationName">The simulation name to look for.</param>
        /// <returns></returns>
        public int GetSimulationID(string simulationName)
        {
            return simulationIDs[simulationName];
        }
    }
}
