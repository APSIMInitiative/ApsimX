//-----------------------------------------------------------------------
// Firebird database connection wrapper
//-----------------------------------------------------------------------

namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using FirebirdSql.Data.FirebirdClient;
    using System.IO;

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
    [Serializable]
    public class Firebird : IDatabaseConnection
    {
        /// <summary>
        /// Firebird connection properties 
        /// </summary>
        private FbConnection fbDBConnection = new FbConnection();

        private DataSet fbDBDataSet = new DataSet();

        //Set the ServerType to 1 for connect to the embedded server
        private FbServerType fbDBServerType = FbServerType.Embedded;

        /// <summary>Property to return true if the database is open.</summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get { return fbDBConnection.State == ConnectionState.Open; } }

        /// <summary>Property to return true if the database is readonly.</summary>
        public bool IsReadOnly { get; private set; }

        // Dictionary to allow the column number for a "long" column name to be accessed quickly
        private Dictionary<string, ColumnNameMap> colInfoMap = new Dictionary<string, ColumnNameMap>(StringComparer.OrdinalIgnoreCase);

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
                    // FbConnection.CreateDatabase(GetConnectionString(path, "localhost", "SYSDBA", "masterkey"), 4096, false, true);
                    FbConnection.CreateDatabase(GetConnectionString(path, "localhost", "SYSDBA", "masterkey"), true);
                    // TODO: may need to create tables here

                }
            }
            OpenSQLConnection(path, "localhost", "SYSDBA", "masterkey");

            IsReadOnly = readOnly;
        }

        /// <summary>
        /// Build a connection string
        /// </summary>
        /// <param name="dbpath"></param>
        /// <param name="source"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        protected string GetConnectionString(string dbpath, string source, string user, string pass)
        {
            FbConnectionStringBuilder cs = new FbConnectionStringBuilder();

            // If Not fbDBServerType = FbServerType.Embedded Then
            //cs.DataSource = source;
            cs.Password = pass;
            cs.UserID = user;
            //cs.Port = 3050;
            // End If

            cs.Pooling = true;
            cs.Database = dbpath;
            cs.Charset = "UTF8";
            cs.ConnectionLifeTime = 30;
            cs.ServerType = fbDBServerType;
            cs.ClientLibrary = "fbclient.dll";

            fbDBDataSet.Locale = CultureInfo.InvariantCulture;
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
            if (fbDBConnection.State == ConnectionState.Open)
            {
                fbDBConnection.Close();
            }
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
                    fbDBDataSet.Locale = CultureInfo.InvariantCulture;
                    fbDBConnection.ConnectionString = GetConnectionString(dbpath, source, user, pass);
                    fbDBConnection.Open();
                }
                if (TableExists("_ColumnInfo"))
                {
                    DataTable colInfoTable = ExecuteQuery("SELECT \"TableName\", \"ColumnName\", \"ColumnNumber\" FROM \"_ColumnInfo\" ORDER BY \"TableName\", \"ColumnNumber\"");
                    string currentTable = string.Empty;
                    ColumnNameMap currentMap = null;
                    foreach (DataRow row in colInfoTable.Rows)
                    {
                        string tableName = ((string)row[0]).Trim();
                        string columnName = ((string)row[1]).Trim();
                        int columnNumber = Convert.ToInt32(row[2], CultureInfo.InvariantCulture);
                        if (tableName != currentTable)
                        {
                            currentTable = tableName;
                            currentMap = new ColumnNameMap();
                            colInfoMap.Add(tableName, currentMap);
                        }
                        currentMap.ColDict.Add(columnName, columnNumber);
                        currentMap.LongNames.Insert(columnNumber, columnName);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new FirebirdException("Cannot open database connection To " + dbpath + "!\r\n" + ex.Message);
            }
        }

        /// <summary>Executes a query that returns no results</summary>
        /// <param name="query">SQL query to execute</param>
        public void ExecuteNonQuery(string query)
        {
            if (!IsOpen)
            {
                fbDBConnection.Open();
            }
            if (IsOpen)
            {
                var transactionOptions = new FbTransactionOptions()
                {
                    TransactionBehavior = FbTransactionBehavior.Concurrency |
                                          FbTransactionBehavior.Wait |
                                          FbTransactionBehavior.NoAutoUndo
                };
                FbTransaction transaction = fbDBConnection.BeginTransaction(transactionOptions);
                query = AdjustQuotedFields(query);
                FbCommand myCmd = new FbCommand(query, fbDBConnection, transaction);
                myCmd.CommandType = CommandType.Text;

                try
                {
                    myCmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Dispose();
                    // this.CloseDatabase();
                    throw new FirebirdException("Cannot execute the SQL statement\r\n " + query + "\r\n" + ex.Message);
                }
                finally
                {
                    if (myCmd != null)
                    {
                        // transaction.Dispose();
                        myCmd.Dispose();
                        myCmd = null;
                    }
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
            if (!IsOpen)
            {
                fbDBConnection.Open();
            }
            if (IsOpen)
            {
                //FbTransaction transaction = fbDBConnection.BeginTransaction();
                query = AdjustQuotedFields(query);
                FbCommand myCmd = new FbCommand(query, fbDBConnection);
                myCmd.CommandType = CommandType.Text;

                try
                {
                    result = myCmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new FirebirdException("Cannot execute the SQL statement\r\n " + query + "\r\n" + ex.Message);
                }
                finally
                {
                    if (myCmd != null)
                    {
                        myCmd.Dispose();
                        myCmd = null;
                    }
                }
            }
            else
                throw new FirebirdException("Firebird database is not open.");
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

            if (!IsOpen)
                fbDBConnection.Open();  // assume the connection string has been set already
            if (IsOpen)
            {
                query = AdjustQuotedFields(query);
                dt = new DataTable();
                FbCommand myCmd = new FbCommand(query, fbDBConnection);
                myCmd.CommandType = CommandType.Text;

                try
                {
                    FbDataAdapter da = new FbDataAdapter(myCmd);
                    da.Fill(dt);
                }
                catch (Exception ex)
                {
                    // this.CloseDatabase();
                    throw new FirebirdException("Cannot execute the SQL statement \r\n" + query + "\r\n" + ex.Message);
                }
                finally
                {
                    if (myCmd != null)
                    {
                        myCmd.Dispose();
                        myCmd = null;
                    }
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
            if (!IsOpen)
                throw new FirebirdException("Firebird database is not open.");

            int ReturnValue = -1;
            DataTable data = ExecuteQuery(query);
            if (data != null)
            {
                DataRow dr = data.Rows[0];
                ReturnValue = Convert.ToInt32(dr[ColumnNumber], CultureInfo.InvariantCulture);
            }

            return ReturnValue;
        }

        /// <summary>Bind all parameters values to the specified query and execute the query.</summary>
        /// <param name="transaction">The Firebird transaction</param>
        /// <param name="query">The query.</param>
        /// <param name="values">The values.</param>
        public void BindParametersAndRunQuery(FbTransaction transaction, string query, object[] values)
        {
            using (FbCommand cmd = fbDBConnection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;
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

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>A list of column names in column order (uppercase)</returns>
        public List<string> GetColumnNames(string tableName)
        {
            List<string> columnNames = new List<string>();

            if (IsOpen)
            {
                string sql = "select rdb$field_name from rdb$relation_fields ";
                sql += "where rdb$relation_name = '" + tableName + "' ";
                sql += "order by rdb$field_position; ";

                DataTable dt = ExecuteQuery(sql);
                foreach (DataRow dr in dt.Rows)
                {
                    string colName = GetLongColumnName(tableName, (string)dr[0]).Trim();
                    if (!String.IsNullOrEmpty(colName))
                        columnNames.Add(colName);
                }
            }
            return columnNames;
        }

        /// <summary>Gets the long name of a column</summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="colNo">Number of the column</param>
        /// <returns>A string holding the long name of the column</returns>
        public string GetLongColumnName(string tableName, int colNo)
        {
            if (colNo >= 0 && colInfoMap.TryGetValue(tableName, out ColumnNameMap nameMap) && colNo < nameMap.LongNames.Count)
                return nameMap.LongNames[colNo];
            else
                return "";
            /*
            string sql = "SELECT [ColumnName] FROM [_ColumnInfo] Where [TableName] = \'" +
                          tableName + "\' AND [ColumnNumber] = " + colNo.ToString();
            DataTable dt = ExecuteQuery(sql);
            if (dt.Rows.Count > 0)
                return ((string)dt.Rows[0][0]).Trim();
            else
                return "";
            */
        }

        /// <summary>Gets the short name of an existing column</summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="longName">Long name of the column</param>
        /// <returns>A string holding the short name of the column</returns>
        public string GetShortColumnName(string tableName, string longName)
        {
            if (tableName.StartsWith("_") || string.IsNullOrEmpty(longName)
                || longName.Equals("SimulationID", StringComparison.OrdinalIgnoreCase)
                || longName.Equals("SimulationName", StringComparison.OrdinalIgnoreCase)
                || longName.Equals("CheckpointID", StringComparison.OrdinalIgnoreCase)
                || longName.Equals("CheckpointName", StringComparison.OrdinalIgnoreCase))
                return longName;
            else if (colInfoMap.TryGetValue(tableName, out ColumnNameMap nameMap) && nameMap.ColDict.TryGetValue(longName, out int colNumber))
                return "COL_" + colNumber.ToString();
            else
                return "";
        }

        /// <summary>Gets the long name of an existing column</summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="shortName">Short name of the column</param>
        /// <returns>A string holding the long name of the column</returns>
        public string GetLongColumnName(string tableName, string shortName)
        {
            if (string.IsNullOrEmpty(shortName) || tableName.StartsWith("_") || !shortName.StartsWith("COL_"))
                return shortName;
            else if (Int32.TryParse(shortName.Substring(4), out int colNo))
                return GetLongColumnName(tableName, colNo);
            else
                return "";
        }

        /// <summary>
        /// Get the column number for a specifed column name
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="colName">Name of the column</param>
        /// <returns></returns>
        public int GetColumnNumber(string tableName, string colName)
        {
            if (colInfoMap.TryGetValue(tableName, out ColumnNameMap nameMap) && (nameMap.ColDict.TryGetValue(colName, out int colNumber)))
                return colNumber;
            else
                return -1;

            /*
            string sql = "SELECT [ColumnNumber] FROM [_ColumnInfo] Where [TableName] = \'" +
                          tableName + "\' AND [ColumnName] = '" + colName + "'";
            object result = ExecuteScalar(sql);
            if (result == null)
                return -1;
            else
                return (int)result;
            */
        }

        /// <summary>Return a list of column names/column type tuples for a table. Never returns null.</summary>
        /// <param name="tableName">The table name to return column names for.</param>
        /// <returns>Can return an empty list but never null.</returns>
        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            var columnNames = new List<Tuple<string, Type>>();

            if (IsOpen)
            {
                // We could test for VARCHAR and BLOB field types, but as a shortcut, just testing for whether there is a non-null value
                // for the character_set_id seems to suffice.
                string sql = "SELECT R.rdb$field_name FROM rdb$relation_fields R JOIN rdb$fields F on F.RDB$FIELD_NAME = R.RDB$FIELD_SOURCE "
                           + "WHERE rdb$relation_name = '" + tableName + "' AND NOT(F.RDB$CHARACTER_SET_ID IS NULL) "
                           + "order by R.rdb$field_position; ";
                DataTable dt = ExecuteQuery(sql);
                foreach (DataRow dr in dt.Rows)
                {
                    string colName = GetLongColumnName(tableName, (string)dr[0]).Trim();
                    if (!String.IsNullOrEmpty(colName))
                        columnNames.Add(new Tuple<string, Type>(colName, null));
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
            if (IsOpen)
            {
                string sql = "SELECT rdb$relation_name ";
                sql += "from rdb$relations ";
                sql += "where rdb$view_blr is null ";
                sql += "and(rdb$system_flag is null or rdb$system_flag = 0) ";
                sql += "order by rdb$relation_name;";

                DataTable dt = ExecuteQuery(sql);
                foreach (DataRow dr in dt.Rows)
                {
                    tableNames.Add(((string)dr[0]).Trim());
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
            if (IsOpen)
            {
                string sql = "SELECT rdb$relation_name ";
                sql += "from rdb$relations ";
                sql += "where rdb$view_blr is not null ";
                sql += "and(rdb$system_flag is null or rdb$system_flag = 0) ";
                sql += "order by rdb$relation_name;";

                DataTable dt = ExecuteQuery(sql);
                foreach (DataRow dr in dt.Rows)
                {
                    viewNames.Add(((string)dr[0]).Trim());
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
            this.ExecuteNonQuery("DELETE FROM \"_ColumnInfo\" WHERE \"TableName\"='" + tableName + " AND \"ColumnName\"='" + colToRemove + "'");
            if (colInfoMap.TryGetValue(tableName, out ColumnNameMap nameMap))
            {
                if (nameMap.ColDict.TryGetValue(colToRemove, out int colNo))
                {
                    nameMap.ColDict.Remove(colToRemove);
                    nameMap.LongNames.RemoveAt(colNo);
                }
            }
        }

        /// <summary>
        /// Do an ALTER on the db table and add a column
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnName">The column to add</param>
        /// <param name="columnType">The db column type</param>
        public void AddColumn(string tableName, string columnName, string columnType)
        {
            int colNo = -1;
            if (IsOpen)
            {
                string sql = "select rdb$field_name from rdb$relation_fields ";
                sql += "where rdb$relation_name = '" + tableName + "' ";

                DataTable dt = ExecuteQuery(sql);
                colNo = dt.Rows.Count;
            }
            AddColumn(tableName, columnName, columnType, colNo);
        }

        /// <summary>
        /// Do an ALTER on the db table and add a column
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnName">The column to add</param>
        /// <param name="columnType">The db column type</param>
        /// <param name="columnNumber">The column number (-1 if not to be used)</param>
        private void AddColumn(string tableName, string columnName, string columnType, int columnNumber)
        {
            string colName;
            if (tableName.StartsWith("_") || columnNumber < 0
                || columnName.Equals("SimulationID", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("SimulationName", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("CheckpointID", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("CheckpointName", StringComparison.OrdinalIgnoreCase))
                colName = columnName.Substring(0, Math.Min(31, columnName.Length));
            else
                colName = "COL_" + columnNumber.ToString();

            string sql;
            if (FieldExists(tableName, colName))
                DropColumn(tableName, colName);

            sql = "ALTER TABLE \"" + tableName + "\" ADD \"" + colName + "\" " + columnType;
            this.ExecuteNonQuery(sql);

            this.ExecuteNonQuery("INSERT INTO \"_ColumnInfo\" VALUES('" + tableName + "', + '" + columnName + "', " + columnNumber.ToString() + ")");

            ColumnNameMap nameMap;
            if (!colInfoMap.TryGetValue(tableName, out nameMap))
            {
                nameMap = new ColumnNameMap();
                colInfoMap.Add(tableName, nameMap);
            }

            nameMap.ColDict.Add(columnName, columnNumber);
            nameMap.LongNames.Insert(columnNumber, columnName);
        }

        /// <summary>
        /// Checks if the field exists
        /// </summary>
        /// <param name="table"></param>
        /// <param name="fieldname"></param>
        /// <returns>True if the field exists in the database</returns>
        public bool FieldExists(string table, string fieldname)
        {
            string sql = "SELECT COUNT(f.rdb$relation_name) ";
            sql += "from rdb$relation_fields f ";
            sql += "join rdb$relations r on f.rdb$relation_name = r.rdb$relation_name ";
            sql += "and UPPER(f.rdb$relation_name) = '" + table.ToUpper() + "' ";
            sql += "and UPPER(f.rdb$field_name) = '" + fieldname.ToUpper() + "' ";
            sql += "and r.rdb$view_blr is null ";
            sql += "and(r.rdb$system_flag is null or r.rdb$system_flag = 0);";

            DataTable dt = ExecuteQuery(sql);
            return (Convert.ToInt32(dt.Rows[0][0], CultureInfo.InvariantCulture) > 0);
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
                ///// sql.Append(columnNames[i]);
                if (tableName.StartsWith("_")
                || columnName.Equals("SimulationID", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("SimulationName", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("CheckpointID", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("CheckpointName", StringComparison.OrdinalIgnoreCase))
                    sql.Append(columnName.Substring(0, Math.Min(31, columnNames[i].Length))); //////
                else
                {
                    int insertCol = GetColumnNumber(tableName, columnNames[i]);
                    sql.Append("COL_" + insertCol.ToString());
                }
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
            // lock (lockThis)
            {
                FbTransaction myTransaction = fbDBConnection.BeginTransaction();

                int index = 0;
                try
                {
                    // Create an insert query
                    string sql = CreateInsertSQL(tableName, columnNames);
                    for (int rowIndex = 0; rowIndex < values.Count; rowIndex++)
                    {
                        index = rowIndex;
                        BindParametersAndRunQuery(myTransaction, sql, values[rowIndex]);
                    }
                    myTransaction.Commit();
                }
                catch (Exception ex)
                {
                    throw new FirebirdException("Exception " + ex.Message + "\r\nCannot insert row for " + tableName + " in InsertRows():" + String.Join(", ", values[index].ToString()).ToArray());
                }
                finally
                {
                    myTransaction.Dispose();
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
            var columnNames = table.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList<string>();
            var sql = CreateInsertSQL(table.TableName, columnNames);
            FbCommand cmd = fbDBConnection.CreateCommand();
            var transactionOptions = new FbTransactionOptions()
            {
                TransactionBehavior = FbTransactionBehavior.Concurrency |
                                      FbTransactionBehavior.Wait |
                                      FbTransactionBehavior.NoAutoUndo
            };
            cmd.Transaction = fbDBConnection.BeginTransaction();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            int i = 1;
            foreach (DataColumn column in table.Columns)
            {
                FbParameter newParam;
                //// if (Convert.IsDBNull(values[i]) || values[i] == null)
                //// {
                ////     cmd.Parameters.Add("@" + ((i + 1).ToString()), FbDbType.Text).Value = string.Empty;
                //// }
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
                else
                    newParam = cmd.Parameters.Add("@" + ((i).ToString()), FbDbType.Text);
                newParam.IsNullable = true;
                i++;
            }
            cmd.Prepare();
            return cmd;
        }

        /// <summary>
        /// Executes a previously prepared bindable query, inserting a new set of parameters
        /// </summary>
        /// <param name="bindableQuery">The prepared query to be executed</param>
        /// <param name="values">The values to be inserted by using the query</param>
        public void RunBindableQuery(object bindableQuery, IEnumerable<object> values)
        {
            FbCommand cmd = (FbCommand)bindableQuery;
            int i = 0;
            foreach (var value in values)
            {
                cmd.Parameters[i].Value = value;
                // Enums have an underlying type of Int32, but we want to store
                // their string representation, not their integer value
                if (value.GetType().IsEnum)
                    cmd.Parameters[i].Value = value.ToString();
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
                command.Transaction.Commit();
            command.Dispose();
        }

        private object lockThis = new object();


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

        /// <summary>Convert .NET type into an Firebird type</summary>
        public string GetDBDataTypeName(Type type)
        {
            return GetDBDataTypeName(true);
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

            // We use the _ColumnInfo table to map between column names and column numbers
            if (TableExists("_ColumnInfo"))
                // We're creating a new table here, so delete any old records
                this.ExecuteNonQuery("DELETE FROM \"_ColumnInfo\" WHERE \"TableName\"='" + tableName + "'");
            else
                this.ExecuteNonQuery("CREATE TABLE \"_ColumnInfo\" (\"TableName\" VARCHAR(500), \"ColumnName\" VARCHAR(500), \"ColumnNumber\" INTEGER)");

            ColumnNameMap nameMap;
            if (colInfoMap.TryGetValue(tableName, out nameMap))
            {
                nameMap.ColDict.Clear();
                nameMap.LongNames.Clear();
            }
            else
            {
                nameMap = new ColumnNameMap();
                colInfoMap.Add(tableName, nameMap);
            }

            StringBuilder sql = new StringBuilder();

            for (int c = 0; c < colNames.Count; c++)
            {
                if (sql.Length > 0)
                    sql.Append(',');

                string columnName = colNames[c];
                sql.Append("\"");
                // sql.Append(colNames[c]);
                if (tableName.StartsWith("_")
                || columnName.Equals("SimulationID", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("SimulationName", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("CheckpointID", StringComparison.OrdinalIgnoreCase)
                || columnName.Equals("CheckpointName", StringComparison.OrdinalIgnoreCase))
                    sql.Append(columnName.Substring(0, Math.Min(31, columnName.Length))); ///// 
                else
                    sql.Append("COL_" + c.ToString());
                sql.Append("\" ");
                if (colTypes[c] == null)
                    sql.Append("INTEGER");
                else
                    sql.Append(colTypes[c]);

                this.ExecuteNonQuery("INSERT INTO \"_ColumnInfo\" VALUES('" + tableName + "', + '" + columnName + "', " +  c.ToString() + ")");
                nameMap.ColDict.Add(columnName, c);
                nameMap.LongNames.Insert(c, columnName);
            }

            sql.Insert(0, "CREATE TABLE \"" + tableName + "\" (");
            sql.Append(')');
            this.ExecuteNonQuery(sql.ToString());

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
            if (!tableName.Equals("_ColumnInfo", StringComparison.OrdinalIgnoreCase))
            {
                // FbConnection.ClearPool(fbDBConnection);
                this.ExecuteNonQuery(string.Format("DROP TABLE \"{0}\"", tableName));
            }
            if (TableExists("_ColumnInfo"))
                this.ExecuteNonQuery("DELETE FROM \"_ColumnInfo\" WHERE \"TableName\" = '" + tableName + "'");
            if (colInfoMap.ContainsKey(tableName))
                colInfoMap.Remove(tableName);
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

        /// <summary>
        /// A class for quick mapping between column name and column number
        /// </summary>
        class ColumnNameMap
        {
            // List of "long" column names - use column number as the index 
            public List<string> LongNames = new List<string>();

            // Dictionary to allow the column number for a "long" column name to be accessed quickly
            public Dictionary<string, int> ColDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
