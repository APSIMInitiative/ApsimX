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

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a current balance column for each CLEM Resource Type\nassociated with the CLEM Resource Groups specified (name only) in the variable list.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/ResourceBalances.htm")]
    public class ReportResourceBalances: Models.Report
    {
        [Link]
        private ResourcesHolder Resources = null;

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

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            dataToWriteToDb = null;
            // sanitise the variable names and remove duplicates
            
            List<string> variableNames = new List<string>();
            if (VariableNames.Where(a => a.Contains("[Clock].Today")).Count() == 0)
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
                                throw new ApsimXException(this, String.Format("@error:Invalid resource group [r={0}] in ReportResourceBalances [{1}]\nEntry has been ignored", this.VariableNames[i], this.Name));
                            }
                            else
                            {
                                if (model.GetType().Name == "Labour")
                                {
                                    for (int j = 0; j < (model as Labour).Items.Count; j++)
                                    {
                                        variableNames.Add("[Resources]." + this.VariableNames[i] + ".Items[" + (j+1).ToString() + "].AvailableDays as " + (model as Labour).Items[j].Name);
                                    }
                                }
                                else
                                {
                                    // get all children
                                    foreach (CLEMModel item in model.Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMModel)))) // Apsim.Children(this, typeof(CLEMModel))) //
                                    {
                                        string amountStr = "Amount";
                                        switch (item.GetType().Name)
                                        {
                                            case "FinanceType":
                                                amountStr = "Balance";
                                                break;
                                            case "LabourType":
                                                amountStr = "AvailableDays";
                                                break;
                                            default:
                                                break;
                                        }
                                        variableNames.Add("[Resources]." + this.VariableNames[i] + "." + item.Name + "." + amountStr + " as " + item.Name);
                                        if(item.GetType().Name == "RuminantType")
                                        {
                                            variableNames.Add("[Resources]." + this.VariableNames[i] + "." + item.Name + ".AmountAE as TotalAE");
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

        /// <summary>A method that can be called by other models to perform a line of output.</summary>
        public new void DoOutput()
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
        public static new void WriteAllTables(IDataStore storage, string fileName)
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


        /// <summary>Called when one of our 'EventNames' events are invoked</summary>
        public new void DoOutputEvent(object sender, EventArgs e)
        {
            DoOutput();
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
                FindFromTo(out from, out to);

            foreach (string fullVariableName in this.VariableNames)
            {
                try
                {
                    if (!string.IsNullOrEmpty(fullVariableName))
                        columns.Add(new ReportColumn(fullVariableName, clock, locator, events, GroupByVariableName, from, to));
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
