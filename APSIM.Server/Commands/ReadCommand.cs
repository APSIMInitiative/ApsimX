using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace APSIM.Server.Commands
{
    /// <summary>
    /// A command to run simulations.
    /// </summary>
    [Serializable]
    public class ReadCommand : ICommand
    {
        /// <summary>
        /// Name of the table from which parameters will be read.
        /// </summary>
        private string table;

        /// <summary>
        /// Parameter names to be read.
        /// </summary>
        public IEnumerable<string> Parameters { get; private set; }

        /// <summary>
        /// The result of the ReadCommand.
        /// Contains the data 
        /// </summary>
        public DataTable Result { get; private set; }

        /// <summary>
        /// Creates a <see cref="RunCommand" /> instance with sensible defaults.
        /// </summary>
        public ReadCommand(string tablename, IEnumerable<string> parameters)
        {
            this.table = tablename;
            this.Parameters = parameters;
        }

        /// <summary>
        /// Run the command.
        /// </summary>
        /// <param name="runner">Job runner.</param>
        public void Run(Runner runner, ServerJobRunner jobRunner, IDataStore storage)
        {
            Result = storage.Reader.GetData(table, fieldNames: Parameters);
        }

        public override string ToString()
        {
            return $"{GetType().Name} with {Parameters.Count()} parameters";
        }
    }
}
