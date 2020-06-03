namespace Models.Storage
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// This class encapsulates all writing to a DataStore
    /// </summary>
    public class DataStoreWriter : IJobManager, IStorageWriter
    {
        /// <summary>Lock object.</summary>
        private object lockObject = new object();

        /// <summary>A list of all write commands.</summary>
        private List<IRunnable> commands = new List<IRunnable>();

        /// <summary>A sleep job to stop the job runner from exiting.</summary>
        private IRunnable sleepJob = new JobRunnerSleepJob();

        /// <summary>The runner used to run commands on a worker thread.</summary>
        private JobRunner commandRunner;

        /// <summary>Are we idle i.e. not writing to database?</summary>
        private bool idle = true;

        /// <summary>Has something been written to the db?</summary>
        private bool somethingHasBeenWriten = false;

        /// <summary>The IDS for all simulations</summary>
        private Dictionary<string, SimulationDetails> simulationIDs = new Dictionary<string, SimulationDetails>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The IDs for all checkpoints</summary>
        private Dictionary<string, Checkpoint> checkpointIDs = new Dictionary<string, Checkpoint>(StringComparer.OrdinalIgnoreCase);

        /// <summary>A list of simulation names that have been cleaned up for each table.</summary>
        private Dictionary<string, List<string>> simulationNamesThatHaveBeenCleanedUp = new Dictionary<string, List<string>>();

        /// <summary>A list of units for each table.</summary>
        private Dictionary<string, List<ColumnUnits>> units = new Dictionary<string, List<ColumnUnits>>();

        /// <summary>A list of names of tables that don't have checkpointid or simulatoinid columns.</summary>
        private static string[] tablesNotNeedingIndexColumns = new string[] { "_Simulations", "_Checkpoints", "_Units" };

        /// <summary>Are we stopping writing to the db?</summary>
        private bool stopping;

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
            if (dbConnection is SQLite && !(dbConnection as SQLite).IsInMemory)
            {
                // For disk-based databases, these pragmas greatly improve performance
                dbConnection.ExecuteQuery("PRAGMA journal_mode=WAL");
                dbConnection.ExecuteQuery("PRAGMA synchronous=NORMAL");
            }
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
        /// Add rows to a table in the db file. Note that the data isn't written immediately.
        /// </summary>
        /// <param name="data">Name of simulation the values correspond to.</param>
        public void WriteTable(ReportData data)
        {
            // NOTE: This can be called from many threads. Don't actually
            // write to the database on these threads. We have a single worker
            // thread to do that.

            Start();
            var table = data.ToTable();
            AddIndexColumns(table, "Current", data.SimulationName, data.FolderName);

            // Add units
            AddUnits(data.TableName, data.ColumnNames, data.ColumnUnits);

            // Delete old rows in table.
            DeleteOldRowsInTable(data.TableName, "Current", new string[] { data.SimulationName });

            lock (lockObject)
            {
                commands.Add(new WriteTableCommand(Connection, table));
            }
        }

        /// <summary>
        /// Write a table of data. Uses the TableName property of the specified DataTable.
        /// </summary>
        /// <param name="table">The data to write.</param>
        public void WriteTable(DataTable table)
        {
            if (table == null)
                return;
            // NOTE: This can be called from many threads. Don't actually
            // write to the database on these threads. We have a single worker
            // thread to do that.

            Start();

            // Delete old rows in table.
            if (table.Columns.Contains("SimulationName"))
            {
                var simulationNames = DataTableUtilities.GetColumnAsStrings(table, "SimulationName").ToList().Distinct();
                DeleteOldRowsInTable(table.TableName, "Current",
                                     simulationNamesThatMayNeedCleaning: simulationNames);
            }
            else
                DeleteOldRowsInTable(table.TableName, "Current");

            AddIndexColumns(table, "Current", null, null);

            lock (lockObject)
            {
                commands.Add(new WriteTableCommand(Connection, table));
            }
        }

        /// <summary>
        /// Deletes a table from the database.
        /// </summary>
        /// <param name="tableName">Name of the table to be deleted.</param>
        public void DeleteTable(string tableName)
        {
            Connection.ExecuteNonQuery($"DROP TABLE {tableName}");
        }

        /// <summary>Wait for all records to be written.</summary>
        public void WaitForIdle()
        {
            if (commandRunner != null)
            {
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
                try
                {
                    WaitForIdle();

                    WriteSimulationIDs();
                    WriteCheckpointIDs();
                    WriteAllUnits();
                }
                catch
                {
                    // Swallow exceptions
                }
                finally
                {
                    WaitForIdle();

                    stopping = true;
                    commandRunner.Stop();
                    commandRunner = null;
                    commands.Clear();
                    simulationIDs.Clear();
                    checkpointIDs.Clear();
                    simulationNamesThatHaveBeenCleanedUp.Clear();
                    units.Clear();
                }
            }
        }

        /// <summary>Called by the job runner when all jobs completed</summary>
        public void AllCompleted() { }

        /// <summary>Return an enumeration of jobs that need running.</summary>
        public IEnumerable<IRunnable> GetJobs()
        {
            // NOTE: This is called from the job runner worker thread.

            while (!stopping)
            {
                IRunnable command = null;
                lock (lockObject)
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
                    idle = true;
                    yield return sleepJob;
                }
                else
                {
                    idle = false;
                    somethingHasBeenWriten = true;
                    yield return command;
                }
            }
        }

        /// <summary>Delete all data in datastore, except for checkpointed data.</summary>
        public void Empty()
        {
            try
            {
                Start();
            }
            catch
            {
                // The call to Start may fail if the database is corrupt, which may well be why we want to empty it.
                // For that reason, catch any exceptions and proceed.
            }
            commands.Add(new EmptyCommand(Connection));
            Stop();
        }

        /// <summary>Save the current data to a checkpoint.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="filesToStore">Files to store the contents of.</param>
        public void AddCheckpoint(string name, IEnumerable<string> filesToStore = null)
        {
            Start();
            commands.Add(new AddCheckpointCommand(this, name, filesToStore));
            Stop();
        }

        /// <summary>Delete a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to delete.</param>
        public void DeleteCheckpoint(string name)
        {
            Start();
            lock (lockObject)
            {
                commands.Add(new DeleteCheckpointCommand(this, GetCheckpointID(name)));
                checkpointIDs.Remove(name);
            }
            Stop();
        }

        /// <summary>Revert a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to revert to.</param>
        public void RevertCheckpoint(string name)
        {
            Start();
            commands.Add(new RevertCheckpointCommand(this, GetCheckpointID(name)));
            Stop();
        }

        /// <summary>Set a checkpoint show on graphs flag.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="showGraphs">Show graphs?</param>
        public void SetCheckpointShowGraphs(string name, bool showGraphs)
        {
            Start();
            if (checkpointIDs.ContainsKey(name))
                checkpointIDs[name].ShowOnGraphs = showGraphs;
            Stop();
        }

        /// <summary>
        /// Add a list of column units for the specified table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnNames">A collection of column names.</param>
        /// <param name="columnUnits">A corresponding collection of column units.</param>
        public void AddUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits)
        {
            // Can be called by many threads simultaneously.
            lock (lockObject)
            {
                for (int i = 0; i < columnNames.Count(); i++)
                {
                    var columnName = columnNames.ElementAt(i);
                    var columnUnit = columnUnits.ElementAt(i);
                    if (columnUnit != null && columnUnit != string.Empty)
                    {
                        if (!units.TryGetValue(tableName, out var tableUnits))
                        {
                            tableUnits = new List<ColumnUnits>();
                            units.Add(tableName, tableUnits);
                        }
                        if (tableUnits.Find(unit => unit.Name == columnName) == null)
                            tableUnits.Add(new ColumnUnits() { Name = columnName, Units = columnUnits.ElementAt(i) });
                    }
                }
            }
        }

        /// <summary>
        /// Get a simulation ID for the specified simulation name. Will
        /// create an ID if the simulationName is unknown.
        /// </summary>
        /// <param name="simulationName">The name of the simulation to look for.</param>
        /// <param name="folderName">The name of the folder the simulation belongs in.</param>
        /// <returns>Always returns a number.</returns>
        public int GetSimulationID(string simulationName, string folderName)
        {
            if (simulationName == null)
                return 0;

            lock (lockObject)
            {
                if (!simulationIDs.TryGetValue(simulationName, out SimulationDetails details))
                {
                    // Not found so create a new ID, add it to our collection of ids
                    int id;
                    if (simulationIDs.Count > 0)
                        id = simulationIDs.Values.Select(v => v.ID).Max() + 1;
                    else
                        id = 1;
                    simulationIDs.Add(simulationName, new SimulationDetails() { ID = id, FolderName = folderName });
                    return id;
                }
                else if (folderName != null)
                    details.FolderName = folderName;
                return details.ID;
            }
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

            lock (lockObject)
            {
                if (!checkpointIDs.TryGetValue(checkpointName, out Checkpoint checkpoint))
                {
                    checkpoint = new Checkpoint();
                    // Not found so create a new ID, add it to our collection of ids
                    if (checkpointIDs.Count > 0)
                        checkpoint.ID = checkpointIDs.Select(c => c.Value.ID).Max() + 1;
                    else
                        checkpoint.ID = 1;
                    checkpointIDs.Add(checkpointName, checkpoint); 
                }
                return checkpoint.ID;
            }
        }

        /// <summary>
        /// Read the database connection for simulation and checkpoint ids.
        /// </summary>
        /// <param name="dbConnection">The database connection to read from.</param>
        private void ReadExistingDatabase(IDatabaseConnection dbConnection)
        {
            if (dbConnection == null)
                return;

            simulationIDs.Clear();
            if (dbConnection.TableExists("_Simulations"))
            {
                var data = dbConnection.ExecuteQuery("SELECT * FROM [_Simulations]");
                foreach (DataRow row in data.Rows)
                {
                    int id = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture);
                    string folderName = null;
                    if (data.Columns.Contains("FolderName"))
                        folderName = row["FolderName"].ToString();
                    simulationIDs.Add(row["Name"].ToString(),
                                      new SimulationDetails() { ID = id, FolderName = folderName });
                }
            }

            checkpointIDs.Clear();
            if (dbConnection.TableExists("_Checkpoints"))
            {
                var data = dbConnection.ExecuteQuery("SELECT * FROM [_Checkpoints]");
                foreach (DataRow row in data.Rows)
                    checkpointIDs.Add(row["Name"].ToString(), new Checkpoint()
                    {
                        ID = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture),
                        ShowOnGraphs = data.Columns["OnGraphs"] != null && 
                                       !Convert.IsDBNull(row["OnGraphs"]) &&
                                       Convert.ToInt32(row["OnGraphs"], CultureInfo.InvariantCulture) == 1
                    });
            }
        }

        /// <summary>
        /// Delete old rows for the specified table, checkpoint and simulation.
        /// </summary>
        /// <param name="tableName">The table name to delete from.</param>
        /// <param name="checkpointName">The checkpoint name to use to match rows to delete.</param>
        /// <param name="simulationNamesThatMayNeedCleaning">Simulation names that may need cleaning up.</param>
        private void DeleteOldRowsInTable(string tableName, string checkpointName = null, IEnumerable<string> simulationNamesThatMayNeedCleaning = null)
        {
            // Can be called by many threads simultaneously.

            List<int> simulationIds = null;
            if (tablesNotNeedingIndexColumns.Contains(tableName))
            {
                // Create a delete row command to remove the rows.
                lock (lockObject)
                {
                    // This will drop the table.
                    commands.Add(new DeleteRowsCommand(Connection, tableName,
                                        0, simulationIds));
                }
            }
            else 
            {
                if (simulationNamesThatMayNeedCleaning != null)
                {
                    IEnumerable<string> simsNeedingCleaning;

                    lock (lockObject)
                    {
                        // Have we written anything to this table yet?
                        if (!simulationNamesThatHaveBeenCleanedUp.TryGetValue(tableName, out var simsThatHaveBeenCleanedUp))
                        {
                            // No - create a empty list of simulation names that we've cleaned up.
                            simulationNamesThatHaveBeenCleanedUp.Add(tableName, new List<string>());
                            simsNeedingCleaning = simulationNamesThatMayNeedCleaning;
                        }
                        else
                        {
                            // Get a list of simulations that haven't been cleaned up for this table.
                            simsNeedingCleaning = simulationNamesThatMayNeedCleaning.Except(simsThatHaveBeenCleanedUp);
                        }

                        simulationIds = new List<int>();
                        if (simsNeedingCleaning.Any())
                        {
                            // Add the simulations we're about to clean to our list so
                            // that they aren't cleaned again. Also get id's for each one.
                            foreach (var simulationName in simsNeedingCleaning)
                            {
                                simulationNamesThatHaveBeenCleanedUp[tableName].Add(simulationName);
                                simulationIds.Add(GetSimulationID(simulationName, null));
                            }
                        }
                    }
                }

                if (simulationNamesThatMayNeedCleaning == null || simulationIds.Any())
                {
                    // Get a checkpoint id.
                    var checkpointID = 0;
                    if (checkpointName != null)
                        checkpointID = GetCheckpointID(checkpointName);

                    // Create a delete row command to remove the rows.
                    lock (lockObject)
                    {
                        commands.Add(new DeleteRowsCommand(Connection, tableName,
                                            checkpointID,
                                            simulationIds));
                    }
                }
            }
        }

        /// <summary>Create a command runner one hasn't already been created.</summary>
        private void Start()
        {
            if (commandRunner == null)
            {
                lock (lockObject)
                {
                    if (commandRunner == null)
                    {
                        stopping = false;
                        commandRunner = new JobRunner(numProcessors:1);
                        commandRunner.Add(this);
                        commandRunner.Run();
                        ReadExistingDatabase(Connection);
                    }
                }
            }
        }

        /// <summary>
        /// Add in checkpoint and simulation ID columns.
        /// </summary>
        /// <param name="table">The table to add the columns to.</param>
        /// <param name="checkpointName">The name of the checkpoint.</param>
        /// <param name="simulationName">The simulation name.</param>
        /// <param name="folderName">The name of the folder the simulation sits in.</param>
        private void AddIndexColumns(DataTable table, string checkpointName, string simulationName, string folderName)
        {
            if (!tablesNotNeedingIndexColumns.Contains(table.TableName))
            {
                if (!table.Columns.Contains("CheckpointID"))
                {
                    var checkpointColumn = table.Columns.Add("CheckpointID", typeof(int));
                    int checkpointNameColumnIndex = table.Columns.IndexOf("CheckpointName");
                    if (checkpointNameColumnIndex != -1)
                    {
                        // A checkpoint name column exists.
                        foreach (DataRow row in table.Rows)
                        {
                            checkpointName = row[checkpointNameColumnIndex].ToString();
                            row[checkpointColumn] = GetCheckpointID(checkpointName); ;
                        }
                        table.Columns.RemoveAt(checkpointNameColumnIndex);
                    }
                    else
                    {
                        var id = GetCheckpointID(checkpointName);
                        foreach (DataRow row in table.Rows)
                            row[checkpointColumn] = id;
                    }
                    checkpointColumn.SetOrdinal(0);
                }

                if (!table.Columns.Contains("SimulationID"))
                {
                    DataColumn simulationColumn = null;
                    int simulationNameColumnIndex = table.Columns.IndexOf("SimulationName");
                    if (simulationNameColumnIndex != -1)
                    {
                        simulationColumn = table.Columns.Add("SimulationID", typeof(int));
                        foreach (DataRow row in table.Rows)
                        {
                            simulationName = row[simulationNameColumnIndex].ToString();
                            row[simulationColumn] = GetSimulationID(simulationName, folderName); ;
                        }
                        table.Columns.RemoveAt(simulationNameColumnIndex);
                    }
                    else if (simulationName != null)
                    {
                        simulationColumn = table.Columns.Add("SimulationID", typeof(int));
                        var id = GetSimulationID(simulationName, folderName);
                        foreach (DataRow row in table.Rows)
                            row[simulationColumn] = id;
                    }
                    if (simulationColumn != null)
                        simulationColumn.SetOrdinal(1);
                }
            }
        }

        /// <summary>
        /// Write all units to our list of units for later writing to the database.
        /// </summary>
        private void WriteAllUnits()
        {
            if (units.Any())
            {
                var unitTable = new DataTable("_Units");
                unitTable.Columns.Add("TableName", typeof(string));
                unitTable.Columns.Add("ColumnHeading", typeof(string));
                unitTable.Columns.Add("Units", typeof(string));

                foreach (var tableUnit in units)
                {
                    foreach (var unit in tableUnit.Value)
                    {
                        var unitRow = unitTable.NewRow();
                        unitRow[0] = tableUnit.Key;
                        unitRow[1] = unit.Name;
                        unitRow[2] = unit.Units;
                        unitTable.Rows.Add(unitRow);
                    }
                }
                WriteTable(unitTable);
            }
        }

        /// <summary>Write the simulations table.</summary>
        private void WriteSimulationIDs()
        {
            // If there are no tables in the database then they must have been emptied.
            // Don't write a simulations table in this case.
            if (simulationIDs.Count > 0 && Connection.GetTableNames().Count > 0)
            {
                var simulationsTable = new DataTable("_Simulations");
                simulationsTable.Columns.Add("ID", typeof(int));
                simulationsTable.Columns.Add("Name", typeof(string));
                simulationsTable.Columns.Add("FolderName", typeof(string));

                foreach (var simulation in simulationIDs)
                {
                    var row = simulationsTable.NewRow();
                    row[0] = simulation.Value.ID;
                    row[1] = simulation.Key;
                    row[2] = simulation.Value.FolderName;
                    simulationsTable.Rows.Add(row);
                }
                WriteTable(simulationsTable);
            }
        }

        /// <summary>Write the checkpoints table.</summary>
        private void WriteCheckpointIDs()
        {
            // If there are no tables in the database then they must have been emptied.
            // Don't write a checkpoints table in this case.
            if (checkpointIDs.Count > 0 && Connection.GetTableNames().Count > 0)
            {
                var checkpointsTable = new DataTable("_Checkpoints");
                checkpointsTable.Columns.Add("ID", typeof(int));
                checkpointsTable.Columns.Add("Name", typeof(string));
                checkpointsTable.Columns.Add("Version", typeof(string));
                checkpointsTable.Columns.Add("Date", typeof(DateTime));
                checkpointsTable.Columns.Add("OnGraphs", typeof(int));

                foreach (var checkpoint in checkpointIDs)
                {
                    var row = checkpointsTable.NewRow();
                    row[0] = checkpoint.Value.ID;
                    row[1] = checkpoint.Key;
                    if (checkpoint.Value.ShowOnGraphs)
                        row[4] = 1;
                    checkpointsTable.Rows.Add(row);
                }
                WriteTable(checkpointsTable);
            }
        }

        void IJobManager.JobHasCompleted(JobCompleteArguments args)
        {
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

        private class SimulationDetails
        {
            public int ID { get; set; }
            public string FolderName { get; set; }
        }
    }
}
