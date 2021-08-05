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
        public DataTable Result { get; set; }

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

        public override bool Equals(object obj)
        {
            if (obj is ReadCommand command)
            {
                if (table != command.table)
                    return false;
                if (Parameters == null && command.Parameters == null)
                    return true;
                if (Parameters == null || command.Parameters == null)
                    return false;
                if (Parameters.Zip(command.Parameters, (x, y) => x != y).Any(x => x))
                    return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (table, Parameters).GetHashCode();
        }
    }
}
