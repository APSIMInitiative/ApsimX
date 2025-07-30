using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
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
    [Description("This report automatically generates the grazing limiters for each RuminantActivityGrazeHerd component present or created at runtime")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/GrazingEfficiency.htm")]
    public class ReportGrazingEfficiency : Models.Report, IScopeDependency
    {
        /// <summary>Scope supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IScope Scope { private get; set; }

        /// <summary>
        /// Includes the potential intake modifier from pasture quality
        /// </summary>
        [Description("Include potential intake modifier from pasture quality")]
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludePastureQualityLimiter { get; set; }

        /// <summary>
        /// Includes the potential intake modifier from pasture biomass
        /// </summary>
        [Description("Include potential intake modifier from pasture biomass")]
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludePastureBiomassLimiter { get; set; }

        /// <summary>
        /// Includes the potential intake modifier from grazing time
        /// </summary>
        [Description("Include potential intake modifier from grazing time")]
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeGrazingTimeLimiter { get; set; }

        /// <summary>
        /// Includes the potential intake modifier from competition
        /// </summary>
        [Description("Include potential intake modifier competition")]
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeCompetitionLimiter { get; set; }

        /// <summary>
        /// Includes the potential intake combined modifier
        /// </summary>
        [Description("Include combined potential intake modifier")]
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeCombinedLimit { get; set; }


        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("FinalInitialise")]
        private void OnCommencing(object sender, EventArgs e)
        {
            var grzes = Scope.FindAll<RuminantActivityGrazePastureHerd>();
            var multiHerds = Scope.FindAll<RuminantType>().Count() > 1;
            var multiPaddock = grzes.Count() > 1;

            List<string> variableNames = new List<string>();
            variableNames.Add("[Clock].Today as Date");
            foreach (var grz in grzes)
            {
                if (IncludePastureQualityLimiter)
                    variableNames.Add($"[{grz.Name}].PotentialIntakePastureQualityLimiter as {PastureHerdIdenifier(grz.Name, multiPaddock, multiHerds, "PastureQualityLimiter")}");
                if (IncludePastureBiomassLimiter)
                    variableNames.Add($"[{grz.Name}].PotentialIntakePastureBiomassLimiter as {PastureHerdIdenifier(grz.Name, multiPaddock, multiHerds, "PastureBiomassLimiter")}");
                if (IncludeGrazingTimeLimiter)
                    variableNames.Add($"[{grz.Name}].PotentialIntakeGrazingTimeLimiter as {PastureHerdIdenifier(grz.Name, multiPaddock, multiHerds, "GrazingTimeLimiter")}");
                if (IncludeCompetitionLimiter)
                    variableNames.Add($"[{grz.Name}].GrazingCompetitionLimiter as {PastureHerdIdenifier(grz.Name, multiPaddock, multiHerds, "CompetitionLimiter")}");
                if (IncludeCombinedLimit)
                    variableNames.Add($"[{grz.Name}].PotentialIntakeLimit as {PastureHerdIdenifier(grz.Name, multiPaddock, multiHerds, "CombinedLimiter")}");
            }

            VariableNames = variableNames.ToArray();

            if (EventNames == null || EventNames.Count() == 0)
                EventNames = new string[] { "[Clock].CLEMEndOfTimeStep" };

            SubscribeToEvents();
        }

        private string PastureHerdIdenifier(string modelName, bool multiPasture, bool multiHerd, string propertyName)
        {
            var nameParts = modelName.Split('_').Select(a => a.Replace(" ", "")).Skip(1).ToList();
            if (!multiPasture)
                nameParts[0] = "";
            if (!multiHerd)
                nameParts[1] = "";

            nameParts.Add(propertyName);
            return nameParts.Where(a => a != "").Join("_");
        }

    }
}
