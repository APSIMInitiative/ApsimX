namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;
    using System.Linq;
    using System.Text;
    using System.Globalization;
    using System.Data.SQLite;
    using System.Data.SQLite.Linq;

    /// <summary>
    /// A custom marshaler that allows us to pass strings to the native SQLite DLL as
    /// the UTF-8 it expects. The default marshaling to a "string" mangles Unicode text
    /// on Windows.
    /// This code copied from https://www.codeproject.com/Articles/138614/Advanced-Topics-in-PInvoke-String-Marshaling
    /// </summary>
    public class UTF8Marshaler : ICustomMarshaler
    {
        static UTF8Marshaler static_instance;

        /// <summary>
        /// Marshals a managed string object into an allocated buffer holding UTF-8 bytes
        /// </summary>
        /// <param name="managedObj">The string object to be marshaled</param>
        /// <returns></returns>
        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;
            if (!(managedObj is string))
                throw new MarshalDirectiveException(
                       "UTF8Marshaler must be used on a string.");

            // not null terminated
            byte[] strbuf = Encoding.UTF8.GetBytes((string)managedObj);
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length + 1);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);

            // write the terminating null
            Marshal.WriteByte(buffer + strbuf.Length, 0);
            return buffer;
        }

        /// <summary>
        /// Marshals a native UTF-8 string into a managed string
        /// </summary>
        /// <param name="pNativeData">A char pointer to a native C-style UTF-8 string</param>
        /// <returns>A string object holding the managed string</returns>
        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            byte* walk = (byte*)pNativeData;

            // find the end of the string
            while (*walk != 0)
            {
                walk++;
            }
            int length = (int)(walk - (byte*)pNativeData);

            // should not be null terminated
            byte[] strbuf = new byte[length];
            // skip the trailing null
            Marshal.Copy((IntPtr)pNativeData, strbuf, 0, length);
            string data = Encoding.UTF8.GetString(strbuf);
            return data;
        }

        /// <summary>
        /// Cleans up the buffer used to hold the native UTF-8 string
        /// </summary>
        /// <param name="pNativeData">A pointer to the buffer to be freed</param>
        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeHGlobal(pNativeData);
        }

        /// <summary>
        /// </summary>
        /// <param name="managedObj"></param>
        public void CleanUpManagedData(object managedObj)
        {
        }

        /// <summary>
        /// Returns the size of the unmanaged data to be marshaled
        /// </summary>
        /// <returns></returns>
        public int GetNativeDataSize()
        {
            return -1;
        }

        /// <summary>
        /// Creates a singleton instance of the marshaler
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns>The marshaler</returns>
        public static ICustomMarshaler GetInstance(string cookie)
        {
            if (static_instance == null)
            {
                return static_instance = new UTF8Marshaler();
            }
            return static_instance;
        }
    }    

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
    public class SQLite : IDatabaseConnection
    {
        /// <summary>The sqlit e_ ok</summary>
        private const int SQLITE_OK = 0;
        /// <summary>The sqlit e_ row</summary>
        private const int SQLITE_ROW = 100;
        /// <summary>The sqlit e_ done</summary>
        private const int SQLITE_DONE = 101;
        /// <summary>The sqlit e_ integer</summary>
        private const int SQLITE_INTEGER = 1;
        /// <summary>The sqlit e_ float</summary>
        private const int SQLITE_FLOAT = 2;
        /// <summary>The sqlit e_ text</summary>
        private const int SQLITE_TEXT = 3;
        /// <summary>The sqlit e_ BLOB</summary>
        private const int SQLITE_BLOB = 4;
        /// <summary>The sqlit e_ null</summary>
        private const int SQLITE_NULL = 5;

        /// <summary>The sqlit e_ ope n_ readonly</summary>
        private const int SQLITE_OPEN_READONLY = 0x00000001;  /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ readwrite</summary>
        private const int SQLITE_OPEN_READWRITE = 0x00000002; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ create</summary>
        private const int SQLITE_OPEN_CREATE = 0x00000004; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ deleteonclose</summary>
        private const int SQLITE_OPEN_DELETEONCLOSE = 0x00000008; /* VFS only */
        /// <summary>The sqlit e_ ope n_ exclusive</summary>
        private const int SQLITE_OPEN_EXCLUSIVE = 0x00000010; /* VFS only */
        /// <summary>The sqlit e_ ope n_ autoproxy</summary>
        private const int SQLITE_OPEN_AUTOPROXY = 0x00000020; /* VFS only */
        /// <summary>The sqlit e_ ope n_ URI</summary>
        private const int SQLITE_OPEN_URI = 0x00000040; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ memory</summary>
        private const int SQLITE_OPEN_MEMORY = 0x00000080; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ mai n_ database</summary>
        private const int SQLITE_OPEN_MAIN_DB = 0x00000100; /* VFS only */
        /// <summary>The sqlit e_ ope n_ tem p_ database</summary>
        private const int SQLITE_OPEN_TEMP_DB = 0x00000200; /* VFS only */
        /// <summary>The sqlit e_ ope n_ transien t_ database</summary>
        private const int SQLITE_OPEN_TRANSIENT_DB = 0x00000400; /* VFS only */
        /// <summary>The sqlit e_ ope n_ mai n_ journal</summary>
        private const int SQLITE_OPEN_MAIN_JOURNAL = 0x00000800; /* VFS only */
        /// <summary>The sqlit e_ ope n_ tem p_ journal</summary>
        private const int SQLITE_OPEN_TEMP_JOURNAL = 0x00001000; /* VFS only */
        /// <summary>The sqlit e_ ope n_ subjournal</summary>
        private const int SQLITE_OPEN_SUBJOURNAL = 0x00002000; /* VFS only */
        /// <summary>The sqlit e_ ope n_ maste r_ journal</summary>
        private const int SQLITE_OPEN_MASTER_JOURNAL = 0x00004000; /* VFS only */
        /// <summary>The sqlit e_ ope n_ nomutex</summary>
        private const int SQLITE_OPEN_NOMUTEX = 0x00008000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ fullmutex</summary>
        private const int SQLITE_OPEN_FULLMUTEX = 0x00010000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ sharedcache</summary>
        private const int SQLITE_OPEN_SHAREDCACHE = 0x00020000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ privatecache</summary>
        private const int SQLITE_OPEN_PRIVATECACHE = 0x00040000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ wal</summary>
        private const int SQLITE_OPEN_WAL = 0x00080000; /* VFS only */

        private SQLiteConnection connection;

        /// <summary>The _open</summary>
        [NonSerialized]
        private bool _open; //whether or not the database is open
        /// <summary>path to the database</summary>
        [NonSerialized]
        private string dbPath; 

        // Windows LoadLibrary entry point
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        // Windows FreeLibrary entry point
        //[DllImport("kernel32.dll")]
        //private static extern bool FreeLibrary(IntPtr hModule);       

        // Static constructor to allow us to pre-load the correct dll (32 vs. 64 bit) on Windows
        static SQLite()
        {
            if (ProcessUtilities.CurrentOS.IsWindows && ProcessUtilities.CurrentOS.Is64BitProcess)
            {
                //string DllPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "x64", "sqlite3.dll");
                //IntPtr lib = LoadLibrary(DllPath);
            }
        }

        /// <summary>Property to return true if the database is open.</summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get { return _open; } }

        /// <summary>Property to return true if the database is readonly.</summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>Return true if the database is in-memory</summary>
        public bool IsInMemory { get; private set; } = false;

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
            connection = new SQLiteConnection($"Data Source={path}");
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

            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a query and stores the results in
        /// a DataTable
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <returns>DataTable of results</returns>
        public System.Data.DataTable ExecuteQuery(string query)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    DataTable result = new DataTable();
                    result.Load(reader);
                    return result;
                }
            }
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

            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public List<string> GetColumnNames(string tableName)
        {
            List<string> columnNames = new List<string>();

            string sql = $"PRAGMA table_info({tableName})";
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
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

            foreach (var columnName in columnNames)
            {
                if (sql[sql.Length-1] == ']')
                    sql.Append(',');
                sql.Append('[');
                sql.Append(columnName);
                sql.Append(']');
            }
            sql.Append(") VALUES (");

            foreach (var columnName in columnNames)
            {
                if (sql[sql.Length - 1] == '?')
                    sql.Append(',');
                sql.Append('?');
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
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    foreach (object value in row)
                        command.Parameters.AddWithValue(null, value);
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

                sql.Append("[");
                sql.Append(colNames[c]);
                sql.Append("] ");
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

                columnNamesCSV.Append("[");
                columnNamesCSV.Append(colNames[c]);
                columnNamesCSV.Append("] ");
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
    }
}