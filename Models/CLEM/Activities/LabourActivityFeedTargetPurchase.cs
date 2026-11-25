using Models.CLEM.Resources;
using Models.CLEM.Extras;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// This activity component defines food type and purchase rules for a <c>LabourActivityFeedToTargets</c>
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourActivityFeedToTargets))]
    [Description("Defines a food type for purchase towards targeted feeding")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedTargetPurchase.htm")]

    public class LabourActivityFeedTargetPurchase : CLEMModel
    {
        /// <summary>
        /// Name of food store to use
        /// </summary>
        /// <remarks>
        /// A list of available <c>HumanFoodStore</c> resources is supplied as a drop-down list in the UI.
        /// </remarks>
        [Description("Food store type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Food store required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(HumanFoodStore) } })]
        public string FoodStoreName { get; set; }

        /// <summary>
        /// The proportion of the remaining target to provide by purchase
        /// </summary>
        [Description("Proportion of remaining target")]
        [Proportion, GreaterThanEqualValue(0)]
        public double TargetProportion { get; set; }

        /// <summary>
        /// The final proportion to use. 
        /// </summary>
        [JsonIgnore]
        public double ProportionToPurchase { get; set; }

        /// <summary>
        /// The human food store linked to this target purchase
        /// </summary>
        [JsonIgnore]
        public HumanFoodStoreType FoodStore { get; set; }
    }
}
