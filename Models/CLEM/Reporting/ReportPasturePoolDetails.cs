using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportView")]
    [PresenterName("UserInterface.Presenters.CLEMReportResultsPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("This report automatically generates a current balance column for each CLEM Resource Type\r\nassociated with the CLEM Resource Groups specified (name only) in the variable list.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/PasturePoolDetails.htm")]
    public class ReportPasturePoolDetails : Models.Report
    {
        /// <summary>
        /// Per ha
        /// </summary>
        [Category("Style", "")]
        [Description("Report per hectare")]
        public bool ReportPerHectare { get; set; }

        /// <summary>
        /// Report in tonnes
        /// </summary>
        [Category("Style", "")]
        [Description("Report in tonnes")]
        public bool ReportInTonnes { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [Category("By pasture", "")]
        [Description("Total")]
        public bool ReportTotal { get; set; }

        /// <summary>
        /// Pasture growth in timestep (kg)
        /// </summary>
        [Category("By pasture", "")]
        [Description("New growth")]
        public bool ReportGrowth { get; set; }

        /// <summary>
        /// Pasture consumed in timestep
        /// </summary>
        [Category("By pasture", "")]
        [Description("Consumed")]
        public bool ReportConsumed { get; set; }

        /// <summary>
        /// Pasture detached in timestep
        /// </summary>
        [Category("By pasture", "")]
        [Description("Detached")]
        public bool ReportDetached { get; set; }

        /// <summary>
        /// Pasture Nitrogen (%)
        /// </summary>
        [Category("By pasture", "")]
        [Description("Nitrogen (%)")]
        public bool ReportNitrogen { get; set; }

        /// <summary>
        /// Dry Matter Digestibility (DMD, %)
        /// </summary>
        [Category("By pasture", "")]
        [Description("Dry matter digestibility (%, DMD)")]
        public bool ReportDMD { get; set; }

        /// <summary>
        /// Average age in timestep
        /// </summary>
        [Category("By pasture", "")]
        [Description("Average pasture age")]
        public bool ReportAge { get; set; }



        /// <summary>
        /// Pools Amount (kg)
        /// </summary>
        [Category("By pools", "")]
        [Description("Total in each pool")]
        public bool ReportPoolsTotal { get; set; }

        /// <summary>
        /// Pools consumed in timestep (kg)
        /// </summary>
        [Category("By pools", "")]
        [Description("Consumed from each pool")]
        public bool ReportPoolsConsumed { get; set; }

        /// <summary>
        /// Pools detached in timestep (kg)
        /// </summary>
        [Category("By pools", "")]
        [Description("Detached from each pool")]
        public bool ReportPoolsDetached { get; set; }

        /// <summary>
        /// Pools nitrogen content (%)
        /// </summary>
        [Category("By pools", "")]
        [Description("Nitrogen (%) of each pool")]
        public bool ReportPoolsNitrogen { get; set; }

        /// <summary>
        /// Pools dry matter digestibility (%)
        /// </summary>
        [Category("By pools", "")]
        [Description("Dry matter digestibility (DMD) of each pool")]
        public bool ReportPoolsDMD { get; set; }


        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            List<string> variableNames = new List<string>();

            // for each grazefoodstore

            ResourcesHolder resHolder = FindInScope<ResourcesHolder>();
            if (resHolder is null)
                return;

            List<string> pastureEntries = new List<string>();
            if (ReportTotal) pastureEntries.Add("Amount");
            if (ReportGrowth) pastureEntries.Add("Growth");
            if (ReportConsumed) pastureEntries.Add("Consumed");
            if (ReportDetached) pastureEntries.Add("Detached");
            if (ReportNitrogen) pastureEntries.Add("Nitrogen");
            if (ReportDMD) pastureEntries.Add("DMD");
            if (ReportAge) pastureEntries.Add("Age");

            List<string> poolEntries = new List<string>();
            if (ReportPoolsTotal) poolEntries.Add("Amount");
            if (ReportPoolsConsumed) poolEntries.Add("Consumed");
            if (ReportPoolsDetached) poolEntries.Add("Detached");
            if (ReportPoolsNitrogen) poolEntries.Add("Nitrogen");
            if (ReportPoolsDMD) poolEntries.Add("DMD");

            foreach (GrazeFoodStoreType pasture in FindAllInScope<GrazeFoodStoreType>())
            {
                // pasture based measures
                foreach (string pastureVariable in pastureEntries)
                    variableNames.Add($"[{resHolder.Name}].{pasture.NameWithParent}.Report(\"{pastureVariable}\", {ReportInTonnes.ToString().ToLower()}, {ReportPerHectare.ToString().ToLower()}, -1)) as {pasture.Name}.{pastureVariable}");

                // by pool measures
                foreach (string poolVariable in poolEntries)
                {
                    for (int j = 0; j <= 12; j++)
                    {
                        variableNames.Add($"[{resHolder.Name}].{pasture.NameWithParent}.Report(\"{poolVariable}\", {ReportInTonnes.ToString().ToLower()}, {ReportPerHectare.ToString().ToLower()}, {j})) as {pasture.Name}.{poolVariable}.{j}");
                    }
                }
            }

            // sort
            variableNames = variableNames.OrderBy(a => a).ToList();
            variableNames.Insert(0, "[Clock].Today as Date");
            VariableNames = variableNames.ToArray();

            if (EventNames == null || EventNames.Count() == 0)
                EventNames = new string[] { "[Clock].CLEMHerdSummary" };

            SubscribeToEvents();
        }

    }
}
