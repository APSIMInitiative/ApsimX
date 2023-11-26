using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Defines a group of individual ruminants for which all activities below the implementation consider
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(CLEMRuminantActivityBase))]
    [Description("Specify individuals applied to all activities at or below this point in the simulation tree")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantActivityGroup.htm")]
    public class RuminantActivityGroup : FilterGroup<Ruminant>
    {
        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return "<div class=\"filtername\">This ruminant filter is applied to this activity and all activities within this branch</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "\r\n</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"filterborder filteractivityborder clearfix\">");
            if (!FindAllChildren<Filter>().Any())
                htmlWriter.Write("<div class=\"filter\">All individuals</div>");
            return htmlWriter.ToString();
        }
        #endregion

    }
}
