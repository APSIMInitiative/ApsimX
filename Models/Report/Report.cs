// -----------------------------------------------------------------------
// <copyright file="Report.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Newtonsoft.Json;
    using Models.Core.Run;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Models.CLEM;

    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    public class Report : Model
    {
        /// <summary>The columns to write to the data store.</summary>
        [NonSerialized]
        private List<IReportColumn> columns = null;

        /// <summary>The data to write to the data store.</summary>
        [NonSerialized]
        private ReportData dataToWriteToDb = null;

        /// <summary>Link to a simulation</summary>
        [Link]
        private Simulation simulation = null;

        /// <summary>Link to a clock model.</summary>
        [Link]
        private IClock clock = null;

        /// <summary>Link to a storage service.</summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>Link to a locator service.</summary>
        [Link]
        private ILocator locator = null;

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// <summary>
        /// Temporarily stores which tab is currently displayed.
        /// Meaningful only within the GUI
        /// </summary>
        [XmlIgnore] public int ActiveTabIndex = 0;

        /// <summary>
        /// Gets or sets variable names for outputting
        /// </summary>
        [Summary]
        [Description("Output variables")]
        public string[] VariableNames { get; set; }

        /// <summary>
        /// Gets or sets event names for outputting
        /// </summary>
        [Summary]
        [Description("Output frequency")]
        public string[] EventNames { get; set; }

        /// <summary>
        /// Date of the last report event.
        /// </summary>
        [JsonIgnore]
        public DateTime DateOfLastOutput { get; set; }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCommencing(object sender, EventArgs e)
        {
            DateOfLastOutput = DateTime.MaxValue;
            dataToWriteToDb = null;

            // sanitise the variable names and remove duplicates
            List<string> variableNames = new List<string>();
            variableNames.Add("Parent.Name as Zone");
            for (int i = 0; i < this.VariableNames.Length; i++)
            {
                bool isDuplicate = StringUtilities.IndexOfCaseInsensitive(variableNames, this.VariableNames[i].Trim()) != -1;
                if (!isDuplicate && this.VariableNames[i] != string.Empty)
                {
                    string variable = this.VariableNames[i];

                    // If there is a comment in this line, ignore everything after (and including) the comment.
                    int commentIndex = variable.IndexOf("//");
                    if (commentIndex >= 0)
                        variable = variable.Substring(0, commentIndex);

                    // No need to add an empty variable
                    if (!string.IsNullOrEmpty(variable))
                        variableNames.Add(variable.Trim());
                }
            }
            this.VariableNames = variableNames.ToArray();
            this.FindVariableMembers();

            // Subscribe to events.
            if (EventNames != null)
            {
                foreach (string eventName in EventNames)
                {
                    if (eventName != string.Empty)
                        events.Subscribe(eventName.Trim(), DoOutputEvent);
                }
            }
        }

        /// <summary>Invoked when a simulation is completed.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            if (dataToWriteToDb != null)
                storage.Writer.WriteTable(dataToWriteToDb);
            dataToWriteToDb = null;
        }

        /// <summary>A method that can be called by other models to perform a line of output.</summary>
        public void DoOutput()
        {
            if (dataToWriteToDb == null)
            {
                string folderName = null;
                var folderDescriptor = simulation.Descriptors.Find(d => d.Name == "FolderName");
                if (folderDescriptor != null)
                    folderName = folderDescriptor.Value;
                dataToWriteToDb = new ReportData()
                {
                    FolderName = folderName,
                    SimulationName = simulation.Name,
                    TableName = Name,
                    ColumnNames = columns.Select(c => c.Name).ToList(),
                    ColumnUnits = columns.Select(c => c.Units).ToList()
                };
            }

            // Create a row ready for writing.
            List<object> valuesToWrite = new List<object>();
            for (int i = 0; i < columns.Count; i++)
                valuesToWrite.Add(columns[i].GetValue());

            // Add row to our table that will be written to the db file
            dataToWriteToDb.Rows.Add(valuesToWrite);

            // Write the table if we reach our threshold number of rows.
            if (dataToWriteToDb.Rows.Count == 100)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
                dataToWriteToDb = null;
            }

            DateOfLastOutput = clock.Today;
        }

        /// <summary>Create a text report from tables in this data store.</summary>
        /// <param name="storage">The data store.</param>
        /// <param name="fileName">Name of the file.</param>
        public static void WriteAllTables(IDataStore storage, string fileName)
        {
            // Write out each table for this simulation.
            foreach (string tableName in storage.Reader.TableNames)
            {
                DataTable data = storage.Reader.GetData(tableName);
                if (data != null && data.Rows.Count > 0)
                {
                    SortColumnsOfDataTable(data);
                    StreamWriter report = new StreamWriter(Path.ChangeExtension(fileName, "." + tableName + ".csv"));
                    DataTableUtilities.DataTableToText(data, 0, ",", true, report);
                    report.Close();
                }
            }
        }

        /// <summary>Sort the columns alphabetically</summary>
        /// <param name="table">The table to sort</param>
        private static void SortColumnsOfDataTable(DataTable table)
        {
            var columnArray = new DataColumn[table.Columns.Count];
            table.Columns.CopyTo(columnArray, 0);
            var ordinal = -1;
            foreach (var orderedColumn in columnArray.OrderBy(c => c.ColumnName))
                orderedColumn.SetOrdinal(++ordinal);

            ordinal = -1;
            int i = table.Columns.IndexOf("SimulationName");
            if (i != -1)
                table.Columns[i].SetOrdinal(++ordinal);

            i = table.Columns.IndexOf("SimulationID");
            if (i != -1)
                table.Columns[i].SetOrdinal(++ordinal);
        }


        /// <summary>Called when one of our 'EventNames' events are invoked</summary>
        public void DoOutputEvent(object sender, EventArgs e)
        {
            DoOutput();
        }

        /// <summary>
        /// Fill the Members list with VariableMember objects for each variable.
        /// </summary>
        private void FindVariableMembers()
        {
            this.columns = new List<IReportColumn>();

            AddExperimentFactorLevels();

            foreach (string fullVariableName in this.VariableNames)
            {
                if (fullVariableName != string.Empty)
                    this.columns.Add(ReportColumn.Create(fullVariableName, clock, storage.Writer, locator, events));
            }
        }

        /// <summary>Add the experiment factor levels as columns.</summary>
        private void AddExperimentFactorLevels()
        {
            if (simulation.Descriptors != null)
            {
                foreach (var descriptor in simulation.Descriptors)
                    if (descriptor.Name != "Zone" && descriptor.Name != "SimulationName")
                        this.columns.Add(new ReportColumnConstantValue(descriptor.Name, descriptor.Value));
                StoreFactorsInDataStore();
            }
        }

        /// <summary>Store descriptors in DataStore.</summary>
        private void StoreFactorsInDataStore()
        {
            if (storage != null && simulation != null && simulation.Descriptors != null)
            {
                var table = new DataTable("_Factors");
                table.Columns.Add("ExperimentName", typeof(string));
                table.Columns.Add("SimulationName", typeof(string));
                table.Columns.Add("FolderName", typeof(string));
                table.Columns.Add("FactorName", typeof(string));
                table.Columns.Add("FactorValue", typeof(string));

                var experimentDescriptor = simulation.Descriptors.Find(d => d.Name == "Experiment");
                var simulationDescriptor = simulation.Descriptors.Find(d => d.Name == "SimulationName");
                var folderDescriptor = simulation.Descriptors.Find(d => d.Name == "FolderName");

                foreach (var descriptor in simulation.Descriptors)
                {
                    if (descriptor.Name != "Experiment" &&
                        descriptor.Name != "SimulationName" &&
                        descriptor.Name != "FolderName" &&
                        descriptor.Name != "Zone")
                    {
                        var row = table.NewRow();
                        if (experimentDescriptor != null)
                            row[0] = experimentDescriptor.Value;
                        if (simulationDescriptor != null)
                            row[1] = simulationDescriptor.Value;
                        if (folderDescriptor != null)
                            row[2] = folderDescriptor.Value;
                        row[3] = descriptor.Name;
                        row[4] = descriptor.Value;
                        table.Rows.Add(row);
                    }
                }
                storage.Writer.WriteTable(table);
            }
        }
    }
}