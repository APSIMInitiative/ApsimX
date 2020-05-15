using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Holder for all initial ruminant cohorts
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyTablePresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This holds the list of initial cohorts for a given (parent) ruminant herd or type.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantInitialCohorts.htm")]
    public class RuminantInitialCohorts : CLEMModel
    {
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
        /// <returns></returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<Ruminant> individuals = new List<Ruminant>();
            foreach (RuminantTypeCohort cohort in Apsim.Children(this, typeof(RuminantTypeCohort)))
            {
                individuals.AddRange(cohort.CreateIndividuals());
            }
            return individuals;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "</table>";
            if(WeightWarningOccurred)
            {
                html += "</br><span class=\"errorlink\">Warning: Initial weight differs from the expected normalised weight by more than 20%</span>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            WeightWarningOccurred = false;
            html += "<table><tr><th>Name</th><th>Gender</th><th>Age</th><th>Weight</th><th>Norm.Wt.</th><th>Number</th><th>Suckling</th><th>Sire</th></tr>";
            return html;
        }

    }
}



