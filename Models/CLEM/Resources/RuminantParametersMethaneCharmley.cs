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
    /// This stores the general parameters for enteric methane based on Charmley et al. estimation.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all general parameters for the Enteric Methane - Charmely")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersMethaneCharmley.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersMethaneCharmley: CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Methane production from intake coefficient
        /// </summary>
        [Category("Farm", "Products")]
        [Description("Methane production from intake coefficient")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(20.7)]
        public double MethaneProductionCoefficient { get; set; } = 20.7;

        /// <summary>
        /// Create clone of this class
        /// </summary>
        /// <returns>A new RuminantParametersMethaneCharmley</returns>
        public object Clone()
        {
            return new RuminantParametersMethaneCharmley()
            { 
                MethaneProductionCoefficient = MethaneProductionCoefficient
            };
        }
    }
}
