using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
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
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all parameters specific to RuminantActivityGrow24")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersLactation.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersLactation : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Number of days for milking
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Number of days for milking")]
        [Required, GreaterThanEqualValue(0)]
        public double MilkingDays { get; set; } = 300;

        /// <summary>
        /// Peak milk yield(kg/day)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Peak milk yield (kg/day)")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakYield { get; set; } = 4.0;

        /// <summary>
        /// Milk curve shape suckling
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape suckling (CL3)")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveSuckling { get; set; } = 0.6;
        
        /// <summary>
        /// Milk curve shape non suckling
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape non suckling (CL4)")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveNonSuckling { get; set; } = 0.11;
        
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
        [Category("Farm", "Lactation")]
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
                MilkCurveSuckling = MilkCurveSuckling,
                MilkCurveNonSuckling = MilkCurveNonSuckling,
                MilkOffsetDay = MilkOffsetDay,
                MilkPeakDay = MilkPeakDay
            };
            return clonedParameters;
        }
    }
}
