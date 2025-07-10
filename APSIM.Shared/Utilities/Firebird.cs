//-----------------------------------------------------------------------
// Firebird database connection wrapper
//-----------------------------------------------------------------------

using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace APSIM.Shared.Utilities
{
    /// <summary>A class representing an exception thrown by this library.</summary>
    [Serializable]
    public class FirebirdException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="FirebirdException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public FirebirdException(string message) :
            base(message)
        {

        }
    }

    /// <summary>
    /// A wrapper for a Firebird database connection
    /// </summary>
    /// <remarks>
    /// Although implementing IDatabaseConnection, this wrapper doesn't just provide 
    /// a single connection. The Firebird architecture does not support mulitple 
    /// threads using the same connection, so what we do here is maintain a pool of 
    /// connections, one for each thread.
    /// Transaction logic is all handled internally. External calls to "BeginTransaction"
    /// and "EndTransaction" are ignored.
    /// </remarks>
    [Serializable]
    public class Firebird : IDatabaseConnection
    {
        /// <summary>
        /// Pool of Firebird connections, indexed by Thread 
        /// </summary>
        private Dictionary<Thread, FbConnection> connectionPool = new Dictionary<Thread, FbConnection>();

        /// <summary>
        /// Holds the connection string. 
        /// Kept as a class member so we can re-use it for each Connection we create.
        /// </summary>
        private string connectionString;

        /// <summary>
        /// Provides the Firebird connection for the current thread. Opens a new
        /// connection if one has not already been established
        /// </summary>
        private FbConnection fbDBConnection
        { 
            get
            {
                Thread t = Thread.CurrentThread;
                FbConnection connection;
                if (!connectionPool.TryGetValue(t, out connection))
                {
                    CleanConnectionPool();
                    connection = new FbConnection();
                    connectionPool.Add(t, connection);
                }
                return connection;
            }
        }

        /// <summary>
        /// Whether embedded server or not
        /// Set the ServerType to FbServerType.Embedded for connection to the embedded server or
        /// FbServerType.Default to operate with "remote" (though possibly localhost) server.
        /// </summary>
        public FbServerType fbDBServerType { get; private set; } = FbServerType.Embedded;

        /// <summary>Property to return true if the database connection is open.</summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get { return fbDBConnection.State == ConnectionState.Open; } }

        /// <summary>Property to return true if the database is readonly.</summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>Begin a transaction.</summary>
        public void BeginTransaction()
        {
        } 

        /// <summary>End a transaction.</summary>
        public void EndTransaction()
        {
        }

        /// <summary>Opens or creates Firebird database with the specified path</summary>
        /// <param name="path">Path to Firebird database</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <exception cref="FirebirdException"></exception>
        public void OpenDatabase(string path, bool readOnly)
        {
            // TODO: somewhere here I need to allow server connections

            if (!readOnly)
            {
                if (!File.Exists(path))
                {
                    // create a new database
                    FbConnection.CreateDatabase(GetConnectionString(path, "localhost", "SYSDBA", "masterkey"), 4096, false, true);
                }
            }
            OpenSQLConnection(path, "localhost", "SYSDBA", "masterkey");
            IsReadOnly = readOnly;
        }

        private FbTransaction OpenTransaction()
        {
            if (!connectionPool.TryGetValue(Thread.CurrentThread, out var connection))
            {
                throw new FirebirdException("Cannot begin Transaction; the Firebird connection has not been initialised on this thread.");
            }
            var transactionOptions = new FbTransactionOptions()
            {
                TransactionBehavior = FbTransactionBehavior.Concurrency |
                          FbTransactionBehavior.Wait |
                          FbTransactionBehavior.NoAutoUndo,
                WaitTimeout = new TimeSpan(0, 0, 15) // allow up to 15 seconds to resolve wait conditions
            };
            return connection.BeginTransaction(transactionOptions);
        }

        private void CloseTransaction(ref FbTransaction transaction)
        {
            if (transaction != null)
            {
                if (transaction.Connection != null)
                    transaction.Commit();
                transaction.Dispose();
                transaction = null;
            }

        }

        /// <summary>
        /// Build a connection string
        /// </summary>
        /// <param name="dbpath"></param>
        /// <param name="source"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        private string GetConnectionString(string dbpath, string source, string user, string pass)
        {
            FbConnectionStringBuilder cs = new FbConnectionStringBuilder();

            //If Not fbDBServerType = FbServerType.Embedded Then
            cs.DataSource = source;
            cs.Password = pass;
            cs.UserID = user;
            cs.Port = 3050;
            // End If

            cs.Pooling = true;
            cs.Database = dbpath;
            cs.Charset = "UTF8";
            cs.ConnectionLifeTime = 30;
            cs.ServerType = fbDBServerType;
            cs.ClientLibrary = "fbclient.dll";
            // cs.Pooling = false; // TEST

            string connstr = cs.ToString();

            if (cs != null)
            {
                cs = null;
            }
            return connstr;
        }

        /// <summary>Closes the Firebird database</summary>
        public void CloseDatabase()
        {
            foreach (var connData in connectionPool)
            {
                var connection = connData.Value;
                if (connection.State == ConnectionState.Open)
                    connection.Close();
                connection.Dispose();
            }
            connectionPool.Clear();
        }

        /// <summary>
        /// Open the Firebird SQL connection
        /// </summary>
        /// <param name="dbpath">Path to database</param>
        /// <param name="source">localhost or server name</param>
        /// <param name="user">db user name</param>
        /// <param name="pass">db password</param>
        /// <returns>True if opened</returns>
        private bool OpenSQLConnection(string dbpath, string source, string user, string pass)
        {
            try
            {
                if (fbDBConnection.State == ConnectionState.Closed)
                {
                    connectionString = GetConnectionString(dbpath, source, user, pass);
                    fbDBConnection.ConnectionString = connectionString;
                    fbDBConnection.Open();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new FirebirdException("Cannot open database connection To " + dbpath + "!\r\n" + ex.Message);
            }
        }

        /// <summary>Lock object.</summary>
        private readonly object poolLockObject = new object();


        private void CleanConnectionPool()
        {
            lock (poolLockObject)
            {
                foreach (var connData in connectionPool)
                {
                    Thread thread = connData.Key as Thread;
                    if (!thread.IsAlive)
                    {
                        var connection = connData.Value;
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                        connection.Dispose();
                        connectionPool.Remove(thread);
                    }
                }
            }
        }
        private bool EnsureOpen()
        {
            if (!IsOpen)
            {
                fbDBConnection.ConnectionString = connectionString;
                try
                {
                    fbDBConnection.Open();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Executes a query that returns no results</summary>
        /// <param name="query">SQL query to execute</param>
        public void ExecuteNonQuery(string query)
        {
            if (EnsureOpen())
            {
                query = AdjustQuotedFields(query);
                FbTransaction transaction = OpenTransaction();
                try
                {
                    using (FbCommand myCmd = new FbCommand(query, fbDBConnection, transaction))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new FirebirdException("Cannot execute the SQL statement\r\n " + query + "\r\n" + ex.Message);
                }
                finally
                {
                    CloseTransaction(ref transaction);
                }
            }
            else
                throw new FirebirdException("Firebird database is not open.");
        }

        /// <summary>Executes a query that returns a single value</summary>
        /// <param name="query">SQL query to execute</param>
        public object ExecuteScalar(string query)
        {
            object result = null;
            if (EnsureOpen())
            {
                query = AdjustQuotedFields(query);
                FbTransaction transaction = OpenTransaction();
                try
                {
                    using (FbCommand myCmd = new FbCommand(query, fbDBConnection, transaction))
                    {
                        myCmd.CommandType = CommandType.Text;
                        result = myCmd.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    throw new FirebirdException("Cannot execute the SQL statement\r\n " + query + "\r\n" + ex.Message);
                }
                finally
                {
                    CloseTransaction(ref transaction);
                }
            }
            else
                throw new FirebirdException("Firebird database is not open.");
            return result;
        }

        private string ExtractAlias(string s)
        {
            string result = s.Replace("\"", "");
            int i = result.IndexOf(" AS ", StringComparison.InvariantCultureIgnoreCase);
            if (i > -1)
                result = result.Substring(i + 4, result.Length - (i + 4));
            return result;
        }

        private List<string> ParseFieldNames(string sql)
        {
            sql = sql.Replace(Environment.NewLine, " ");
            int start = sql.IndexOf("SELECT ", StringComparison.InvariantCultureIgnoreCase) + 7;
            int end = sql.IndexOf(" FROM ", StringComparison.InvariantCultureIgnoreCase);
            if (start < 0 || end < 0)
                return new List<string>();

            string selects = sql.Substring(start, end - start);
            var result = selects.Split(',').Select(s => ExtractAlias(s)).ToList();
            return result;
        }

        /// <summary>
        /// Executes a query and stores the results in a DataTable
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <returns>DataTable of results</returns>
        public System.Data.DataTable ExecuteQuery(string query)
        {
            DataTable dt = null;
            if (EnsureOpen())
            {
                query = AdjustQuotedFields(query);
                dt = new DataTable();
                FbTransaction transaction = OpenTransaction();
                try
                {
                    using (FbCommand myCmd = new FbCommand(query, fbDBConnection, transaction))
                    {
                        List<string> fields = ParseFieldNames(query);
                        myCmd.CommandType = CommandType.Text;


                        using (FbDataAdapter da = new FbDataAdapter(myCmd))
                        {
                            da.Fill(dt);
                            // UGLY HACK
                            // The ADO driver for Firebird Embedded currently (version 9.1.1.) truncates the
                            // column names in the returned DataTable to 31 characters. This is an attempt to
                            // set the column names to their desired values.
                            // This is far from fool-proof; for example, having "*" in the select list
                            // could be problematic. That's one reason for comparing the number of columns with
                            // the number of "fields" we think we have.
                            if (fbDBServerType == FbServerType.Embedded && dt.Columns.Count == fields.Count)
                            {
                                int i = 0;
                                foreach (string field in fields)
                                {
                                    if (field.Length > 31)
                                    {
                                        dt.Columns[i].ColumnName = field;
                                    }
                                    i++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // this.CloseDatabase();
                    throw new FirebirdException("Cannot execute the SQL statement \r\n" + query + "\r\n" + ex.Message);
                }
                finally
                {
                    CloseTransaction(ref transaction);
                }
            }
            else
                throw new FirebirdException("Firebird database is not open.");
            return dt;
        }

        /// <summary>
        /// Executes a query and return a single integer value to caller. Returns -1 if not found.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="ColumnNumber">The column number.</param>
        /// <returns>The integer for the column (0-n) for the first row</returns>
        public int ExecuteQueryReturnInt(string query, int ColumnNumber)
        {
            if (!EnsureOpen())
                throw new FirebirdException("Firebird database is not open.");

            int ReturnValue = -1;
            DataTable data = ExecuteQuery(query);
            if (data != null && data.Rows.Count > 0)
            {
                DataRow dr = data.Rows[0];
                ReturnValue = Convert.ToInt32(dr[ColumnNumber], CultureInfo.InvariantCulture);
            }

            return ReturnValue;
        }

        /// <summary>Bind all parameters values to the specified query and execute the query.</summary>
        /// <param name="query">The query.</param>
        /// <param name="values">The values.</param>
        public void BindParametersAndRunQuery(string query, object[] values)
        {
            if (!EnsureOpen())
                throw new FirebirdException("Firebird database is not open.");

            FbTransaction transaction = OpenTransaction();
            try
            {
                using (FbCommand cmd = new FbCommand(query, fbDBConnection, transaction))
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (Convert.IsDBNull(values[i]) || values[i] == null)
                        {
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Text).Value = string.Empty;
                        }
                        // Enums have an underlying type of Int32, but we want to store
                        // their string representation, not their integer value
                        else if (values[i].GetType().IsEnum)
                        {
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Text).Value = values[i].ToString();
                        }
                        else if (values[i].GetType() == typeof(DateTime))
                        {
                            DateTime d = (DateTime)values[i];
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Text).Value = d.ToString("dd.MM.yyyy, hh:mm:ss.000");
                        }
                        else if (values[i].GetType() == typeof(int))
                        {
                            int integer = (int)values[i];
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Integer).Value = integer;
                        }
                        else if (values[i].GetType() == typeof(float))
                        {
                            float f = (float)values[i];
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Float).Value = f;
                        }
                        else if (values[i].GetType() == typeof(double))
                        {
                            double d = (double)values[i];
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Double).Value = d;
                        }
                        else if (values[i].GetType() == typeof(byte[]))
                        {
                            byte[] bytes = values[i] as byte[];
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Binary).Value = bytes;
                        }
                        else
                            cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Text).Value = values[i] as string;
                    }
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }
            }
            finally
            {
                CloseTransaction(ref transaction);
            }
        }

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>A list of column names in column order</returns>
        public List<string> GetColumnNames(string tableName)
        {
            List<string> columnNames = new List<string>();
            EnsureOpen();
            if (EnsureOpen())
            {
                DataTable dt = fbDBConnection.GetSchema("Columns", new string[] { null, null, tableName });
                foreach (DataRow dr in dt.Rows)
                {
                    string colName = ((string)dr["COLUMN_NAME"]);
                    if (!String.IsNullOrEmpty(colName) && colName != "rowid")
                        columnNames.Add(colName);
                }
                
            }
            return columnNames;
        }

        /// <summary>Return a list of column names/column type tuples for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            var columnNames = new List<Tuple<string, Type>>();
            if (EnsureOpen())
            {
                using (DataTable dt = fbDBConnection.GetSchema("Columns", new string[] { null, null, tableName }))
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string colName = ((string)dr["COLUMN_NAME"]);
                        if (!String.IsNullOrEmpty(colName) && colName != "rowid")
                        {
                            string colType = ((string)dr["COLUMN_DATA_TYPE"]);
                            Type type = null;
                            if (colType == "integer")
                                type = typeof(int);
                            else if (colType == "varchar")
                                type = typeof(string);
                            else if (colType == "timestamp")
                                type = typeof(DateTime);
                            else if (colType == "double precision")
                                type = typeof(double);
                            columnNames.Add(new Tuple<string, Type>(colName, type));
                        }
                    }
                }
            }
            return columnNames;
        }

        /// <summary>Return a list of column names for the specified table</summary>
        /// <param name="tableName">The table name to get columns from.</param>
        public List<string> GetTableColumns(string tableName)
        {
            return GetColumnNames(tableName);
        }

        /// <summary>Return a list of table names</summary>
        /// <returns>A list of table names in sorted order (upper case)</returns>
        public List<string> GetTableNames()
        {
            List<string> tableNames = new List<string>();
            if (EnsureOpen())
            {
                using (DataTable userTables = fbDBConnection.GetSchema("Tables", new string[] { null, null, null, "TABLE" }))
                {
                    foreach (DataRow dr in userTables.Rows)
                    {
                        string tableName = ((string)dr["TABLE_NAME"]);
                        if (!String.IsNullOrEmpty(tableName) && tableName != "keyset")
                            tableNames.Add(tableName);
                    }
                }
            }
            return tableNames;
        }

        /// <summary>
        /// Get table and view names
        /// </summary>
        /// <returns></returns>
        public List<string> GetViewNames()
        {
            List<string> viewNames = new List<string>();
            if (EnsureOpen()) 
            {
                using (DataTable dt = fbDBConnection.GetSchema("Views"))
                {
                    foreach (DataRow dr in dt.Rows)
                        viewNames.Add(((string)dr["VIEW_NAME"]));
                }
            }
            return viewNames;
        }

        /// <summary>Return a list of table and view names</summary>
        /// <returns>A list of table and view names in sorted order (upper case)</returns>
        public List<string> GetTableAndViewNames()
        {
            return GetTableNames().Union(GetViewNames()).ToList();
        }

        /// <summary>Does the specified view exist?</summary>
        /// <param name="viewName">The view name to look for</param>
        public bool ViewExists(string viewName)
        {
            List<string> viewNames = GetViewNames();
            return viewNames.Contains(viewName);
        }

        /// <summary>Does the specified table exist?</summary>
        /// <param name="tableName">The table name to look for</param>
        public bool TableExists(string tableName)
        {
            List<string> tableNames = GetTableNames();
            return tableNames.Contains(tableName);
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
                result = ExecuteQueryReturnInt("SELECT COUNT(*) FROM \"" + tableName + "\"", 0) == 0;
            return result;
        }

        /// <summary>
        /// Drop (remove) columns from a table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="colsToRemove"></param>
        public void DropColumns(string tableName, IEnumerable<string> colsToRemove)
        {
            foreach (string columnName in colsToRemove)
                DropColumn(tableName, columnName);
        }

        /// <summary>
        /// Drop a single column from a table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="colToRemove"></param>
        private void DropColumn(string tableName, string colToRemove)
        {
            this.ExecuteNonQuery("ALTER TABLE \"" + tableName + "\" DROP \"" + colToRemove + "\"");
        }

        /// <summary>
        /// Do an ALTER on the db table and add a column
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnName">The column to add</param>
        /// <param name="columnType">The db column type</param>
        public void AddColumn(string tableName, string columnName, string columnType)
        {
            string colName = columnName;
            if (colName.Length > 63)
            {
                throw new FirebirdException("Unable to add a column named " + columnName + "because the name is too long.");
            }

            string sql;
            if (FieldExists(tableName, colName))
                DropColumn(tableName, colName);

            sql = "ALTER TABLE \"" + tableName + "\" ADD \"" + colName + "\" " + columnType;
            this.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Checks if the field exists
        /// </summary>
        /// <param name="table"></param>
        /// <param name="fieldname"></param>
        /// <returns>True if the field exists in the database</returns>
        public bool FieldExists(string table, string fieldname)
        {
            if (EnsureOpen())
            {
                DataTable dt = fbDBConnection.GetSchema("Columns", new string[] { null, null, table, fieldname });
                return dt.Rows.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnNames">Column names</param>
        /// <returns></returns>
        public string CreateInsertSQL(string tableName, List<string> columnNames)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO \"");
            sql.Append(tableName);
            sql.Append("\" (");

            for (int i = 0; i < columnNames.Count; i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.Append("\"");
                string columnName = columnNames[i];
                if (columnName.Length > 63)
                {
                    throw new FirebirdException("Unable to add a column named " + columnName + "because the name is too long.");
                }
                sql.Append(columnName); 
                sql.Append("\"");
            }
            sql.Append(") VALUES (");

            for (int i = 0; i < columnNames.Count; i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.Append('@' + (i + 1).ToString());
            }

            sql.Append(')');

            return sql.ToString();
        }

        /// <summary>
        /// Insert a range of rows
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public int InsertRows(string tableName, List<string> columnNames, List<object[]> values)
        {
            if (!EnsureOpen())
                throw new FirebirdException("Firebird database is not open.");

            {
                int index = 0;
                try
                {
                    // Create an insert query
                    string sql = CreateInsertSQL(tableName, columnNames);
                    for (index = 0; index < values.Count; index++)
                    {
                        BindParametersAndRunQuery(sql, values[index]);
                    }
                }
                catch (Exception ex)
                {
                    throw new FirebirdException("Exception " + ex.Message + "\r\nCannot insert row for " + tableName + " in InsertRows():" + String.Join(", ", values[index].ToString()).ToArray());
                }
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
            if (!EnsureOpen())
                throw new FirebirdException("Firebird database is not open.");

            var columnNames = table.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList<string>();
            var sql = CreateInsertSQL(table.TableName, columnNames);
            FbTransaction transaction = OpenTransaction();
            FbCommand cmd = new FbCommand(sql, fbDBConnection, transaction);
            cmd.CommandType = CommandType.Text;
            int i = 1;
            foreach (DataColumn column in table.Columns)
            {
                FbParameter newParam;

                // Enums have an underlying type of Int32, but we want to store
                // their string representation, not their integer value
                if (column.DataType.IsEnum)
                {
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Text);
                }
                else if (column.DataType == typeof(DateTime))
                {
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Date);
                }
                else if (column.DataType == typeof(int))
                {
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Integer);
                }
                else if (column.DataType == typeof(float))
                {
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Float);
                }
                else if (column.DataType == typeof(double))
                {
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Double);
                }
                else if (column.DataType == typeof(byte[]))
                {
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Binary);
                }
                else if (table.TableName.StartsWith("_"))
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Text);
                else
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.VarChar);
                newParam.IsNullable = true;
                i++;
            }
            try
            {
                cmd.Prepare();
            }
            catch (Exception)
            {
                CloseTransaction(ref transaction);
                cmd.Transaction = null;
                throw;
            }
            return cmd;
        }

        /// <summary>
        /// Executes a previously prepared bindable query, inserting a new set of parameters
        /// </summary>
        /// <param name="bindableQuery">The prepared query to be executed</param>
        /// <param name="values">The values to be inserted by using the query</param>
        public void RunBindableQuery(object bindableQuery, IEnumerable<object> values)
        {
            if (!EnsureOpen())
                throw new FirebirdException("Firebird database is not open.");

            FbCommand cmd = (FbCommand)bindableQuery;
            int i = 0;
            foreach (var value in values)
            {
                // Enums have an underlying type of Int32, but we want to store
                // their string representation, not their integer value
                if (value.GetType().IsEnum)
                    cmd.Parameters[i].Value = value.ToString();
                else if ((value.GetType() == typeof(string)) && (cmd.Parameters[i].FbDbType == FbDbType.VarChar))
                {
                    string val = (value as string).Substring(0, Math.Min(50, (value as string).Length)); // Using VARCHAR(50); truncate to 50 characters 
                    // Handles a problem that should be dealt with elsewhere: with Excel input, the database
                    // table may have already been created, but the Excel reader might assign data types to columns
                    // that differ from those already present. Databases actually handle this reasonably well,
                    // but Firebird will throw in Exception if an attempt is made to set what it thinks is a double
                    // field when the provided value is an empty string.
                    if (String.IsNullOrEmpty(val))
                        cmd.Parameters[i].Value = null;
                    else
                        cmd.Parameters[i].Value = val;
                }
                else
                    cmd.Parameters[i].Value = value;
                i++;
            }
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Finalises and destroys a prepared bindable query
        /// </summary>
        /// <param name="bindableQuery">The query to be finalised</param>
        public void FinalizeBindableQuery(object bindableQuery)
        {
            FbCommand command = (FbCommand)bindableQuery;
            if (command.Transaction != null)
            {
                FbTransaction transaction = command.Transaction;
                CloseTransaction(ref transaction);
                command.Transaction = null;
            }
            command.Dispose();
        }

        /// <summary>Convert .NET type into an Firebird type</summary>
        public string GetDBDataTypeName(object value)
        {
            // Convert the value we found above into an Firebird data type string and return it.
            Type type = null;
            if (value == null)
                return null;
            else
                type = value.GetType();

            return GetDBDataTypeName(type);
        }

        /// <summary>Convert .NET type into an Firebird type</summary>
        public string GetDBDataTypeName(Type type)
        {
            return GetDBDataTypeName(type, false);
        }


        /// <summary>Convert .NET type into an Firebird type</summary>
        public string GetDBDataTypeName(Type type, bool allowLongStrings)
        {
            if (type == null)
                return "INTEGER";
            else if (type.ToString() == "System.DateTime")
                return "TIMESTAMP";
            else if (type.ToString() == "System.Int32")
                return "INTEGER";
            else if (type.ToString() == "System.Single")
                return "FLOAT";
            else if (type.ToString() == "System.Double")
                return "DOUBLE PRECISION";
            else if (allowLongStrings)
                return "BLOB SUB_TYPE TEXT";
            else
                return "VARCHAR(50)";
        }

        /// <summary>Convert Firebird type into .NET type.</summary>
        public Type GetTypeFromFirebirdType(string firebirdType)
        {
            if (firebirdType == null)
                return typeof(int);
            else if (firebirdType == "TIMESTAMP")
                return typeof(DateTime);
            else if (firebirdType == "INTEGER")
                return typeof(int);
            else if (firebirdType == "FLOAT")
                return typeof(float);
            else if (firebirdType == "DOUBLE PRECISION")
                return typeof(double);
            else
                return typeof(string);
        }

        /// <summary>Create the new table</summary>
        public void CreateTable(string tableName, List<string> colNames, List<string> colTypes)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append("CREATE TABLE \"" + tableName + "\" (");
            sql.Append("\"rowid\" bigint generated by default as identity primary key");
            for (int c = 0; c < colNames.Count; c++)
            {
                sql.Append(',');

                string columnName = colNames[c];
                sql.Append("\"");
                if (columnName.Length > 63)
                    throw new FirebirdException("Unable to add a column named " + columnName + "because the name is too long.");
                sql.Append(columnName);
                sql.Append("\" ");
                if (colTypes[c] == null)
                    sql.Append("INTEGER");
                else
                    sql.Append(colTypes[c]);
            }

            sql.Append(')');
            this.ExecuteNonQuery(sql.ToString());
        }

        /// <summary>Create a new table</summary>
        public void CreateTable(DataTable table)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append("CREATE TABLE \"" + table.TableName + "\" (");
            sql.Append("\"rowid\" bigint generated by default as identity primary key");
            var columnNames = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                string columnName = column.ColumnName;
                if (columnName.Length > 63)
                    throw new FirebirdException("Unable to add a column named " + columnName + "because the name is too long.");
                columnNames.Add(columnName);
                sql.Append(',');
                sql.Append("\"");
                sql.Append(column.ColumnName);
                sql.Append("\" ");
                sql.Append(GetDBDataTypeName(column.DataType));
            }

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

                columnNamesCSV.Append("\"");
                columnNamesCSV.Append(colNames[c]);
                columnNamesCSV.Append("\" ");
            }

            string uniqueString = null;
            if (isUnique)
                uniqueString = "UNIQUE";

            var sql = String.Format("CREATE {0} INDEX \"{1}Index\" ON \"{1}\" ({2})",
                                    uniqueString, tableName, columnNamesCSV.ToString());
            ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Drop a table from the database
        /// </summary>
        /// <param name="tableName"></param>
        public void DropTable(string tableName)
        {
            this.ExecuteNonQuery(string.Format("DROP TABLE \"{0}\"", tableName));
        }

        /// <summary>
        /// Change any [] fields in the sql that may be remnants of other sql
        /// </summary>
        /// <param name="sql">The source SQL</param>
        /// <returns>Correctly quoted SQL for Firebird</returns>
        private string AdjustQuotedFields(string sql)
        {
            return sql.Replace("[", "\"").Replace("]", "\"");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AsSQLString(DateTime value)
        {
            // "dd.MM.yyyy, hh:mm:ss.000"
            return string.Format("'{0, 1}.{1, 1}.{2, 1:d4}, {3, 1}:{4, 1}:{5, 1}.000'", value.Day, value.Month, value.Year, value.Hour, value.Minute, value.Second);
        }

        private DataTable msgTable = null;

        /// <summary>
        /// Indicates that writing to the database has concluded (for the moment).
        /// Provides a chance to clean up any buffers still in use.
        /// </summary>
        public void EndWriting()

        {
            WriteMsgTable();
        }

        private void WriteMsgTable()
        {
            if (msgTable != null && msgTable.Rows.Count > 0)
            {
                using (FbCommand messageQuery = PrepareBindableInsertQuery(msgTable) as FbCommand)
                {
                    try
                    {
                        // Write all rows.
                        foreach (DataRow row in msgTable.Rows)
                            RunBindableQuery(messageQuery, row.ItemArray);
                        msgTable.Rows.Clear();
                    }
                    finally
                    {
                        var transaction = messageQuery.Transaction;
                        CloseTransaction(ref transaction);
                    }
                }
            }
        }

        /// <summary>Lock object.</summary>
        private readonly object msgLockObject = new object();

        /// <summary>
        /// Insert a single record from the "_Messages" table
        /// </summary>
        /// <remarks>
        /// Committing a single line at a time slows things down. We can deal with that
        /// by accumulating the records into a DataTable until we hit some arbitrary length 
        /// (say 250 rows), then write them all out at once. We need to be sure to write out
        /// whatever is in the table at the end of the run, as well.
        /// </remarks>
        /// <param name="table"></param>
        public void InsertMessageRecord(DataTable table)
        {
            lock (msgLockObject)
            {
                if (table.TableName != "_Messages")
                    throw new FirebirdException("Incorrect table passed to InsertMessageRecord");

                if (msgTable == null)
                    msgTable = table.Clone();
                foreach (DataRow row in table.Rows)
                    msgTable.ImportRow(row);

                if (msgTable.Rows.Count >= 250)
                    WriteMsgTable();
            }
        }

        /// <summary>
        /// Insert entire table using batch command
        /// I had expected this approach to be noticeably faster than using a prepared
        /// bindable query, but it actually seems to be slower. So I'm not going to use
        /// this at the moment, but things may change with newer versions of Firebird
        /// and its ADO driver, so I'm leaving it here just in case.
        /// </summary>
        /// <param name="table">Table with values to be inserted</param>
        public void InsertTableBatch(DataTable table)
        {
            if (!EnsureOpen())
                throw new FirebirdException("Firebird database is not open.");

            FbTransaction transaction = OpenTransaction();
            FbBatchCommand cmd = fbDBConnection.CreateBatchCommand();
            try
            {
                var columnNames = table.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList<string>();

                StringBuilder sql = new StringBuilder();
                sql.Append("INSERT INTO \"");
                sql.Append(table.TableName);
                sql.Append("\" (");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sql.Append(',');
                    sql.Append("\"");
                    string columnName = columnNames[i];
                    sql.Append(columnName);
                    sql.Append("\"");
                }
                sql.Append(") VALUES (");

                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sql.Append(", ");
                    sql.Append("@i");
                    sql.Append(i.ToString());
                }
                sql.Append(')');
                cmd.CommandText = sql.ToString();
                cmd.Transaction = transaction;
                foreach (DataRow row in table.Rows)
                {
                    FbParameterCollection batchRow = cmd.AddBatchParameters();
                    for (int i = 0; i < columnNames.Count; i++)
                    {
                        var value = row[i];
                        // Enums have an underlying type of Int32, but we want to store
                        // their string representation, not their integer value
                        if (value.GetType().IsEnum)
                            batchRow.Add("i" + i.ToString(), value.ToString());
                        else
                            batchRow.Add("i" + i.ToString(), value);
                    }
                }
                cmd.ExecuteNonQuery();

            }
            finally
            {
                cmd.Dispose();
                CloseTransaction(ref transaction);
            }
        }
    }
}
