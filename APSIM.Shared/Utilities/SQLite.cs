using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace APSIM.Shared.Utilities
{
    /// <summary>A class for accessing an SQLite database.</summary>
    [Serializable]
    public class SQLite : IDatabaseConnection
    {
        /// <summary>
        /// Connection of SQLite database
        /// </summary>
        [NonSerialized]
        private SqliteConnection _connection;
        /// <summary>Indicates whether or not the database is open</summary>
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
        public bool IsInMemory { get; private set; } = false;

        /// <summary>A lock object to prevent multiple threads from starting a transaction at the same time</summary>
        private readonly object transactionLock = new object();

        /// <summary>Begin a transaction. Any code between begin and end needs to be in a try-finally so that the lock
        /// is unlocked if there is an exception thrown.</summary>
        public void BeginTransaction()
        {
            Monitor.Enter(transactionLock);
            try {
                ExecuteNonQuery("BEGIN");
            }
            catch(Exception ex)
            {
                EndTransaction();
                throw new Exception(ex.Message);
            }
        }

        /// <summary>End a transaction.</summary>
        public void EndTransaction()
        {
            try
            {
                ExecuteNonQuery("END");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                Monitor.Exit(transactionLock);
            }
        }

        /// <summary>Opens or creates SQLite database with the specified path</summary>
        /// <param name="path">Path to SQLite database</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        public void OpenDatabase(string path, bool readOnly)
        {
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
                DefaultTimeout = 40000
            };
            _connection = new SqliteConnection(builder.ToString());
            _connection.Open();

            _open = true;
            dbPath = path;
            IsReadOnly = readOnly;
            IsInMemory = path.ToLower().Contains(":memory:");
        }

        /// <summary>Closes the SQLite database</summary>
        public void CloseDatabase()
        {
            if (_open)
            {
                if (_connection != null)
                {
                    try
                    {
                        _connection?.Close();
                        SqliteConnection.ClearPool(_connection);
                        _connection?.Dispose();
                    }
                    catch
                    {
                        Console.WriteLine("SQLite failed to close correctly when closing database.");
                    }
                    _connection = null;
                }
                _open = false;
            }
        }

        /// <summary>Executes a query that returns no results</summary>
        /// <param name="query">SQL query to execute</param>
        public void ExecuteNonQuery(string query)
        {
            using (SqliteCommand command = new SqliteCommand(query, _connection))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a query and stores the results in
        /// a DataTable
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <returns>DataTable of results</returns>
        public System.Data.DataTable ExecuteQuery(string query)
        {
            DataTable table = new DataTable();
            SqliteCommand cmd = new SqliteCommand(query, _connection);
            SqliteDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                // "Load" would be really simple to use here, but because SQLite doesn't support
                // true DATE fields, it doesn't handle returned dates well.
                //
                // The approach taken here is to examine the "type" of each column as it was
                // defined when the SQLite table was created, and create a DataColumn of a
                // compatible type. We then read the returned data row by row and add the values
                // to the resulting DataTable. Note that DataTables have stricter expectations
                // about "type" than does SQLite, and errors may occur if the data values do not
                // match the expected type. This may occur when column names were used
                // inconsistently across multiple simulations or across multiple files of
                // obeserved data imported from Excel.
                // We could possibly just treat all DataColumns as being of type "Object", but it's
                // probably better to let any dataype inconsistencies raise exceptions so that they
                // can be identified and corrected.
                // table.Load(reader);

                //get the number of returned columns
                int columnCount = reader.FieldCount;

                Type[] colTypes = new Type[columnCount];
                // Add datatable columns of appropriate type
                for (int i = 0; i < columnCount; i++)
                {
                    colTypes[i] = GetTypeFromSQLiteType(reader.GetDataTypeName(i));
                    table.Columns.Add(reader.GetName(i), colTypes[i]);
                }

                // Add the data rows
                object[] values = new object[columnCount];
                while (reader.Read())
                {
                    DataRow row = table.NewRow();
                    reader.GetValues(values);

                    for (int i = 0; i < values.Length; i++)
                    {
                        // This test is needed to handle some odd things that can happen
                        // when values are imported from multiple Excel files and column
                        // data types cannot be determined by the importer.
                        if (colTypes[i] != typeof(string) && (values[i] is string) && String.IsNullOrEmpty(values[i] as string))
                            row[i] = DBNull.Value;
                        else
                            row[i] = values[i];

                    }
                    table.Rows.Add(row);
                }
            }

            try
            {
                reader.Close();
                reader.DisposeAsync();

                cmd.DisposeAsync();
            }
            catch
            {
                Console.WriteLine("SQLite failed to dispose correctly.");
            }
            

            return table;
        }
        

        /// <summary>
        /// Executes a query and return a single integer value to caller. Returns -1 if not found.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="columnNumber">The column number.</param>
        /// <returns></returns>
        public int ExecuteQueryReturnInt(string query, int columnNumber)
        {
            using (SqliteCommand cmd = new SqliteCommand(query, _connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        return reader.GetInt32(columnNumber);
                    }
                    return -1;
                }
            }
        }

        /// <summary>Bind all parameters values to the specified query and execute the query.</summary>
        /// <param name="query">The query.</param>
        /// <param name="values">The values.</param>
        public void BindParametersAndRunQuery(SqliteCommand query, IEnumerable<object> values)
        {
            int i = 0;
            query.Parameters.Clear();
            foreach (var value in values)
            {
                // Enums have an underlying type of Int32, but we want to store
                // their string representation, not their integer value
                if (value.GetType().IsEnum)
                {
                    query.Parameters.AddWithValue($"@param{i + 1}", value.ToString());
                }
                // SQLite doesn't actually know about dates
                // Store dates as a string representation
                else if (value.GetType() == typeof(DateTime))
                {
                    DateTime d = (DateTime)value;
                    query.Parameters.AddWithValue($"@param{i + 1}", d.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                // Microsoft.Data.Sqlite doesn't let us store NaN values, so just store DBNull
                else if (value.GetType() == typeof(Double) && Double.IsNaN((double)value))
                {
                    query.Parameters.AddWithValue($"@param{i + 1}", DBNull.Value);
                }
                else
                {
                    query.Parameters.AddWithValue($"@param{i + 1}", value);
                }
                i++;
            }
            query.ExecuteNonQuery();
        }

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public List<string> GetColumnNames(string tableName)
        {
            List<string> colNames = new List<string>();
            string sql = $"select * from [{tableName}] LIMIT 0";
            using (SqliteCommand cmd = new SqliteCommand(sql, _connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                        colNames.Add(reader.GetName(i));
                }
            }
            return colNames;
        }

        /// <summary>Return a list of column names/column type tuples for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            var columns = new List<Tuple<string, Type>>();
            DataTable columnData = ExecuteQuery($"pragma table_info('{tableName}')");

            foreach (DataRow row in columnData.Rows)
                columns.Add(new Tuple<string,Type>(row["name"].ToString(), GetTypeFromSQLiteType(row["type"].ToString())));

            return columns;
        }

        /// <summary>Return a list of column names for the specified table</summary>
        /// <param name="tableName">The table name to get columns from.</param>
        public List<string> GetTableColumns(string tableName)
        {
            List<string> columns = new List<string>();
            DataTable columnData = ExecuteQuery($"pragma table_info('{tableName}')");

            foreach (DataRow row in columnData.Rows)
                columns.Add(row["name"].ToString());

            return columns;
        }

        /// <summary>Return a list of table names</summary>
        public List<string> GetTableNames()
        {
            List<string> tableNames = new List<string>();
            DataTable tableData = ExecuteQuery("SELECT * FROM sqlite_master");
            var names = DataTableUtilities.GetColumnAsStrings(tableData, "name", CultureInfo.InvariantCulture);
            var types = DataTableUtilities.GetColumnAsStrings(tableData, "type", CultureInfo.InvariantCulture);
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
            var names = DataTableUtilities.GetColumnAsStrings(tableData, "Name", CultureInfo.InvariantCulture);
            var types = DataTableUtilities.GetColumnAsStrings(tableData, "Type", CultureInfo.InvariantCulture);
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

        /// <summary>Does the specified view exist?</summary>
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
            if (!TableExists(tableName))
                return false;
            return ExecuteQueryReturnInt($"SELECT COUNT(*) FROM [{tableName}]", 0) == 0;
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

            string columnsSeparated = null;
            foreach (string columnName in updatedTableColumns)
            {
                if (columnsSeparated != null)
                    columnsSeparated += ",";
                columnsSeparated += $"[{columnName}]";
            }
            if (updatedTableColumns.Count > 0)
            {
                BeginTransaction();

                // Rename old table
                ExecuteNonQuery($"ALTER TABLE [{tableName}] RENAME TO [{tableName}_old]");

                // Creating the new table based on old table
                ExecuteNonQuery($"CREATE TABLE [{tableName}] AS SELECT {columnsSeparated} FROM [{tableName}_old]");

                // Drop old table
                ExecuteNonQuery($"DROP TABLE [{tableName}_old]");

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
            string sql = $"ALTER TABLE [{tableName}] ADD COLUMN [{columnName}] {columnType}";
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
            sql.Append($"INSERT INTO [{tableName}] (");

            for (int i = 0; i < columnNames.Count(); i++)
            {
                string columnName = columnNames.ElementAt(i);
                if (i > 0)
                    sql.Append(',');
                sql.Append($"\"{columnName}\"");
            }
            sql.Append(") VALUES (");

            for (int i = 0; i < columnNames.Count(); i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.Append($"@param{i + 1}");
            }

            sql.Append(')');

            return sql.ToString();
        }

        /// <summary>Create a prepared insert query</summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnNames">Column names</param>
        private SqliteCommand CreateInsertQuery(string tableName, List<string> columnNames)
        {
            string sql = CreateInsertSQL(tableName, columnNames);
            return new SqliteCommand(sql, _connection);
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
            // Create an insert query
            using (SqliteCommand preparedInsertQuery = CreateInsertQuery(tableName, columnNames))
            {
                for (int rowIndex = 0; rowIndex < values.Count; rowIndex++)
                    BindParametersAndRunQuery(preparedInsertQuery, values[rowIndex]);
            }
            return 0;
        }

        /// <summary>
        /// Prepares a bindable query for the insertion of all columns of a datatable into the database
        /// </summary>
        /// <param name="table">A DataTable to be inserted</param>
        /// <returns>A "handle" for the resulting query</returns>
        public object PrepareBindableInsertQuery(DataTable table)
        {
            IntPtr queryHandle = IntPtr.Zero;
            // Get a list of column names.
            var columnNames = table.Columns.Cast<DataColumn>().Select(col => col.ColumnName);
            var sql = CreateInsertSQL(table.TableName, columnNames);
            SqliteCommand command = new SqliteCommand(sql, _connection);
            command.Prepare();
            return command;
        }

        /// <summary>
        /// Executes a previously prepared bindable query, inserting a new set of parameters
        /// </summary>
        /// <param name="bindableQuery">The prepared query to be executed</param>
        /// <param name="values">The values to be inserted by using the query</param>
        public void RunBindableQuery(object bindableQuery, IEnumerable<object> values)
        {
            BindParametersAndRunQuery((SqliteCommand)bindableQuery, values);
        }

        /// <summary>
        /// Finalises and destroys a prepared bindable query
        /// </summary>
        /// <param name="bindableQuery">The query to be finalised</param>
        public void FinalizeBindableQuery(object bindableQuery)
        {
            (bindableQuery as SqliteCommand).Dispose();
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

        /// <summary>Convert an SQLite type into a .NET type</summary>
        public string GetDBDataTypeName(Type type)
        {
            if (type == null)
                return "integer";
            // Note that "date" is not actually an SQL datatype. However, we can declare a 
            // column to be of type "date" and SQLite will retain that information
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
            else if (type.ToString() == "System.String")
                return "text";
            else if (type.ToString() == "System.Byte[]")
                return "blob";
            else
                return "text";
        }

        /// <summary>Convert SQLite type into .NET type.</summary>
        public Type GetTypeFromSQLiteType(string sqliteType)
        {
            if (sqliteType == null)
                return typeof(int);
            string lcType = sqliteType.ToLower();
            // Note that "date" is not actually an SQL datatype. However, we can declare a 
            // column to be of type "date" and SQLite will retain that information
            if (lcType == "date")
                return typeof(DateTime);
            else if (lcType.Contains("int"))
                return typeof(int);
            else if (lcType.Contains("text") || lcType.Contains("char") || lcType.Contains("clob"))
                return typeof(string);
            else if (lcType.Contains("blob"))
                return typeof(byte[]);
            else if (lcType.Contains("real") || lcType.Contains("floa") || lcType.Contains("doub"))
                return typeof(double);
            else
                return typeof(object);
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

            sql.Insert(0, $"CREATE TABLE [{tableName}] (");
            sql.Append(')');
            ExecuteNonQuery(sql.ToString());
        }

        /// <summary>Create a new table</summary>
        public void CreateTable(DataTable table)
        {
            StringBuilder sql = new StringBuilder();

            var columnNames = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columnNames.Add(column.ColumnName);
                if (sql.Length > 0)
                    sql.Append(',');

                sql.Append("\"");
                sql.Append(column.ColumnName);
                sql.Append("\" ");
                sql.Append(GetDBDataTypeName(column.DataType));
            }

            sql.Insert(0, $"CREATE TABLE [{table.TableName}] (");
            sql.Append(')');
            ExecuteNonQuery(sql.ToString());

            List<object[]> rowValues = new List<object[]>();
            foreach (DataRow row in table.Rows)
                rowValues.Add(row.ItemArray);
            InsertRows(table.TableName, columnNames, rowValues);
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
                columnNamesCSV.Append($"\"{colNames[c]}\" ");
            }

            string uniqueString = null;
            if (isUnique)
                uniqueString = "UNIQUE";

            var sql = $"CREATE {uniqueString} INDEX [{tableName}Index] ON [{tableName}] ({columnNamesCSV.ToString()})";
            ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Drop a table from the database
        /// </summary>
        /// <param name="tableName"></param>
        public void DropTable(string tableName)
        {
            ExecuteNonQuery($"DROP TABLE [{tableName}]");
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
        /// Indicates that writing to the database has concluded (for the moment).
        /// Provides a chance to clean up any buffers still in use.
        /// </summary>
        public void EndWriting() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AsSQLString(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss"); 
        }
    }
}