using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    [Version(1, 0, 2, "Includes attribute specification for whole herd")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantInitialCohorts.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantType) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class RuminantInitialCohorts : CLEMModel
    {
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
        /// Create the individual ruminant animals for this Ruminant Type (Breed)
        /// </summary>
        /// <returns>A list of ruminants</returns>
        public List<Ruminant> CreateIndividuals(DateTime date)
        {
            List<ISetAttribute> initialCohortAttributes = this.FindAllChildren<ISetAttribute>().ToList();
            List<Ruminant> individuals = new ();
            foreach (RuminantTypeCohort cohort in this.FindAllChildren<RuminantTypeCohort>())
                individuals.AddRange(cohort.CreateIndividuals(initialCohortAttributes.ToList(), date));

            return individuals;
        }

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
            return "";
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



