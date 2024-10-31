using DocumentFormat.OpenXml.Drawing.Diagrams;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for ruminant SCA07 growth model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all SCA07 growth parameters for the RuminantType")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowSCA07.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]

    public class RuminantParametersGrowSCA07 : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// The maintenance breed factor
        /// </summary>
        [Description("Breed based maintenance factor")]
        public double MaintenanceFactor { get; set; } = 1.4; // cattle. 1.0 sheep

        /// <summary>
        /// Determine whether wool production is included.
        /// </summary>
        [Description("Include wool production")]
        public bool IncludeWool { get; set; } = false;

        /// <summary>
        /// Create a clone of this class
        /// </summary>
        /// <returns>A copy of the class</returns>
        public object Clone()
        {
            RuminantParametersGrowSCA07 clonedParameters = new()
            {
                MaintenanceFactor = this.MaintenanceFactor,
                IncludeWool = this.IncludeWool
            };
            return clonedParameters;
        }
    }
}
