using Models.Core;
using Models.WholeFarm.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individul ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourceActivitySell))]
    [Description("This labour filter group determines the days required for selling a resource based on specific individuals from the labour pool using any number of Labour Filters.")]
    public class LabourFilterGroupUnit : LabourFilterGroup
    {
        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [Description("Days labour required [per unit or fixed] (days)")]
        public double LabourPerUnit { get; set; }

        /// <summary>
        /// Labour unit type
        /// </summary>
        [Description("Units for 'Size of unit'")]
        public LabourUnitType UnitType { get; set; }
    }
}
