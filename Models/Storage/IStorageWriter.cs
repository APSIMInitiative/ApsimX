namespace Models.Storage
{
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Interface for reading and writing data to/from permanent storage.
    /// </summary>
    public interface IStorageWriter
    {
        /// <summary>
        /// Add rows to a table in the db file. Note that the data isn't written immediately.
        /// </summary>
        /// <param name="data">Name of simulation the values correspond to.</param>
        void WriteTable(ReportData data);

        /// <summary>
        /// Write a table of data. Uses the TableName property of the specified DataTable.
        /// </summary>
        /// <param name="data">The data to write.</param>
        void WriteTable(DataTable data);

        /// <summary>
        /// Deletes a table from the database.
        /// </summary>
        /// <param name="tableName">Name of the table to be deleted.</param>
        void DeleteTable(string tableName);

        /// <summary>Delete all data in datastore, except for checkpointed data.</summary>
        void Empty();

        /// <summary>Save the current data to a checkpoint.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="filesToStore">Files to store the contents of.</param>
        void AddCheckpoint(string name, IEnumerable<string> filesToStore = null);

        /// <summary>Delete a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to delete.</param>
        void DeleteCheckpoint(string name);
        
            /// <summary>Revert a checkpoint.</summary>
        /// <param name="name">Name of checkpoint to revert to.</param>
        void RevertCheckpoint(string name);

        /// <summary>Set a checkpoint show on graphs flag.</summary>
        /// <param name="name">Name of checkpoint.</param>
        /// <param name="showGraphs">Show graphs?</param>
        void SetCheckpointShowGraphs(string name, bool showGraphs);

        /// <summary>Wait for all records to be written.</summary>
        void WaitForIdle();

        /// <summary>Stop all writing to database.</summary>
        void Stop();

        /// <summary>
        /// Add a list of column units for the specified table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnNames">A collection of column names.</param>
        /// <param name="columnUnits">A corresponding collection of column units.</param>
        void AddUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits);

        /// <summary>
        /// Get a checkpoint ID for the specified name. Will
        /// create an ID if the Name is unknown.
        /// </summary>
        /// <param name="checkpointName">The name of the checkpoint to look for.</param>
        /// <returns>Always returns a number.</returns>
        int GetCheckpointID(string checkpointName);

        /// <summary>
        /// Get a simulation ID for the specified simulation name. Will
        /// create an ID if the simulationName is unknown.
        /// </summary>
        /// <param name="simulationName">The name of the simulation to look for.</param>
        /// <param name="folderName">The name of the folder the simulation belongs in.</param>
        /// <returns>Always returns a number.</returns>
        int GetSimulationID(string simulationName, string folderName);
    }
}