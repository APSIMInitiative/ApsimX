namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// # [Name]
    /// A storage service for reading and writing to/from a database.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.DataStoreView")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class DataStore : Model, IStorageReader, IStorageWriter, IDisposable
    {
        /// <summary>A database connection</summary>
        [NonSerialized]
        private IDatabaseConnection connection = null;

        /// <summary>
        /// Selector for the database type. Set in the constructors.
        /// </summary>
        private bool useFirebird;

        /// <summary>A List of tables that needs writing.</summary>
        [NonSerialized]
        private List<Table> tables = new List<Table>();

        /// <summary>Data that needs writing</summary>
        [NonSerialized]
        private List<Table> dataToWrite = new List<Table>();

        /// <summary>The IDS for all simulations</summary>
        [NonSerialized]
        private Dictionary<string, int> simulationIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The IDs for all checkpoints</summary>
        [NonSerialized]
        private Dictionary<string, int> checkpointIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Are we stopping writing to the DB?</summary>
        private bool stoppingWriteToDB;

        /// <summary>A task, run asynchronously, that writes to the .db</summary>
        [NonSerialized]
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
                return tables.FindAll(t => !t.Name.StartsWith("_")).Select(t => t.Name);
            }
        }

        /// <summary>Returns the file name of the .db file</summary>
        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>Constructor</summary>
        public DataStore()
        {
            useFirebird = false;    // select Firebird or SQLite
        }

        /// <summary>Constructor</summary>
        public DataStore(string fileNameToUse)
        {
            FileName = fileNameToUse;
            if (string.Compare(Path.GetExtension(FileName), ".fdb", true) == 0)
                useFirebird = true; 
        }

        /// <summary>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~DataStore()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed = false;

        /// <summary>Dispose method</summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
            Close();
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    // component.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                Close();

                // Note disposing has been done.
                disposed = true;
            }
        }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite)
        {
            Table t;
            int checkpointID = checkpointIDs["Current"];
            int simulationID = -1;
            if (simulationName != null)
            {
                if (!simulationIDs.TryGetValue(simulationName, out simulationID))
                    simulationID = 0;  // Denotes a simulation name was supplied but it isn't one we know about.
            }
            lock (dataToWrite)
            {
                t = dataToWrite.Find(table => table.Name == tableName);
                if (t == null)
                {
                    t = new Table(tableName);
                    dataToWrite.Add(t);
                }
            }
            t.AddRow(connection, checkpointID, simulationID, columnNames, columnUnits, valuesToWrite);
        }

        /// <summary>Simulation runs are about to begin.</summary>
        [EventSubscribe("RunCommencing")]
        private void OnRunCommencing(IEnumerable<string> knownSimulationNames = null,
                                     IEnumerable<string> simulationNamesBeingRun = null)
        {
            stoppingWriteToDB = false;
            if (knownSimulationNames != null)
            {
                Open(readOnly: false);
                CleanupDB(knownSimulationNames, simulationNamesBeingRun);
            }
            StartDBWriteThread();
        }

        /// <summary>Finish writing to DB file</summary>
        [EventSubscribe("EndRun")]
        private void OnEndRun(object sender, EventArgs e)
        {
            StopDBWriteThread();

            Open(readOnly: true);

            // Cleanup unused fields.
            CleanupUnusedFields();

            // Call the all completed event in all models
            if (Parent != null)
            {
                object[] args = new object[] { this, new EventArgs() };
                foreach (IPostSimulationTool tool in Apsim.FindAll(Parent, typeof(IPostSimulationTool)))
                {
                    if ((tool as IModel).Enabled)
                        tool.Run(this);
                }
            }
        }

        /// <summary>
        /// Return all data from the specified simulation and table name. If simulationName = "*"
        /// the all simulation data will be returned.
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint.</param>
        /// <param name="simulationName">Name of the simulation.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="fieldNames">Optional column names to retrieve from storage</param>
        /// <param name="filter">Optional filter</param>
        /// <param name="from">Optional start index. Only used when 'count' specified. The record number to offset.</param>
        /// <param name="count">Optional number of records to return or all if 0.</param>
        /// <returns></returns>
        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null,
                                 string filter = null,
                                 int from = 0, int count = 0)
        {
            Open(readOnly: true);

            Table table = tables.Find(t => t.Name == tableName);
            if (connection == null || table == null)
                return null;

            StringBuilder sql = new StringBuilder();

            bool hasToday = false;

            // Write SELECT clause
            List<string> fieldList = null;
            if (fieldNames == null)
                fieldList = table.Columns.Select(col => col.Name).ToList();
            else
                fieldList = fieldNames.ToList();

            bool hasSimulationName = fieldList.Contains("SimulationID") || fieldList.Contains("SimulationName") || simulationName != null;

            sql.Append("SELECT C.Name AS CheckpointName, C.ID AS CheckpointID");
            if (hasSimulationName)
                sql.Append(", S.Name AS SimulationName,S.ID AS SimulationID");

            fieldList.Remove("CheckpointID");
            fieldList.Remove("SimulationName");
            fieldList.Remove("SimulationID");

            foreach (string fieldName in fieldList)
            {
                if (table.HasColumn(fieldName))
                {
                    sql.Append(",T.");
                    sql.Append("[");
                    sql.Append(fieldName);
                    sql.Append(']');
                    if (fieldName == "Clock.Today")
                        hasToday = true;
                }
            }

            // Write FROM clause
            sql.Append(" FROM [_Checkpoints] C");
            if (hasSimulationName)
                sql.Append(", [_Simulations] S");
            sql.Append(", [" + tableName);
            sql.Append("] T ");

            // Write WHERE clause
            sql.Append("WHERE CheckpointID = C.ID");
            if (hasSimulationName)
            {
                sql.Append(" AND SimulationID = S.ID");
                if (simulationName != null)
                {
                    sql.Append(" AND S.Name = '");
                    sql.Append(simulationName);
                    sql.Append('\'');
                }
            }

            // Write checkpoint name
            if (checkpointName == null)
                sql.Append(" AND C.Name = 'Current'");
            else
                sql.Append(" AND C.Name = '" + checkpointName + "'");

            if (filter != null)
            {
                sql.Append(" AND (");
                sql.Append(filter);
                sql.Append(")");
            }

            // Write ORDER BY clause
            if (hasSimulationName)
            {
                sql.Append(" ORDER BY S.ID");
                if (hasToday)
                    sql.Append(", T.[Clock.Today]");
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
                if (column != null && !String.IsNullOrEmpty(column.Units))
                    return "(" + column.Units + ")";
            }
            return null;
        }

        /// <summary>
        /// Add units to table. Removes old units first.
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="columnNames">The column names to add</param>
        /// <param name="columnUnits">The column units to add</param>
        public void AddUnitsForTable(string tableName, List<string> columnNames, List<string> columnUnits)
        {
            Table foundTable = tables.Find(t => t.Name == tableName);
            if (foundTable != null)
            {
                for (int i = 0; i < columnNames.Count; i++)
                {
                    Table.Column column = foundTable.Columns.Find(c => c.Name == columnNames[i]);
                    if (column != null)
                        column.Units = columnUnits[i];
                }
            }

            connection.ExecuteNonQuery("DELETE FROM [_Units] WHERE TableName = '" + tableName + "'");

            List<object[]> values = new List<object[]>();
            for (int i = 0; i < columnNames.Count; i++)
            {
                values.Add(new object[] { tableName, columnNames[i], columnUnits[i] });
            }
            List<string> unitsColumns = new List<string>(new string[] { "TableName", "ColumnHeading", "Units" });
            connection.InsertRows("_Units", unitsColumns, values);
        }

        /// <summary>
        /// Create a table in the database based on the specified data. If a 'SimulationName'
        /// column is found a corresponding 'SimulationID' column will be created.
        /// </summary>
        /// <param name="data">The data to write</param>
        public void WriteTable(DataTable data)
        {
            Open(readOnly: false);
            SortedSet<string> simulationNames = new SortedSet<string>();

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
                simulationNames.Add(simulationName);
            }

            bool startWriteThread = writeTask == null || writeTask.IsCompleted;
            if (startWriteThread)
            {
                StartDBWriteThread();
                StopDBWriteThread();
            }
        }

        /// <summary>Delete the specified table.</summary>
        /// <param name="tableName">Name of the table.</param>
        public void DeleteDataInTable(string tableName)
        {
            Open(readOnly: false);

            int checkpointID = checkpointIDs["Current"];

            if (tables.Find(table => table.Name == tableName) != null)
                connection.ExecuteNonQuery("DELETE FROM [" + tableName + "] WHERE [CheckpointID] = " + checkpointID);
        }

        /// <summary>Return all data from the specified simulation and table name.</summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public DataTable RunQuery(string sql)
        {
            Open(readOnly: true);

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
        /// <param name="tableName">The table name</param>
        public IEnumerable<string> ColumnNames(string tableName)
        {
            Open(readOnly: true);
            Table table = tables.Find(t => t.Name == tableName);
            if (table != null)
                return table.Columns.Select(c => c.Name);
            return new string[0];
        }

        /// <summary>Delete all tables</summary>
        public void EmptyDataStore()
        {
            bool openForReadOnly = true;
            if (connection != null)
                openForReadOnly = connection.IsReadOnly;

            // Don't iterate through the table or TableNames properties directly,
            // since the deletion operation can mess up the iterator.
            string[] tableNames = TableNames.ToArray();
            foreach (string tableName in tableNames)
            {
                DeleteDataInTable(tableName);
            }

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

        /// <summary>Get a checkpoint ID for the specified checkpoint name</summary>
        /// <param name="checkpointName">The simulation name to look for</param>
        /// <returns>The database ID or -1 if not found</returns>
        public int GetCheckpointID(string checkpointName)
        {
            int id;
            if (checkpointIDs.TryGetValue(checkpointName, out id))
                return id;
            return -1;
        }

        /// <summary>
        /// Return list of checkpoints.
        /// </summary>
        /// <returns></returns>
        public List<string> Checkpoints()
        {
            Open(readOnly: false);
            return checkpointIDs.Keys.ToList();
        }

        /// <summary>Add a checkpoint</summary>
        /// <param name="name">Name of checkpoint</param>
        /// <param name="filesToCheckpoint">Files to checkpoint</param>
        public void AddCheckpoint(string name, IEnumerable<string> filesToCheckpoint = null)
        {
            if (checkpointIDs.ContainsKey(name))
                DeleteCheckpoint(name);

            Open(readOnly: false);
            if (!useFirebird)
                connection.ExecuteNonQuery("BEGIN");

            int checkpointID = checkpointIDs["Current"];
            int newCheckpointID = checkpointIDs.Values.Max() + 1;

            foreach (Table t in tables)
            {
                List<string> columnNames = t.Columns.Select(column => column.Name).ToList();
                if (t.Name != "_CheckpointFiles" && columnNames.Contains("CheckpointID"))
                {
                    columnNames.Remove("CheckpointID");

                    string csvFieldNames = null;
                    foreach (string columnName in columnNames)
                    {
                        if (csvFieldNames != null)
                            csvFieldNames += ",";
                        csvFieldNames += "[" + columnName + "]";
                    }

                    connection.ExecuteNonQuery("INSERT INTO [" + t.Name + "] (" + "CheckpointID," + csvFieldNames + ")" +
                                               " SELECT " + newCheckpointID + "," + csvFieldNames +
                                               " FROM " + t.Name +
                                               " WHERE CheckpointID = " + checkpointID);
                }
            }
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string now = connection.AsSQLString(DateTime.Now);
            connection.ExecuteNonQuery("INSERT INTO [_Checkpoints] (ID, Name, Version, Date) VALUES (" + newCheckpointID + ", '" + name + "', '" + version + "', '" + now + "')");

            if (filesToCheckpoint != null)
            {
                // Add in all referenced files.
                Simulations sims = Apsim.Parent(this, typeof(Simulations)) as Simulations;

                List<object[]> valueList = new List<object[]>();
                foreach (string fileName in filesToCheckpoint)
                {
                    object[] values = new object[3];
                    if (File.Exists(fileName))
                    {
                        values[0] = newCheckpointID;
                        values[1] = fileName;
                        values[2] = File.ReadAllBytes(fileName);
                        valueList.Add(values);
                    }
                }

                List<string> colNames = new List<string>(new string[] { "CheckpointID", "FileName", "Contents" });
                connection.InsertRows("_CheckpointFiles", colNames, valueList);
            }

            if (!useFirebird)
                connection.ExecuteNonQuery("END");
            Open(readOnly: true);

            checkpointIDs.Add(name, newCheckpointID);
        }

        /// <summary>Delete a checkpoint</summary>
        /// <param name="name">Name of checkpoint</param>
        public void DeleteCheckpoint(string name)
        {
            if (!checkpointIDs.ContainsKey(name))
                throw new Exception("Cannot find checkpoint: " + name);

            if (!useFirebird)
                connection.ExecuteNonQuery("BEGIN");
            int checkpointID = checkpointIDs[name];
            foreach (Table t in tables)
            {
                List<string> columnNames = t.Columns.Select(column => column.Name).ToList();
                if (columnNames.Contains("CheckpointID"))
                    connection.ExecuteNonQuery("DELETE FROM [" + t.Name +
                                               "] WHERE [CheckpointID] = " + checkpointID);
            }
            connection.ExecuteNonQuery("DELETE FROM [_Checkpoints]" +
                                        " WHERE [ID] = " + checkpointID);
            if (!useFirebird)
                connection.ExecuteNonQuery("END");
            checkpointIDs.Remove(name);
        }

        /// <summary>Revert a checkpoint</summary>
        /// <param name="name">Name of checkpoint</param>
        public void RevertCheckpoint(string name)
        {
            if (!checkpointIDs.ContainsKey(name))
                throw new Exception("Cannot find checkpoint: " + name);

            // Revert all files.
            var files = GetCheckpointFiles(name);
            foreach (DataStore.CheckpointFile checkpointFile in files)
                File.WriteAllBytes(checkpointFile.fileName, checkpointFile.contents);

            // Revert data
            if (!useFirebird)
                connection.ExecuteNonQuery("BEGIN");
            int checkpointID = checkpointIDs[name];
            int currentID = checkpointIDs["Current"];
            foreach (Table t in tables)
            {
                List<string> columnNames = t.Columns.Select(column => column.Name).ToList();

                if (t.Name != "_CheckpointFiles" && columnNames.Contains("CheckpointID"))
                {
                    // Get a comma separated list of column names.
                    string csvFieldNames = null;
                    foreach (string columnName in columnNames)
                    {
                        if (csvFieldNames != null)
                            csvFieldNames += ",";
                        csvFieldNames += "[" + columnName + "]";
                    }

                    // Delete old current values.
                    connection.ExecuteNonQuery("DELETE FROM [" + t.Name +
                                               "] WHERE CheckpointID = " + currentID);

                    // Copy checkpoint values to current values.
                    connection.ExecuteNonQuery("INSERT INTO [" + t.Name + "] (" + "CheckpointID," + csvFieldNames + ")" +
                                               " SELECT " + currentID + "," + csvFieldNames +
                                               " FROM [" + t.Name +
                                               "] WHERE CheckpointID = " + checkpointID);
                }
            }
            if (!useFirebird)
                connection.ExecuteNonQuery("END");
        }

        /// <summary>Return a list of checkpoint files</summary>
        /// <param name="name">Name of checkpoint</param>
        public IEnumerable<CheckpointFile> GetCheckpointFiles(string name)
        {
            List<CheckpointFile> files = new List<CheckpointFile>();
            int checkpointID = GetCheckpointID(name);
            if (checkpointID != -1)
            {
                DataTable data = connection.ExecuteQuery("SELECT * FROM [_CheckpointFiles] WHERE CheckpointID = " + checkpointID);
                foreach (DataRow row in data.Rows)
                {
                    CheckpointFile file = new CheckpointFile();
                    file.fileName = row["FileName"] as string;
                    file.contents = row["Contents"] as byte[];
                    files.Add(file);
                }
            }
            return files;
        }

        /// <summary>Wait for all records to be written.</summary>
        private void WaitForAllRecordsToBeWritten()
        {
            // Make sure all existing writing has completed.
            if (writeTask != null && !writeTask.IsCompleted)
                while (IsDataToWrite())
                    Thread.Sleep(100);
        }

        /// <summary>Is there data to be written?</summary>
        private bool IsDataToWrite()
        {
            foreach (Table data in dataToWrite)
            {
                if (data.NumRowsToWrite > 0)
                    return true;
            }
            return false;
        }

        /// <summary>Start the thread that writes to the .db</summary>
        private void StartDBWriteThread()
        {
            stoppingWriteToDB = false;
            writeTask = Task.Run(() => WriteDBWorker());
        }

        /// <summary>Stop the thread that writes to the .db</summary>
        private void StopDBWriteThread()
        {
            stoppingWriteToDB = true;
            writeTask.Wait();
        }

        /// <summary>Worker method for writing to the .db file. This runs in own thread.</summary>
        private void WriteDBWorker()
        {
            Table dataToWriteToDB = null;
            try
            {
                while (true)
                {
                    dataToWriteToDB = null;
                    lock (dataToWrite)
                    {
                        // Find the table with the most number of rows to write.
                        int maxNumRows = 0;
                        foreach (Table t in dataToWrite)
                        {
                            if (t.NumRowsToWrite > maxNumRows)
                            {
                                maxNumRows = t.NumRowsToWrite;
                                dataToWriteToDB = t;
                            }
                        }
                    }

                    if (dataToWriteToDB == null)
                    {
                        if (stoppingWriteToDB)
                            break;
                        else
                            Thread.Sleep(100);
                    }
                    else
                    {
                        lock (dataToWriteToDB)
                        {
                            if (!useFirebird)
                                connection.ExecuteNonQuery("BEGIN");
                            try
                            {
                                dataToWriteToDB.WriteRows(connection, simulationIDs);
                            }
                            finally
                            {
                                if (!useFirebird)
                                    connection.ExecuteNonQuery("END");
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                string msg = "Error writing to database";
                if (dataToWriteToDB != null)
                    msg += " \"" + dataToWriteToDB.Name + "\"";
                if (Char.IsNumber(dataToWriteToDB.Name[0]))
                    msg += ": sheet name must not begin with a number!";
                throw new Exception(msg, err);
            }

            WriteUnitsTable();

            foreach (Table table in dataToWrite)
            {
                Table foundTable = tables.Find(t => t.Name == table.Name);
                if (foundTable == null)
                    tables.Add(table);
                else
                    foundTable.MergeColumns(table);
            }
            dataToWrite.Clear();
        }

        /// <summary>Write a _units table to .db</summary>
        private void WriteUnitsTable()
        {
            if (useFirebird)
            {
                StringBuilder insertSql = new StringBuilder();
                foreach (Table table in dataToWrite)
                {
                    insertSql.Append("DELETE FROM [_Units] WHERE TableName = '" + table.Name + "';\n");

                    foreach (Table.Column column in table.Columns)
                    {
                        if (column.Units != null)
                        {
                            insertSql.Append("INSERT INTO [_Units] (TableName, ColumnHeading, Units) VALUES ('");
                            insertSql.Append(table.Name);
                            insertSql.Append("','");
                            insertSql.Append(column.Name);
                            insertSql.Append("','");
                            insertSql.Append(column.Units);
                            insertSql.Append("\');\n");

                        }
                    }
                }

                if (insertSql.Length > 0)
                {
                    StringBuilder sql = new StringBuilder();
                    sql.Append("set term ^ ;\n");
                    sql.Append("EXECUTE BLOCK AS BEGIN\n");
                    sql.Append(insertSql);    
                    sql.Append("END ^");
                    connection.ExecuteNonQuery(sql.ToString());
                }
            }
            else
            {
                connection.ExecuteNonQuery("BEGIN");
                foreach (Table table in dataToWrite)
                {
                    connection.ExecuteQuery("DELETE FROM [_Units] WHERE TableName = '" + table.Name + "'");

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
                connection.ExecuteNonQuery("END");
            }
        }

        /// <summary>Open the database.</summary>
        /// <param name="readOnly">Open for readonly access?</param>
        /// <returns>True if file was successfully opened</returns>
        public bool Open(bool readOnly)
        {
            if (connection != null && !connection.IsOpen)
                connection = null;
            if (connection != null && readOnly == connection.IsReadOnly)
                return true;  // already open.

            if (connection != null && readOnly && !connection.IsReadOnly)
                return false;  // can't open for reading as we are currently writing

            if (FileName == null)
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null)
                {
                    if (!useFirebird)
                        FileName = Path.ChangeExtension(simulations.FileName, ".db");
                    else
                        FileName = Path.ChangeExtension(simulations.FileName, ".fdb");
                }
                else
                    throw new Exception("Cannot find a filename for the DataStore database.");
            }

            Close();

            if (useFirebird)
            {
                connection = new Firebird();
            }
            else
            {
                connection = new SQLite();
            }

            if (!File.Exists(FileName))
            {
                connection.OpenDatabase(FileName, readOnly: false);
            }
            else
                connection.OpenDatabase(FileName, readOnly);

            // ## would be great to find a nicer place to describe this. The Firebird and SQLite objects need to remain generic
            // and not contain Apsim specific table designs, but separating the code is needed but not ideal to have it in 
            // the DataStore as the aim is to make this general. Importantly the DataStore public interface looks generic (not database specific)!
            if (!useFirebird)
            {
                if (!connection.TableExists("_Checkpoints"))
                {
                    connection.ExecuteNonQuery("CREATE TABLE _Checkpoints (ID INTEGER PRIMARY KEY ASC, Name TEXT, Version TEXT, Date TEXT)");
                    connection.ExecuteNonQuery("INSERT INTO [_Checkpoints] (Name) VALUES ('Current')");
                }
                if (!connection.TableExists("_CheckpointFiles"))
                    connection.ExecuteNonQuery("CREATE TABLE _CheckpointFiles (CheckpointID INTEGER, FileName TEXT, Contents BLOB)");
                if (!connection.TableExists("_Simulations"))
                    connection.ExecuteNonQuery("CREATE TABLE _Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT COLLATE NOCASE)");
                if (!connection.TableExists("_Messages"))
                    connection.ExecuteNonQuery("CREATE TABLE _Messages (CheckpointID INTEGER, ComponentID INTEGER, SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");
                if (!connection.TableExists("_Units"))
                    connection.ExecuteNonQuery("CREATE TABLE _Units (TableName TEXT, ColumnHeading TEXT, Units TEXT)");
            }
            else
            {
                if (!connection.TableExists("_Checkpoints"))
                {
                    connection.ExecuteNonQuery("CREATE TABLE \"_Checkpoints\" (ID INTEGER generated by default as identity PRIMARY KEY NOT NULL, Name VARCHAR(50), Version VARCHAR(50), \"Date\" TIMESTAMP)");
                    connection.ExecuteNonQuery("INSERT INTO [_Checkpoints] (Name) VALUES ('Current')");
                }
                if (!connection.TableExists("_CheckpointFiles"))
                    connection.ExecuteNonQuery("CREATE TABLE \"_CheckpointFiles\" (CheckpointID INTEGER, FileName VARCHAR(50), Contents BLOB SUB_TYPE BINARY)");
                if (!connection.TableExists("_Simulations"))
                    connection.ExecuteNonQuery("CREATE TABLE \"_Simulations\" (ID INTEGER generated by default as identity PRIMARY KEY NOT NULL, Name VARCHAR(50) )");
                if (!connection.TableExists("_Messages"))
                    connection.ExecuteNonQuery("CREATE TABLE \"_Messages\" (CheckpointID INTEGER, ComponentID INTEGER, SimulationID INTEGER, ComponentName VARCHAR(50), \"Date\" TIMESTAMP, Message VARCHAR(100), MessageType INTEGER)");
                if (!connection.TableExists("_Units"))
                    connection.ExecuteNonQuery("CREATE TABLE \"_Units\" (TableName VARCHAR(50), ColumnHeading VARCHAR(50), Units VARCHAR(15))");
            }

            connection.CloseDatabase();

            connection.OpenDatabase(FileName, readOnly);

            Refresh();

            return true;
        }

        /// <summary>Close the database.</summary>
        private void Close()
        {
            if (connection != null)
            {
                tables.Clear();
                simulationIDs.Clear();
                checkpointIDs.Clear();
                connection.CloseDatabase();
                connection = null;
            }
        }

        /// <summary>Refresh our tables structure and simulation Ids</summary>
        private void Refresh()
        {
            // Get a list of table names.
            List<string> tableNames = connection.GetTableNames();
            foreach (string tableName in tableNames)
            {
                Table table = tables.Find(t => t.Name == tableName);
                if (table == null)
                {
                    table = new Table(tableName);
                    tables.Add(table);
                }
                table.SetConnection(connection);
            }

            // Get a list of simulation names
            simulationIDs.Clear();

            bool haveSimulationTable = tables.Find(table => table.Name == "_Simulations") != null;
            if (haveSimulationTable)
            {
                DataTable simulationTable = connection.ExecuteQuery("SELECT ID, Name FROM [_Simulations] ORDER BY Name");
                foreach (DataRow row in simulationTable.Rows)
                {
                    string name = row["Name"].ToString();
                    if (!simulationIDs.ContainsKey(name))
                        simulationIDs.Add(name, Convert.ToInt32(row["ID"]));
                }
            }

            // Get a list of checkpoint names
            checkpointIDs.Clear();

            bool haveCheckpointTable = tables.Find(table => table.Name == "_Checkpoints") != null;
            if (haveCheckpointTable)
            {
                DataTable checkpointTable = connection.ExecuteQuery("SELECT ID, Name FROM [_Checkpoints] ORDER BY Name");
                foreach (DataRow row in checkpointTable.Rows)
                {
                    string name = row["Name"].ToString();
                    if (!checkpointIDs.ContainsKey(name))
                        checkpointIDs.Add(name, Convert.ToInt32(row["ID"]));
                }
            }
        }

        /// <summary>Remove all simulations from the database that don't exist in 'simulationsToKeep'</summary>
        /// <param name="knownSimulationNames">A list of simulation names in the .apsimx file</param>
        /// <param name="simulationNamesBeingRun">The names of the simulations about to be run</param>
        private void CleanupDB(IEnumerable<string> knownSimulationNames, IEnumerable<string> simulationNamesBeingRun = null)
        {
            // Get a list of simulation names that are in the .db but we know nothing about them
            // i.e. they are old and no longer needed.
            List<string> unknownSimulationNames = simulationIDs.Keys.Where(simName => !knownSimulationNames.Contains(simName)).Cast<string>().ToList();

            if (unknownSimulationNames.Count() > 0)
            {
                unknownSimulationNames = unknownSimulationNames.ConvertAll(s=> s.ToUpper());
                // Delete the unknown simulation names from the simulations table. Case insensitive check.
                ExecuteDeleteQuery("DELETE FROM [_Simulations] WHERE UPPER([Name]) IN (", unknownSimulationNames, ")");

                // Delete all data for simulations we know nothing about - even from checkpoints
                foreach (Table table in tables)
                    if (table.Columns.Find(c => c.Name == "SimulationID") != null)
                        ExecuteDeleteQueryUsingIDs("DELETE FROM [" + table.Name + "] WHERE [SimulationID] IN (", unknownSimulationNames, ")");
            }
            // Delete all data that we are about to run,
            if (checkpointIDs.Any())
            {
                int currentCheckpointID = checkpointIDs["Current"];
                foreach (Table table in tables)
                    if (table.Columns.Find(c => c.Name == "SimulationID") != null)
                        ExecuteDeleteQueryUsingIDs("DELETE FROM [" + table.Name + "] WHERE [SimulationID] IN (", simulationNamesBeingRun, ") AND [CheckpointID]=" + currentCheckpointID);
            }

            // Make sure each known simulation name has an ID in the simulations table in the .db
            ExecuteInsertQuery("_Simulations", "Name", knownSimulationNames);

            // Refresh our simulation table in memory now that we have removed unwanted ones.
            Refresh();
        }

        /// <summary>
        /// Cleanup all null fields in all tables.
        /// </summary>
        private void CleanupUnusedFields()
        {
            foreach (Table table in tables)
            {
                if (!table.Name.StartsWith("_"))
                {
                    var columnsToRemove = new List<string>();
                    foreach (Table.Column column in table.Columns)
                    {
                        string bracketedColumnName = "[" + column.Name + "]";
                        DataTable data = connection.ExecuteQuery("SELECT " + bracketedColumnName + " FROM [" + table.Name + "] WHERE " + bracketedColumnName + " IS NOT NULL LIMIT 1");
                        if (data.Rows.Count == 0)
                            columnsToRemove.Add(column.Name);
                    }
                    if (columnsToRemove.Count > 0)
                    {
                        table.Columns.RemoveAll(column => columnsToRemove.Contains(column.Name));
                        connection.DropColumns(table.Name, columnsToRemove);
                    }
                }
            }
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

            if (useFirebird)
            {
                StringBuilder insertSql = new StringBuilder();
                for (int i = 0; i < simulationNames.Count(); i++)
                {
                    if (!simulationIDs.ContainsKey(simulationNames.ElementAt(i)))
                    {
                        insertSql.AppendFormat("INSERT INTO \"{0}\"({1}) VALUES('{2}');\n", tableName, columnName, simulationNames.ElementAt(i));
                        connection.ExecuteNonQuery(insertSql.ToString());
                    }
                }
                /*if (insertSql.Length > 0)
                {
                    sql.Append("set term ^ ;\n");
                    sql.Append("EXECUTE BLOCK AS BEGIN\n");
                    sql.Append(insertSql);
                    sql.Append("END ^");
                    connection.ExecuteNonQuery(sql.ToString());
                }*/
            }
            else
            {
                for (int i = 0; i < simulationNames.Count(); i++)
                {
                    if (!simulationIDs.ContainsKey(simulationNames.ElementAt(i)))
                    {
                        if (sql.Length > 0)
                            sql.Append(',');
                        sql.AppendFormat("('{0}')", simulationNames.ElementAt(i));
                    }

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

            if (useFirebird)
            {
                for (int i = 0; i < simulationNames.Count(); i++)
                {
                    if (sql.Length > 0)
                        sql.Append(',');
                    sql.AppendFormat("'{0}'", simulationNames.ElementAt(i));
                }
                if (sql.Length > 0)
                    connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
            }
            else
            {
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

            if (useFirebird)
            {
                for (int i = 0; i < simulationNames.Count(); i++)
                {
                    string simulationName = simulationNames.ElementAt(i);
                    if (simulationIDs.ContainsKey(simulationName))
                    {
                        if (sql.Length > 0)
                            sql.Append(',');
                        sql.Append(simulationIDs[simulationName]);
                    }
                }
                if (sql.Length > 0)
                    connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
            }
            else
            {
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
                    if (sql.Length > 0 && ((i + 1) % 100 == 0 || i == simulationNames.Count() - 1))
                    {
                        connection.ExecuteNonQuery(sqlPrefix + sql + sqlSuffix);
                        sql.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Using the SimulationName column in the specified 'table', add a
        /// SimulationID column.
        /// </summary>
        /// <param name="table">The table.</param>
        private void AddSimulationIDColumnToTable(DataTable table)
        {
            table.Columns.Add("CheckpointID", typeof(int)).SetOrdinal(0);
            table.Columns.Add("SimulationID", typeof(int)).SetOrdinal(0);
            int checkpointID = checkpointIDs["Current"];
            foreach (DataRow row in table.Rows)
            {
                row["CheckpointID"] = checkpointID;
                string simulationName = row["SimulationName"].ToString();
                if (simulationName != null)
                {
                    int id = 0;
                    simulationIDs.TryGetValue(simulationName, out id);
                    if (id > 0)
                        row["SimulationID"] = id;
                }
            }
        }

        /// <summary>
        /// Get the table column names
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <returns>List of table names</returns>
        public List<string> GetTableColumns(string tableName)
        {
            return connection.GetTableColumns(tableName);
        }

        /// <summary>Encapsulates a file that has been checkpointed</summary>
        public struct CheckpointFile
        {
            /// <summary>Name of file</summary>
            public string fileName;

            /// <summary>Contents of file</summary>
            public byte[] contents;
        }
    }
}
