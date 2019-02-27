namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// This class encapsulates all writing to a DataStore
    /// </summary>
    public class DataStoreWriter : IJobManager, IStorageWriter
    {
        /// <summary>A list of all write commands.</summary>
        private List<IRunnable> commands = new List<IRunnable>();

        /// <summary>A sleep job to stop the job runner from exiting.</summary>
        private IRunnable sleepJob = new JobRunnerSleepJob();

        /// <summary>The runner used to run commands on a worker thread.</summary>
        private IJobRunner commandRunner;

        /// <summary>Are we idle i.e. not writing to database?</summary>
        private bool idle = true;

        /// <summary>Has something been written to the db?</summary>
        private bool somethingHasBeenWriten = false;

        /// <summary>A cache of prepared INSERT queries for each table.</summary>
        private List<InsertQuery> insertQueries = new List<InsertQuery>();

        /// <summary>The IDS for all simulations</summary>
        private Dictionary<string, int> simulationIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The IDs for all checkpoints</summary>
        private Dictionary<string, int> checkpointIDs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The units for all fields in all tables.</summary>
        private Dictionary<string, List<ColumnUnits>> units = new Dictionary<string, List<ColumnUnits>>();

        /// <summary>A list of simulation names that have been cleaned up for each table.</summary>
        private Dictionary<string, List<string>> simulationNamesThatHaveBeenCleanedUp = new Dictionary<string, List<string>>();

        /// <summary>Number of write commands in the current transaction.</summary>
        private int numCommandsInTransaction = 0;

        /// <summary>Default constructor.</summary>
        public DataStoreWriter()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dbConnection">Data database connection to write to.</param>
        public DataStoreWriter(IDatabaseConnection dbConnection)
        {
            SetConnection(dbConnection);
        }

        /// <summary>Set the database connection.Constructor</summary>
        /// <param name="dbConnection">The database connection to write to.</param>
        public void SetConnection(IDatabaseConnection dbConnection)
        {
            Connection = dbConnection;
            ReadExistingDatabase(dbConnection);
        }

        /// <summary>The database connection to write to.</summary>
        public IDatabaseConnection Connection { get; private set; }

        /// <summary>Has something been written to the database since the last call to this property?</summary>
        public bool SomethingBeenWritten
        {
            get
            {
                bool returnValue = somethingHasBeenWriten;
                somethingHasBeenWriten = false;
                return returnValue;
            }
        }

        /// <summary>
        /// Add a row to a table in the db file.
        /// </summary>
        /// <param name="simulationName">Name of simulation the values correspond to.</param>
        /// <param name="tableName">Name of the table to write to.</param>
        /// <param name="columnNames">The column names relating to the values.</param>
        /// <param name="columnUnits">The units of each of the values.</param>
        /// <param name="rowValues">The values making up the row to write.</param>
        public void WriteRow(string simulationName, string tableName, 
                            IList<string> columnNames, IList<string> columnUnits, IList<object> rowValues)
        {
            // NOTE: This can be called from many threads. Don't actually
            // write to the database on these threads. We have a single worker
            // thread to do that.
            lock (commands)
            {
                Start();
                CleanupOldRowsInTable(tableName, "Current", simulationName);

                commands.Add(new AddRowCommand(this, insertQueries,
                                               "Current", simulationName, tableName,
                                               columnNames,
                                               columnUnits,
                                               rowValues));
            }
        }

        /// <summary>Wait for all records to be written.</summary>
        public void WaitForIdle()
        {
            if (commandRunner != null)
            {
                // Make sure all existing writing has completed.
                while (commands.Count > 0 || !idle)
                    Thread.Sleep(100);

                // Write all units
                WriteAllUnits();

                // Make sure all existing writing has completed.
                while (commands.Count > 0 || !idle)
                    Thread.Sleep(100);
            }
        }

        /// <summary>Stop all writing to database.</summary>
        public void Stop()
        {
            if (commandRunner != null)
            {
                WaitForIdle();

                if (numCommandsInTransaction > 0)
                {
                    Connection.EndTransaction();
                    numCommandsInTransaction = 0;
                }

                commandRunner.Stop();
                commandRunner = null;
                foreach (var query in insertQueries)
                    query.Close(Connection);
                commands.Clear();
                insertQueries.Clear();
                simulationIDs.Clear();
                checkpointIDs.Clear();
                units.Clear();
                simulationNamesThatHaveBeenCleanedUp.Clear();
            }
        }

        /// <summary>Return the next command to run.</summary>
        public IRunnable GetNextJobToRun()
        {
            // NOTE: This is called from the job runner worker thread.

            // Try and get a command to execute.
            IRunnable command = null;
            lock (commands)
            {
                if (commands.Count > 0)
                {
                    command = commands[0];
                    commands.RemoveAt(0);
                }
            }

            // If nothing was found to run then return a sleep job 
            // so that the job runner doesn't exit.
            if (command == null)
            {
                command = sleepJob;
                idle = true;
            }
            else
            {
                idle = false;
                somethingHasBeenWriten = true;

                // Create DB transaction. We group commands in batches of 100
                // This leads to much quicker writes to the db.
                if (numCommandsInTransaction == 0)
                    Connection.BeginTransaction();
                else if (numCommandsInTransaction == 100)
                {
                    numCommandsInTransaction = 0;
                    Connection.EndTransaction();
                    Connection.BeginTransaction();
                }
                numCommandsInTransaction++;
            }

            return command;
        }

        /// <summary>
        /// Get a simulation ID for the specified simulation name. Will
        /// create an ID if the simulationName is unknown.
        /// </summary>
        /// <param name="simulationName">The name of the simulation to look for.</param>
        /// <returns>Always returns a number.</returns>
        public int GetSimulationID(string simulationName)
        {
            if (simulationName == null)
                return 0;

            if (!simulationIDs.TryGetValue(simulationName, out int id))
            {
                // Not found so create a new ID, add it to our collection of ids
                // and write it to the database.
                if (simulationIDs.Count > 0)
                    id = simulationIDs.Values.Max() + 1;
                else
                    id = 1;
                simulationIDs.Add(simulationName, id);

                commands.Add(new AddRowCommand(this, insertQueries,
                                               null, null, "_Simulations",
                                               new string[] { "ID", "Name", "FolderName" },
                                               null,
                                               new object[] { id, simulationName, string.Empty }));
            }

            return id;
        }

        /// <summary>
        /// Get a checkpoint ID for the specified name. Will
        /// create an ID if the Name is unknown.
        /// </summary>
        /// <param name="checkpointName">The name of the checkpoint to look for.</param>
        /// <returns>Always returns a number.</returns>
        public int GetCheckpointID(string checkpointName)
        {
            if (checkpointName == null)
                return 0;
            if (!checkpointIDs.TryGetValue(checkpointName, out int id))
            {
                // Not found so create a new ID, add it to our collection of ids
                // and write it to the database.
                if (checkpointIDs.Count > 0)
                    id = simulationIDs.Values.Max() + 1;
                else
                    id = 1;
                checkpointIDs.Add(checkpointName, id);
                commands.Add(new AddRowCommand(this, insertQueries,
                               null, null, "_Checkpoints",
                               new string[] { "ID", "Name", "Version", "Date" },
                               null,
                               new object[] { id, checkpointName, string.Empty, string.Empty }));
            }

            return id;
        }

        /// <summary>Ensure the specified table matches our columns and row values.</summary>
        /// <param name="tableName">Name of the table to write to.</param>
        /// <param name="columnNames">The column names relating to the values.</param>
        /// <param name="columnUnits">The units of each of the values.</param>
        /// <param name="rowValues">The values making up the row to write.</param>
        public void EnsureTableHasColumnNames(string tableName,
                                              IEnumerable<string> columnNames,
                                              IEnumerable<string> columnUnits,
                                              IEnumerable<object> rowValues)
        {
            // Check to make sure the table exists and has our columns.
            if (Connection.TableExists(tableName))
                AlterTable(tableName, columnNames, columnUnits, rowValues);
            else
                CreateTable(tableName, columnNames, columnUnits, rowValues);
        }

        /// <summary>
        /// Write a table of data. Uses the TableName property of the specified DataTable.
        /// </summary>
        /// <param name="data">The data to write.</param>
        public void WriteTable(DataTable data)
        {
            Start();

            // Get a list of column names.
            List<string> columnNames = new List<string>();
            List<string> columnUnits = new List<string>();
            foreach (DataColumn column in data.Columns)
            {
                var columnName = column.ColumnName;
                string columnUnit = null;
                if (column.ColumnName.Contains("("))
                    columnUnit = StringUtilities.SplitOffBracketedValue(ref columnName, '(', ')');
                
                columnNames.Add(columnName);
                columnUnits.Add(columnUnit);
            }

            DeleteRowsInTable(data.TableName);

            // For each row in data, call AddRow method.
            foreach (DataRow row in data.Rows)
            {
                object[] values = new object[columnNames.Count];
                string simulationName = null;
                if (data.Columns.Contains("SimulationName"))
                    simulationName = row["SimulationName"].ToString();
                for (int colIndex = 0; colIndex < data.Columns.Count; colIndex++)
                    values[colIndex] = row[colIndex];
                WriteRow(simulationName, data.TableName, columnNames, columnUnits, values);
            }
        }

        /// <summary>
        /// Delete rows for the specified table, checkpoint and simulation.
        /// </summary>
        /// <param name="tableName">The table name to delete from.</param>
        /// <param name="checkpointName">The checkpoint name to use to match rows to delete.</param>
        /// <param name="simulationName">The simulation name to use to match rows to delete.</param>
        public void DeleteRowsInTable(string tableName, string checkpointName = null, string simulationName = null)
        {
            if (Connection.TableExists(tableName))
                commands.Add(new DeleteRowsCommand(Connection, tableName,
                                                    GetCheckpointID(checkpointName),
                                                    GetSimulationID(simulationName)));
        }

        /// <summary>Delete all data in datastore, except for checkpointed data.</summary>
        public void Empty()
        {
            Start();
            commands.Add(new EmptyCommand(Connection));
        }

        /// <summary>Save the current data to a checkpoint.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="filesToStore">Files to store the contents of.</param>
        public void AddCheckpoint(string name, IEnumerable<string> filesToStore = null)
        {
            Start();
            commands.Add(new AddCheckpointCommand(this, name, filesToStore));
        }

        /// <summary>Delete a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to delete.</param>
        public void DeleteCheckpoint(string name)
        {
            Start();
            commands.Add(new DeleteCheckpointCommand(this, GetCheckpointID(name)));
        }

        /// <summary>Revert a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to revert to.</param>
        public void RevertCheckpoint(string name)
        {
            Start();
            commands.Add(new RevertCheckpointCommand(this, GetCheckpointID(name)));
        }

        /// <summary>
        /// Read the database connection for simulation and checkpoint ids.
        /// </summary>
        /// <param name="dbConnection">The database connection to read from.</param>
        private void ReadExistingDatabase(IDatabaseConnection dbConnection)
        {
            simulationIDs.Clear();
            if (dbConnection.TableExists("_Simulations"))
            {
                var data = dbConnection.ExecuteQuery("SELECT * FROM _Simulations");
                foreach (DataRow row in data.Rows)
                    simulationIDs.Add(row["Name"].ToString(), Convert.ToInt32(row["ID"]));
            }

            checkpointIDs.Clear();
            if (dbConnection.TableExists("_Checkpoints"))
            {
                var data = dbConnection.ExecuteQuery("SELECT * FROM _Checkpoints");
                foreach (DataRow row in data.Rows)
                    checkpointIDs.Add(row["Name"].ToString(), Convert.ToInt32(row["ID"]));
            }
        }

        /// <summary>Create a table that matches our columns and row values.</summary>
        /// <param name="tableName">Name of the table to write to.</param>
        /// <param name="columnNames">The column names relating to the values.</param>
        /// <param name="columnUnits">The units of each of the values.</param>
        /// <param name="rowValues">The values making up the row to write.</param>
        private void CreateTable(string tableName,
                                 IEnumerable<string> columnNames,
                                 IEnumerable<string> columnUnits,
                                 IEnumerable<object> rowValues)
        {
            List<string> colNames = new List<string>();
            List<string> colTypes = new List<string>();

            colNames.AddRange(columnNames);
            foreach (var value in rowValues)
                colTypes.Add(Connection.GetDBDataTypeName(value));

            Connection.CreateTable(tableName, colNames, colTypes);

            StoreUnits(tableName, columnNames, columnUnits);
        }

        /// <summary>Alter an existing table ensuring all columns exist.</summary>
        /// <param name="tableName">Name of the table to write to.</param>
        /// <param name="columnNames">The column names relating to the values.</param>
        /// <param name="columnUnits">The units of each of the values.</param>
        /// <param name="rowValues">The values making up the row to write.</param>
        private void AlterTable(string tableName,
                                IEnumerable<string> columnNames,
                                IEnumerable<string> columnUnits,
                                IEnumerable<object> rowValues)
        {
            // Get a list of column names from the database file.
            List<string> existingColumns = Connection.GetTableColumns(tableName);

            List<string> columnNamesToWriteUnitsFor = null;
            List<string> columnUnitsToWrite = null;
            for (int i = 0; i < columnNames.Count(); i++)
            {
                string columnName = columnNames.ElementAt(i);
                if (!existingColumns.Contains(columnName, StringComparer.CurrentCultureIgnoreCase))
                {
                    // Column is missing from database file - write it.
                    Connection.AddColumn(tableName, columnName, Connection.GetDBDataTypeName(rowValues.ElementAt(i)));

                    // Store units if not null.
                    if (columnUnits != null && columnUnits.ElementAt(i) != null)
                    {
                        if (columnNamesToWriteUnitsFor == null)
                        {
                            columnNamesToWriteUnitsFor = new List<string>();
                            columnUnitsToWrite = new List<string>();
                        }
                        columnNamesToWriteUnitsFor.Add(columnName);
                        columnUnitsToWrite.Add(columnUnits.ElementAt(i));
                    }
                }
            }

            // If we found some units then store them for later writing.
            if (columnNamesToWriteUnitsFor != null)
                StoreUnits(tableName, columnNamesToWriteUnitsFor, columnUnitsToWrite);
        }

        /// <summary>
        /// Delete old rows for the specified table, checkpoint and simulation.
        /// </summary>
        /// <param name="tableName">The table name to delete from.</param>
        /// <param name="checkpointName">The checkpoint name to use to match rows to delete.</param>
        /// <param name="simulationName">The simulation name to use to match rows to delete.</param>
        private void CleanupOldRowsInTable(string tableName, string checkpointName, string simulationName)
        {
            // Have we written anything to this table yet?
            if (!simulationNamesThatHaveBeenCleanedUp.ContainsKey(tableName))
            {
                // No - create a empty list of simulation names that we've cleaned up.
                simulationNamesThatHaveBeenCleanedUp.Add(tableName, new List<string>());
            }

            // Have we cleaned up this simulation in this table?
            if (simulationName != null &&
                !simulationNamesThatHaveBeenCleanedUp[tableName].Contains(simulationName))
            {
                simulationNamesThatHaveBeenCleanedUp[tableName].Add(simulationName);
                DeleteRowsInTable(tableName, checkpointName, simulationName);
            }
        }

        /// <summary>
        /// Add units to our list of units for later writing to the database.
        /// </summary>
        /// <param name="tableName">The name of the table the units relate to.</param>
        /// <param name="columnNames">The column names.</param>
        /// <param name="columnUnits">The column units.</param>
        private void StoreUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits)
        {
            // Do we have any units to write?
            if (columnUnits != null)
            {
                // Yes - Have we already registered units for this table?
                List<ColumnUnits> unitsForTable;
                if (!units.TryGetValue(tableName, out unitsForTable))
                {
                    // No - Create a units table.
                    unitsForTable = new List<ColumnUnits>();
                    units.Add(tableName, unitsForTable);
                }

                // Go through all units and store the non null ones.
                for (int i = 0; i < columnUnits.Count(); i++)
                {
                    if (columnUnits.ElementAt(i) != null)
                    {
                        // Not null - have we already got units for this column?
                        var foundColumnUnits = unitsForTable.Find(u => u.Name == columnNames.ElementAt(i));
                        if (foundColumnUnits == null)
                        {
                            // No so store the units for this column.
                            unitsForTable.Add(new ColumnUnits() { Name = columnNames.ElementAt(i), Units = columnUnits.ElementAt(i) });
                        }
                    }
                }
            }
        }

        /// <summary>Write all units to the database.</summary>
        private void WriteAllUnits()
        {
            foreach (var tableUnits in units)
            {
                foreach (var column in tableUnits.Value)
                {
                    commands.Add(new AddRowCommand(this, insertQueries,
                                                   null, null, "_Units",
                                                   new string[] { "TableName", "ColumnHeading", "Units" },
                                                   null,
                                                   new object[] { tableUnits.Key, column.Name, column.Units }));
                }
            }
        }

        /// <summary>Create a command runner one hasn't already been created.</summary>
        private void Start()
        {
            if (commandRunner == null)
            {
                commandRunner = new JobRunnerSync();
                commandRunner.Run(this);
                ReadExistingDatabase(Connection);
            }
        }


        /// <summary>
        /// A class for encapsulating column units.
        /// </summary>
        private class ColumnUnits
        {
            /// <summary>Name of column.</summary>
            public string Name { get; set; }

            /// <summary>Units of column.</summary>
            public string Units { get; set; }
        }


    }
}
