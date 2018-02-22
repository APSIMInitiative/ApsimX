using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [Description("This ruminant filter group selects specific individuals from the ruminant herd using any number of Ruminant Filters.\nThis filter group includes feeding rules. No filters will apply rules to current herd.\nMultiple feeding groups will select groups of individuals required.")]
    public class RuminantFeedGroup: CLEMModel
    {
        /// <summary>
        /// Daily value to supply for each month
        /// </summary>
        [Description("Daily value to supply for each month")]
        [ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFeedGroup()
        {
            MonthlyValues = new double[12];
        }
    }
}
