using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.Storage
{
    /// <summary>
    /// A storage service for writing to a SQLITE database.
    /// </summary>
    public class DataStore2 : Model, IStorage, IDisposable
    {
        /// <summary>Encapsulates a row that needs writing to the database.</summary>
        private class Row
        {
            public IEnumerable<string> ColumnNames;
            public IEnumerable<string> ColumnUnits;
            public IEnumerable<object> Values;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="columnNames">Column names</param>
            /// <param name="columnUnits">Column units</param>
            /// <param name="valuesToWrite">A row of values to write</param>
            public Row(IEnumerable<string> columnNames,
                       IEnumerable<string> columnUnits,
                       IEnumerable<object> valuesToWrite)
            {
                this.ColumnNames = columnNames;
                this.ColumnUnits = columnUnits;
                this.Values = valuesToWrite;
            }
        }

        /// <summary>Encapsulates a table that needs writing to the database.</summary>
        private class Table
        {
            /// <summary>Constructor</summary>
            /// <param name="tableName">Name of table.</param>
            public Table(string tableName)
            {
                Name = tableName;
            }

            /// <summary>Name of table.</summary>
            public string Name { get; private set; }

<<<<<<< Updated upstream
=======
            /// <summary>Are there any rows that need writing?</summary>
            public bool HasRowsToWrite { get { return RowsToWrite.Count > 0; } }

            /// <summary>Write the specified number of rows.</summary>
            public void WriteRows(SQLite connection, int numRows)
            {

            }

>>>>>>> Stashed changes
            public List<Row> RowsToWrite = new List<Row>();
            public IEnumerable<string> preparedInsertQueryColumnNames;
            public IntPtr preparedInsertQuery;

<<<<<<< Updated upstream
            
=======

            /// <summary>Write a row to the .db file</summary>
            /// <param name="connection">The SQLite connection to write to</param>
            /// <param name="rowToWrite"></param>
            private void WriteRowToDB(SQLite connection, Row rowToWrite)
            {
                if (rowToWrite.ColumnNames != preparedInsertQueryColumnNames)
                {
                    FlattenRow(rowToWrite);

                    // If this is the first time we've written to the table, then create table.
                    if (preparedInsertQueryColumnNames == null)
                    {
                        connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS " + Name + " (SimulationID INTEGER)");
                    }

                    // Do we need to (re)create the prepared SQL statement
                    if (preparedInsertQueryColumnNames != rowToWrite.ColumnNames)
                    {
                        preparedInsertQuery = PrepareInsertIntoTable(Name, rowToWrite.ColumnNames);
                        preparedInsertQueryColumnNames = rowToWrite.ColumnNames;
                    }

                    // Write the row to the .db
                    connection.BindParametersAndRunQuery(preparedInsertQuery, rowToWrite.Values);
                }
            }
>>>>>>> Stashed changes
        }

        /// <summary>Name of the database table holding information on units of measurement</summary>
        private static string UnitsTableName = "_Units";

        /// <summary>File name for SQLite database</summary>
        private string fileName;

        /// <summary>A SQLite connection shared between all instances of this DataStore.</summary>
        [NonSerialized]
        private SQLite connection = null;

        /// <summary>A List of tables that needs writing.</summary>
        private List<Table> tables = new List<Table>();

        /// <summary>A List of table names in the .db</summary>
        private List<string> tableNames = new List<string>();

        /// <summary>The simulations table in the .db</summary>
        private DataTable simulationsTable = new DataTable();

        /// <summary>Are we stopping writing to the DB?</summary>
        private bool stoppingWriteToDB;

        /// <summary>A task, run asynchronously, that writes to the .db</summary>
        private Task writeTask;

        /// <summary>Constructor</summary>
        public DataStore2() { }

        /// <summary>Constructor</summary>
        public DataStore2(string fileName) { this.fileName = fileName; }

        /// <summary>Dispose method</summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite)
        {
            // Find the table.
            lock (tables)
            {
                Table table = tables.Find(t => t.Name == tableName);
                if (table == null)
                {
                    table = new Table(tableName);
                    tables.Add(table);
                }
                table.RowsToWrite.Add(new Row(columnNames, columnUnits, valuesToWrite));
            }
        }

        /// <summary>Begin writing to DB file</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file</param>
        /// <param name="simulationNamesBeingRun">Collection of simulation names being run</param>
        public void BeginWriting(IEnumerable<string> knownSimulationNames, IEnumerable<string> simulationNamesBeingRun)
        {
            writeTask = Task.Run(() => WriteDBWorker(knownSimulationNames, simulationNamesBeingRun));
        }

        /// <summary>Finish writing to DB file</summary>
        public void EndWriting()
        {
            stoppingWriteToDB = true;
            writeTask.Wait();
        }

        /// <summary>Worker method for writing to the .db file. This runs in own thread.</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file</param>
        /// <param name="simulationNamesBeingRun">Collection of simulation names being run</param>
        private void WriteDBWorker(IEnumerable<string> knownSimulationNames, IEnumerable<string> simulationNamesBeingRun)
        {
            try
            {
                Open(readOnly: false);

                CleanupDB(knownSimulationNames, simulationNamesBeingRun);

                while (true)
                {
                    Table tableWithRows = null;
                    Row rowToWrite = null;
                    lock (tables)
                    {
                        // Find first table that has rows.
                        tableWithRows = tables.Find(table => table.RowsToWrite.Count > 0);
                        if (tableWithRows == null)
                        {
                            if (stoppingWriteToDB)
                                break;
                            else
                                Thread.Sleep(100);
                        }
                        else
                        {
                            rowToWrite = tables[0].RowsToWrite[0];
                            tables[0].RowsToWrite.RemoveAt(0);
                        }
                    }

                    WriteRowToDB(tableWithRows, rowToWrite);
                }
            }
            finally
            {
                Close();
            }
        }

<<<<<<< Updated upstream
        /// <summary>Write a row to the .db file</summary>
        /// <param name="tableWithRows">The table with the row</param>
        /// <param name="rowToWrite"></param>
        private void WriteRowToDB(Table tableWithRows, Row rowToWrite)
        {
            if (rowToWrite.ColumnNames != tableWithRows.preparedInsertQueryColumnNames)
            {
                FlattenRow(rowToWrite);

                // If this is the first time we've written to the table, then create table.
                if (tableWithRows.preparedInsertQueryColumnNames == null)
                {
                    connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS " + tableWithRows + " (SimulationID INTEGER)");
                }

                // Do we need to (re)create the prepared SQL statement
                if (tableWithRows.preparedInsertQueryColumnNames != rowToWrite.ColumnNames)
                {
                    tableWithRows.preparedInsertQuery = PrepareInsertIntoTable(tableWithRows.Name, rowToWrite.ColumnNames);
                    tableWithRows.preparedInsertQueryColumnNames = rowToWrite.ColumnNames;
                }

                // Write the row to the .db
                connection.BindParametersAndRunQuery(tableWithRows.preparedInsertQuery, rowToWrite.Values);
            }
        }
=======
>>>>>>> Stashed changes

        /// <summary>
        /// 'Flatten' the row passed in, into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        /// <param name="row">Row of data values.</param>
        private static void FlattenRow(Row row)
        {
            List<string> newColumnNames = new List<string>();
            List<string> newColumnUnits = new List<string>();
            List<object> newValues = new List<object>();

            for (int i = 0; i < row.Values.Count(); i++)
                FlattenValue(row.ColumnNames.ElementAt(i),
                             row.ColumnUnits.ElementAt(i),
                             row.Values.ElementAt(i), 
                             newColumnNames, newColumnUnits, newValues);

            row.ColumnNames = newColumnNames;
            row.ColumnUnits = newColumnUnits;
            row.Values = newValues;
        }

        /// <summary>
        /// 'Flatten' a value (if it is an array or structure) into something that can be
        /// stored in a flat database table.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="units"></param>
        /// <param name="value"></param>
        /// <param name="newColumnNames"></param>
        /// <param name="newColumnUnits"></param>
        /// <param name="newValues"></param>
        private static void FlattenValue(string name, string units, object value,
                                         List<string> newColumnNames, List<string> newColumnUnits, List<object> newValues)
        {
            if (value == null || value.GetType() == typeof(DateTime) || value.GetType() == typeof(string) || !value.GetType().IsClass)
            {
                // Scalar
                newColumnNames.Add(name);
                newColumnUnits.Add(units);
                newValues.Add(value);
            }
            else if (value.GetType().IsArray)
            {
                // Array
                Array array = value as Array;

                for (int columnIndex = 0; columnIndex < array.Length; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array.GetValue(columnIndex);
                    FlattenValue(heading, units, arrayElement,
                                 newColumnNames, newColumnUnits, newValues);  // recursion
                }
            }
            else if (value.GetType().GetInterface("IList") != null)
            {
                // List
                IList array = value as IList;
                for (int columnIndex = 0; columnIndex < array.Count; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array[columnIndex];
                    FlattenValue(heading, units, arrayElement,
                                 newColumnNames, newColumnUnits, newValues);  // recursion                }
                }
            }
            else
            {
                // A struct or class
                foreach (PropertyInfo property in ReflectionUtilities.GetPropertiesSorted(value.GetType(), BindingFlags.Instance | BindingFlags.Public))
                {
                    object[] attrs = property.GetCustomAttributes(true);
                    string propUnits = null;
                    bool ignore = false;
                    foreach (object attr in attrs)
                    {
                        if (attr is XmlIgnoreAttribute)
                        {
                            ignore = true;
                            continue;
                        }
                        Core.UnitsAttribute unitsAttr = attr as Core.UnitsAttribute;
                        if (unitsAttr != null)
                            propUnits = unitsAttr.ToString();
                    }
                    if (ignore)
                        continue;
                    string heading = name + "." + property.Name;
                    object classElement = property.GetValue(value, null);
                    FlattenValue(heading, propUnits, classElement, 
                                 newColumnNames, newColumnUnits, newValues);  // recursion
                }
            }
        }

        /// <summary>Open the SQLite database.</summary>
        /// <param name="readOnly">Open for readonly access?</param>
        /// <returns>True if file was successfully opened</returns>
        private bool Open(bool readOnly)
        {
            if (connection != null && readOnly == connection.IsReadOnly)
                return true;  // already open.

            if (connection != null && readOnly && !connection.IsReadOnly)
                return false;  // can't open for reading as we are currently writing

            if (fileName == null)
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null)
                    fileName = Path.ChangeExtension(simulations.FileName, ".db");
                else
                    throw new Exception("Cannot find a filename for the SQLite database.");
            }

            Close();
            connection = new SQLite();
            if (!File.Exists(fileName))
            {
                connection.OpenDatabase(fileName, readOnly: false);
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT COLLATE NOCASE)");
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Messages (SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS " + UnitsTableName + " (SimulationID INTEGER, TableName TEXT, ColumnHeading TEXT, Units TEXT)");
                Close();
            }

            connection = new SQLite();
            connection.OpenDatabase(fileName, readOnly);

            Refresh();

            return true;
        }

        /// <summary>Close the SQLite database.</summary>
        private void Close()
        {
            if (connection != null)
            {
                connection.CloseDatabase();
                connection = null;
            }
        }

        /// <summary>Refresh our tables structure and simulation Ids</summary>
        private void Refresh()
        {
            // Get a list of table names.
            DataTable tableData = connection.ExecuteQuery("SELECT * FROM sqlite_master");
            foreach (string tableName in DataTableUtilities.GetColumnAsStrings(tableData, "Name"))
            {
                Table table = tables.Find(t => t.Name == tableName);
                if (table == null)
                    table = new Table(tableName);
                tableNames.AddRange();
            }

            // Get a list of simulation names
            simulationsTable = connection.ExecuteQuery("SELECT ID, Name FROM Simulations ORDER BY Name");
        }

        /// <summary>Remove all simulations from the database that don't exist in 'simulationsToKeep'</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file</param>
        /// <param name="simulationNamesToBeRun">The simulation names about to be run.</param>
        private void CleanupDB(IEnumerable<string> knownSimulationNames, IEnumerable<string> simulationNamesToBeRun)
        {
            // Make sure each known simulation name has an ID in the simulations table in the .db
            ExecuteInsertQuery("Simulations", "Name", knownSimulationNames);

            // Get a list of simulation names that are in the .db but we know nothing about them
            // i.e. they are old and no longer needed.
            // Then delete the unknown simulation names from the simulations table.
            string[] simulationNamesInDB = DataTableUtilities.GetColumnAsStrings(simulationsTable, "Name");
            List<string> unknownSimulationNames = new List<string>();
            foreach (string simulationNameInDB in simulationNamesInDB)
                if (!knownSimulationNames.Contains(simulationNameInDB))
                    unknownSimulationNames.Add(simulationNameInDB);
            ExecuteDeleteQuery("DELETE FROM Simulations WHERE [SimulationName] IN (", unknownSimulationNames, ")");

            // Refresh our simulation table in memory now that we have removed unwanted ones.
            Refresh();

            // Delete all data that we are about to run from all tables except Simulations and Units 
            foreach (string tableName in tableNames)
                if (tableName != "Simulations")
                    ExecuteDeleteQueryUsingIDs("DELETE FROM " + tableName + " WHERE [SimulationID] IN (", simulationNamesToBeRun, ")");

            // Refresh our simulation table in memory now that we have removed unwanted ones.
            Refresh();
        }

        /// <summary>
        /// Execute an INSERT or DELETE query, inserting or deleting the specified simulation names.
        /// </summary>
        /// <param name="tableName">Name of table to insert into</param>
        /// <param name="columnName">Name of column in table to insert values for</param>
        /// <param name="simulationNames">The names of the simulations</param>
        private void ExecuteInsertQuery(string tableName, string columnName, IEnumerable<string> simulationNames)
        {

            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO [" + tableName + "] (" + columnName + ") VALUES ");
            for (int i = 0; i < simulationNames.Count(); i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.AppendFormat("('{0}')", simulationNames.ElementAt(i));

                // It appears that SQLite can't handle lots of values in SQL INSERT INTO statements
                // so we will run the query on batches of ~100 values at a time.
                if (sql.Length > 0 && (i+1 % 100 == 0 || i == simulationNames.Count() - 1))
                    connection.ExecuteNonQuery(sql.ToString());
            }
        }

        /// <summary>
        /// Execute an INSERT or DELETE query, inserting or deleting the specified simulation names.
        /// </summary>
        /// <param name="sqlPrefix">SQL prefix</param>
        /// <param name="simulationNames">The names of the simulations</param>
        /// <param name="sqlSuffix">SQL suffix</param>
        private void ExecuteDeleteQuery(string sqlPrefix, IEnumerable<string> simulationNames, string sqlSuffix)
        {
            StringBuilder sql = new StringBuilder();
            for (int i = 0; i < simulationNames.Count(); i++)
            {
                if (i > 0)
                    sql.Append(',');
                sql.AppendFormat("'{0}'", simulationNames.ElementAt(i));

                // It appears that SQLite can't handle lots of values in SQL INSERT INTO statements
                // so we will run the query on batches of ~100 values at a time.
                if (sql.Length > 0 && (i + 1 % 100 == 0 || i == simulationNames.Count() - 1))
                    connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
            }
        }

        /// <summary>
        /// Execute an INSERT or DELETE query, inserting or deleting the specified simulation names.
        /// This method will use IDs.
        /// </summary>
        /// <param name="sqlPrefix">SQL prefix</param>
        /// <param name="simulationNames">The names of the simulations</param>
        /// <param name="sqlSuffix">SQL suffix</param>
        private void ExecuteDeleteQueryUsingIDs(string sqlPrefix, IEnumerable<string> simulationNames, string sqlSuffix)
        {
            DataView simulationTableView = new DataView(simulationsTable);

            StringBuilder sql = new StringBuilder();
            for (int i = 0; i < simulationNames.Count(); i++)
            {
                if (i > 0)
                    sql.Append(',');
                simulationTableView.RowFilter = "Name='" + simulationNames.ElementAt(i) + "'";
                if (simulationTableView.Count == 1)
                {
                    int id = (int) simulationTableView[0]["ID"];
                    sql.Append(id);
                }

                // It appears that SQLite can't handle lots of values in SQL DELETE statements
                // so we will run the query on batches of ~100 values at a time.
                if (sql.Length > 0 && (i+1 % 100 == 0 || i == simulationNames.Count() - 1))
                    connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
            }
        }

        /// <summary>Go prepare an insert into query and return the query.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        private IntPtr PrepareInsertIntoTable(string tableName, IEnumerable<string> names)
        {
            string command = "INSERT INTO " + tableName + "(";

            for (int i = 0; i < names.Count(); i++)
            {
                if (i > 0)
                    command += ",";
                command += "[" + names.ElementAt(i) + "]";
            }
            command += ") VALUES (";

            for (int i = 0; i < names.Count(); i++)
            {
                if (i > 0)
                    command += ",";
                command += "?";
            }
            command += ")";
            return connection.Prepare(command);
        }

    }
}
