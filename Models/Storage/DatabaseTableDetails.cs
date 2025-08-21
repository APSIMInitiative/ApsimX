using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using PdfSharpCore.Pdf.Content;

namespace Models.Storage
{

    class DatabaseTableDetails
    {
        private static readonly string[] indexColumns = { "ID", "CheckpointID", "SimulationID" };

        /// <summary>The datastore connection.</summary>
        private IDatabaseConnection connection;

        /// <summary>A list of column names in the table?</summary>
        private HashSet<string> columnNamesInDb = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseConnection">The datastore connection.</param>
        /// <param name="tableName">Name of table.</param>
        public DatabaseTableDetails(IDatabaseConnection databaseConnection, string tableName)
        {
            connection = databaseConnection;
            Name = tableName;
            TableExistsInDb = connection.TableExists(tableName);
            if (TableExistsInDb)
                connection.GetTableColumns(tableName).ForEach(col => columnNamesInDb.Add(col));
        }

        /// <summary>Name of table.</summary>
        public string Name { get; private set; }

        /// <summary>Does the table exist in the .db file?</summary>
        public bool TableExistsInDb { get; private set; }

        /// <summary>Lock object.</summary>
        private static object lockObject = new object();

        /// <summary>Ensure the specified table matches our columns and row values.</summary>
        /// <param name="table">The table definition to write to the database.</param>
        public void EnsureTableExistsAndHasRequiredColumns(ref DataTable table)
        {
            lock (lockObject)
            {
                // Check to make sure the table exists and has our columns.
                if (TableExistsInDb)
                    AlterTable(ref table);
                else
                    CreateTable(table);
            }
        }

        /// <summary>Create a table that matches the specified table.</summary>
        /// <param name="table">The table definition to write to the database.</param>
        private void CreateTable(DataTable table)
        {
            List<string> colTypes = new List<string>();

            foreach (DataColumn column in table.Columns)
            {
                columnNamesInDb.Add(column.ColumnName);
                colTypes.Add(connection.GetDBDataTypeName(column.DataType));
            }
            connection.CreateTable(Name, columnNamesInDb.ToList(), colTypes);

            if (table.Columns.Contains("ID") && table.TableName.StartsWith("_"))
                connection.CreateIndex(table.TableName, new List<string>() { "ID" }, true);
            else if (table.Columns.Contains("CheckpointID") && table.Columns.Contains("SimulationID"))
                connection.CreateIndex(table.TableName, new List<string>() { "CheckpointID", "SimulationID" }, false);

            TableExistsInDb = true;
        }

        /// <summary>Alter an existing table ensuring all columns exist.
        /// Adjust column names in the DataTable to correspond in case with 
        /// those already defined in the database.</summary>
        /// <param name="table">The table definition to write to the database.</param>
        private void AlterTable(ref DataTable table)
        {
            List<string> names = new List<string>();
            List<string> columns = new List<string>();
            List<Type> dataTypes = new List<Type>();

            foreach (DataColumn column in table.Columns)
            {
                if (columnNamesInDb.TryGetValue(column.ColumnName, out string actualName))
                {
                    column.ColumnName = actualName;
                }
                else
                {
                    // Column is missing from database file - write it.
                    names.Add(Name);
                    columns.Add(column.ColumnName);
                    dataTypes.Add(column.DataType);
                    columnNamesInDb.Add(column.ColumnName);
                }
            }

            connection.BeginTransaction();
            try {
                for (int i = 0; i < names.Count; i++)
                {
                    connection.AddColumn(names[i], columns[i], connection.GetDBDataTypeName(dataTypes[i]));
                }
            }
            finally {
                connection.EndTransaction();
            }
        }
    }
}
