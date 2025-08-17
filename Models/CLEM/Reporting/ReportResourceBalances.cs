using APSIM.Shared.Utilities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;

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
    public class ReportResourceBalances : Models.Report, ICLEMUI
    {
        [Link]
        private ResourcesHolder resources = null;
        [Link]
        private Summary summary = null;

        private IEnumerable<IActivityTimer> timers;

        /// <summary>
        /// Gets or sets report groups for outputting
        /// </summary>
        [Description("Resource groups")]
        [Category("General", "Resources")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "At least one Resource group must be provided for the Balances Report")]
        public string[] ResourceGroupsToReport { get; set; }

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
        /// Report combined values for herd when using categories
        /// </summary>
        [Category("Report", "Ruminants")]
        [Description("Include herd totals")]
        public bool ReportHerdTotals { get; set; }

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

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

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
            if (ResourceGroupsToReport is null || !ResourceGroupsToReport.Any())
                return;

            timers = Structure.FindChildren<IActivityTimer>();

            List<string> variableNames = new List<string>();
            if (ResourceGroupsToReport.Where(a => a.Contains("[Clock].Today")).Any() is false)
                variableNames.Add("[Clock].Today as Date");

            if (ResourceGroupsToReport != null)
            {
                for (int i = 0; i < this.ResourceGroupsToReport.Length; i++)
                {
                    // each variable name is now a ResourceGroup
                    bool isDuplicate = StringUtilities.IndexOfCaseInsensitive(variableNames, this.ResourceGroupsToReport[i].Trim()) != -1;
                    if (!isDuplicate && this.ResourceGroupsToReport[i] != string.Empty)
                    {
                        if (this.ResourceGroupsToReport[i].StartsWith("["))
                            variableNames.Add(this.ResourceGroupsToReport[i]);
                        else
                        {
                            // check it is a ResourceGroup
                            CLEMModel model = resources.FindResource<ResourceBaseWithTransactions>(this.ResourceGroupsToReport[i]);
                            if (model == null)
                                summary.WriteMessage(this, $"Invalid resource group [r={this.ResourceGroupsToReport[i]}] in ReportResourceBalances [{this.Name}]{Environment.NewLine}Entry has been ignored", MessageType.Warning);
                            else
                            {
                                if (model is Labour)
                                {
                                    string amountStr = "Amount";
                                    if (ReportLabourIndividuals)
                                        amountStr = "Individuals";

                                    for (int j = 0; j < (model as Labour).Items.Count; j++)
                                    {
                                        if (ReportAmount)
                                            variableNames.Add("[Resources]." + this.ResourceGroupsToReport[i] + ".Items[" + (j + 1).ToString() + $"].{amountStr} as " + (model as Labour).Items[j].Name);

                                        //TODO: what economic metric is needed for labour
                                        //TODO: add ability to report labour value if required
                                    }
                                }
                                else
                                {
                                    foreach (CLEMModel item in Structure.FindChildren<CLEMModel>(relativeTo: model))
                                    {
                                        string amountStr = "Amount";
                                        switch (item)
                                        {
                                            case FinanceType ftype:
                                                amountStr = "Balance";
                                                break;
                                            case LandType ltype:
                                                if (ReportLandEntire)
                                                    amountStr = "LandArea";
                                                break;
                                            default:
                                                break;
                                        }
                                        if (item is RuminantType)
                                        {
                                            // add each variable needed
                                            foreach (var category in (model as RuminantHerd).GetReportingGroups(item as RuminantType))
                                            {
                                                if (ReportAmount)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"{category}\").Count as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.Count");
                                                if (ReportAnimalEquivalents)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"{category}\").TotalAdultEquivalent as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.TotalAdultEquivalent");
                                                if (ReportAnimalWeight)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"{category}\").TotalWeight as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.TotalWeight");
                                                if (ReportValue)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"{category}\").TotalPrice as {item.Name.Replace(" ", "_")}{(((model as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined) ? $".{category.Replace(" ", "_")}" : "")}.TotalPrice");
                                            }
                                            if (ReportHerdTotals & ((item as RuminantType).Parent as RuminantHerd).TransactionStyle != RuminantTransactionsGroupingStyle.Combined)
                                            {
                                                if (ReportAmount)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"\").Count as {item.Name.Replace(" ", "_")}.All.Count");
                                                if (ReportAnimalEquivalents)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"\").TotalAdultEquivalent as {item.Name.Replace(" ", "_")}.All.TotalAdultEquivalent");
                                                if (ReportAnimalWeight)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"\").TotalWeight as {item.Name.Replace(" ", "_")}.All.TotalWeight");
                                                if (ReportValue)
                                                    variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.GetRuminantReportGroup(\"{(item as IModel).Name}\",\"\").TotalPrice as {item.Name.Replace(" ", "_")}.All.TotalPrice");
                                            }
                                        }
                                        else
                                        {
                                            if (ReportAmount)
                                                variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.{item.Name}.{amountStr} as {item.Name.Replace(" ", "_")}_Amount");
                                            if (ReportValue & item.GetType() != typeof(FinanceType))
                                                variableNames.Add($"[Resources].{this.ResourceGroupsToReport[i]}.{item.Name}.Value as {item.Name.Replace(" ", "_")}_DollarValue");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            VariableNames = variableNames.ToArray();
            // Subscribe to events.
            if (EventNames == null || !EventNames.Where(a => a.Trim() != "").Any())
                EventNames = new string[] { "[Clock].CLEMFinalizeTimeStep" };

            SubscribeToEvents();
        }

        /// <inheritdoc/>
        public override void DoOutputEvent(object sender, EventArgs e)
        {
            //  support timers
            if (timers is null || !timers.Any() || timers.Sum(a => (a.ActivityDue ? 1 : 0)) > 0)
                DoOutput();
        }
    }
}
