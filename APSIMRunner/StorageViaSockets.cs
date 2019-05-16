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
        private Guid id;

        public StorageViaSockets(string filename, Guid jobId)
        {
            FileName = filename;
            id = jobId;
        }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="tableData">Table data to write.</param>
        public void WriteTable(ReportData data)
        {
            var rowData = new JobRunnerMultiProcess.TransferReportData()
            {
                data = data,
                id = id
            };

            if (data.Rows.Count > 0)
            {
                SocketServer.CommandObject transferRowCommand = new SocketServer.CommandObject() { name = "TransferData", data = rowData };
                SocketServer.Send("127.0.0.1", 2222, transferRowCommand);
            }
        }

        public void WriteTable(DataTable data)
        {
            var rowData = new JobRunnerMultiProcess.TransferDataTable()
            {
                data = data,
                id = id
            };

            if (data.Rows.Count > 0)
            {
                SocketServer.CommandObject transferRowCommand = new SocketServer.CommandObject() { name = "TransferData", data = rowData };
                SocketServer.Send("127.0.0.1", 2222, transferRowCommand);
            }
        }

        public string FileName { get; set; }

        public IStorageReader Reader => throw new System.NotImplementedException();

        public IStorageWriter Writer { get { return this; } }


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

        public void AddUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits)
        {
            throw new NotImplementedException();
        }

        public int GetCheckpointID(string checkpointName)
        {
            throw new NotImplementedException();
        }

        public int GetSimulationID(string simulationName, string folderName)
        {
            throw new NotImplementedException();
        }

        public void Open()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the list of column names within a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <returns></returns>
        public IEnumerable<string> ColumnNames(string tableName)
        {
            throw new NotImplementedException();
        }
    }
}
