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
    class Table
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

        public List<Row> RowsToWrite = new List<Row>();

        /// <summary>Name of table.</summary>
        public string Name { get; private set; }

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

        /// <summary>Set the connection</summary>
        /// <param name="connection">The SQLite connection</param>
        public void SetConnection(SQLite connection)
        {
            Open(connection);
        }

        /// <summary>Open the table and get a list of columns and simulation ids.</summary>
        /// <param name="connection">The SQLite connection to open</param>
        private void Open(SQLite connection)
        {
            Columns.Clear();
            DataTable data = connection.ExecuteQuery("pragma table_info('" + Name + "')");
            foreach (DataRow row in data.Rows)
            {
                string columnName = row["Name"].ToString();
                string units = null;
                units = LookupUnitsForColumn(connection, columnName);
                Columns.Add(new Column(columnName, units));
            }
        }

        /// <summary>Write the specified number of rows.</summary>
        /// <param name="connection">The SQLite connection to write to</param>
        /// <param name="simulationIDs">A dictionary of simulation IDs</param>
        public void WriteRows(SQLite connection, Dictionary<string, int> simulationIDs)
        { 
            connection.ExecuteNonQuery("BEGIN");

            IntPtr preparedInsertQuery = IntPtr.Zero;
            try
            {
                // Flatten all rows.
                RowsToWrite.ForEach(r => r.Flatten());

                // Update our Columns variable from the columns in all rows.
                UpdateColumnsFromRowsToWrite();

                // If the table exists, make sure it has the required columns, otherwise create the table
                if (TableExists(connection, Name))
                    AlterTable(connection);
                else
                    CreateTable(connection);

                // Create an insert query
                preparedInsertQuery = CreateInsertQuery(connection);

                object[] values = new object[Columns.Count];
                List<string> columnNames = Columns.Select(col => col.Name).ToList();
                for (int rowIndex = 0; rowIndex < RowsToWrite.Count; rowIndex++)
                {
                    Array.Clear(values, 0, values.Length);
                    RowsToWrite[rowIndex].GetValues(columnNames, ref values, simulationIDs);
                    connection.BindParametersAndRunQuery(preparedInsertQuery, values);
                }
            }
            finally
            {
                connection.ExecuteNonQuery("END");
                if (preparedInsertQuery != IntPtr.Zero)
                    connection.Finalize(preparedInsertQuery);
            }
        }

        /// <summary>Merge columns</summary>
        public void MergeColumns(Table table)
        {
            foreach (Column column in table.Columns)
            {
                if (Columns.Find(c => c.Name == column.Name) == null)
                    Columns.Add(column);
            }
        }

        /// <summary>Does the specified table exist?</summary>
        /// <param name="connection">SQLite connection</param>
        /// <param name="tableName">The table name to look for</param>
        private bool TableExists(SQLite connection, string tableName)
        {
            List<string> tableNames = DataTableUtilities.GetColumnAsStrings(connection.ExecuteQuery("SELECT * FROM sqlite_master"), "Name").ToList();
            return tableNames.Contains(tableName);
        }

        /// <summary>Lookup and return units for the specified column.</summary>
        /// <param name="connection">SQLite connection</param>
        /// <param name="columnName">The column name to return units for</param>
        private string LookupUnitsForColumn(SQLite connection, string columnName)
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

        /// <summary>Alter an existing table ensuring all columns exist.</summary>
        private void UpdateColumnsFromRowsToWrite()
        {
            Dictionary<string, string> allColumnNames = new Dictionary<string, string>();
            foreach (Row row in RowsToWrite)
                for (int colIndex = 0; colIndex < row.ColumnNames.Count(); colIndex++)
                {
                    if (!allColumnNames.ContainsKey(row.ColumnNames.ElementAt(colIndex)))
                        allColumnNames.Add(row.ColumnNames.ElementAt(colIndex), row.ColumnUnits.ElementAt(colIndex));
                }

            foreach (KeyValuePair<string,string> column in allColumnNames)
                if (Columns.Find(col => col.Name == column.Key) == null)
                    Columns.Add(new Column(column.Key, column.Value));
        }

        /// <summary>Ensure columns exist in .db file</summary>
        /// <param name="connection">The SQLite connection to write to</param>
        private void CreateTable(SQLite connection)
        {
            StringBuilder sql = new StringBuilder();

            bool hasSimulationName = RowsToWrite.Count > 0 && RowsToWrite[0].SimulationName != null;
            if (hasSimulationName)
                Columns.Insert(0, new Column("SimulationID", null));

            foreach (Column col in Columns)
            {
                if (sql.Length > 0)
                    sql.Append(',');

                sql.Append("[");
                sql.Append(col.Name);
                sql.Append("] ");
                sql.Append(GetSQLiteDataType(col.Name));
            }
            sql.Insert(0, "CREATE TABLE " + Name + " (");
            sql.Append(')');
            connection.ExecuteNonQuery(sql.ToString());
        }

        /// <summary>Alter an existing table ensuring all columns exist.</summary>
        /// <param name="connection">The SQLite connection to write to</param>
        private void AlterTable(SQLite connection)
        {
            DataTable columnData = connection.ExecuteQuery("pragma table_info('" + Name + "')");
            List<string> existingColumns = DataTableUtilities.GetColumnAsStrings(columnData, "Name").ToList();

            bool hasSimulationName = RowsToWrite.Count > 0 && RowsToWrite[0].SimulationName != null;
            if (hasSimulationName && Columns.Find(col => col.Name == "SimulationID") == null)
                Columns.Insert(0, new Column("SimulationID", null));

            foreach (Column col in Columns)
            {
                if (!existingColumns.Contains(col.Name))
                {
                    string sql = "ALTER TABLE " + Name + " ADD COLUMN [" + col.Name + "] " + GetSQLiteDataType(col.Name);
                    connection.ExecuteNonQuery(sql);
                }
            }
        }

        /// <summary>Convert .NET type into an SQLite type</summary>
        private string GetSQLiteDataType(string columnName)
        {
            // Find the first non null value if possible.
            object value = null;
            foreach (Row row in RowsToWrite)
            {
                int i = row.ColumnNames.ToList().IndexOf(columnName);
                if (i != -1)
                {
                    value = row.Values.ElementAt(i);
                    if (value != null) break;
                }
            }

            // Convert the value we found above into an SQLite data type string and return it.
            Type type = null;
            if (value != null)
                type = value.GetType();

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
        private IntPtr CreateInsertQuery(SQLite connection)
        {
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
            return connection.Prepare(sql.ToString());
        }

    }
}
