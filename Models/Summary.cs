using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Documentation.Extensions;
using Models.Core;
using Models.Logging;
using Models.Storage;

namespace Models
{
    /// <summary>
    /// Collects simulation initial conditions and writes messages to the DataStore
    /// during a simulation run. Also provides an API for querying stored messages
    /// and initial conditions after a run.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.SummaryView")]
    [PresenterName("UserInterface.Presenters.SummaryPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Summary : Model, ISummary, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>A link to a storage service.</summary>
        [Link]
        private IDataStore _storage = null;

        /// <summary>A link to the clock in the simulation.</summary>
        [Link]
        private IClock _clock = null;

        /// <summary>A link to the parent simulation.</summary>
        [Link]
        private Simulation _simulation = null;

        [NonSerialized]
        private DataTable _messages;

        [NonSerialized]
        private bool _afterCompleted = false;

        /// <summary>This setting controls what type of messages will be captured by the summary.</summary>
        public MessageType Verbosity { get; set; } = MessageType.All;

        /// <summary>Write a message to the summary.</summary>
        /// <param name="author">The model writing the message.</param>
        /// <param name="message">The message to write.</param>
        /// <param name="messageType">Message output/verbosity level.</param>
        public void WriteMessage(IModel author, string message, MessageType messageType)
        {
            if (Verbosity >= messageType)
            {
                if (_storage == null)
                {
                    if (author == null)
                        throw new Exception("No datastore is available!");
                    else
                        throw new ApsimXException(author, "No datastore is available!");
                }

                if (_messages == null)
                {
                    _messages = new DataTable("_Messages");
                    _messages.Columns.Add("SimulationName", typeof(string));
                    _messages.Columns.Add("ComponentName", typeof(string));
                    _messages.Columns.Add("Date", typeof(DateTime));
                    _messages.Columns.Add("Message", typeof(string));
                    _messages.Columns.Add("MessageType", typeof(int));
                }

                // Remove the path of the simulation within the .apsimx file.
                string relativeModelPath = null;
                if (author != null)
                    relativeModelPath = author.FullPath.Replace($"{_simulation.FullPath}.", string.Empty);

                DataRow row = _messages.NewRow();
                row[0] = _simulation.Name;
                row[1] = relativeModelPath;
                row[2] = _clock.Today;
                row[3] = message;
                row[4] = (int)messageType;
                _messages.Rows.Add(row);

                // This message has come in after the simulation has completed, potentially due to a late or mis-ordered event.
                if (_afterCompleted)
                    WriteMessagesToDataStore();
            }
        }

        /// <summary>Writes all stored messages to the datastore.</summary>
        public void WriteMessagesToDataStore()
        {
            if (_messages != null)
            {
                _storage?.Writer?.WriteTable(_messages, false);
                _messages = null;
            }
        }

        /// <summary>
        /// Retrieve all messages for the given simulation from the data store.
        /// </summary>
        /// <param name="simulationName">Name of the simulation.</param>
        public IEnumerable<Message> GetMessages(string simulationName)
        {
            IDataStore storage = _storage ?? Structure.Find<IDataStore>();
            if (storage == null)
                yield break;
            DataTable messages = storage.Reader.GetData("_Messages", simulationNames: simulationName.ToEnumerable());
            if (messages == null)
                yield break;

            string simulationPath = Structure.Find<Simulation>(simulationName)?.FullPath;
            foreach (DataRow row in messages.Rows)
            {
                DateTime date = (DateTime)row["Date"];
                string text = row["Message"]?.ToString();
                string relativePath = row["ComponentName"]?.ToString();
                IModel model = simulationPath == null ? Structure.Find<IModel>(relativePath) : Structure.Get(simulationPath + "." + relativePath) as IModel;
                if (!Enum.TryParse<MessageType>(row["MessageType"]?.ToString(), out MessageType severity))
                    severity = MessageType.Information;
                yield return new Message(date, text, model, severity, simulationName, relativePath);
            }
        }

        /// <summary>
        /// Retrieve all initial conditions tables for the given simulation from the data store.
        /// </summary>
        /// <param name="simulationName">Name of the simulation.</param>
        public IEnumerable<InitialConditionsTable> GetInitialConditions(string simulationName)
        {
            IDataStore storage = _storage ?? Structure.Find<IDataStore>();
            if (storage == null)
                yield break;
            DataTable table = storage.Reader.GetData("_InitialConditions", simulationNames: simulationName.ToEnumerable());
            if (table == null)
                yield break;

            string simulationPath = Structure.Find<Simulation>(simulationName)?.FullPath;
            foreach (IGrouping<string, DataRow> group in table.AsEnumerable().GroupBy(r => r["ModelPath"]?.ToString()))
            {
                string relativePath = group.Key;
                IModel model = simulationPath == null ? Structure.Find<IModel>(relativePath) : Structure.Get(simulationPath + "." + relativePath) as IModel;
                yield return new InitialConditionsTable(model, group.Select(r => new InitialCondition()
                {
                    Name = r["Name"]?.ToString(),
                    Description = r["Description"]?.ToString(),
                    TypeName = r["DataType"]?.ToString(),
                    Units = r["Units"]?.ToString(),
                    DisplayFormat = r["DisplayFormat"]?.ToString(),
                    Value = r["Value"]?.ToString()
                }), relativePath);
            }
        }

        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs args)
        {
            _messages = null;
            _afterCompleted = false;
        }

        /// <summary>When the simulation is completed, write all messages to the datastore.</summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs args)
        {
            WriteMessagesToDataStore();
            _afterCompleted = true;
        }

        /// <summary>Capture and store the initial conditions table at the start of the simulation.</summary>
        [EventSubscribe("DoInitialSummary")]
        private void OnDoInitialSummary(object sender, EventArgs e)
        {
            DataTable initialConditions = InitialConditionsBuilder.Build(_simulation);
            _storage.Writer.WriteTable(initialConditions, false);
        }
    }
}
