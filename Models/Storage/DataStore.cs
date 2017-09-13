namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// A storage service for reading and writing to/from a SQLITE database.
    /// </summary>
    public class DataStore : Model, IStorageReader, IStorageWriter, IDisposable
    {
        /// <summary>A SQLite connection shared between all instances of this DataStore.</summary>
        [NonSerialized]
        private SQLite connection = null;

        /// <summary>A List of tables that needs writing.</summary>
        private List<Table> tables = new List<Table>();

        /// <summary>The simulations table in the .db</summary>
        private Dictionary<string, int> simulationIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Are we stopping writing to the DB?</summary>
        private bool stoppingWriteToDB;

        /// <summary>A task, run asynchronously, that writes to the .db</summary>
        private Task writeTask;

        /// <summary>Return a list of simulations names or empty string[]. Never returns null.</summary>
        public string[] SimulationNames
        {
            get
            {
                if (FileName == null)
                    Open(readOnly: true);
                return simulationIDs.Select(p => p.Key).ToArray();
            }
        }

        /// <summary>Returns a list of table names</summary>
        public IEnumerable<string> TableNames
        {
            get
            {
                if (FileName == null)
                    Open(readOnly: true);
                return tables.FindAll(t => t.Exists && !t.Name.StartsWith("_")).Select(t => t.Name);
            }
        }

        /// <summary>Returns the file name of the .db file</summary>
        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>Constructor</summary>
        public DataStore() { }

        /// <summary>Constructor</summary>
        public DataStore(string fileNameToUse) { FileName = fileNameToUse; }

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
                table.RowsToWrite.Add(new Row(simulationName, columnNames, columnUnits, valuesToWrite));
            }
        }

        /// <summary>Begin writing to DB file</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file. If null no cleanup will be performed.</param>
        /// <param name="simulationNamesBeingRun">Collection of simulation names being run. If null no cleanup will be performed.</param>
        public void BeginWriting(IEnumerable<string> knownSimulationNames = null, IEnumerable<string> simulationNamesBeingRun = null)
        {
            stoppingWriteToDB = false;
            writeTask = Task.Run(() => WriteDBWorker(knownSimulationNames, simulationNamesBeingRun));
        }

        /// <summary>Finish writing to DB file</summary>
        public void EndWriting()
        {
            stoppingWriteToDB = true;
            writeTask.Wait();
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Optional column names to retrieve from storage</param>
        /// <param name="filter">Optional filter</param>
        /// <param name="from">Optional start index. Only used when 'count' specified. The record number to offset.</param>
        /// <param name="count">Optional number of records to return or all if 0.</param>
        /// <returns></returns>
        public DataTable GetData(string tableName, string simulationName = null, IEnumerable<string> fieldNames = null,
                                 string filter = null,
                                 int from = 0, int count = 0)
        {
            Open(readOnly: true);

            // If currently writing, wait for all records to be written.
            WaitForAllRecordsToBeWritten();

            Table table = tables.Find(t => t.Name == tableName);
            if (connection == null || table == null || !table.Exists)
                return null;

            StringBuilder sql = new StringBuilder();

            // Write SELECT clause
            sql.Append("SELECT S.Name AS SimulationName, ");
            if (fieldNames == null)
                sql.Append("T.*");
            else
            {
                sql.Append("SimulationID");
                for (int i = 0; i < fieldNames.Count(); i++)
                {
                    sql.Append(',');
                    sql.Append('[');
                    sql.Append(fieldNames.ElementAt(i));
                    sql.Append(']');
                }
            }


            // Write FROM clause
            sql.Append(" FROM _Simulations S, ");
            sql.Append(tableName);
            sql.Append(" T ");

            // Write WHERE clause
            sql.Append("WHERE SimulationID = ID");
            if (simulationName != null)
            {
                sql.Append(" AND S.Name = '");
                sql.Append(simulationName);
                sql.Append('\'');
            }
            if (filter != null)
            {
                sql.Append(" AND (");
                sql.Append(filter);
                sql.Append(")");
            }

            // Write LIMIT/OFFSET clause
            if (count > 0)
            {
                sql.Append(" LIMIT ");
                sql.Append(count);
                sql.Append(" OFFSET ");
                sql.Append(from);
            }

            return connection.ExecuteQuery(sql.ToString());
        }

        /// <summary>
        /// Obtain the units for a column of data
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnHeading">Name of the data column</param>
        /// <returns>The units (with surrounding parentheses), or null if not available</returns>
        public string GetUnits(string tableName, string columnHeading)
        {
            Table table = tables.Find(t => t.Name == tableName);
            if (table != null)
            {
                Table.Column column = table.Columns.Find(c => c.Name == columnHeading);
                if (column != null)
                    return "(" + column.Units + ")";
            }
            return null;
        }

        /// <summary>
        /// Create a table in the database based on the specified data. If a 'SimulationName'
        /// column is found a corresponding 'SimulationID' column will be created.
        /// </summary>
        /// <param name="data">The data to write</param>
        public void WriteTable(DataTable data)
        {
            bool startWriteThread = writeTask == null || writeTask.IsCompleted;
            if (startWriteThread)
                BeginWriting();
            else
                WaitForAllRecordsToBeWritten();

            List<string> columnNames = new List<string>();
            foreach (DataColumn column in data.Columns)
                columnNames.Add(column.ColumnName);
            string[] units = new string[columnNames.Count];

            foreach (DataRow row in data.Rows)
            {
                object[] values = new object[columnNames.Count];
                string simulationName = null;
                if (data.Columns.Contains("SimulationName"))
                    simulationName = row["SimulationName"].ToString();
                for (int colIndex = 0; colIndex < data.Columns.Count; colIndex++)
                    values[colIndex] = row[colIndex];
                WriteRow(simulationName, data.TableName, columnNames, units, values);
            }

            if (startWriteThread)
                EndWriting();
        }

        /// <summary>Delete the specified table.</summary>
        /// <param name="tableName">Name of the table.</param>
        public void DeleteTable(string tableName)
        {
            Open(readOnly: false);
            Table tableToDelete = tables.Find(t => t.Name == tableName);
            if (tableToDelete != null)
            {
                string sql = "DROP TABLE " + tableName;
                connection.ExecuteNonQuery(sql);
                tables.Remove(tableToDelete);
            }
        }

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public DataTable RunQuery(string sql)
        {
            Open(readOnly: true);

            // If currently writing, wait for all records to be written.
            WaitForAllRecordsToBeWritten();

            try
            {
                return connection.ExecuteQuery(sql);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Return a list of simulations names or empty string[]. Never returns null.</summary>
        public IEnumerable<string> ColumnNames(string tableName)
        {
            Table table = tables.Find(t => t.Name == tableName);
            if (table != null)
                return table.Columns.Select(c => c.Name);
            return new string[0];
        }

        /// <summary>Delete all tables</summary>
        public void DeleteAllTables()
        {
            Console.WriteLine("Deleting all tables in file: " + FileName);
            bool openForReadOnly = true;
            if (connection != null)
                openForReadOnly = connection.IsReadOnly;
            Close();
            File.Delete(FileName);
            Open(openForReadOnly);
        }

        /// <summary>Get a simulation ID for the specified simulation name</summary>
        /// <param name="simulationName">The simulation name to look for</param>
        /// <returns>The database ID or -1 if not found</returns>
        public int GetSimulationID(string simulationName)
        {
            int id;
            if (simulationIDs.TryGetValue(simulationName, out id))
                return id;
            return -1;
        }

        /// <summary>Wait for all records to be written.</summary>
        private void WaitForAllRecordsToBeWritten()
        {
            // Make sure all existing writing has completed.
            if (writeTask != null && !writeTask.IsCompleted)
                while (tables.Find(table => table.HasRowsToWrite) != null)
                    Thread.Sleep(100);
        }

        /// <summary>Worker method for writing to the .db file. This runs in own thread.</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file</param>
        /// <param name="simulationNamesBeingRun">Collection of simulation names being run</param>
        private void WriteDBWorker(IEnumerable<string> knownSimulationNames, IEnumerable<string> simulationNamesBeingRun)
        {
            try
            {
                Open(readOnly: false);

                if (knownSimulationNames != null && simulationNamesBeingRun != null)
                    CleanupDB(knownSimulationNames, simulationNamesBeingRun);

                while (true)
                {
                    Table tableWithRows = null;
                    lock (tables)
                    {
                        // Find first table that has rows.
                        tableWithRows = tables.Find(table => table.HasRowsToWrite);
                    }

                    if (tableWithRows == null)
                    {
                        if (stoppingWriteToDB)
                            break;
                        else
                            Thread.Sleep(100);
                    }

                    if (tableWithRows != null)
                    {
                        int numRowsWritten = tableWithRows.WriteRows(connection, simulationIDs);
                        if (numRowsWritten > 0)
                        {
                            lock (tables)
                            {
                                tableWithRows.RowsToWrite.RemoveRange(0, numRowsWritten);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
            finally   
            {
                WriteUnitsTable();
                Close();
                Open(readOnly: true);
            }
        }

        /// <summary>Write a _units table to .db</summary>
        private void WriteUnitsTable()
        {
            connection.ExecuteQuery("DELETE FROM _Units");
            foreach (Table table in tables)
            {
                foreach (Table.Column column in table.Columns)
                {
                    if (column.Units != null)
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.Append("INSERT INTO [_Units] (TableName, ColumnHeading, Units) VALUES ('");
                        sql.Append(table.Name);
                        sql.Append("','");
                        sql.Append(column.Name);
                        sql.Append("','");
                        sql.Append(column.Units);
                        sql.Append("\')");
                        connection.ExecuteNonQuery(sql.ToString());
                    }
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

            if (FileName == null)
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null)
                    FileName = Path.ChangeExtension(simulations.FileName, ".db");
                else
                    throw new Exception("Cannot find a filename for the SQLite database.");
            }

            Close();
            connection = new SQLite();
            if (!File.Exists(FileName))
            {
                connection.OpenDatabase(FileName, readOnly: false);
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS _Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT COLLATE NOCASE)");
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS _Messages (SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS _Units (TableName TEXT, ColumnHeading TEXT, Units TEXT)");
                connection.CloseDatabase();
            }

            connection.OpenDatabase(FileName, readOnly);

            Refresh();

            return true;
        }

        /// <summary>Close the SQLite database.</summary>
        private void Close()
        {
            if (connection != null)
            {
                Console.WriteLine("DataStore closing file " + FileName);

                // Get a list of all table instances that don't have rows to write to the .db file. 
                List<Table> tablesToRemove = tables.FindAll(t => !t.HasRowsToWrite);

                // Dispose of and remove all table instances that don't have rows to write.
                tablesToRemove.ForEach(t => t.Dispose());
                tablesToRemove.ForEach(t => tables.Remove(t));

                simulationIDs.Clear();
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
                {
                    table = new Table(tableName, connection);
                    tables.Add(table);
                }
                else
                    table.SetConnection(connection);
            }

            // Get a list of simulation names
            simulationIDs.Clear();
            DataTable simulationTable = connection.ExecuteQuery("SELECT ID, Name FROM _Simulations ORDER BY Name");
            foreach (DataRow row in simulationTable.Rows)
            {
                string name = row["Name"].ToString();
                if (!simulationIDs.ContainsKey(name))
                    simulationIDs.Add(name, Convert.ToInt32(row["ID"]));
            }
        }

        /// <summary>Remove all simulations from the database that don't exist in 'simulationsToKeep'</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file</param>
        /// <param name="simulationNamesToBeRun">The simulation names about to be run.</param>
        private void CleanupDB(IEnumerable<string> knownSimulationNames, IEnumerable<string> simulationNamesToBeRun)
        {
            // Delete all tables in .db when all sims are being run. 
            if (knownSimulationNames.SequenceEqual(simulationNamesToBeRun))
                DeleteAllTables();
            else
            {
                // Get a list of simulation names that are in the .db but we know nothing about them
                // i.e. they are old and no longer needed.
                // Then delete the unknown simulation names from the simulations table.
                string[] simulationNamesInDB = simulationIDs.Keys.ToArray();
                List<string> unknownSimulationNames = new List<string>();
                foreach (string simulationNameInDB in simulationNamesInDB)
                    if (!knownSimulationNames.Contains(simulationNameInDB))
                        unknownSimulationNames.Add(simulationNameInDB);
                ExecuteDeleteQuery("DELETE FROM _Simulations WHERE [Name] IN (", unknownSimulationNames, ")");

                // Delete all data that we are about to run, plus all data from simulations we
                // know nothing about, from all tables except Simulations and Units 
                unknownSimulationNames.AddRange(simulationNamesToBeRun);
                foreach (Table table in tables)
                    if (table.Columns.Find(c => c.Name == "SimulationID") != null)
                        ExecuteDeleteQueryUsingIDs("DELETE FROM " + table.Name + " WHERE [SimulationID] IN (", unknownSimulationNames, ")");
            }

            // Make sure each known simulation name has an ID in the simulations table in the .db
            ExecuteInsertQuery("_Simulations", "Name", knownSimulationNames);

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
            for (int i = 0; i < simulationNames.Count(); i++)
            {
                if (!simulationIDs.ContainsKey(simulationNames.ElementAt(i)))
                {
                    if (sql.Length > 0)
                        sql.Append(',');
                    sql.AppendFormat("('{0}')", simulationNames.ElementAt(i));

                    // It appears that SQLite can't handle lots of values in SQL INSERT INTO statements
                    // so we will run the query on batches of ~100 values at a time.
                    if (sql.Length > 0 && ((i + 1) % 100 == 0 || i == simulationNames.Count() - 1))
                    {
                        sql.Insert(0, "INSERT INTO [" + tableName + "] (" + columnName + ") VALUES ");
                        connection.ExecuteNonQuery(sql.ToString());
                        sql.Clear();
                    }
                }
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
                if (sql.Length > 0)
                    sql.Append(',');
                sql.AppendFormat("'{0}'", simulationNames.ElementAt(i));

                // It appears that SQLite can't handle lots of values in SQL INSERT INTO statements
                // so we will run the query on batches of ~100 values at a time.
                if (sql.Length > 0 && ((i + 1) % 100 == 0 || i == simulationNames.Count() - 1))
                {
                    connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
                    sql.Clear();
                }
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
            StringBuilder sql = new StringBuilder();
            for (int i = 0; i < simulationNames.Count(); i++)
            {
                string simulationName = simulationNames.ElementAt(i);
                if (simulationIDs.ContainsKey(simulationName))
                {
                    if (sql.Length > 0)
                        sql.Append(',');
                    sql.Append(simulationIDs[simulationName]);
                }

                // It appears that SQLite can't handle lots of values in SQL DELETE statements
                // so we will run the query on batches of ~100 values at a time.
                if (sql.Length > 0 && ((i+1) % 100 == 0 || i == simulationNames.Count() - 1))
                {
                    connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
                    sql.Clear();
                }
            }
        }
    }
}
