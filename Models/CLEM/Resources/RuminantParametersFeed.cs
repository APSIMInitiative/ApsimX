using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters relating to RuminantActivityGrowSCA for a ruminant Type
    /// All default values are provided for cattle and Bos indicus breeds where values apply.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This model provides all parameters specific to RuminantActivityGrowth (SCA Version)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantActivityGrowSCA.htm")]
    public class RuminantParametersFeed: CLEMModel
    {
        /// <summary>
        /// Potential intake modifier for maximum intake possible when overfeeding
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Potential intake modifer for max overfeeding intake")]
        [Required, GreaterThanEqualValue(1)]
        [System.ComponentModel.DefaultValue(1)]
        public double OverfeedPotentialIntakeModifier { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersFeed()
        {
            this.SetDefaults();
        }
    }
}
