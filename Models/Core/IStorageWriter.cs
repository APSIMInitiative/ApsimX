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
        void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite);

        ///// <summary>Add a checkpoint</summary>
        ///// <param name="name">Name of checkpoint</param>
        ///// <param name="filesToCheckpoint">Files to checkpoint</param>
        //void AddCheckpoint(string name, IEnumerable<string> filesToCheckpoint = null);

    }
}