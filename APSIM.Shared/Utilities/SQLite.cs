// -----------------------------------------------------------------------
// <copyright file="SQLite.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;
    using System.Linq;
    using System.Text;
    using System.Globalization;

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
        static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.CustomMarshaler,
         MarshalTypeRef=typeof(UTF8Marshaler))]string filename, out IntPtr db, int flags, string zVfs);

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

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_column_blob(IntPtr stmHandle, int columnNumber);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_column_bytes(IntPtr stmHandle, int columnNumber);

        /// <summary>Sqlite3_bind_doubles the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <param name="Value">The value.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern string sqlite3_bind_double(IntPtr Query, int ParameterNumber, double Value);

        /// <summary>Sqlite3_bind_ints the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <param name="Value">The value.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern string sqlite3_bind_int(IntPtr Query, int ParameterNumber, int Value);

        /// <summary>Sqlite3_bind_nulls the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern string sqlite3_bind_null(IntPtr Query, int ParameterNumber);

        /// <summary>Sqlite3_bind_texts the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <param name="ParameterNumber">The parameter number.</param>
        /// <param name="Value">The value.</param>
        /// <param name="n">The n.</param>
        /// <param name="CallBack">The call back.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_bind_text(IntPtr Query, int ParameterNumber, string Value, int n, IntPtr CallBack);

        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_bind_blob(IntPtr Query, int iIndex, byte[] iParam, int iBytes, IntPtr iOperation);

        /// <summary>Sqlite3_resets the specified query.</summary>
        /// <param name="Query">The query.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_reset(IntPtr Query);

        /// <summary>Sqlite3_threadsafes this instance.</summary>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_threadsafe();

        /// <summary>Sqlite3_busy_timeouts the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <param name="ms">The ms.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_busy_timeout(IntPtr db, int ms);

        /// <summary>Sqlite3_db_mutexes the specified database.</summary>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_db_mutex(IntPtr db);

        /// <summary>Sqlite3_mutex_enters the specified sqlite3_mutex.</summary>
        /// <param name="sqlite3_mutex">The sqlite3_mutex.</param>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sqlite3_mutex_enter(IntPtr sqlite3_mutex);

        /// <summary>Sqlite3_mutex_leaves the specified sqlite3_mutex.</summary>
        /// <param name="sqlite3_mutex">The sqlite3_mutex.</param>
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sqlite3_mutex_leave(IntPtr sqlite3_mutex);
        #endregion

        /// <summary>The _DB</summary>
        [NonSerialized]
        private IntPtr _db; //pointer to SQLite database
        /// <summary>The _open</summary>
        [NonSerialized]
        private bool _open; //whether or not the database is open

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
            ExecuteNonQuery("BEGIN");
        }

        /// <summary>End a transaction.</summary>
        public void EndTransaction()
        {
            ExecuteNonQuery("END");
        }

        /// <summary>Opens or creates SQLite database with the specified path</summary>
        /// <param name="path">Path to SQLite database</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <exception cref="SQLiteException"></exception>
        public void OpenDatabase(string path, bool readOnly)
        {
            int id;
            if (readOnly)
                id = sqlite3_open_v2(path, out _db, SQLITE_OPEN_READONLY | SQLITE_OPEN_FULLMUTEX, null);
            else
                id = sqlite3_open_v2(path, out _db, SQLITE_OPEN_READWRITE | SQLITE_OPEN_FULLMUTEX | SQLITE_OPEN_CREATE, null);

            if (id != SQLITE_OK)
            {
                string errorMessage = Marshal.PtrToStringAnsi(sqlite3_errmsg(_db));
                throw new SQLiteException(errorMessage);
            }

            _open = true;
            IsReadOnly = readOnly;
            IsInMemory = path.ToLower().Contains(":memory:");
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

        private class Column
        {
            public string name;
            public Type dataType;
            public List<object> values = new List<object>();

            public void addIntValue(int value)
            {
                if (dataType == null)
                    dataType = typeof(int);
                values.Add(value);
            }

            public void addDoubleValue(double value)
            {
                if (dataType == null || dataType == typeof(int))
                    dataType = typeof(double);
                values.Add(value);
            }
            public void addByteArrayValue(byte[] value)
            {
                if (dataType == null || dataType == typeof(byte[]))
                    dataType = typeof(byte[]);
                values.Add(value);
            }
            public void addTextValue(string value)
            {
                DateTime date;
                if (DateTime.TryParseExact(value, "yyyy-MM-dd hh:mm:ss", null, System.Globalization.DateTimeStyles.None, out date))
                {
                    if (dataType == null)
                        dataType = typeof(DateTime);
                    values.Add(date);
                }
                else
                {
                    dataType = typeof(string);
                    values.Add(value);
                }
            }
            public void addNull()
            {
                values.Add(null);
            }

            internal object GetValue(int rowIndex)
            {
                if (rowIndex >= values.Count)
                    throw new Exception("Not enough values found when creating DataTable from SQLITE query.");
                if (values[rowIndex] == null)
                    return DBNull.Value;
                else if (dataType == typeof(int))
                    return Convert.ToInt32(values[rowIndex], CultureInfo.InvariantCulture);
                else if (dataType == typeof(double))
                    return Convert.ToDouble(values[rowIndex], System.Globalization.CultureInfo.InvariantCulture);
                else if (dataType == typeof(DateTime))
                    return Convert.ToDateTime(values[rowIndex], CultureInfo.InvariantCulture);
                else if (dataType == typeof(byte[]))
                    return values[rowIndex];
                else
                {
                    if (values[rowIndex].GetType() == typeof(DateTime))
                        return Convert.ToDateTime(values[rowIndex], CultureInfo.InvariantCulture).ToString("yyyy-MM-dd hh:mm:ss");
                    return values[rowIndex].ToString();
                }

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
            if (!_open)
                throw new SQLiteException("SQLite database is not open.");

            //prepare the statement
            IntPtr stmHandle = Prepare(query);

            //get the number of returned columns
            int columnCount = sqlite3_column_count(stmHandle);

            // Create a datatable that may have column of type object. This occurs
            // when the first row of a table has null values.
            List<Column> columns = new List<Column>();
            while (sqlite3_step(stmHandle) == SQLITE_ROW)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    if (i >= columns.Count)
                    {
                        // add a new column
                        string columnName = Marshal.PtrToStringAnsi(sqlite3_column_name(stmHandle, i));
                        columns.Add(new Column() { name = columnName });
                    }

                    int sqliteType = sqlite3_column_type(stmHandle, i);

                    if (sqliteType == SQLITE_INTEGER)
                        columns[i].addIntValue(sqlite3_column_int(stmHandle, i));
                    else if (sqliteType == SQLITE_FLOAT)
                        columns[i].addDoubleValue(sqlite3_column_double(stmHandle, i));
                    else if (sqliteType == SQLITE_TEXT)
                    {
                        IntPtr iptr = sqlite3_column_text(stmHandle, i);
                        columns[i].addTextValue(Marshal.PtrToStringAnsi(iptr));
                    }
                    else if (sqliteType == SQLITE_BLOB)
                    {
                        int length = sqlite3_column_bytes(stmHandle, i);
                        byte[] bytes = new byte[length];
                        Marshal.Copy(sqlite3_column_blob(stmHandle, i), bytes, 0, length);
                        columns[i].addByteArrayValue(bytes);

                    }
                    else
                        columns[i].addNull();
                }
            }

            Finalize(stmHandle);

            // At this point we have a list of columns, each with values for each row.
            // Need to convert this to a DataTable.

            DataTable table = new DataTable();
            if (columns.Count > 0)
            {
                foreach (Column column in columns)
                {
                    if (column.dataType == null)
                        table.Columns.Add(column.name, typeof(object));
                    else
                        table.Columns.Add(column.name, column.dataType);
                }

                for (int row = 0; row != columns[0].values.Count; row++)
                {
                    DataRow newRow = table.NewRow();
                    for (int col = 0; col != columns.Count; col++)
                        newRow[col] = columns[col].GetValue(row);
                    table.Rows.Add(newRow);
                }
            }
            return table;
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
        public IntPtr Prepare(string query)
        {
            IntPtr stmHandle;

            if (sqlite3_prepare_v2(_db, query, query.Length,
                  out stmHandle, IntPtr.Zero) != SQLITE_OK)
                throw new SQLiteException(Marshal.PtrToStringAnsi(sqlite3_errmsg(_db)));

            return stmHandle;
        }

        /// <summary>Finalizes a SQLite statement</summary>
        /// <param name="stmHandle">Pointer to SQLite prepared statement</param>
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
        /// <param name="query">The query.</param>
        /// <param name="values">The values.</param>
        public void BindParametersAndRunQuery(IntPtr query, IEnumerable<object> values)
        {
            int i = 0;
            foreach (var value in values)
            {
                if (Convert.IsDBNull(value) || value == null)
                {
                    sqlite3_bind_null(query, i + 1);
                }
                // Enums have an underlying type of Int32, but we want to store
                // their string representation, not their integer value
                else if (value.GetType().IsEnum)
                {
                    sqlite3_bind_text(query, i + 1, value.ToString(), -1, new IntPtr(-1));
                }
                else if (value.GetType() == typeof(DateTime))
                {
                    DateTime d = (DateTime)value;
                    sqlite3_bind_text(query, i + 1, d.ToString("yyyy-MM-dd hh:mm:ss"), -1, new IntPtr(-1));
                }
                else if (value.GetType() == typeof(int))
                {
                    int integer = (int)value;
                    sqlite3_bind_int(query, i + 1, integer);
                }
                else if (value.GetType() == typeof(float))
                {
                    float f = (float)value;
                    sqlite3_bind_double(query, i + 1, f);
                }
                else if (value.GetType() == typeof(double))
                {
                    double d = (double)value;
                    sqlite3_bind_double(query, i + 1, d);
                }
                else if (value.GetType() == typeof(byte[]))
                {
                    byte[] bytes = value as byte[];
                    IntPtr SQLITE_TRANSIENT = new IntPtr(-1);
                    sqlite3_bind_blob(query, i + 1, bytes, bytes.Length, SQLITE_TRANSIENT);
                }
                else if (value.GetType() == typeof(bool))
                    sqlite3_bind_text(query, i + 1, value.ToString(), -1, new IntPtr(-1));
                else
                    sqlite3_bind_text(query, i + 1, value as string, -1, new IntPtr(-1));

                i++;
            }

            if (sqlite3_step(query) != SQLITE_DONE)
            {
                string errorMessage = Marshal.PtrToStringAnsi(sqlite3_errmsg(_db));
                throw new SQLiteException(errorMessage);
            }
            sqlite3_reset(query);
        }

        /// <summary>Return a list of column names.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public List<string> GetColumnNames(string tableName)
        {
            string sql = "select * from " + tableName + " LIMIT 0";

            //prepare the statement
            IntPtr stmHandle = Prepare(sql);

            //get the number of returned columns
            int columnCount = sqlite3_column_count(stmHandle);

            List<string> columnNames = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                string columnName = Marshal.PtrToStringAnsi(sqlite3_column_name(stmHandle, i));
                columnNames.Add(columnName);
            }

            Finalize(stmHandle);
            return columnNames;
        }

        /// <summary>Return a list of column names with a data type of string.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public List<string> GetStringColumnNames(string tableName)
        {
            List<string> columns = new List<string>();
            DataTable columnData = ExecuteQuery("pragma table_info('" + tableName + "')");

            foreach (DataRow row in columnData.Rows)
            {
                if (row["type"].ToString() == "char(50)")
                    columns.Add(row["name"].ToString());
            }

            return columns;
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

        /// <summary>Return a list of column names for the specified table</summary>
        /// <param name="tableName">The table name to get columns from.</param>
        public List<string> GetTableColumns(string tableName)
        {
            List<string> columns = new List<string>();
            DataTable columnData = ExecuteQuery("pragma table_info('" + tableName + "')");

            foreach (DataRow row in columnData.Rows)
                columns.Add(row["name"].ToString());

            return columns;
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
            sql.Append("INSERT INTO ");
            sql.Append(tableName);
            sql.Append('(');

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

        /// <summary>Create a prepared insert query</summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnNames">Column names</param>
        private IntPtr CreateInsertQuery(string tableName, List<string> columnNames)
        {
            string sql = CreateInsertSQL(tableName, columnNames);
            return Prepare(sql.ToString());
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
            IntPtr preparedInsertQuery = CreateInsertQuery(tableName, columnNames);

            try
            {
                // Create an insert query
                for (int rowIndex = 0; rowIndex < values.Count; rowIndex++)
                    BindParametersAndRunQuery(preparedInsertQuery, values[rowIndex]);
            }
            finally
            {
                if (preparedInsertQuery != IntPtr.Zero)
                    Finalize(preparedInsertQuery);
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
            queryHandle = Prepare(sql);
            return queryHandle;
        }

        /// <summary>
        /// Executes a previously prepared bindable query, inserting a new set of parameters
        /// </summary>
        /// <param name="bindableQuery">The prepared query to be executed</param>
        /// <param name="values">The values to be inserted by using the query</param>
        public void RunBindableQuery(object bindableQuery, IEnumerable<object> values)
        {
            BindParametersAndRunQuery((IntPtr)bindableQuery, values);
        }

        /// <summary>
        /// Finalises and destroys a prepared bindable query
        /// </summary>
        /// <param name="bindableQuery">The query to be finalised</param>
        public void FinalizeBindableQuery(object bindableQuery)
        {
            Finalize((IntPtr)bindableQuery);
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

            sql.Insert(0, "CREATE TABLE " + tableName + " (");
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
            ExecuteNonQuery("VACUUM");
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