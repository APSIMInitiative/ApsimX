using Docker.DotNet.Models;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Holder for all initial ruminant cohorts
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyMultiModelView")]
    [PresenterName("UserInterface.Presenters.PropertyMultiModelPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Holds the list of initial cohorts for a given ruminant herd")]
    [Version(1, 0, 3, "Includes ruminant cohort file reader option")]
    [Version(1, 0, 2, "Includes attribute specification for whole herd")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantInitialCohorts.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantInitialCohorts : CLEMModel
    {
        [Link]
        private ResourcesHolder resources = null;
        private string nameOfManagedPastureFound = "";

        /// <summary>
        /// Managed pasture to move to
        /// </summary>
        [Description("Pasture to place on")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified", typeof(GrazeFoodStore) } })]
        public string ManagedPastureName { get; set; } = "Not specified";

        /// <summary>
        /// Name of model linked to ManagedPastureName that individuals will be moved to.
        /// </summary>
        public string NameOfManagedPastureForLocation => nameOfManagedPastureFound;

        /// <summary>
        /// Determines if any SetPreviousConception components were found
        /// </summary>
        [JsonIgnore]
        public bool ConceptionsFound { get; set; } = false;

        /// <summary>
        /// Determines if any SetAttribute components were found
        /// </summary>
        [JsonIgnore]
        public bool AttributesFound { get; set; } = false;

        /// <summary>
        /// Records if a warning about set weight occurred
        /// </summary>
        public bool WeightWarningOccurred = false;

        /// <summary>
        /// Overrides the base class method to allow for initialization and needs to be done before StartOfSimulation.
        /// </summary>
        [EventSubscribe("DoInitialSummary")]
        private void OnDoInitialSummary(object sender, EventArgs e)
        {
            if (ManagedPastureName is not null && ManagedPastureName != "" && ManagedPastureName.StartsWith("Not specified") == false)
            {
                CLEMModel managedPasture = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ManagedPastureName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as CLEMModel;
                nameOfManagedPastureFound = managedPasture?.Name ?? "";
            }

            foreach (FileRuminantCohorts cohortsReader in Structure.FindChildren<FileRuminantCohorts>().ToList())
            {
                foreach (RuminantTypeCohort cohort in cohortsReader.ReadCohortsFromFile())
                {
                    cohort.Parent = this;
                    cohort.MinimumTimeStepInterval = this.MinimumTimeStepInterval;
                    Structure.AddChild(cohort);
                    Links links = new();
                    links.Resolve(cohort as IModel, true, recurse: false);
                }
            }
        }

        /// <summary>
        /// Create the individual ruminant animals for this Ruminant Type (Breed)
        /// </summary>
        /// <returns>A list of ruminants</returns>
        public List<Ruminant> CreateIndividuals(DateTime date)
        {
            List<ISetAttribute> initialCohortAttributes = [.. Structure.FindChildren<ISetAttribute>()];
            List<Ruminant> individuals = [];
            foreach (RuminantTypeCohort cohort in Structure.FindChildren<RuminantTypeCohort>())
            {
                individuals.AddRange(cohort.CreateIndividuals(initialCohortAttributes, date));
            }
            return individuals;
        }
    }
}



