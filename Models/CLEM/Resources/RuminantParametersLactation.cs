using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters relating to RuminantActivityGrowSCA for a ruminant Type
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("General ruminant lactation parameters")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersLactation.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersLactation : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Number of days for milking
        /// </summary>
        [Category("Farm:Summary", "Lactation")]
        [Description("Number of days for milking")]
        [Required, GreaterThanEqualValue(0)]
        public double MilkingDays { get; set; } = 300;

        /// <summary>
        /// Peak milk yield(kg/day)
        /// </summary>
        [Category("Farm:Summary", "Lactation")]
        [Description("Peak milk yield (kg/day)")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakYield { get; set; } = 4.0;

        /// <summary>
        /// Milk curve shape suckling
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape suckling (CL3)")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveSuckling_CL3 { get; set; } = 0.6;
        
        /// <summary>
        /// Milk curve shape non suckling
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape non suckling (CL4)")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveNonSuckling_CL4 { get; set; } = 0.11;
        
        /// <summary>
        /// Milk offset day
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk offset day (CL1)")]
        [Required, GreaterThanValue(0)]
        public double MilkOffsetDay { get; set; } = 4;
        
        /// <summary>
        /// Milk peak day
        /// </summary>
        [Category("Farm:Summary", "Lactation")]
        [Description("Milk peak day (CL2)")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakDay { get; set; } = 45;

        /// <summary>
        /// Create copy of the class
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RuminantParametersLactation clonedParameters = new()
            {
                MilkingDays = MilkingDays,
                MilkPeakYield = MilkPeakYield,
                MilkCurveSuckling_CL3 = MilkCurveSuckling_CL3,
                MilkCurveNonSuckling_CL4 = MilkCurveNonSuckling_CL4,
                MilkOffsetDay = MilkOffsetDay,
                MilkPeakDay = MilkPeakDay
            };
            return clonedParameters;
        }
    }
}
