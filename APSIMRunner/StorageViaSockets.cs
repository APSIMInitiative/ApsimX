using APSIM.Shared.Utilities;
using Models.Core;
using System.Collections.Generic;
using System.Linq;
using Models.Core.Runners;
using System;

namespace APSIMRunner
{
    class StorageViaSockets : Model, IStorageWriter
    {
        public void CompletedWritingSimulationData(string simulationName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite)
        {
            JobManagerMultiProcess.TransferRowInTable rowData = new JobManagerMultiProcess.TransferRowInTable()
            {
                simulationName = simulationName,
                tableName = tableName,
                columnNames = columnNames.ToArray(),
                values = valuesToWrite
            };

            SocketServer.CommandObject transferRowCommand = new SocketServer.CommandObject() { name = "TransferData", data = rowData };

            SocketServer.Send("127.0.0.1", 2222, transferRowCommand);
        }
    }
}
