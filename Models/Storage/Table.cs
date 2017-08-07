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
            if (preparedInsertQuery != IntPtr.Zero)
                connection.Finalize(preparedInsertQuery);
            preparedInsertQuery = IntPtr.Zero;
            connection = null;
        }

        /// <summary>Set the connection</summary>
        /// <param name="existingConnection"></param>
        public void SetConnection(SQLite existingConnection)
        {
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
            int numRows = RowsToWrite.Count;
            for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
            {
                RowsToWrite[rowIndex].Flatten();

                // If this is the first time we've written to the table, then create table.
                if (!Exists)
                    CreateTable(sqliteConnection, RowsToWrite[0].ColumnNames, RowsToWrite[0].ColumnUnits, RowsToWrite[0].Values.Select(v => v.GetType()));

                EnsureColumnsExistInDB(connection, RowsToWrite[rowIndex].ColumnNames, RowsToWrite[rowIndex].ColumnUnits, RowsToWrite[rowIndex].Values.Select(v => v.GetType()));

                Array.Resize(ref values, preparedInsertQueryColumnNames.Count);
                Array.Clear(values, 0, values.Length);
                RowsToWrite[rowIndex].GetValues(preparedInsertQueryColumnNames, ref values, simulationIDs);
                connection.BindParametersAndRunQuery(preparedInsertQuery, values);
            }

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
        /// <param name="columnNames">The column names for the table</param>
        /// <param name="columnUnits">The column units for the table</param>
        /// <param name="columnTypes">A column types for the table</param>
        private void CreateTable(SQLite sqliteConnection, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<Type> columnTypes)
        {
            connection = sqliteConnection;
            Columns.Clear();
            StringBuilder sql = new StringBuilder();
            sql.Append("CREATE TABLE ");
            sql.Append(Name);
            sql.Append(" (SimulationID INTEGER");
            Columns.Add(new Column("SimulationID", null));
            for (int i = 0; i < columnNames.Count(); i++)
            {
                string columnName = columnNames.ElementAt(i);
                string columnUnit = columnUnits.ElementAt(i);
                string type = GetSQLiteDataType(columnTypes.ElementAt(i));
                sql.Append(",[");
                sql.Append(columnName);
                sql.Append("] ");
                sql.Append(type);
                Columns.Add(new Column(columnName, columnUnit));
            }
            sql.Append(')');
            connection.ExecuteNonQuery(sql.ToString());
        }

        /// <summary>Ensure columns exist in .db file</summary>
        /// <param name="connection">The SQLite connection to write to</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="columnTypes">Column types</param>
        private void EnsureColumnsExistInDB(SQLite connection, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<Type> columnTypes)
        {
            bool columnsWereAdded = false;
            for (int i = 0; i < columnNames.Count(); i++)
            {
                string columnName = columnNames.ElementAt(i);
                string columnUnit = columnUnits.ElementAt(i);
                Type columnType = columnTypes.ElementAt(i);
                if (Columns.Find(c => c.Name == columnName) == null)
                {
                    string sql = "ALTER TABLE " + Name + " ADD COLUMN [" + columnName + "] " + GetSQLiteDataType(columnType);
                    connection.ExecuteNonQuery(sql);
                    Columns.Add(new Column(columnName, columnUnit));
                    columnsWereAdded = true;
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
