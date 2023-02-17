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
    using System.Text.RegularExpressions;

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
        private DataTable units;

        /// <summary>Return a list of simulation names or empty string[]. Never returns null.</summary>
        public List<string> SimulationNames
        {
            get
            {
                var data = Connection.ExecuteQuery("select Name from [_Simulations]");
                return DataTableUtilities.GetColumnAsStrings(data, "Name", CultureInfo.InvariantCulture).ToList();
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
            if (units != null && units.Rows.Count > 0)
            {
                var unitsView = new DataView(units);
                unitsView.RowFilter = string.Format("TableName='{0}' AND ColumnHeading='{1}'",
                                                tableName, columnHeading);
                if (unitsView.Count == 1)
                    return unitsView[0]["Units"].ToString();
                else if (unitsView.Count > 1)
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
            if (tables.TryGetValue(tableName, out List<string> columnNames))
                return columnNames;
            else
                return new List<string>();
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
            foreach (var tableName in Connection.GetTableAndViewNames())
                tables.Add(tableName, Connection.GetTableColumns(tableName));

            // Get the units table.
            units = GetData("_Units");
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint.</param>
        /// <param name="simulationNames">Name of the simulations.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Optional column names to retrieve from storage</param>
        /// <param name="filter">Optional filter</param>
        /// <param name="from">Optional start index. Only used when 'count' specified. The record number to offset.</param>
        /// <param name="count">Optional number of records to return or all if 0.</param>
        /// <param name="orderByFieldNames">Optional column name to order by</param>
        /// <param name="distinct">Only return distinct values for field?</param>
        /// <returns></returns>
        public DataTable GetData(string tableName, string checkpointName = "Current", 
                                 IEnumerable<string> simulationNames = null, 
                                 IEnumerable<string> fieldNames = null,
                                 string filter = null,
                                 int from = 0, int count = 0,
                                 IEnumerable<string> orderByFieldNames = null,
                                 bool distinct = false)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            // Get the field names in the table
            var table = tables.TryGetValue(tableName, out List<string> fieldNamesInTable);

            // Return null if there are no fields in the table (or the table is missing) 
            // and we have no view.
            if ((fieldNamesInTable == null || fieldNamesInTable.Count == 0)  // no table.
                && !Connection.ViewExists(tableName))                        // no view
                return null;

            // Calculate the SELECT field names.
            if (fieldNames == null)
                fieldNames = fieldNamesInTable;
            fieldNames = new string[] { "CheckpointID", "SimulationID" }
                              .Union(fieldNames)
                              .Intersect(fieldNamesInTable, StringComparer.OrdinalIgnoreCase)
                              .Enclose("\"", "\"");

            var firebirdFirstStatement = string.Empty;
            var sqLiteLimitStatement = string.Empty;

            // Calculate DISTINCT keyword
            var distinctKeyword = string.Empty;
            if (distinct)
                distinctKeyword = "DISTINCT";

            // Add checkpointID to filter.
            if (fieldNamesInTable.Contains("CheckpointID") && checkpointIDs.ContainsKey(checkpointName))
                filter = AddToFilter(filter, $"CheckpointID={checkpointIDs[checkpointName].ID}");

            filter = RemoveSimulationNameFromFilter(filter);

            if (filter != null && filter.Contains("SimulationName"))
                throw new Exception("Internal error: Don't pass SimulationName in a filter to DataStoreReader.GetData. Use the SimulationNames argument instead.");

            // Add simulationIDs to filter
            if (simulationNames != null)
                filter = AddToFilter(filter, $"SimulationID in ({ToSimulationIDs(simulationNames).Join(",")})");

            // Calculate Firebird bits
            if (Connection is Firebird)
            {
                fieldNames = ConvertFieldNameToFirebird(fieldNames, tableName);
                if (count > 0)
                    firebirdFirstStatement = $"FIRST {count} SKIP {from}";

                var output = filter.Split('[', ']').Where((item, index) => index % 2 != 0).ToList();
                foreach (string field in output)
                {
                    var shortName = (Connection as Firebird).GetShortColumnName(tableName, field);
                    if (!string.IsNullOrEmpty(shortName))
                    {
                        filter = filter.Replace("[" + field + "]", "[" + shortName + "]");
                    }
                }
            }

            // Get orderby fields
            var orderByFields = new List<string>();
            if (fieldNamesInTable.Contains("SimulationID"))
                orderByFields.Insert(0, "SimulationID");
            if (fieldNamesInTable.Contains("Clock.Today"))
                orderByFields.Insert(0, "Clock.Today");
            if (orderByFieldNames != null)
                orderByFields.AddRange(orderByFieldNames);

            // Build SQL statement
            var sql = $"SELECT {distinctKeyword} {firebirdFirstStatement} {fieldNames.Join(",")}" +
                      $" FROM \"{tableName}\"";
            if (!string.IsNullOrEmpty(filter))
                sql += $" WHERE {filter}";
            if (orderByFields.Count > 0)
                sql += $" ORDER BY {orderByFields.Enclose("\"", "\"").Join(",")}";
            if (Connection is SQLite && count > 0)
                sql += $" LIMIT {count} OFFSET {from}";

            // Run query.
            DataTable result = Connection.ExecuteQuery(sql);

            // Add SimulationName and CheckpointName if necessary.
            if (result.Rows.Count > 0 && fieldNamesInTable.Contains("SimulationID"))
            {
                result.Columns.Add("CheckpointName", typeof(string));
                result.Columns.Add("SimulationName", typeof(string));
                result.Columns["CheckpointName"].SetOrdinal(0);
                result.Columns["SimulationName"].SetOrdinal(2);
                foreach (DataRow row in result.Rows)
                {
                    string simulationName = null;
                    if (!Convert.IsDBNull(row["SimulationID"]) && Int32.TryParse(row["SimulationID"].ToString(), out int simulationID))
                        simulationName = simulationIDs.FirstOrDefault(x => x.Value == simulationID).Key;
                    else
                    {
                        throw new Exception($"In table {tableName}, SimulationID has a value of {row["SimulationID"].ToString()} which is not valid");
                    }

                    row["CheckpointName"] = checkpointName;
                    row["SimulationName"] = simulationName;
                }
            }

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

        /// <summary>
        /// Remove 'Simulation = xxxx' from filter and replace with 'SimulationiD=xx'
        /// </summary>
        /// <param name="filter"></param>
        private string RemoveSimulationNameFromFilter(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                string pattern = "\\[*SimulationName\\]*\\W*(=|<>)\\W*[\"|'](\\w+)[\"|']";
                return Regex.Replace(filter, pattern, delegate (Match m)
                {
                    var oper = m.Groups[1].Value;
                    var simulationName = m.Groups[2].Value;
                    if (TryGetSimulationID(simulationName, out int id))
                        return $"SimulationID{oper}{id}";
                    else
                        return "";
                });
            }
            return null;
        }

        /// <summary>Add a clause to the filter.</summary>
        /// <param name="filter">The filter to add to.</param>
        /// <param name="filterClause">The clause to add e.g. Exp = 'Exp1'.</param>
        private string AddToFilter(string filter, string filterClause)
        {
            if (!string.IsNullOrEmpty(filterClause))
            {
                if (string.IsNullOrEmpty(filter))
                    return filterClause;
                else
                    return filter + " AND " + filterClause;
            }
            return filter;
        }

        /// <summary>Convert field names to Firebird format.</summary>
        /// <param name="fieldNames">The field names.</param>
        /// <param name="tableName">The table name.</param>
        private IEnumerable<string> ConvertFieldNameToFirebird(IEnumerable<string> fieldNames, string tableName)
        {

            foreach (var fieldName in fieldNames)
                yield return $"COL_{(Connection as Firebird).GetColumnNumber(tableName, fieldName)}";
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

        /// <summary>Execute sql.</summary>
        /// <param name="sql">The SQL.</param>
        public void ExecuteSql(string sql)
        {
            Connection.ExecuteQuery(sql);
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
        /// <param name="simulationID">The simulation ID (if it exists).</param>
        public bool TryGetSimulationID(string simulationName, out int simulationID)
        {
            return simulationIDs.TryGetValue(simulationName, out simulationID);
        }

        /// <summary>
        /// Convert a collection of simulation names to ids.
        /// </summary>
        /// <param name="simulationNames">The simulation names to convert to Ids.</param>
        /// <returns></returns>
        public IEnumerable<int> ToSimulationIDs(IEnumerable<string> simulationNames)
        {
            foreach (var simulationName in simulationNames)
                if (TryGetSimulationID(simulationName, out int simulationID))
                    yield return simulationID;
        }
    }
}
