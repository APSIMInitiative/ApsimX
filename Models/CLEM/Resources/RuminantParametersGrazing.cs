using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class RuminantParametersGrazing: CLEMModel
    {
        /// <summary>
        /// Maximum green in diet
        /// </summary>
        [Category("Farm", "Grazing")]
        [Description("Maximum green in diet")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.98)]
        public double GreenDietMax { get; set; }

        /// <summary>
        /// Shape of curve for diet vs pasture
        /// </summary>
        [Category("Breed", "Grazing")]
        [Description("Shape of curve for diet vs pasture")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.15)]
        public double GreenDietCoefficient { get; set; }

        /// <summary>
        /// Proportion green in pasture at zero in diet
        /// was %
        /// </summary>
        [Category("Farm", "Diet")]
        [Description("Proportion green in pasture at zero in diet")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.04)]
        public double GreenDietZero { get; set; }

        /// <summary>
        /// Coefficient to adjust intake for herbage biomass
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Coefficient to adjust intake for herbage biomass")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.01)]

        public double IntakeCoefficientBiomass { get; set; }

        /// <summary>
        /// Enforce strict feeding limits
        /// </summary>
        [Category("Farm", "Diet")]
        [Description("Enforce strict feeding limits")]
        [Required]
        [System.ComponentModel.DefaultValue(true)]
        public bool StrictFeedingLimits { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrazing()
        {
            this.SetDefaults();
        }
    }
}
