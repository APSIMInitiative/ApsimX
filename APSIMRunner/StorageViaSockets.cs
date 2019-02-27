namespace APSIMRunner
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Models.Core.Runners;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;

    class StorageViaSockets : Model, IDataStore, IStorageWriter
    {
        List<JobRunnerMultiProcess.TransferRowInTable> data = new List<JobRunnerMultiProcess.TransferRowInTable>();
        private Guid jobKey;

        public StorageViaSockets(Guid key)
        {
            jobKey = key;
        }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IList<string> columnNames, IList<string> columnUnits, IList<object> valuesToWrite)
        {
            JobRunnerMultiProcess.TransferRowInTable rowData = new JobRunnerMultiProcess.TransferRowInTable()
            {
                key = jobKey,
                SimulationName = simulationName,
                TableName = tableName,
                ColumnNames = columnNames.ToArray(),
                Values = valuesToWrite
            };

            data.Add(rowData);
            if (data.Count == 100)
                WriteAllData();
        }

        /// <summary>Write all the data we stored</summary>
        public void WriteAllData()
        {
            if (data.Count > 0)
            {
                SocketServer.CommandObject transferRowCommand = new SocketServer.CommandObject() { name = "TransferData", data = data };
                SocketServer.Send("127.0.0.1", 2222, transferRowCommand);
                data.Clear();
            }
        }

        public string FileName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public IStorageReader Reader => throw new System.NotImplementedException();

        public IStorageWriter Writer { get { return this; } }

        public void WriteTable(DataTable data)
        {
            throw new System.NotImplementedException();
        }

        public void Empty()
        {
            throw new System.NotImplementedException();
        }

        public void AddCheckpoint(string name, IEnumerable<string> filesToStore = null)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteCheckpoint(string name)
        {
            throw new System.NotImplementedException();
        }

        public void RevertCheckpoint(string name)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void WaitForIdle()
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }

}
