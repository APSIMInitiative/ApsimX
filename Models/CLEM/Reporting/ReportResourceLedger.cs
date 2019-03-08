using APSIM.Shared.Utilities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Core.Run;
using Models.Report;
using Models.Storage;
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
    [HelpUri(@"content/features/reporting/ledgers.htm")]
    public class ReportResourceLedger : Models.Report.Report
    {
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
            dataToWriteToDb = null;
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
                    this.columns.Add(ReportColumn.Create(fullVariableName, clock, storage.Writer, locator, events));
                }
            }
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
        public new void DoOutput()
        {
            if (dataToWriteToDb == null)
                dataToWriteToDb = new ReportData()
                {
                    SimulationName = simulation.Name,
                    TableName = Name,
                    ColumnNames = columns.Select(c => c.Name).ToList(),
                    ColumnUnits = columns.Select(c => c.Units).ToList()
                };

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


    }
}
