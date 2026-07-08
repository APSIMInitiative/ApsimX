using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Models.Surface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing pasture pool output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.CLEMReportResultsPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report provides pasture details as well as reporting the properties of all pasture pools present.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/PasturePoolDetails.htm")]
    public class ReportPasturePoolDetails : Report, ICLEMUI
    {
        /// <summary>
        /// Per ha
        /// </summary>
        [Category("Report", "Units")]
        [Description("Report per hectare")]
        public bool ReportPerHectare { get; set; }

        /// <summary>
        /// Report in tonnes
        /// </summary>
        [Category("Report", "Units")]
        [Description("Report in tonnes")]
        public bool ReportInTonnes { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("Total")]
        public bool ReportTotal { get; set; }

        /// <summary>
        /// Pasture growth in timestep (kg)
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("New growth")]
        public bool ReportGrowth { get; set; }

        /// <summary>
        /// Pasture consumed in timestep
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("Consumed")]
        public bool ReportConsumed { get; set; }

        /// <summary>
        /// Pasture detached in timestep
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("Detached")]
        public bool ReportDetached { get; set; }

        /// <summary>
        /// Pasture Nitrogen (%)
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("Nitrogen (%)")]
        public bool ReportNitrogen { get; set; }

        /// <summary>
        /// Dry Matter Digestibility (DMD, %)
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("Dry matter digestibility (%, DMD)")]
        public bool ReportDMD { get; set; }

        /// <summary>
        /// Average age in timestep
        /// </summary>
        [Category("Report", "By pasture")]
        [Description("Average pasture age")]
        public bool ReportAge { get; set; }

        /// <summary>
        /// Pools Amount (kg)
        /// </summary>
        [Category("Report", "By pools")]
        [Description("Total in each pool")]
        public bool ReportPoolsTotal { get; set; }

        /// <summary>
        /// Pools consumed in timestep (kg)
        /// </summary>
        [Category("Report", "By pools")]
        [Description("Consumed from each pool")]
        public bool ReportPoolsConsumed { get; set; }

        /// <summary>
        /// Pools detached in timestep (kg)
        /// </summary>
        [Category("Report", "By pools")]
        [Description("Detached from each pool")]
        public bool ReportPoolsDetached { get; set; }

        /// <summary>
        /// Pools nitrogen content (%)
        /// </summary>
        [Category("Report", "By pools")]
        [Description("Nitrogen (%) of each pool")]
        public bool ReportPoolsNitrogen { get; set; }

        /// <summary>
        /// Pools dry matter digestibility (%)
        /// </summary>
        [Category("Report", "By pools")]
        [Description("Dry matter digestibility (DMD) of each pool")]
        public bool ReportPoolsDMD { get; set; }

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("FinalInitialise")]
        private void OnCommencing(object sender, EventArgs e)
        {
            List<string> variableNames = new List<string>();

            // for each graze food store

            ResourcesHolder resHolder = Structure.Find<ResourcesHolder>();
            if (resHolder is null)
                return;

            List<string> pastureEntries = new();
            if (ReportTotal) pastureEntries.Add("Amount");
            if (ReportGrowth) pastureEntries.Add("Growth");
            if (ReportConsumed) pastureEntries.Add("Consumed");
            if (ReportDetached) pastureEntries.Add("Detached");
            if (ReportNitrogen) pastureEntries.Add("Nitrogen");
            if (ReportDMD) pastureEntries.Add("DMD");
            if (ReportAge) pastureEntries.Add("Age");

            List<string> poolEntries = new();
            if (ReportPoolsTotal) poolEntries.Add("Amount");
            if (ReportPoolsConsumed) poolEntries.Add("Consumed");
            if (ReportPoolsDetached) poolEntries.Add("Detached");
            if (ReportPoolsNitrogen) poolEntries.Add("Nitrogen");
            if (ReportPoolsDMD) poolEntries.Add("DMD");

            foreach (IGrazeFoodStoreType pasture in Structure.FindAll<IGrazeFoodStoreType>())
            {
                GrazeFoodStoreAPSIMLink resHolderAPSIM = null;
                if (pasture is GrazeFoodStoreAPSIMLink)
                {
                    resHolderAPSIM = pasture as GrazeFoodStoreAPSIMLink;
                }

                // pasture based measures
                foreach (string pastureVariable in pastureEntries)
                {
                    variableNames.Add($"[{resHolder.Name}].{(pasture as CLEMModel).NameWithParent}.Report(\"{pastureVariable}\", {ReportInTonnes.ToString().ToLower()}, {ReportPerHectare.ToString().ToLower()}, -1) as {pasture.Name}.{pastureVariable}");
                    if (resHolderAPSIM is not null && pastureVariable == "Amount")
                    {
                        variableNames.Add($"[{resHolder.Name}].{(pasture as CLEMModel).NameWithParent}.Report(\"AmountConsumable\", {ReportInTonnes.ToString().ToLower()}, {ReportPerHectare.ToString().ToLower()}, -1) as {pasture.Name}.Consumable");
                    }
                }


                // by pool measures
                // todo: does not currently work for APSIMlinked pastures
                foreach (string poolVariable in poolEntries)
                {
                    string extraName = "";
                    int poolCount = 12;
                    if (resHolderAPSIM is not null)
                    {
                        poolCount = resHolderAPSIM.NumberOfItakeStores - 1;
                    }

                    for (int j = 0; j <= poolCount; j++)
                    {
                        if (resHolderAPSIM is not null)
                        {
                            extraName = resHolderAPSIM.GetStoreName(j);
                        }
                        else
                        {
                            extraName = j.ToString();
                        }
                        variableNames.Add($"[{resHolder.Name}].{(pasture as CLEMModel).NameWithParent}.Report(\"{poolVariable}\", {ReportInTonnes.ToString().ToLower()}, {ReportPerHectare.ToString().ToLower()}, {j}) as {pasture.Name}.{extraName}.{poolVariable}");
                    }
                }
            }

            variableNames = variableNames.OrderBy(a => a).ToList();
            variableNames.Insert(0, "[Clock].Today as Date");
            VariableNames = variableNames.ToArray();

            if (EventNames == null || EventNames.Count() == 0)
            {
                EventNames = new string[] { "[Clock].CLEMEvents.CLEMAnimalSell" }; 
            }

            SubscribeToEvents();
        }
    }
}
