using APSIM.Shared.Utilities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Core.Run;
using Models;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing resource ledger output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.CLEMReportResultsPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a ledger of all shortfalls in CLEM Resource requests.")]
    [Version(1, 0, 4, "Report style property allows Type and Amount transaction reporting")]
    [Version(1, 0, 3, "Now includes Category and RelatesTo fields for grouping in analysis.")]
    [Version(1, 0, 2, "Updated to enable ResourceUnitsConverter to be used.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/Ledgers.htm")]
    public class ReportResourceLedger : Models.Report
    {
        /// <summary>
        /// Style of transaction report to use
        /// </summary>
        [Description("Style of report")]
        public ReportTransactionStyle ReportStyle { get; set; }

        /// <summary>
        /// Gets or sets variable names for output
        /// </summary>
        [Summary]
        [Description("Resource group name")]
        public new string[] VariableNames { get; set; }

        /// <summary>
        /// Report all losses as -ve values
        /// </summary>
        [Summary]
        [Description("Report losses as negative")]
        public bool ReportLossesAsNegative { get; set; }

        /// <summary>
        /// Include price conversion if available
        /// </summary>
        [Summary]
        [Description("Include resource pricing")]
        public bool IncludePrice { get; set; }

        /// <summary>
        /// Include unit conversion if available
        /// </summary>
        [Summary]
        [Description("Include all unit conversions")]
        public bool IncludeConversions { get; set; }

        /// <summary>
        /// Gets or sets event names for outputting
        /// </summary>
        [JsonIgnore]
        public new string[] EventNames { get; set; }

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
            int lossModifier = 1;
            if (ReportLossesAsNegative)
            {
                lossModifier = -1;
            }

            // check if running from a CLEM.Market
            bool market = (FindAncestor<Zone>().GetType() == typeof(Market));

            dataToWriteToDb = null;
            // sanitise the variable names and remove duplicates
            List<string> variableNames = new List<string>
            {
                "[Clock].Today as Date"
            };
            if (VariableNames != null && VariableNames.Count() > 0)
            {
                if(VariableNames.Count() > 1)
                {
                    Summary.WriteWarning(this, String.Format("Multiple resource groups not permitted in ReportResourceLedger [{0}]\r\nAdditional entries have been ignored", this.Name));
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
                            Summary.WriteWarning(this, String.Format("Invalid resource group [{0}] in ReportResourceBalances [{1}]\r\nEntry has been ignored", this.VariableNames[i], this.Name));
                        }
                        else
                        {
                            bool pricingIncluded = false;
                            if (model.GetType() == typeof(RuminantHerd))
                            {
                                pricingIncluded = model.FindAllDescendants<AnimalPricing>().Where(a => a.Enabled).Count() > 0;

                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.ID as uID");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Breed as Breed");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Gender as Sex");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Age as Age");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Weight as Weight");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.AdultEquivalent as AE");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.SaleFlag as Category");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.Category as Class");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.HerdName as RelatesTo");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ExtraInformation.PopulationChangeDirection as Change");

                                // ToDo: add pricing for ruminants including buy and sell pricing
                                // Needs update in CLEMResourceTypeBase and link between ResourcePricing and AnimalPricing.
                            }
                            else
                            {
                                pricingIncluded = model.FindAllDescendants<ResourcePricing>().Where(a => a.Enabled).Count() > 0;

                                if (ReportStyle == ReportTransactionStyle.GainAndLossColumns)
                                {
                                    variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Gain as Gain");
                                    variableNames.Add("[Resources]." + this.VariableNames[i] + $".LastTransaction.Loss * {lossModifier} as Loss");
                                }
                                else
                                {
                                    variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.TransactionType as Type");
                                    variableNames.Add("[Resources]." + this.VariableNames[i] + $".LastTransaction.AmountModifiedForLoss({ReportLossesAsNegative}) as Amount");
                                }

                                // get all converters for this type of resource
                                if (IncludeConversions)
                                {
                                    var converterList = model.FindAllDescendants<ResourceUnitsConverter>().Select(a => a.Name).Distinct();
                                    if (converterList != null)
                                    {
                                        foreach (var item in converterList)
                                        {
                                            if (ReportStyle == ReportTransactionStyle.GainAndLossColumns)
                                            {
                                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ConvertTo(" + item + $",\"gain\",{ReportLossesAsNegative}) as " + item + "_Gain");
                                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ConvertTo(" + item + $",\"loss\",{ReportLossesAsNegative}) as " + item + "_Loss");
                                            }
                                            else
                                            {
                                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ConvertTo(" + item + $"\", {ReportLossesAsNegative}) as " + item + "_Amount");
                                            }
                                        }
                                    } 
                                }

                                // add pricing
                                if (IncludePrice && pricingIncluded)
                                {
                                    if (ReportStyle == ReportTransactionStyle.GainAndLossColumns)
                                    {
                                        variableNames.Add("[Resources]." + this.VariableNames[i] + $".LastTransaction.ConvertTo(\"$gain\",\"gain\", {ReportLossesAsNegative}) as Price_Gain");
                                        variableNames.Add("[Resources]." + this.VariableNames[i] + $".LastTransaction.ConvertTo(\"$loss\",\"loss\", {ReportLossesAsNegative}) as Price_Loss"); 
                                    }
                                    else
                                    {
                                        variableNames.Add("[Resources]." + this.VariableNames[i] + $".LastTransaction.ConvertTo(\"$gain\", {ReportLossesAsNegative}) as Price_Amount");
                                    }
                                }

                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.ResourceType.Name as Resource");
                                // if this is a multi CLEM model simulation then add a new column with the parent Zone name
                                if(FindAncestor<Simulation>().FindChild<Market>()!=null)
                                {
                                    variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Activity.CLEMParentName as Source");
                                }
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Activity.Name as Activity");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.RelatesToResource as RelatesTo");
                                variableNames.Add("[Resources]." + this.VariableNames[i] + ".LastTransaction.Category as Category");
                            }
                        }
                    }
                }
                events.Subscribe("[Resources]." + this.VariableNames[0] + ".TransactionOccurred", DoOutputEvent);
            }
            // Tidy up variable/event names.
            base.VariableNames = variableNames.ToArray();
            VariableNames = variableNames.ToArray();
            base.VariableNames = TidyUpVariableNames();
            VariableNames = base.VariableNames;
            base.EventNames = TidyUpEventNames();
            EventNames = base.EventNames;
            this.FindVariableMembers();
        }

    }
}
