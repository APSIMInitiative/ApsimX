namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;
    using System.Linq;
    using System.Text;
    using System.Globalization;
    using Microsoft.Data.Sqlite;

    /// <summary>A class representing an exception thrown by this library.</summary>
    [Serializable]
    public class SQLiteException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="SQLiteException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public SQLiteException(string message) :
            base(message)
        {

        }
    }

    /// <summary>A class for accessing an SQLite database.</summary>
    [Serializable]
    public class SQLite : IDatabaseConnection, IDisposable
    {
        /// <summary>Connection to the SQLite database.</summary>
        private SqliteConnection connection;

        /// <summary>Keeps track of whether the database is open.</summary>
        [NonSerialized]
        private bool _open;

        /// <summary>path to the database</summary>
        [NonSerialized]
        private string dbPath; 

        /// <summary>Property to return true if the database is open.</summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get { return _open; } }

        /// <summary>Property to return true if the database is readonly.</summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>Return true if the database is in-memory</summary>
        public bool IsInMemory { get; private set; }

        /// <summary>Begin a transaction.</summary>
        public void BeginTransaction()
        {
            ExecuteNonQuery("BEGIN TRANSACTION");
        }

        /// <summary>End a transaction.</summary>
        public void EndTransaction()
        {
            ExecuteNonQuery("END TRANSACTION");
        }

        /// <summary>Opens or creates SQLite database with the specified path</summary>
        /// <param name="path">Path to SQLite database</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <exception cref="SQLiteException"></exception>
        public void OpenDatabase(string path, bool readOnly)
        {
            connection = new SqliteConnection($"Data Source={path}");
            connection.Open();
            _open = true;
            dbPath = path;
            IsReadOnly = readOnly;
            IsInMemory = path.ToLower().Contains(":memory:");
        }

        /// <summary>Closes the SQLite database</summary>
        public void CloseDatabase()
        {
            if (_open)
                connection.Close();

            _open = false;
        }

        /// <summary>Executes a query that returns no results</summary>
        /// <param name="query">SQL query to execute</param>
        public void ExecuteNonQuery(string query)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            using (SqliteCommand command = new SqliteCommand(query, connection))
                command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a query and stores the results in
        /// a DataTable
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <returns>DataTable of results</returns>
        public DataTable ExecuteQuery(string query)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            using (SqliteCommand command = new SqliteCommand(query, connection))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    DataTable result = new DataTable();
                    Dictionary<string, string> columnTypes = GetColumnTypes(reader);
                    using (DataSet tmpDs = new DataSet() { EnforceConstraints = false })
                    {
                        tmpDs.Tables.Add(result);
                        result.Load(reader, LoadOption.OverwriteChanges);
                        tmpDs.Tables.Remove(result);
                    }

                    while (result.Constraints.Count > 0)
                        result.Constraints.RemoveAt(0);

                    // Need to convert date strings to DateTime type.
                    DataTable dtCloned = result.Clone();
                    foreach (DataColumn col in dtCloned.Columns)
                        if (columnTypes[col.ColumnName] == "date")
                            col.DataType = typeof(DateTime);
                    foreach (DataRow row in result.Rows) 
                        dtCloned.ImportRow(row);

                    while (dtCloned.Constraints.Count > 0)
                        dtCloned.Constraints.RemoveAt(0);
                    return dtCloned;
                }
            }
        }

        private Dictionary<string, string> GetColumnTypes(SqliteDataReader reader)
        {
            Dictionary<string, string> types = new Dictionary<string, string>();
            foreach (DataRow row in reader.GetSchemaTable().Rows)
            {
                string columnName = row["ColumnName"]?.ToString();
                string type = row["DataTypeName"]?.ToString();
                if (columnName != null && type != null)
                    types[columnName] = type;
            }
            return types;
        }

        /// <summary>
        /// Executes a query and return a single integer value to caller. Returns -1 if not found.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="ColumnNumber">The column number.</param>
        /// <returns></returns>
        public int ExecuteQueryReturnInt(string query, int ColumnNumber)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            using (SqliteCommand command = new SqliteCommand(query, connection))
                return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public List<string> GetColumnNames(string tableName)
        {
            List<string> columnNames = new List<string>();

            string sql = $"PRAGMA table_info({tableName})";
            using (SqliteCommand command = new SqliteCommand(sql, connection))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    DataTable result = new DataTable();
                    result.Load(reader);
                    return result.AsEnumerable().Select(r => r["name"].ToString()).ToList();
                }
            }
        }

        /// <summary>Return a list of column names/column type tuples for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            var columns = new List<Tuple<string, Type>>();
            DataTable columnData = ExecuteQuery("pragma table_info('" + tableName + "')");

            foreach (DataRow row in columnData.Rows)
                columns.Add(new Tuple<string,Type>(row["name"].ToString(), GetTypeFromSQLiteType(row["type"].ToString())));

            return columns;
        }

        /// <summary>Return a list of column names for the specified table</summary>
        /// <param name="tableName">The table name to get columns from.</param>
        public List<string> GetTableColumns(string tableName)
        {
            // why???
            return GetColumnNames(tableName);
        }

        /// <summary>Return a list of table names</summary>
        public List<string> GetTableNames()
        {
            List<string> tableNames = new List<string>();
            DataTable tableData = ExecuteQuery("SELECT * FROM sqlite_master");
            var names = DataTableUtilities.GetColumnAsStrings(tableData, "Name");
            var types = DataTableUtilities.GetColumnAsStrings(tableData, "Type");
            for (int i = 0; i < names.Length; i++)
            {
                if (types[i] == "table")
                    tableNames.Add(names[i]);
            }
            return tableNames;
        }

        /// <summary>Return a list of table names</summary>
        public List<string> GetViewNames()
        {
            List<string> tableNames = new List<string>();
            DataTable tableData = ExecuteQuery("SELECT * FROM sqlite_master");
            var names = DataTableUtilities.GetColumnAsStrings(tableData, "Name");
            var types = DataTableUtilities.GetColumnAsStrings(tableData, "Type");
            for (int i = 0; i < names.Length; i++)
            {
                if (types[i] == "view")
                    tableNames.Add(names[i]);
            }
            return tableNames;
        }

        /// <summary>Return a list of table and view names</summary>
        /// <returns>A list of table and view names in sorted order (upper case)</returns>
        public List<string> GetTableAndViewNames()
        {
            return GetTableNames().Union(GetViewNames()).ToList();
        }

        /// <summary>Does the specified table exist?</summary>
        /// <param name="tableName">The table name to look for</param>
        public bool TableExists(string tableName)
        {
            List<string> tableNames = GetTableNames();
            return tableNames.Contains(tableName);
        }

        /// <summary>Does the specified table exist?</summary>
        /// <param name="viewName">The view name to look for</param>
        public bool ViewExists(string viewName)
        {
            List<string> viewNames = GetViewNames();
            return viewNames.Contains(viewName);
        }

        /// <summary>
        /// Returns true if the specified table exists, but holds no records
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns></returns>
        public bool TableIsEmpty(string tableName)
        {
            bool result = false;
            if (TableExists(tableName))
            {
                DataTable dTable = ExecuteQuery("SELECT COUNT(*) FROM [" + tableName + "]");
                if (dTable != null)
                    result = Convert.ToInt32(dTable.Rows[0][0], CultureInfo.InvariantCulture) == 0;
            }
            return result;
        }

        /// <summary>
        /// Drop (remove) columns from a table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="colsToRemove"></param>
        public void DropColumns(string tableName, IEnumerable<string> colsToRemove)
        {
            List<string> updatedTableColumns = GetTableColumns(tableName);
            IEnumerable<string> columnsToRemove = colsToRemove.ToList();

            // Remove the columns we don't want anymore from the table's list of columns
            updatedTableColumns.RemoveAll(column => columnsToRemove.Contains(column));

            string columnsSeperated = null;
            foreach (string columnName in updatedTableColumns)
            {
                if (columnsSeperated != null)
                    columnsSeperated += ",";
                columnsSeperated += "[" + columnName + "]";
            }
            if (updatedTableColumns.Count > 0)
            {
                BeginTransaction();

                // Rename old table
                ExecuteNonQuery("ALTER TABLE [" + tableName + "] RENAME TO [" + tableName + "_old]");

                // Creating the new table based on old table
                ExecuteNonQuery("CREATE TABLE [" + tableName + "] AS SELECT " + columnsSeperated + " FROM [" + tableName + "_old]");

                // Drop old table
                ExecuteNonQuery("DROP TABLE [" + tableName + "_old]");

                EndTransaction();
            }
        }

        /// <summary>
        /// Do and ALTER on the db table and add a column
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnName">The column to add</param>
        /// <param name="columnType">The db column type</param>
        public void AddColumn(string tableName, string columnName, string columnType)
        {
            string sql = "ALTER TABLE [" + tableName + "] ADD COLUMN [" + columnName + "] " + columnType;
            this.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public string CreateInsertSQL(string tableName, IEnumerable<string> columnNames)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO [");
            sql.Append(tableName);
            sql.Append("](");

            for (int i = 0; i < columnNames.Count(); i++)
            {
                string columnName = columnNames.ElementAt(i);
                if (i > 0)
                    sql.Append(',');
                sql.Append('"');
                sql.Append(columnName);
                sql.Append('"');
            }
            sql.Append(") VALUES (");

            for (int i = 0; i < columnNames.Count(); i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.Append($"@Parameter{i}");
            }

            sql.Append(')');

            return sql.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public int InsertRows(string tableName, List<string> columnNames, List<object[]> values)
        {
            string sql = CreateInsertSQL(tableName, columnNames);

            // Create an insert query
            foreach (object[] row in values)
            {
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    for (int i = 0; i < row.Length; i++)
                        command.Parameters.AddWithValue($"@Parameter{i}", row[i]);
                    command.ExecuteNonQuery();
                }
            }

            return 0;
        }

        /// <summary>Convert .NET type into an SQLite type</summary>
        public string GetDBDataTypeName(object value)
        {
            // Convert the value we found above into an SQLite data type string and return it.
            Type type = null;
            if (value == null)
                return null;
            else
                type = value.GetType();

            return GetDBDataTypeName(type);
        }

        /// <summary>Convert .NET type into an SQLite type</summary>
        public string GetDBDataTypeName(Type type)
        {
            if (type == null)
                return "integer";
            else if (type.ToString() == "System.DateTime")
                return "date";
            else if (type.ToString() == "System.Int32")
                return "integer";
            else if (type.ToString() == "System.Single")
                return "real";
            else if (type.ToString() == "System.Double")
                return "real";
            else if (type.ToString() == "System.Boolean")
                return "integer";
            else
                return "text";
        }

        /// <summary>Convert SQLite type into .NET type.</summary>
        public Type GetTypeFromSQLiteType(string sqliteType)
        {
            if (sqliteType == null)
                return typeof(int);
            else if (sqliteType == "date")
                return typeof(DateTime);
            else if (sqliteType == "integer")
                return typeof(int);
            else if (sqliteType == "real")
                return typeof(double);
            else
                return typeof(string);
        }

        /// <summary>Convert .NET type into an SQLite type</summary>
        public string GetDBDataTypeName(Type type, bool allowLongStrings)
        {
            return GetDBDataTypeName(type);
        }

        /// <summary>Create the new table</summary>
        public void CreateTable(string tableName, List<string> colNames, List<string> colTypes)
        {
            StringBuilder sql = new StringBuilder();

            for (int c = 0; c < colNames.Count; c++)
            {
                if (sql.Length > 0)
                    sql.Append(',');

                sql.Append("\"");
                sql.Append(colNames[c]);
                sql.Append("\" ");
                if (colTypes[c] == null)
                    sql.Append("integer");
                else
                    sql.Append(colTypes[c]);
            }

            sql.Insert(0, "CREATE TABLE [" + tableName + "] (");
            sql.Append(')');
            ExecuteNonQuery(sql.ToString());
        }

        /// <summary>
        /// Create an index.
        /// </summary>
        /// <param name="tableName">The table to create the index on.</param>
        /// <param name="colNames">The column names of the index.</param>
        /// <param name="isUnique">Is the index a primary key?</param>
        public void CreateIndex(string tableName, List<string> colNames, bool isUnique)
        {
            StringBuilder columnNamesCSV = new StringBuilder();
            for (int c = 0; c < colNames.Count; c++)
            {
                if (columnNamesCSV.Length > 0)
                    columnNamesCSV.Append(',');

                columnNamesCSV.Append('"');
                columnNamesCSV.Append(colNames[c]);
                columnNamesCSV.Append("\" ");
            }

            string uniqueString = null;
            if (isUnique)
                uniqueString = "UNIQUE";

            var sql = String.Format("CREATE {0} INDEX [{1}Index] ON [{1}] ({2})",
                                    uniqueString, tableName, columnNamesCSV.ToString());
            ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Drop a table from the database
        /// </summary>
        /// <param name="tableName"></param>
        public void DropTable(string tableName)
        {
            ExecuteNonQuery(string.Format("DROP TABLE [{0}]", tableName));
        }

        /// <summary>
        /// "Vacuum" the database, to defragment or clean up unused space
        /// </summary>
        public void Vacuum()
        {
            if (_open)
            {
                ExecuteNonQuery("VACUUM");
                // Close to force the vacuuming to take immediate effect
                // Then re-open to get back to where we were
                CloseDatabase();
                OpenDatabase(dbPath, IsReadOnly);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AsSQLString(DateTime value)
        {
            return value.ToString("yyyy-MM-dd hh:mm:ss"); 
        }

        /// <summary>
        /// Dispose of the SQLite connection.
        /// </summary>
        public void Dispose()
        {
            if (connection != null)
                connection.Dispose();
        }
    }
}