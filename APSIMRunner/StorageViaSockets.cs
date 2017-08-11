using APSIM.Shared.Utilities;
using Models.Core;
using System.Collections.Generic;
using static Models.Core.Runners.JobManagerMultiProcess;
using System.Linq;

namespace APSIMRunner
{
    class StorageViaSockets : Model, IStorageWriter
    {
        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite)
        {
            TransferRowInTable rowData = new TransferRowInTable()
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
