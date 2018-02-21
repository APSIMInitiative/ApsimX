namespace APSIMRunner
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Runners;
    using System.Collections.Generic;
    using System.Linq;

    class StorageViaSockets : Model, IStorageWriter
    {
        List<JobRunnerMultiProcess.TransferRowInTable> data = new List<JobRunnerMultiProcess.TransferRowInTable>();

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite)
        {
            JobRunnerMultiProcess.TransferRowInTable rowData = new JobRunnerMultiProcess.TransferRowInTable()
            {
                simulationName = simulationName,
                tableName = tableName, 
                columnNames = columnNames.ToArray(),
                values = valuesToWrite
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
    }
}
