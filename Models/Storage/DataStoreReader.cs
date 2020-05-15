namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A class for reading from a database connection.
    /// </summary>
    public class DataStoreReader : IStorageReader
    {
        /// <summary>A database connection</summary>
        public IDatabaseConnection Connection { get; private set; } = null;

        /// <summary>A list of field names for each table.</summary>
        private Dictionary<string, List<string>> tables = new Dictionary<string, List<string>>();

        /// <summary>The IDS for all simulations</summary>
        private Dictionary<string, int> simulationIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The IDs for all checkpoints</summary>
        private Dictionary<string, Checkpoint> checkpointIDs = new Dictionary<string, Checkpoint>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A copy of the units table.
        /// </summary>
        private DataView units;

        /// <summary>Return a list of simulation names or empty string[]. Never returns null.</summary>
        public List<string> SimulationNames
        {
            get
            {
                var data = Connection.ExecuteQuery("select Name from [_Simulations]");
                return DataTableUtilities.GetColumnAsStrings(data, "Name").ToList();
            }
        }

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
            Connection = database;
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
            if (units.Table != null && units.Table.Rows.Count > 0)
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
            return Connection.GetColumnNames(tableName);
        }

        /// <summary>Return a list of column names/column type tuples for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            return Connection.GetColumns(tableName);
        }

        /// <summary>
        /// Gets a "brief" column name for a column
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="fullColumnName">The "full" name of the column</param>
        /// <returns>The "brief" name of the column</returns>
        public string BriefColumnName(string tablename, string fullColumnName)
        {
            if (Connection is Firebird)
                return (Connection as Firebird).GetShortColumnName(tablename, fullColumnName);
            else
                return fullColumnName;
        }

        /// <summary>
        /// Gets the "full" column name for a column
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="queryColumnName"></param>
        /// <returns>The "full" name of the column</returns>
        public string FullColumnName(string tablename, string queryColumnName)
        {
            if (Connection is Firebird)
                return (Connection as Firebird).GetLongColumnName(tablename, queryColumnName);
            else
                return queryColumnName;
        }

        /// <summary>Returns a list of table names</summary>
        public List<string> TableNames { get { return Connection.GetTableNames().FindAll(t => !t.StartsWith("_")); } }

        /// <summary>Returns a list of table names</summary>
        public List<string> ViewNames { get { return Connection.GetViewNames().FindAll(t => !t.StartsWith("_")); } }

        /// <summary>Returns a list of table and view names</summary>
        public List<string> TableAndViewNames { get { return Connection.GetTableAndViewNames().FindAll(t => !t.StartsWith("_")); } }

        /// <summary>Refresh this instance to reflect the database connection.</summary>
        public void Refresh()
        {
            simulationIDs.Clear();
            checkpointIDs.Clear();
            tables.Clear();

            // Read in simulation ids.
            if (Connection.TableExists("_Simulations"))
            {
                var data = Connection.ExecuteQuery("SELECT * FROM [_Simulations]");
                foreach (DataRow row in data.Rows)
                    simulationIDs.Add(row["Name"].ToString(), Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture));
            }

            // Read in checkpoint ids.
            if (Connection.TableExists("_Checkpoints"))
            {
                var data = Connection.ExecuteQuery("SELECT * FROM [_Checkpoints]");
                foreach (DataRow row in data.Rows)
                {
                    checkpointIDs.Add(row["Name"].ToString(), new Checkpoint()
                    {
                        ID = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture),
                        ShowOnGraphs = data.Columns["OnGraphs"] != null &&
                                       !Convert.IsDBNull(row["OnGraphs"]) &&
                                       Convert.ToInt32(row["OnGraphs"], CultureInfo.InvariantCulture) == 1
                    });
                }
            }

            // For each table in the database, read in field names.
            foreach (var tableName in Connection.GetTableNames())
                tables.Add(tableName, Connection.GetTableColumns(tableName));

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
        /// <param name="distinct">Only return distinct values for field?</param>
        /// <returns></returns>
        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null,
                                 string filter = null,
                                 int from = 0, int count = 0,
                                 string orderBy = null,
                                 bool distinct = false)
        {
            if (!Connection.TableExists(tableName) && !Connection.ViewExists(tableName))
                return null;

            var fieldNamesInTable = Connection.GetColumnNames(tableName);

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

            bool hasSimulationName = fieldList.Contains("SimulationID") || fieldList.Contains("SimulationName") || simulationName != null || (filter != null && filter.Contains("SimulationName"));
            bool hasCheckpointName = fieldNamesInTable.Contains("CheckpointID") || fieldNamesInTable.Contains("CheckpointName") || checkpointName != null;

            sql.Append("SELECT ");

            if (distinct)
                sql.Append(" DISTINCT ");

            if (count > 0 && Connection is Firebird)
            {
                sql.Append("FIRST ");
                sql.Append(count);
                sql.Append(" SKIP ");
                sql.Append(from);
                sql.Append(" ");
            }

            bool firstField = true;
            if (hasCheckpointName)
            {
                sql.Append("C.[Name] AS [CheckpointName], C.[ID] AS [CheckpointID]");
                firstField = false;
            }

            if (hasSimulationName)
            {
                if (!firstField)
                    sql.Append(", ");
                sql.Append("S.[Name] AS [SimulationName], S.[ID] AS [SimulationID]");
                firstField = false;
            }

            fieldList.Remove("CheckpointID");
            fieldList.Remove("CheckpointName");
            fieldList.Remove("SimulationName");
            fieldList.Remove("SimulationID");

            foreach (string fieldName in fieldList)
            {
                if (fieldNamesInTable.Contains(fieldName))
                {
                    if (!firstField)
                        sql.Append(", ");
                    firstField = false;
                    sql.Append("T.");
                    sql.Append("[");
                    if (!(Connection is Firebird) || tableName.StartsWith("_")
                      || fieldName.Equals("SimulationID", StringComparison.OrdinalIgnoreCase)
                      || fieldName.Equals("SimulationName", StringComparison.OrdinalIgnoreCase)
                      || fieldName.Equals("CheckpointID", StringComparison.OrdinalIgnoreCase)
                      || fieldName.Equals("CheckpointName", StringComparison.OrdinalIgnoreCase))
                        sql.Append(fieldName);
                    else
                        sql.Append("COL_" + (Connection as Firebird).GetColumnNumber(tableName, fieldName).ToString());
                    sql.Append(']');
                    if (fieldName == "Clock.Today")
                        hasToday = true;
                }
            }

            bool firstFrom = true;
            // Write FROM clause
            sql.Append(" FROM ");
            if (hasCheckpointName)
            {
                sql.Append("[_Checkpoints] C");
                firstFrom = false;
            }
            if (hasSimulationName)
            {
                if (!firstFrom)
                    sql.Append(", ");
                sql.Append("[_Simulations] S");
                firstFrom = false;
            }
            if (!firstFrom)
                sql.Append(", ");
            sql.Append("[" + tableName);
            sql.Append("] T ");

            if (hasCheckpointName || hasSimulationName || !string.IsNullOrWhiteSpace(filter))
            {
                bool firstWhere = true;
                // Write WHERE clause
                sql.Append("WHERE ");
                if (hasCheckpointName)
                {
                    sql.Append("T.[CheckpointID] = C.[ID]");
                    // Write checkpoint name
                    if (checkpointName == null)
                        sql.Append(" AND C.[Name] = 'Current'");
                    else
                        sql.Append(" AND C.[Name] = '" + checkpointName + "'");
                    firstWhere = false;
                }
                if (hasSimulationName)
                {
                    if (!firstWhere)
                        sql.Append(" AND ");
                    sql.Append("T.[SimulationID] = S.[ID]");
                    if (simulationName != null)
                    {
                        sql.Append(" AND S.[Name] = '");
                        sql.Append(simulationName);
                        sql.Append('\'');
                    }
                    firstWhere = false;
                }

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    string copyFilter = filter;
                    // For Firebird, we need to convert column names to their short form to perform the query
                    if (Connection is Firebird)
                    {
                        List<string> output = copyFilter.Split('[', ']').Where((item, index) => index % 2 != 0).ToList();
                        foreach (string field in output)
                        {
                            string shortName = (Connection as Firebird).GetShortColumnName(tableName, field);
                            if (!string.IsNullOrEmpty(shortName))
                            {
                                copyFilter = copyFilter.Replace("[" + field + "]", "[" + shortName + "]");
                            }
                        }
                    }

                    if (!firstWhere)
                        sql.Append(" AND ");
                    firstWhere = false;
                    sql.Append("(");
                    sql.Append(copyFilter);
                    sql.Append(")");
                }
            }
            // Write ORDER BY clause
            if (orderBy == null)
            {
                if (hasSimulationName)
                {
                    sql.Append(" ORDER BY S.[ID]");
                    if (hasToday)
                    {
                        if (Connection is Firebird)
                            sql.Append(", T.[COL_" + (Connection as Firebird).GetColumnNumber(tableName, "Clock.Today").ToString() + "]");
                        else
                            sql.Append(", T.[Clock.Today]");
                    }
                }
            }
            else
            {
                sql.Append(" ORDER BY " + orderBy);
            }

            if (Connection is SQLite)
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
            if (Connection is SQLite)
                sql = sql.Replace("SimulationName IN ", "SimulationName COLLATE NOCASE IN ");
            else if (Connection is Firebird)
                sql = sql.Replace("SimulationName ", "S.[Name] ");
            var st = sql.ToString();
            DataTable result = Connection.ExecuteQuery(st);
            // For Firebird, we need to recover the full names of the data columns
            if (Connection is Firebird && !tableName.StartsWith("_"))
            {
                foreach (DataColumn dataCol in result.Columns)
                {
                    if (dataCol.ColumnName.StartsWith("COL_"))
                    {
                        int colNo;
                        if (Int32.TryParse(dataCol.ColumnName.Substring(4), out colNo))
                        {
                            dataCol.ColumnName = (Connection as Firebird).GetLongColumnName(tableName, colNo);
                        }
                    }
                }
            }
            return result;
        }

        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public DataTable GetDataUsingSql(string sql)
        {
            try
            {
                return Connection.ExecuteQuery(sql);
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
            return checkpointIDs[checkpointName].ID;
        }

        /// <summary>
        /// Return true if checkpoint is to be shown on graphs.
        /// </summary>
        /// <param name="checkpointName">The checkpoint name to look for.</param>
        /// <returns></returns>
        public bool GetCheckpointShowOnGraphs(string checkpointName)
        {
            return checkpointIDs[checkpointName].ShowOnGraphs;
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
