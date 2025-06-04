using DocumentFormat.OpenXml.Drawing;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Attributes;
using Models.PMF.Organs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class RuminantInitialCohorts : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Managed pasture to move to
        /// </summary>
        [Description("Pasture to place on")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified", typeof(GrazeFoodStore) } })]
        public string ManagedPastureName { get; set; } = "Not specified";

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
        /// Constructor
        /// </summary>
        protected RuminantInitialCohorts()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Overrides the base class method to allow for initialization and needs to be done before StartOfSimulation.
        /// </summary>
        [EventSubscribe("DoInitialSummary")]
        private void OnDoInitialSummary(object sender, EventArgs e)
        {
            foreach (FileRuminantCohorts cohortsReader in FindAllChildren<FileRuminantCohorts>())
            {
                foreach (RuminantTypeCohort cohort in cohortsReader.ReadCohortsFromFile())
                {
                    cohort.Parent = this;
                    cohort.MinimumTimeStepInterval = this.MinimumTimeStepInterval;
                    Structure.Add(cohort, this);
                }
            }
        }

        /// <summary>
        /// Create the individual ruminant animals for this Ruminant Type (Breed)
        /// </summary>
        /// <returns>A list of ruminants</returns>
        public List<Ruminant> CreateIndividuals(DateTime date)
        {
            List<ISetAttribute> initialCohortAttributes = FindAllChildren<ISetAttribute>().ToList();
            List<Ruminant> individuals = new();
            foreach (RuminantTypeCohort cohort in FindAllChildren<RuminantTypeCohort>())
            {
                individuals.AddRange(cohort.CreateIndividuals(initialCohortAttributes, date));
            }
            return individuals;
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ManagedPastureName == "Not specified")
            {
                GrazeFoodStore grazeFoodStore = FindInScope<GrazeFoodStore>(ManagedPastureName);
                if (grazeFoodStore == null)
                    yield return new ValidationResult($"Could not find the GrazeFoodStore (pasture) in which to place new individuals from {this.NameWithParent}", new string[] { "ManagedPastureName" });
            }
        }
        #endregion


        #region descriptive summary

        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<ISetAttribute>().Cast<IModel>(), false, "", "", "")
            };
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");

            if (FindAllChildren<FileRuminantCohorts>().Any())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Ruminant cohort file readers will be used to provide initial cohorts");
                if (FindAllChildren<RuminantTypeCohort>().Any())
                {
                    htmlWriter.Write($" which will be included with the cohorts also provided");
                }
                htmlWriter.Write(".</div>");
            }

            if (ManagedPastureName != "Not specified")
            {
                bool overridePasture = FindAllChildren<RuminantTypeCohort>().Where(a => a.ManagedPastureName != "Not specified").Any();

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (overridePasture) {
                    htmlWriter.Write($"New ");
                }
                else
                {
                    htmlWriter.Write($"All new ");
                }
                htmlWriter.Write($"individuals will be placed on the pasture <span class=\"setvalue\">{ManagedPastureName}</span>");
                if (overridePasture)
                {
                    htmlWriter.Write(" unless overridden by the cohort pasture setting");
                }
                htmlWriter.Write(".</div>");
            }

            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            if (WeightWarningOccurred)
                return "</table></br><span class=\"errorlink\">Warning: Initial weight differs from the expected normalised weight by more than 20%</span>";
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            WeightWarningOccurred = false;
            ConceptionsFound = this.FindAllDescendants<SetPreviousConception>().Any();
            AttributesFound = this.FindAllDescendants<SetAttributeWithValue>().Any();
            return $"<table><tr><th>Name</th><th>Sex</th><th>Age</th><th>Weight</th><th>Norm.Wt.</th><th>Number</th><th>Suckling</th><th>Sire</th>{(ConceptionsFound ? "<th>Pregnant</th>" : "")}{(AttributesFound ? "<th>Attributes</th>" : "")}</tr>";
        }

        #endregion
    }
}



