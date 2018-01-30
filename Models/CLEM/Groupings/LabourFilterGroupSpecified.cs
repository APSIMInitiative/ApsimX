using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individul ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IATGrowCropLabour))]
    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
    [ValidParent(ParentType = typeof(RuminantActivityMuster))]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [ValidParent(ParentType = typeof(RuminantActivityHerdCost))]
    [ValidParent(ParentType = typeof(RuminantActivityMilking))]
    [ValidParent(ParentType = typeof(OtherAnimalsActivityBreed))]
    [ValidParent(ParentType = typeof(OtherAnimalsActivityFeed))]
    [ValidParent(ParentType = typeof(RuminantActivityCollectManureAll))]
    [ValidParent(ParentType = typeof(RuminantActivityCollectManurePaddock))]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [ValidParent(ParentType = typeof(ResourceActivitySell))]
    [Description("This labour filter group determines the days required based on labour per unit resource of specific individuals from the labour pool using any number of Labour Filters. This is used by a range of activities.. Multiple filters will select groups of individuals required.")]
    public class LabourFilterGroupSpecified: LabourFilterGroup
    {
        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [Description("Days labour required [per unit or fixed] (days)")]
        [Required, GreaterThanEqualValue(0)]
        public double LabourPerUnit { get; set; }

        /// <summary>
        /// Size of unit
        /// </summary>
        [Description("Size of unit")]
        [Required, GreaterThanEqualValue(0)]
        public double UnitSize { get; set; }

        /// <summary>
        /// Labour unit type
        /// </summary>
        [Description("Units for 'Size of unit'")]
        [Required]
        public LabourUnitType UnitType { get; set; }

    }
}
