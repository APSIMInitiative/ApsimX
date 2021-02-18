namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.CLEM;
    using Models.Core;
    using Models.Storage;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

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
        [JsonIgnore]
        public List<IReportColumn> Columns { get; private set; } = null;

        /// <summary>The data to write to the data store.</summary>
        [NonSerialized]
        private ReportData dataToWriteToDb = null;

        /// <summary>List of strings representing dates to report on.</summary>
        private List<string> dateStringsToReportOn = new List<string>();

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
        [JsonIgnore] public int ActiveTabIndex = 0;

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
        /// Date of the day after last time report did write to storage.
        /// </summary>
        [JsonIgnore]
        public DateTime DayAfterLastOutput { get; set; }

        /// <summary>Group by variable name.</summary>
        public string GroupByVariableName{ get; set; }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("FinalInitialise")]
        private void OnFinalInitialise(object sender, EventArgs e)
        {
            DayAfterLastOutput = clock.Today;
            dataToWriteToDb = null;

            // Tidy up variable/event names.
            VariableNames = TidyUpVariableNames();
            EventNames = TidyUpEventNames();

            // Locate reporting variables.
            FindVariableMembers();

            // Silently do nothing if no event names present.
            if (EventNames == null || EventNames.Length < 1)
                return;

            // Subscribe to events.
            foreach (string eventName in EventNames)
                events.Subscribe(eventName, DoOutputEvent);
        }

        /// <summary>An event handler called at the end of each day.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("DoReport")]
        private void OnDoReport(object sender, EventArgs e)
        {
            foreach (var dateString in dateStringsToReportOn)
            {
                if (DateUtilities.DatesAreEqual(dateString, clock.Today))
                    DoOutput();
            }
        }

        /// <summary>
        /// Sanitises the event names and removes duplicates/comments.
        /// </summary>
        /// <returns></returns>
        protected string[] TidyUpEventNames()
        {
            List<string> eventNames = new List<string>();
            for (int i = 0; i < EventNames?.Length; i++)
            {
                string eventName = EventNames[i];

                // If there is a comment in this line, ignore everything after (and including) the comment.
                int commentIndex = eventName.IndexOf("//");
                if (commentIndex >= 0)
                    eventName = eventName.Substring(0, commentIndex);

                if (!string.IsNullOrWhiteSpace(eventName))
                {
                    if (DateUtilities.validateDateString(eventName) != null)
                        dateStringsToReportOn.Add(eventName);                       
                    else
                        eventNames.Add(eventName.Trim());
                }
            }

            return eventNames.ToArray();
        }

        /// <summary>
        /// Sanitises the variable names and removes duplicates/comments.
        /// </summary>
        protected string[] TidyUpVariableNames()
        {
            List<string> variableNames = new List<string>();
            IModel zone = FindAncestor<Zone>();
            if (zone == null)
                zone = simulation;
            variableNames.Add($"[{zone.Name}].Name as Zone");
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

            return variableNames.ToArray();
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
                var folderDescriptor = simulation.Descriptors?.Find(d => d.Name == "FolderName");
                if (folderDescriptor != null)
                    folderName = folderDescriptor.Value;
                dataToWriteToDb = new ReportData()
                {
                    FolderName = folderName,
                    SimulationName = simulation.Name,
                    TableName = Name,
                    ColumnNames = Columns.Select(c => c.Name).ToList(),
                    ColumnUnits = Columns.Select(c => c.Units).ToList()
                };
            }

            // Get number of groups.
            var numGroups = Math.Max(1, Columns.Max(c => c.NumberOfGroups));

            for (int groupIndex = 0; groupIndex < numGroups; groupIndex++)
            {
                // Create a row ready for writing.
                List<object> valuesToWrite = new List<object>();
                List<string> invalidVariables = new List<string>();
                for (int i = 0; i < Columns.Count; i++)
                {
                    try
                    {
                        valuesToWrite.Add(Columns[i].GetValue(groupIndex));
                    }
                    catch (Exception err)
                    {
                        // Should we include exception message?
                        invalidVariables.Add($"{Columns[i].Name}: {err.Message}");
                    }
                }
                if (invalidVariables != null && invalidVariables.Count > 0)
                    throw new Exception($"Error in report {Name}: Invalid report variables found:\n{string.Join("\n", invalidVariables)}");

                // Add row to our table that will be written to the db file
                dataToWriteToDb.Rows.Add(valuesToWrite);
            }

            // Write the table if we reach our threshold number of rows.
            if (dataToWriteToDb.Rows.Count >= 100)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
                dataToWriteToDb = null;
            }

            DayAfterLastOutput = clock.Today.AddDays(1);
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
        protected void FindVariableMembers()
        {
            this.Columns = new List<IReportColumn>();

            AddExperimentFactorLevels();

            // If a group by variable was specified then all columns need to be aggregated
            // columns. Find the first aggregated column so that we can, later, use its from and to
            // variables to create an agregated column that doesn't have them.
            string from = null;
            string to =  null;
            if (!string.IsNullOrEmpty(GroupByVariableName))
                FindFromTo(out from, out to);

            foreach (string fullVariableName in this.VariableNames)
            {
                try
                {
                    if (!string.IsNullOrEmpty(fullVariableName))
                        Columns.Add(new ReportColumn(fullVariableName, clock, locator, events, GroupByVariableName, from, to));
                }
                catch (Exception err)
                {
                    throw new Exception($"Error while creating report column '{fullVariableName}'", err);
                }
            }
        }

        /// <summary>
        /// Find and return a from and to clause in a variable.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        protected void FindFromTo(out string from, out string to)
        {
            // Find the first aggregation column.
            var firstAggregatedVariableName = VariableNames.ToList().Find(var => var.Contains(" from "));
            if (firstAggregatedVariableName == null)
                throw new Exception("A report 'group by' can only be specified if there is at least one temporal aggregated column.");

            var pattern = @".+from\W(?<from>.+)\Wto\W(?<to>.+)\Was\W.+";
            var regEx = new Regex(pattern);
            var match = regEx.Match(firstAggregatedVariableName);
            if (!match.Success)
                throw new Exception($"Invalid format for report agregation variable {firstAggregatedVariableName}");
            from = match.Groups["from"].Value;
            to = match.Groups["to"].Value;
        }

        /// <summary>Add the experiment factor levels as columns.</summary>
        private void AddExperimentFactorLevels()
        {
            if (simulation.Descriptors != null)
            {
                foreach (var descriptor in simulation.Descriptors)
                    if (descriptor.Name != "Zone" && descriptor.Name != "SimulationName")
                        this.Columns.Add(new ReportColumnConstantValue(descriptor.Name, descriptor.Value));
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