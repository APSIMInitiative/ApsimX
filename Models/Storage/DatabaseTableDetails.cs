using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;

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

        /// <summary>Ensure the specified table matches our columns and row values.</summary>
        /// <param name="table">The table definition to write to the database.</param>
        public void EnsureTableExistsAndHasRequiredColumns(DataTable table)
        {
            // Check to make sure the table exists and has our columns.
            if (TableExistsInDb)
                AlterTable(table);
            else
                CreateTable(table);
        }

        /// <summary>Create a table that matches the specified table.</summary>
        /// <param name="table">The table definition to write to the database.</param>
        private void CreateTable(DataTable table)
        {
            List<string> colTypes = new List<string>();

            foreach (DataColumn column in table.Columns)
            {
                columnNamesInDb.Add(column.ColumnName);
                bool allowLongStrings = table.TableName.StartsWith("_");
                colTypes.Add(connection.GetDBDataTypeName(column.DataType, allowLongStrings));
            }
            connection.CreateTable(Name, columnNamesInDb.ToList(), colTypes);

            if (table.Columns.Contains("ID") && table.TableName.StartsWith("_"))
                connection.CreateIndex(table.TableName, new List<string>() { "ID" }, true);
            else if (table.Columns.Contains("CheckpointID") && table.Columns.Contains("SimulationID"))
                connection.CreateIndex(table.TableName, new List<string>() { "CheckpointID", "SimulationID" }, false);

            TableExistsInDb = true;
        }

        /// <summary>Alter an existing table ensuring all columns exist.</summary>
        /// <param name="table">The table definition to write to the database.</param>
        private void AlterTable(DataTable table)
        {
            bool haveBegunTransaction = false;

            foreach (DataColumn column in table.Columns)
            {
                if (!columnNamesInDb.Contains(column.ColumnName))
                {
                    if (!haveBegunTransaction)
                    {
                        haveBegunTransaction = true;
                        connection.BeginTransaction();
                    }

                    // Column is missing from database file - write it.
                    bool allowLongStrings = table.TableName.StartsWith("_");
                    connection.AddColumn(Name, column.ColumnName, connection.GetDBDataTypeName(column.DataType, allowLongStrings));
                    columnNamesInDb.Add(column.ColumnName);
                }
            }

            // End the transaction that we started above.
            if (haveBegunTransaction)
                connection.EndTransaction();
        }
    }
}
