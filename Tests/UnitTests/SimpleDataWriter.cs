using Models.Core;
using Models.Core.Run;
using Models.Storage;
using System;
using System.Data;

namespace UnitTests
{
    /// <summary>
    /// A simple post-simulation tool which will write the specified data.
    /// </summary>
    [Serializable]
    class SimpleDataWriter : Model, IPostSimulationTool
    {
        [Link] private IDataStore storage = null;
        private DataTable data;

        /// <summary>
        /// Craete a <see cref="SimpleDataWriter"/> instance.
        /// </summary>
        /// <param name="dataToWrite">Data to be written to the database.</param>
        public SimpleDataWriter(DataTable dataToWrite)
        {
            data = dataToWrite;
        }

        public void Run()
        {
            storage.Writer.WriteTable(data);
        }
    }
}
