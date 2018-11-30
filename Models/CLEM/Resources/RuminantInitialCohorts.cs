using System;
using System.Collections.Generic;
using System.Linq;
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
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "")]
    public class RuminantInitialCohorts : CLEMModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected RuminantInitialCohorts()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Create the individual ruminant animals for this Ruminant Type (Breed)
        /// </summary>
        /// <returns></returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<Ruminant> Individuals = new List<Ruminant>();
            foreach (RuminantTypeCohort cohort in Apsim.Children(this, typeof(RuminantTypeCohort)))
            {
                Individuals.AddRange(cohort.CreateIndividuals());
            }
            return Individuals;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool FormatForParentControl)
        {
            string html = "";
            html += "</table>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool FormatForParentControl)
        {
            string html = "";
            html += "<table><tr><th>Name</th><th>Gender</th><th>Age</th><th>Weight</th><th>Number</th><th>Suckling</th><th>Sire</th></tr>";
            return html;
        }

    }
}



