using APSIM.Shared.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Models.Storage
{
    /// <summary>Encapsulates a table that needs writing to the database.</summary>
    class Table : IDisposable
    {
        public class Column
        {
            public string Name { get; private set; }
            public string Units { get; private set; }

            /// <summary>Constructor</summary>
            /// <param name="columnName">Name of column</param>
            /// <param name="columnUnits">Units of column</param>
            public Column(string columnName, string columnUnits)
            {
                Name = columnName;
                Units = columnUnits;
            }
        }

        public SQLite connection;
        private List<string> preparedInsertQueryColumnNames;
        private IntPtr preparedInsertQuery;
        private object[] values;
        public List<Row> RowsToWrite = new List<Row>();

        /// <summary>Name of table.</summary>
        public string Name { get; private set; }

        /// <summary>Does the table exist in the .db</summary>
        public bool Exists { get { return connection != null; } }

        /// <summary>Are there any rows that need writing?</summary>
        public bool HasRowsToWrite { get { return RowsToWrite.Count > 0; } }

        /// <summary>Column names in table</summary>
        public List<Column> Columns { get; private set; }

        /// <summary>Constructor</summary>
        /// <param name="tableName">Name of table</param>
        public Table(string tableName)
        {
            Name = tableName;
            Columns = new List<Column>();
        }

        /// <summary>Constructor</summary>
        /// <param name="tableName">Name of table</param>
        /// <param name="sqliteConnection">SQLite connection</param>
        public Table(string tableName, SQLite sqliteConnection)
        {
            Name = tableName;
            Columns = new List<Column>();
            connection = sqliteConnection;
            Open();
        }

        /// <summary>Dispose of the table</summary>
        public void Dispose()
        {
            Console.WriteLine("Table " + Name + " being disposed: Stack trace:");
            Console.WriteLine(Environment.StackTrace);
            if (preparedInsertQuery != IntPtr.Zero)
                connection.Finalize(preparedInsertQuery);
            preparedInsertQuery = IntPtr.Zero;
            connection = null;
        }

        /// <summary>Set the connection</summary>
        /// <param name="existingConnection"></param>
        public void SetConnection(SQLite existingConnection)
        {
            Console.WriteLine("Setting connection in table: " + Name);
            connection = existingConnection;
            Open();
        }

        /// <summary>Open the table and get a list of columns and simulation ids.</summary>
        private void Open()
        {
            DataTable data = connection.ExecuteQuery("pragma table_info('" + Name + "')");
            foreach (DataRow row in data.Rows)
            {
                string columnName = row["Name"].ToString();
                Columns.Add(new Column(columnName, LookupUnitsForColumn(columnName)));
            }
        }

        /// <summary>Write the specified number of rows.</summary>
        /// <param name="sqliteConnection">The SQLite connection to write to</param>
        /// <param name="simulationIDs">A dictionary of simulation IDs</param>
        /// <returns>The number of rows written to the .db</returns>
        public int WriteRows(SQLite sqliteConnection, Dictionary<string, int> simulationIDs)
        {
            sqliteConnection.ExecuteNonQuery("BEGIN");
            int numRows = RowsToWrite.Count;
            for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
            {
                RowsToWrite[rowIndex].Flatten();

                // If this is the first time we've written to the table, then create table.
                if (!Exists)
                    CreateTable(sqliteConnection);

                EnsureColumnsExistInDB(connection, RowsToWrite[rowIndex]);

                Array.Resize(ref values, preparedInsertQueryColumnNames.Count);
                Array.Clear(values, 0, values.Length);
                RowsToWrite[rowIndex].GetValues(preparedInsertQueryColumnNames, ref values, simulationIDs);
                connection.BindParametersAndRunQuery(preparedInsertQuery, values);
            }
            sqliteConnection.ExecuteNonQuery("END");

            return numRows;
        }

        /// <summary>
        /// Lookup and return units for the specified column.
        /// </summary>
        /// <param name="columnName">The column name to return units for</param>
        private string LookupUnitsForColumn(string columnName)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT Units FROM _Units WHERE TableName='");
            sql.Append(Name);
            sql.Append("' AND ColumnHeading='");
            sql.Append(columnName);
            sql.Append("'");
            DataTable data = connection.ExecuteQuery(sql.ToString());
            if (data.Rows.Count > 0)
                return (string)data.Rows[0][0];
            else
                return null;
        }

        /// <summary>Ensure columns exist in .db file</summary>
        /// <param name="sqliteConnection">The SQLite connection to write to</param>
        private void CreateTable(SQLite sqliteConnection)
        {
            if (connection == null)
                Console.WriteLine("Connection is null in table: " + Name);
            Console.WriteLine("Creating table: " + Name);
            connection = sqliteConnection;
            Columns.Clear();
            StringBuilder sql = new StringBuilder();
            sql.Append("CREATE TABLE ");
            sql.Append(Name);
            sql.Append(" (");

            bool needToAppendComma = false;

            bool hasSimulationName = RowsToWrite.Count > 0 && RowsToWrite[0].SimulationName != null;
            if (hasSimulationName)
            {
                sql.Append("SimulationID INTEGER");
                Columns.Add(new Column("SimulationID", null));
                needToAppendComma = true;
            }

            Row row = RowsToWrite[0];
            for (int i = 0; i < row.ColumnNames.Count(); i++)
            {
                string columnName = row.ColumnNames.ElementAt(i);
                string columnUnit = row.ColumnUnits.ElementAt(i);
                object value = row.Values.ElementAt(i);
                if (value != null)
                {
                    string type = GetSQLiteDataType(value.GetType());

                    if (needToAppendComma)
                        sql.Append(',');
                    sql.Append("[");
                    sql.Append(columnName);
                    sql.Append("] ");
                    sql.Append(type);
                    needToAppendComma = true;
                    Columns.Add(new Column(columnName, columnUnit));
                }
            }
            sql.Append(')');
            connection.ExecuteNonQuery(sql.ToString());
        }

        /// <summary>Ensure columns exist in .db file</summary>
        /// <param name="connection">The SQLite connection to write to</param>
        /// <param name="row">The row</param>
        private void EnsureColumnsExistInDB(SQLite connection, Row row)
        {
            bool columnsWereAdded = false;
            for (int i = 0; i < row.ColumnNames.Count(); i++)
            {
                string columnName = row.ColumnNames.ElementAt(i);
                if (Columns.Find(c => c.Name == columnName) == null)
                {
                    string columnUnit = row.ColumnUnits.ElementAt(i);
                    object value = row.Values.ElementAt(i);
                    if (value != null)
                    {
                        string sql = "ALTER TABLE " + Name + " ADD COLUMN [" + columnName + "] " + GetSQLiteDataType(value.GetType());
                        connection.ExecuteNonQuery(sql);
                        Columns.Add(new Column(columnName, columnUnit));
                        columnsWereAdded = true;
                    }
                }
            }

            if (columnsWereAdded || preparedInsertQuery == IntPtr.Zero)
                CreateInsertQuery(connection);
        }

        /// <summary>Convert .NET type into an SQLite type</summary>
        private string GetSQLiteDataType(Type type)
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
                return "char(50)";
        }

        /// <summary>Create a prepared insert query</summary>
        /// <param name="connection">The SQLite connection to write to</param>
        private void CreateInsertQuery(SQLite connection)
        {
            if (preparedInsertQuery != IntPtr.Zero)
                connection.Finalize(preparedInsertQuery);
            preparedInsertQuery = IntPtr.Zero;

            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO ");
            sql.Append(Name);
            sql.Append('(');

            for (int i = 0; i < Columns.Count; i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.Append('[');
                sql.Append(Columns[i].Name);
                sql.Append(']');
            }
            sql.Append(") VALUES (");

            for (int i = 0; i < Columns.Count; i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.Append('?');
            }
            sql.Append(')');
            preparedInsertQuery = connection.Prepare(sql.ToString());
            preparedInsertQueryColumnNames = Columns.Select(c => c.Name).ToList();
        }

    }
}
