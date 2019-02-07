using APSIM.Shared.Utilities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Report;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing resource ledger output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.ReportPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a ledger of all shortfalls in CLEM Resource requests.")]
    [Version(1, 0, 1, "")]
    public class ReportResourceLedger : Models.Report.Report
    {
        /// <summary>The columns to write to the data store.</summary>
        private List<IReportColumn> columns = null;

        /// <summary>An array of column names to write to storage.</summary>
        private IEnumerable<string> columnNames = null;

        /// <summary>An array of columns units to write to storage.</summary>
        private IEnumerable<string> columnUnits = null;

        /// <summary>Link to a simulation</summary>
        [Link]
        private Simulation simulation = null;

        /// <summary>Link to a clock model.</summary>
        [Link]
        private IClock clock = null;

        /// <summary>Link to a storage service.</summary>
        [Link]
        private IStorageWriter storage = null;

        /// <summary>Link to a locator service.</summary>
        [Link]
        private ILocator locator = null;

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        [Link]
        private ResourcesHolder Resources = null;

        [Link]
        ISummary Summary = null;

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            // sanitise the variable names and remove duplicates
            List<string> variableNames = new List<string>();
            variableNames.Add("Parent.Name as Zone");
            variableNames.Add("[Clock].Today");
            if (VariableNames != null && VariableNames.Count() > 0)
            {
                if(VariableNames.Count() > 1)
                {
                    Summary.WriteWarning(this, String.Format("Multiple resource groups not permitted in ReportResourceLedger [{0}]\nAdditional entries have been ignored", this.Name));
                }

                for (int i = 0; i < 1; i++)
                {
                    // each variable name is now a ResourceGroup
                    bool isDuplicate = StringUtilities.IndexOfCaseInsensitive(variableNames, this.VariableNames[i].Trim()) != -1;
                    if (!isDuplicate && this.VariableNames[i] != string.Empty)
                    {
                        // check it is a ResourceGroup
                        CLEMModel model = Resources.GetResourceGroupByName(this.VariableNames[i]) as CLEMModel;
                        if (model == null)
                        {
                            Summary.WriteWarning(this, String.Format("Invalid resource group [{0}] in ReportResourceBalances [{1}]\nEntry has been ignored", this.VariableNames[i], this.Name));
                        }
                        else
                        {
                            if (model.GetType() == typeof(Ruminant))
                            {
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.ID as uID");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Breed as Breed");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.HerdName as Herd");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.GenderAsString as Sex");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Age as Age");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Weight as Weight");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.SaleFlagAsString as Reason");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.PopulationChangeDirection as Change");
                            }
                            else
                            {
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Gain as Gain");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Loss * -1.0 as Loss");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ResourceType as Resource");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Activity as Activity");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ActivityType as ActivityType");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Reason as Reason");
                            }
                        }
                    }
                }
                events.Subscribe("[Resources]." + this.VariableNames[0] + ".TransactionOccurred", DoOutputEvent);

            }
            base.VariableNames = variableNames.ToArray();
            this.FindVariableMembers();
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
                {
                    this.columns.Add(ReportColumn.Create(fullVariableName, clock, storage, locator, events));
                }
            }
            columnNames = columns.Select(c => c.Name);
            columnUnits = columns.Select(c => c.Units);
        }

        /// <summary>Add the experiment factor levels as columns.</summary>
        private void AddExperimentFactorLevels()
        {
            if (ExperimentFactorValues != null)
            {
                for (int i = 0; i < ExperimentFactorNames.Count; i++)
                {
                    this.columns.Add(new ReportColumnConstantValue(ExperimentFactorNames[i], ExperimentFactorValues[i]));
                }
            }
        }

        /// <summary>Create a text report from tables in this data store.</summary>
        /// <param name="storage">The data store.</param>
        /// <param name="fileName">Name of the file.</param>
        public static new void WriteAllTables(IStorageReader storage, string fileName)
        {
            // Write out each table for this simulation.
            foreach (string tableName in storage.TableNames)
            {
                DataTable data = storage.GetData(tableName);
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

        /// <summary>A method that can be called by other models to perform a line of output.</summary>
        public new void DoOutput()
        {
            object[] valuesToWrite = new object[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                valuesToWrite[i] = columns[i].GetValue();
            }

            storage.WriteRow(simulation.Name, Name, columnNames, columnUnits, valuesToWrite);
        }
    }
}
