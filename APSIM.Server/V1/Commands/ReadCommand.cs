using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        public string TableName { get; set; }

        /// <summary>
        /// Parameter names to be read.
        /// </summary>
        public List<string> Parameters { get; set; }

        /// <summary>
        /// The result of the ReadCommand.
        /// Contains the data 
        /// </summary>
        [JsonConverter(typeof(DataTableConverter))]
        public DataTable Result { get; set; }

        /// <summary>
        /// Parameterless constructor for serialization
        /// </summary>
        public ReadCommand() { }

        /// <summary>
        /// Creates a <see cref="RunCommand" /> instance with sensible defaults.
        /// </summary>
        public ReadCommand(string tablename, IEnumerable<string> parameters)
        {
            this.TableName = tablename;
            this.Parameters = parameters.ToList();
        }

        /// <summary>
        /// Run the command.
        /// </summary>
        /// <param name="runner">Job runner.</param>
        public void Run(Runner runner, ServerJobRunner jobRunner, IDataStore storage)
        {
            if (!storage.Reader.TableNames.Contains(TableName))
                throw new Exception($"Table {TableName} does not exist in the database.");
            Result = storage.Reader.GetData(TableName, fieldNames: Parameters);
            if (Result == null)
                throw new Exception($"Unable to read table {TableName} from datastore (cause unknown - but the table appears to exist)");
            foreach (string param in Parameters)
                if (Result.Columns[param] == null)
                    throw new Exception($"Column {param} does not exist in table {TableName}");
            Result.TableName = TableName;
        }

        public override string ToString()
        {
            return $"{GetType().Name} with {Parameters.Count()} parameters";
        }

        public override bool Equals(object obj)
        {
            if (obj is ReadCommand command)
            {
                if (TableName != command.TableName)
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
            return (TableName, Parameters).GetHashCode();
        }
    }
}
