using System;
using System.Collections.Generic;
using System.Data;

namespace Models.Core
{
    /// <summary>
    /// Interface for reading and writing data to/from permanent storage.
    /// </summary>
    public interface IStorageWriter
    {

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        void WriteRow(string simulationName, string tableName, IList<string> columnNames, IList<string> columnUnits, IList<object> valuesToWrite);

        /// <summary>
        /// Write a table of data. Uses the TableName property of the specified DataTable.
        /// </summary>
        /// <param name="data">The data to write.</param>
        void WriteTable(DataTable data);

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

        /// <summary>Wait for all records to be written.</summary>
        void WaitForIdle();

        /// <summary>Stop all writing to database.</summary>
        void Stop();
    }
}