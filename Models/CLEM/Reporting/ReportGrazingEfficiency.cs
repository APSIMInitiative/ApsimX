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
using System.Globalization;
using Models.CLEM.Activities;

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
    [Description("This report automatically generates the grazing limiters for each RuminantActivityGrazeHerd component present or created at runtime")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/GrazingEfficiency.htm")]
    public class ReportGrazingEfficiency: Models.Report
    {
        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("FinalInitialise")]
        private void OnCommencing(object sender, EventArgs e)
        {
            var grzes = FindAllInScope<RuminantActivityGrazePastureHerd>();

            List<string> variableNames = new List<string>();
            variableNames.Add("[Clock].Today as Date");
            foreach (var grz in grzes)
            {
                variableNames.Add($"[{grz.Name}].PotentialIntakePastureQualityLimiter as {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}PastureQualityLimiter");
                variableNames.Add($"[{grz.Name}].PotentialIntakePastureBiomassLimiter as {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}PastureBiomassLimiter");
                variableNames.Add($"[{grz.Name}].PotentialIntakeGrazingTimeLimiter as {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}GrazingTimeLimiter");
                variableNames.Add($"[{grz.Name}].GrazingCompetitionLimiter as {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}CompetitionLimiter");
                //variableNames.Add($"({PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}CompetitionLimiter * {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}PastureQualityLimiter * {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}PastureBiomassLimiter * {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}GrazingTimeLimiter) as {PastureHerdIdenifier(grz.Name, grzes.Count() > 1)}TotalReduction");
            }

            VariableNames = variableNames.ToArray();

            if (EventNames == null || EventNames.Count() == 0)
                EventNames = new string[] { "[Clock].CLEMEndOfTimeStep" };

            SubscribeToEvents();
        }

        private string PastureHerdIdenifier(string modelName, bool multiHerdPasture)
        {
            if (!multiHerdPasture) return "";
            return $"{modelName.Split("_").Skip(1).Join("_")}_";

        }

    }
}
