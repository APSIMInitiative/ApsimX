namespace Models.PostSimulationTools
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
	using System.Globalization;
	using System.Linq;
    using System.Text;

    /// <summary>
    /// A post processing model that send one or more tables from the DataStore to 
    /// a SQLServer database.
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(DataStore))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class WriteTableToSqlServer : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>Connection string.</summary>
        [Description("Connection string to connect to database.")]
        public string ConnectionString { get; set; }

        /// <summary>The name(s) of the tables to write to an SQLServer database.</summary>
        [Description("Table name(s)")]
        public string[] TableNames { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (dataStore?.Writer != null)
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    foreach (var tableName in TableNames)
                    {
                        if (dataStore.Reader.TableNames.Contains(tableName))
                        {
                            var data = dataStore.Reader.GetData(tableName);
                            data.TableName = tableName;
                            if (data != null)
                            {
                                // Strip out unwanted columns.
                                data.Columns.Remove("CheckpointName");
                                data.Columns.Remove("CheckpointID");
                                data.Columns.Remove("SimulationID");

                                CreateTableIfNotExists(connection, data);

                                var columnNames = DataTableUtilities.GetColumnNames(data).ToList();
                                var sql = CreateInsertSQL(tableName, columnNames);

                                using (SqlCommand cmd = new SqlCommand(sql, connection))
                                {
                                    cmd.Prepare();

                                    foreach (DataRow row in data.Rows)
                                        BindParametersAndRunQuery(cmd, columnNames, row.ItemArray);
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>Create a new table if it doesn't already exist.</summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="table">The table.</param>
        public void CreateTableIfNotExists(SqlConnection connection, DataTable table)
        {
            if (!GetTableNames(connection).Contains(table.TableName))
            {
                var sql = new StringBuilder();
                var columnNames = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    columnNames.Add(column.ColumnName);
                    if (sql.Length > 0)
                        sql.Append(',');

                    sql.Append("\"");
                    sql.Append(column.ColumnName);
                    sql.Append("\" ");
                    if (column.DataType == null)
                        sql.Append("int");
                    else
                        sql.Append(GetDBDataTypeName(column.DataType));
                }

                sql.Insert(0, "CREATE TABLE [" + table.TableName + "] (");
                sql.Append(')');
                using (SqlCommand cmd = new SqlCommand(sql.ToString(), connection))
                    cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Return a list of table names</summary>
        /// <param name="connection">Database connection.</param>
        public List<string> GetTableNames(SqlConnection connection)
        {
            List<string> tableNames = new List<string>();
            var sql = "SELECT * FROM INFORMATION_SCHEMA.TABLES";
            using (SqlCommand cmd = new SqlCommand(sql, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    var tableData = new DataTable();
                    tableData.Load(reader);
                    var names = DataTableUtilities.GetColumnAsStrings(tableData, "TABLE_NAME", CultureInfo.InvariantCulture);
                    var types = DataTableUtilities.GetColumnAsStrings(tableData, "TABLE_TYPE", CultureInfo.InvariantCulture);
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (types[i].Contains("TABLE"))
                            tableNames.Add(names[i]);
                    }
                    return tableNames;
                }
            }
        }

        /// <summary>Convert .NET type into an SQLServer type</summary>
        /// <param name="type">The .NET type</param>
        public string GetDBDataTypeName(Type type)
        {
            if (type == null)
                return "int";
            else if (type.ToString() == "System.DateTime")
                return "date";
            else if (type.ToString() == "System.Int32")
                return "int";
            else if (type.ToString() == "System.Single")
                return "real";
            else if (type.ToString() == "System.Double")
                return "real";
            else if (type.ToString() == "System.Boolean")
                return "int";
            else
                return "text";
        }

        /// <summary>Create an SQL insert statement with parameters.</summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columnNames">The column names</param>
        private string CreateInsertSQL(string tableName, List<string> columnNames)
        {
            var sql = new StringBuilder();
            sql.Append("INSERT INTO [");
            sql.Append(tableName);
            sql.Append("](");

            for (int i = 0; i < columnNames.Count; i++)
            {
                string columnName = columnNames[i];
                if (i > 0)
                    sql.Append(',');
                sql.Append('[');
                sql.Append(columnName);
                sql.Append(']');
            }
            sql.Append(") VALUES (");

            foreach (var columnName in columnNames)
            {
                if (sql[sql.Length - 1] != '(')
                    sql.Append(',');
                sql.Append($"@{columnName.Replace(".", "")}");
            }

            sql.Append(')');

            return sql.ToString();
        }

        /// <summary>Bind all parameters values to the specified command and execute it.</summary>
        /// <param name="command">The SQL command.</param>
        /// <param name="columnNames">The column names.</param>
        /// <param name="values">The row values.</param>
        public void BindParametersAndRunQuery(SqlCommand command, List<string> columnNames, IEnumerable<object> values)
        {
            command.Parameters.Clear();
            for (int i = 0; i < columnNames.Count; i++)
            {
                string parameterName = columnNames[i].Replace(".", "");
                object value = values.ElementAt(i);
                if (Convert.IsDBNull(value) || value == null)
                    command.Parameters.AddWithValue(parameterName, Convert.DBNull);

                // Enums have an underlying type of Int32, but we want to store
                // their string representation, not their integer value
                else if (value.GetType().IsEnum)
                    command.Parameters.AddWithValue(parameterName, value.ToString());
                else
                    command.Parameters.AddWithValue(parameterName, value);

            }
            command.ExecuteNonQuery();
        }
    }
}
