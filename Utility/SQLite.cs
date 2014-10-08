// -----------------------------------------------------------------------
// <copyright file="SQLite.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;

    /// <summary>A class representing an exception thrown by this library.</summary>
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
    public class SQLite
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
        private const int SQLITE_OPEN_READONLY       =   0x00000001;  /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ readwrite</summary>
        private const int SQLITE_OPEN_READWRITE      =   0x00000002; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ create</summary>
        private const int SQLITE_OPEN_CREATE         =   0x00000004; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ deleteonclose</summary>
        private const int SQLITE_OPEN_DELETEONCLOSE  =   0x00000008; /* VFS only */
        /// <summary>The sqlit e_ ope n_ exclusive</summary>
        private const int SQLITE_OPEN_EXCLUSIVE      =   0x00000010; /* VFS only */
        /// <summary>The sqlit e_ ope n_ autoproxy</summary>
        private const int SQLITE_OPEN_AUTOPROXY      =   0x00000020; /* VFS only */
        /// <summary>The sqlit e_ ope n_ URI</summary>
        private const int SQLITE_OPEN_URI            =   0x00000040; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ memory</summary>
        private const int SQLITE_OPEN_MEMORY         =   0x00000080; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ mai n_ database</summary>
        private const int SQLITE_OPEN_MAIN_DB        =   0x00000100; /* VFS only */
        /// <summary>The sqlit e_ ope n_ tem p_ database</summary>
        private const int SQLITE_OPEN_TEMP_DB        =   0x00000200; /* VFS only */
        /// <summary>The sqlit e_ ope n_ transien t_ database</summary>
        private const int SQLITE_OPEN_TRANSIENT_DB   =   0x00000400; /* VFS only */
        /// <summary>The sqlit e_ ope n_ mai n_ journal</summary>
        private const int SQLITE_OPEN_MAIN_JOURNAL   =   0x00000800; /* VFS only */
        /// <summary>The sqlit e_ ope n_ tem p_ journal</summary>
        private const int SQLITE_OPEN_TEMP_JOURNAL   =   0x00001000; /* VFS only */
        /// <summary>The sqlit e_ ope n_ subjournal</summary>
        private const int SQLITE_OPEN_SUBJOURNAL     =   0x00002000; /* VFS only */
        /// <summary>The sqlit e_ ope n_ maste r_ journal</summary>
        private const int SQLITE_OPEN_MASTER_JOURNAL =   0x00004000; /* VFS only */
        /// <summary>The sqlit e_ ope n_ nomutex</summary>
        private const int SQLITE_OPEN_NOMUTEX        =   0x00008000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ fullmutex</summary>
        private const int SQLITE_OPEN_FULLMUTEX      =   0x00010000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ sharedcache</summary>
        private const int SQLITE_OPEN_SHAREDCACHE    =   0x00020000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ privatecache</summary>
        private const int SQLITE_OPEN_PRIVATECACHE   =   0x00040000; /* Ok for sqlite3_open_v2() */
        /// <summary>The sqlit e_ ope n_ wal</summary>
        private const int SQLITE_OPEN_WAL            =   0x00080000; /* VFS only */

        #region Externals
        //When using sqlite3 without .dll the platforms are intelligent enough to add the OS specific details.
        //On Linux-Mono the lib .so artifacts appear to be accounted for.
        /// <summary>Sqlite3_opens the specified filename.</summary>
        /// <param name="filename">The filename.</param>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_open(string filename, out IntPtr db);

        /// <summary>Sqlite3_open_v2s the specified filename.</summary>
        /// <param name="filename">The filename.</param>
        /// <param name="db">The database.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="zVfs">The z VFS.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_open_v2(string filename, out IntPtr db, int flags, string zVfs);

        /// <summary>Sqlite3_closes the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_close(IntPtr db);

        /// <summary>Sqlite3_prepare_v2s the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <param name="zSql">The z SQL.</param>
        /// <param name="nByte">The n byte.</param>
        /// <param name="ppStmpt">The pp STMPT.</param>
        /// <param name="pzTail">The pz tail.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_prepare_v2(IntPtr db, string zSql,
            int nByte, out IntPtr ppStmpt, IntPtr pzTail);

        /// <summary>Sqlite3_steps the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_step(IntPtr stmHandle);

        /// <summary>Sqlite3_finalizes the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_finalize(IntPtr stmHandle);

        /// <summary>Sqlite3_errmsgs the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_errmsg", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr sqlite3_errmsg(IntPtr db);

        /// <summary>Sqlite3_column_counts the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_column_count(IntPtr stmHandle);

        /// <summary>Sqlite3_column_names the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <param name="iCol">The i col.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr sqlite3_column_name(IntPtr stmHandle, int iCol);

        /// <summary>Sqlite3_column_types the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <param name="iCol">The i col.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_column_type(IntPtr stmHandle, int iCol);

        /// <summary>Sqlite3_column_ints the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <param name="iCol">The i col.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_column_int(IntPtr stmHandle, int iCol);

        /// <summary>Sqlite3_column_texts the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <param name="iCol">The i col.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr sqlite3_column_text(IntPtr stmHandle, int iCol);

        /// <summary>Sqlite3_column_doubles the specified STM handle.</summary>
        /// <param name="stmHandle">The STM handle.</param>
        /// <param name="iCol">The i col.</param>
        /// <returns></returns>
        [DllImport("sqlite3", EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
        static extern double sqlite3_column_double(IntPtr stmHandle, int iCol);

        /// <summary>Sqlite3_bind_doubles the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <param name="Value">The value.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern string sqlite3_bind_double(IntPtr Query, int ParameterNumber, double Value);

        /// <summary>Sqlite3_bind_ints the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <param name="Value">The value.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern string sqlite3_bind_int(IntPtr Query, int ParameterNumber, int Value);

        /// <summary>Sqlite3_bind_nulls the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern string sqlite3_bind_null(IntPtr Query, int ParameterNumber);

        /// <summary>Sqlite3_bind_texts the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <param name="Value">The value.</param>
        /// <param name="n">The n.</param>
        /// <param name="CallBack">The call back.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_bind_text(IntPtr Query, int ParameterNumber, string Value, int n, IntPtr CallBack);

        /// <summary>Sqlite3_resets the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_reset(IntPtr Query);

        /// <summary>Sqlite3_threadsafes this instance.</summary>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_threadsafe();

        /// <summary>Sqlite3_busy_timeouts the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <param name="ms">The ms.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_busy_timeout(IntPtr db, int ms);

        /// <summary>Sqlite3_db_mutexes the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_db_mutex(IntPtr db);

        /// <summary>Sqlite3_mutex_enters the specified sqlite3_mutex.</summary>
        /// <param name="sqlite3_mutex">The sqlite3_mutex.</param>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sqlite3_mutex_enter(IntPtr sqlite3_mutex);

        /// <summary>Sqlite3_mutex_leaves the specified sqlite3_mutex.</summary>
        /// <param name="sqlite3_mutex">The sqlite3_mutex.</param>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sqlite3_mutex_leave(IntPtr sqlite3_mutex);
        #endregion

        /// <summary>The _DB</summary>
        [NonSerialized]
        private IntPtr _db; //pointer to SQLite database
        /// <summary>The _open</summary>
        [NonSerialized]
        private bool _open; //whether or not the database is open

        /// <summary>Property to return true if the database is open.</summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get { return _open; } }

        /// <summary>Opens or creates SQLite database with the specified path</summary>
        /// <param name="path">Path to SQLite database</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <exception cref="Utility.SQLiteException"></exception>
        public void OpenDatabase(string path, bool readOnly)
        {
            int id;
            if (readOnly)
                id = sqlite3_open_v2(path, out _db, SQLITE_OPEN_READONLY | SQLITE_OPEN_NOMUTEX, null);
            else
                id = sqlite3_open_v2(path, out _db, SQLITE_OPEN_READWRITE | SQLITE_OPEN_NOMUTEX | SQLITE_OPEN_CREATE, null);

            if (id != SQLITE_OK)
            {
                string errorMessage = Marshal.PtrToStringAnsi(sqlite3_errmsg(_db));
                throw new SQLiteException(errorMessage);
            }

            _open = true;
            sqlite3_busy_timeout(_db, 40000);
        }

        /// <summary>Closes the SQLite database</summary>
        public void CloseDatabase()
        {
            if (_open)
                sqlite3_close(_db);

            _open = false;
        }

        /// <summary>Executes a query that returns no results</summary>
        /// <param name="query">SQL query to execute</param>
        /// <exception cref="Utility.SQLiteException">
        /// SQLite database is not open.
        /// or
        /// </exception>
        public void ExecuteNonQuery(string query)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            //prepare the statement
            IntPtr stmHandle = Prepare(query);

            int code = sqlite3_step(stmHandle);
            if (code != SQLITE_DONE)
            {
                string errorMessage = Marshal.PtrToStringAnsi(sqlite3_errmsg(_db));
                throw new SQLiteException(errorMessage);
            }

            Finalize(stmHandle);
        }

        /// <summary>
        /// Executes a query and stores the results in
        /// a DataTable
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <returns>DataTable of results</returns>
        /// <exception cref="Utility.SQLiteException">SQLite database is not open.</exception>
        public System.Data.DataTable ExecuteQuery(string query)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            //prepare the statement
            IntPtr stmHandle = Prepare(query);

            //get the number of returned columns
            int columnCount = sqlite3_column_count(stmHandle);

            // Create a datatable that may have column of type object. This occurs
            // when the first row of a table has null values.
            bool missingDataFound = false;
            System.Data.DataTable dTable = null;
            while (sqlite3_step(stmHandle) == SQLITE_ROW)
            {
                // create datatable and columns    
                if (dTable == null)
                {
                    dTable = new System.Data.DataTable();
                    for (int i = 0; i < columnCount; i++)
                    {
                        string ColumnName = Marshal.PtrToStringAnsi(sqlite3_column_name(stmHandle, i));
                        if (dTable.Columns.Contains(ColumnName))
                            ColumnName = "Report." + ColumnName;

                        int ColType = sqlite3_column_type(stmHandle, i);
                        if (ColType == SQLITE_INTEGER)
                            dTable.Columns.Add(ColumnName, typeof(int));
                        else if (ColType == SQLITE_FLOAT)
                            dTable.Columns.Add(ColumnName, typeof(double));
                        else if (ColType == SQLITE_TEXT)
                        {
                            IntPtr iptr = sqlite3_column_text(stmHandle, i);
                            string Value = Marshal.PtrToStringAnsi(iptr);
                            DateTime D;
                            if (DateTime.TryParse(Value, out D))
                                dTable.Columns.Add(ColumnName, typeof(DateTime));
                            else
                                dTable.Columns.Add(ColumnName, typeof(string));
                        }
                        else
                        {
                            dTable.Columns.Add(ColumnName, typeof(object));
                            missingDataFound = true;
                        }
                    }
                }

                // create rows in data table.
                object[] row = new object[columnCount];
                for (int i = 0; i < columnCount; i++)
                {
                    int colType = sqlite3_column_type(stmHandle, i);
                    if (colType != SQLITE_NULL)
                    {
                        if (dTable.Columns[i].DataType == typeof(DateTime))
                        {
                            IntPtr iptr = sqlite3_column_text(stmHandle, i);
                            string Value = Marshal.PtrToStringAnsi(iptr);
                            if (Value != null)
                                row[i] = DateTime.ParseExact(Value, "yyyy-MM-dd hh:mm:ss", null);
                        }
                        else if (dTable.Columns[i].DataType == typeof(int))
                            row[i] = sqlite3_column_int(stmHandle, i);
                        else if (dTable.Columns[i].DataType == typeof(double))
                            row[i] = sqlite3_column_double(stmHandle, i);
                        else if (dTable.Columns[i].DataType == typeof(string) ||
                                 dTable.Columns[i].DataType == typeof(object))
                        {
                            IntPtr iptr = sqlite3_column_text(stmHandle, i);
                            row[i] = Marshal.PtrToStringAnsi(iptr);
                        }
                    }
                }
                dTable.Rows.Add(row);
            }

            Finalize(stmHandle);

            // At this point the data table may have columns of type object because the 
            // first row was null for that column. When this happens (missing data) we 
            // want to convert the column type to a better data type. As .NET doesn't 
            // allow data tables of tables to change once they have data in them, we
            // will need to create a new data table with the correct column data types
            // and them copy all rows to it. When can then return this new data table.
            // This will maintain the proper column order.
            if (missingDataFound)
            {
                System.Data.DataTable newDataTable = new System.Data.DataTable();
                foreach (DataColumn column in dTable.Columns)
                {
                    if (column.DataType.Equals(typeof(object)))
                    {
                        object firstNonNullValue = FindFirstNonNullValueInColumn(column);
                        if (firstNonNullValue == null)
                            newDataTable.Columns.Add(column.ColumnName, typeof(object));
                        else
                            newDataTable.Columns.Add(column.ColumnName, firstNonNullValue.GetType());
                    }
                    else
                    {
                        newDataTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }

                // Now we can copy all data to the new table.
                foreach (DataRow row in dTable.Rows)
                {
                    newDataTable.ImportRow(row);
                }

                return newDataTable;
            }
            
            return dTable;
        }

        /// <summary>Find the first non null value in the specified column.</summary>
        /// <param name="dataColumn">The data column to look in</param>
        /// <returns>The first non null value. Null is returned when all values are null in column.</returns>
        private object FindFirstNonNullValueInColumn(DataColumn dataColumn)
        {
            foreach (DataRow row in dataColumn.Table.Rows)
            {
                if (!Convert.IsDBNull(row[dataColumn]))
                {
                    string stringValue = row[dataColumn].ToString();
                    double doubleValue;
                    DateTime dateValue;
                    if (double.TryParse(stringValue, out doubleValue))
                    {
                        return doubleValue;
                    }
                    else if (DateTime.TryParse(stringValue, out dateValue))
                    {
                        return dateValue;
                    }
                    else
                    {
                        return stringValue;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Executes a query and return a single integer value to caller. Returns -1 if not found.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="ColumnNumber">The column number.</param>
        /// <returns></returns>
        /// <exception cref="Utility.SQLiteException">SQLite database is not open.</exception>
        public int ExecuteQueryReturnInt(string query, int ColumnNumber)
        {
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            //prepare the statement
            IntPtr stmHandle = Prepare(query);

            //get the number of returned columns
            int columnCount = sqlite3_column_count(stmHandle);

            int ReturnValue = -1;
            if (sqlite3_step(stmHandle) == SQLITE_ROW && ColumnNumber < columnCount)
                ReturnValue = sqlite3_column_int(stmHandle, ColumnNumber);

            Finalize(stmHandle);
            return ReturnValue;
        }

        /// <summary>Prepares a SQL statement for execution</summary>
        /// <param name="query">SQL query</param>
        /// <returns>Pointer to SQLite prepared statement</returns>
        /// <exception cref="Utility.SQLiteException"></exception>
        public IntPtr Prepare(string query)
        {
            IntPtr stmHandle;

            if (sqlite3_prepare_v2(_db, query, query.Length,
                  out stmHandle, IntPtr.Zero) != SQLITE_OK)
                throw new SQLiteException( Marshal.PtrToStringAnsi(sqlite3_errmsg(_db)));

            return stmHandle;
        }

        /// <summary>Finalizes a SQLite statement</summary>
        /// <param name="stmHandle">Pointer to SQLite prepared statement</param>
        /// <exception cref="Utility.SQLiteException"></exception>
        public void Finalize(IntPtr stmHandle)
        {
            int code = sqlite3_finalize(stmHandle);
            if (code != SQLITE_OK)
            {
                string errorMessage = Marshal.PtrToStringAnsi(sqlite3_errmsg(_db));
                throw new SQLiteException(errorMessage);
            }
        }

        /// <summary>Bind all parameters values to the specified query and execute the query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="Values">The values.</param>
        /// <exception cref="Utility.SQLiteException"></exception>
        public void BindParametersAndRunQuery(IntPtr Query, object[] Values)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                if (Convert.IsDBNull(Values[i]) || Values[i] == null)
                {
                    sqlite3_bind_null(Query, i+1);
                }
                else if (Values[i].GetType().ToString() == "System.DateTime")
                {
                    DateTime d = (DateTime)Values[i];
                    sqlite3_bind_text(Query, i + 1, d.ToString("yyyy-MM-dd hh:mm:ss"), -1, new IntPtr(-1));
                }
                else if (Values[i].GetType().ToString() == "System.Int32")
                {
                    int integer = (int)Values[i];
                    sqlite3_bind_int(Query, i + 1, integer);
                }
                else if (Values[i].GetType().ToString() == "System.Single")
                {
                    float f = (float)Values[i];
                    sqlite3_bind_double(Query, i + 1, f);
                }
                else if (Values[i].GetType().ToString() == "System.Double")
                {
                    double d = (double)Values[i];
                    sqlite3_bind_double(Query, i + 1, d);
                }
                else
                {
                    sqlite3_bind_text(Query, i + 1, Values[i] as string, -1, new IntPtr(-1));

                }

            }

            if (sqlite3_step(Query) != SQLITE_DONE)
            {
                string errorMessage = Marshal.PtrToStringAnsi(sqlite3_errmsg(_db));
                throw new SQLiteException(errorMessage);
            }
            sqlite3_reset(Query);
        }

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public List<string> GetColumnNames(string tableName)
        {
            string sql = "select * from "+tableName+" LIMIT 0";

            //prepare the statement
            IntPtr stmHandle = Prepare(sql);

            //get the number of returned columns
            int columnCount = sqlite3_column_count(stmHandle);

            List<string> columnNames = new List<string>();
            for(int i = 0; i < columnCount; i++)
            {
                string columnName = Marshal.PtrToStringAnsi(sqlite3_column_name(stmHandle, i));
                columnNames.Add(columnName);    
            }

            Finalize(stmHandle);
            return columnNames;
        }

        /// <summary>Lock the mutex</summary>
        public void MutexEnter()
        {
            sqlite3_mutex_enter(sqlite3_db_mutex(_db));
        }

        /// <summary>Unlock the mutex</summary>
        public void MutexLeave()
        {
            sqlite3_mutex_leave(sqlite3_db_mutex(_db));
        }
    }
}