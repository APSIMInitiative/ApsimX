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
    /// This stores the parameters for ruminant Oddy growth model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all Oddy growth parameters for the RuminantType")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowOddy.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]

    public class RuminantParametersGrowOddy : CLEMModel, ISubParameters, ICloneable
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

        // Parameters
        /// <summary>
        /// 
        /// </summary>
        public double pm { get; set; } = 0.226; //0.209 #0.226 
        /// <summary>
        /// 
        /// </summary>
        public double e0 { get; set; } = 0.249; //0.253 #0.2486 
        /// <summary>
        /// 
        /// </summary>
        public double cs1 { get; set; } = 0.676; //mei
        /// <summary>
        /// 
        /// </summary>
        public double cs2 { get; set; } = 2.061; //m41
        /// <summary>
        /// 
        /// </summary>
        public double cs3 { get; set; } = 0.53; //md
        /// <summary>
        /// 
        /// </summary>
        public double bm { get; set; } = 0.0190; //*0.9
        /// <summary>
        /// 
        /// </summary>
        public double bv { get; set; } = 0.185; //*0.9
        /// <summary>
        /// 
        /// </summary>
        public double pv { get; set; } = 0.05; //0.093 #0.0625 #0.055
        /// <summary>
        /// 
        /// </summary>
        public double km { get; set; } = 0.7;
        /// <summary>
        /// 
        /// </summary>
        public double kp { get; set; } = 0.4; //0.36 #also kw
        /// <summary>
        /// 
        /// </summary>
        public double kf { get; set; } = 0.7; //0.72
        /// <summary>
        /// 
        /// </summary>
        public double lf { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public double lp { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public double kl { get; set; } = 0.5;
        /// <summary>
        /// 
        /// </summary>
        public double kc { get; set; } = 0.133;

        /// <summary>
        /// Create a clone of this class
        /// </summary>
        /// <returns>A copy of the class</returns>
        public object Clone()
        {
            RuminantParametersGeneral clonedParameters = new()
            {
            };
            return clonedParameters;
        }
    }
}
