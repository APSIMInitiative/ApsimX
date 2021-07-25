using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using APSIM.Shared.Utilities;
using System.Data;
using System.IO;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using Models.Core.Run;
using Models.Storage;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing resource balances to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.CLEMReportResultsPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a current balance column for each CLEM Resource Type\r\nassociated with the CLEM Resource Groups specified (name only) in the variable list.")]
    [Version(1, 0, 3, "Respects herd transaction style in reporting herd breakdown columns")]
    [Version(1, 0, 2, "Includes value as reportable columns")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/ResourceBalances.htm")]
    public class ReportResourceBalances: Models.Report
    {
        [Link]
        private ResourcesHolder Resources = null;
        [Link]
        private Summary Summary = null;

        /// <summary>
        /// Gets or sets report groups for outputting
        /// </summary>
        [Description("Resource groups")]
        //[Display(Type = DisplayType.MultiLineText)]
        [Category("General", "Resources")]
        public override string[] VariableNames { get; set; }

        /// <summary>
        /// Gets or sets event names for outputting
        /// </summary>
        [JsonIgnore]
        [Description("")]
        public override string[] EventNames { get; set; }

        /// <summary>
        /// Report balances of amount
        /// </summary>
        [Category("Report", "General")]
        [Description("Report physical amount")]
        public bool ReportAmount { get; set; }

        /// <summary>
        /// Report balances of value
        /// </summary>
        [Category("Report", "Economics")]
        [Description("Report dollar value")]
        public bool ReportValue { get; set; }

        /// <summary>
        /// Report balances of animal equivalents
        /// </summary>
        [Category("Report", "Ruminants")]
        [Description("Report ruminant Adult Equivalents")]
        public bool ReportAnimalEquivalents { get; set; }

        /// <summary>
        /// Report balances of animal weight
        /// </summary>
        [Category("Report", "Ruminants")]
        [Description("Report Ruminant total weight")]
        public bool ReportAnimalWeight { get; set; }

        /// <summary>
        /// Report available land as balance
        /// </summary>
        [Category("Report", "Land")]
        [Description("Report Land as area present")]
        public bool ReportLandEntire { get; set; }

        /// <summary>
        /// Report available labour in individuals
        /// </summary>
        [Category("Report", "Land")]
        [Description("Report Labour as individuals")]
        public bool ReportLabourIndividuals { get; set; }


        /// <summary>The columns to write to the data store.</summary>
        [NonSerialized]
        private List<IReportColumn> columns = null;
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

        private IEnumerable<IActivityTimer> timers;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportResourceBalances()
        {
            ReportAmount = true;
        }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("FinalInitialise")] // "Commencing"
        private void OnCommencing(object sender, EventArgs e)
        {
            timers = FindAllChildren<IActivityTimer>();

            dataToWriteToDb = null;
            // sanitise the variable names and remove duplicates
            
            List<string> variableNames = new List<string>();
            if (VariableNames.Where(a => a.Contains("[Clock].Today")).Any() is false)
            {
                variableNames.Add("[Clock].Today as Date");
            }

            if (VariableNames != null)
            {
                for (int i = 0; i < this.VariableNames.Length; i++)
                {
                    // each variable name is now a ResourceGroup
                    bool isDuplicate = StringUtilities.IndexOfCaseInsensitive(variableNames, this.VariableNames[i].Trim()) != -1;
                    if (!isDuplicate && this.VariableNames[i] != string.Empty)
                    {
                        if (this.VariableNames[i].StartsWith("["))
                        {
                            variableNames.Add(this.VariableNames[i]);
                        }
                        else
                        {
                            // check it is a ResourceGroup
                            CLEMModel model = Resources.GetResourceGroupByName(this.VariableNames[i]) as CLEMModel;
                            if (model == null)
                            {
                                Summary.WriteWarning(this, $"Invalid resource group [r={this.VariableNames[i]}] in ReportResourceBalances [{this.Name}]{Environment.NewLine}Entry has been ignored");
                            }
                            else
                            {
                                if (model.GetType().Name == "Labour")
                                {
                                    string amountStr = "Amount";
                                    if (ReportLabourIndividuals)
                                    {
                                        amountStr = "Individuals";
                                    }

                                    for (int j = 0; j < (model as Labour).Items.Count; j++)
                                    {
                                        if (ReportAmount)
                                        {
                                            variableNames.Add("[Resources]." + this.VariableNames[i] + ".Items[" + (j + 1).ToString() + $"].{amountStr} as " + (model as Labour).Items[j].Name); 
                                        }

                                        //TODO: what economic metric is needed for labour
                                        //TODO: add ability to report labour value if required
                                    }
                                }
                                else
                                {
                                    // get all children
                                    foreach (CLEMModel item in model.Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMModel)))) // this.FindAllChildren<CLEMModel>()) //
                                    {
                                        string amountStr = "Amount";
                                        switch (item.GetType().Name)
                                        {
                                            case "FinanceType":
                                                amountStr = "Balance";
                                                break;
                                            case "LandType":
                                                if (ReportLandEntire)
                                                {
                                                    amountStr = "LandArea";
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                        if (item.GetType().Name == "RuminantType")
                                        {
                                            // add each variable needed
                                            foreach (var category in (model as RuminantHerd).GetReportingGroups(item as RuminantType))
                                            {
                                                if (ReportAmount)
                                                {
                                                    variableNames.Add($"[Resources].{this.VariableNames[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"{category}\").Count as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.Count");
                                                }
                                                if (ReportAnimalEquivalents)
                                                {
                                                    variableNames.Add($"[Resources].{this.VariableNames[i]}.GetRuminantReportGroup({(item as IModel).Name},{category}).TotalAE as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.AE");
                                                }
                                                if (ReportAnimalWeight)
                                                {
                                                    variableNames.Add($"[Resources].{this.VariableNames[i]}.GetRuminantReportGroup({(item as IModel).Name},{category}).TotalWeight as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.Weight");
                                                }
                                                if (ReportValue)
                                                {
                                                    variableNames.Add($"[Resources].{this.VariableNames[i]}.GetRuminantReportGroup({(item as IModel).Name},{category}).TotalValue as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.Value");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ReportAmount)
                                            {
                                                variableNames.Add($"[Resources].{this.VariableNames[i]}.{ item.Name}.{ amountStr } as { item.Name.Replace(" ", "_") }_Amount");
                                            }
                                            if (ReportValue & item.GetType().Name != "FinanceType")
                                            {
                                                variableNames.Add($"[Resources].{this.VariableNames[i]}.{ item.Name}.CalculateValue({ $"[Resources].{this.VariableNames[i]}.{ item.Name}.{ amountStr }" }, False) as { item.Name.Replace(" ", "_") }_Value");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            VariableNames = variableNames.ToArray();
            // Tidy up variable/event names.
            VariableNames = TidyUpVariableNames();
            EventNames = TidyUpEventNames();
            this.FindVariableMembers();

            if (EventNames.Length == 0 || EventNames[0] == "")
            {
                events.Subscribe("[Clock].CLEMEndOfTimeStep", DoOutputEvent);
            }
            else
            {
                // Subscribe to events.
                foreach (string eventName in EventNames)
                {
                    if (eventName != string.Empty)
                    {
                        events.Subscribe(eventName.Trim(), DoOutputEvent);
                    }
                }
            }
        }

        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            if (dataToWriteToDb != null)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
            }
            dataToWriteToDb = null;
        }
        /// <inheritdoc/>
        public override void DoOutputEvent(object sender, EventArgs e)
        {
            if (timers == null || timers.Sum(a => (a.ActivityDue ? 1 : 0)) > 0)
            {
                DoOutput();
            }
        }

        /// <summary>A method that can be called by other models to perform a line of output.</summary>
        public new void DoOutput()
        {
            if (dataToWriteToDb == null)
            {
                string folderName = null;
                var folderDescriptor = simulation.Descriptors.Find(d => d.Name == "FolderName");
                if (folderDescriptor != null)
                {
                    folderName = folderDescriptor.Value;
                }

                dataToWriteToDb = new ReportData()
                {
                    FolderName = folderName,
                    SimulationName = simulation.Name,
                    TableName = Name,
                    ColumnNames = columns.Select(c => c.Name).ToList(),
                    ColumnUnits = columns.Select(c => c.Units).ToList()
                };
            }

            // Get number of groups.
            var numGroups = Math.Max(1, columns.Max(c => c.NumberOfGroups));

            for (int groupIndex = 0; groupIndex < numGroups; groupIndex++)
            {
                // Create a row ready for writing.
                List<object> valuesToWrite = new List<object>();
                List<string> invalidVariables = new List<string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    try
                    {
                        valuesToWrite.Add(columns[i].GetValue(groupIndex));
                    }
                    catch// (Exception err)
                    {
                        // Should we include exception message?
                        invalidVariables.Add(columns[i].Name);
                    }
                }
                if (invalidVariables != null && invalidVariables.Count > 0)
                {
                    throw new Exception($"Error in report {Name}: Invalid report variables found:\r\n{string.Join("\r\n", invalidVariables)}");
                }

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

        /// <summary>Sort the columns alphabetically</summary>
        /// <param name="table">The table to sort</param>
        private static void SortColumnsOfDataTable(DataTable table)
        {
            var columnArray = new DataColumn[table.Columns.Count];
            table.Columns.CopyTo(columnArray, 0);
            var ordinal = -1;
            foreach (var orderedColumn in columnArray.OrderBy(c => c.ColumnName))
            {
                orderedColumn.SetOrdinal(++ordinal);
            }

            ordinal = -1;
            int i = table.Columns.IndexOf("SimulationName");
            if (i != -1)
            {
                table.Columns[i].SetOrdinal(++ordinal);
            }

            i = table.Columns.IndexOf("SimulationID");
            if (i != -1)
            {
                table.Columns[i].SetOrdinal(++ordinal);
            }
        }

        /// <summary>
        /// Fill the Members list with VariableMember objects for each variable.
        /// </summary>
        private new void FindVariableMembers()
        {
            this.columns = new List<IReportColumn>();

            AddExperimentFactorLevels();

            // If a group by variable was specified then all columns need to be aggregated
            // columns. Find the first aggregated column so that we can, later, use its from and to
            // variables to create an agregated column that doesn't have them.
            string from = null;
            string to = null;
            if (!string.IsNullOrEmpty(GroupByVariableName))
            {
                FindFromTo(out from, out to);
            }

            foreach (string fullVariableName in this.VariableNames)
            {
                try
                {
                    if (!string.IsNullOrEmpty(fullVariableName))
                    {
                        columns.Add(new ReportColumn(fullVariableName, clock, locator, events, GroupByVariableName, from, to));
                    }
                }
                catch (Exception err)
                {
                    throw new Exception($"Error while creating report column '{fullVariableName}'", err);
                }
            }
        }

        /// <summary>Add the experiment factor levels as columns.</summary>
        private void AddExperimentFactorLevels()
        {
            if (simulation.Descriptors != null)
            {
                foreach (var descriptor in simulation.Descriptors)
                {
                    if (descriptor.Name != "Zone" && descriptor.Name != "SimulationName")
                    {
                        this.columns.Add(new ReportColumnConstantValue(descriptor.Name, descriptor.Value));
                    }
                }
            }
        }
    }
}
