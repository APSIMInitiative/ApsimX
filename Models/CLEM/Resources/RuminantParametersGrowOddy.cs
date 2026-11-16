using Models.CLEM.Extras;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for ruminant Oddy growth model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("Ruminant growth parameters specific to Oddy approach")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowOddy.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]

    public class RuminantParametersGrowOddy : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// The maintenance breed factor
        /// </summary>
        [Category("Core", "General")]
        [Description("Breed based maintenance factor")]
        public double MaintenanceFactor { get; set; } = 1.4; // cattle. 1.0 sheep

        // Parameters
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("pm")]
        [Required, GreaterThanValue(0)]
        public double Pm { get; set; } = 0.226; //0.209 #0.226 
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("e0")]
        [Required, GreaterThanValue(0)]
        public double E0 { get; set; } = 0.249; //0.253 #0.2486 
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("cs")]
        [Required, GreaterThanValue(0)]
        public double Cs1 { get; set; } = 0.676; //mei
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("cs2")]
        [Required, GreaterThanValue(0)]
        public double Cs2 { get; set; } = 2.061; //m41
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("cs3")]
        [Required, GreaterThanValue(0)]
        public double Cs3 { get; set; } = 0.53; //md
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("bm")]
        [Required, GreaterThanValue(0)]
        public double Bm { get; set; } = 0.0190; //*0.9
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("bv")]
        [Required, GreaterThanValue(0)]
        public double Bv { get; set; } = 0.185; //*0.9
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("pv")]
        [Required, GreaterThanValue(0)]
        public double Pv { get; set; } = 0.05; //0.093 #0.0625 #0.055
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("km")]
        [Required, GreaterThanValue(0)]
        public double Km { get; set; } = 0.7;
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("kp")]
        [Required, GreaterThanValue(0)]
        public double Kp { get; set; } = 0.4; //0.36 #also kw
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("kf")]
        [Required, GreaterThanValue(0)]
        public double Kf { get; set; } = 0.7; //0.72
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("lf")]
        [Required, GreaterThanValue(0)]
        public double Lf { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("lp")]
        [Required, GreaterThanValue(0)]
        public double Lp { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("kl")]
        [Required, GreaterThanValue(0)]
        public double Kl { get; set; } = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [Category("Core", "General")]
        [Description("kc")]
        [Required, GreaterThanValue(0)]
        public double Kc { get; set; } = 0.133;

        /// <summary>
        /// Oddy pPrpM
        /// </summary>
        [Description("Protein content fat-free muscle (Oddy pPrpM)")]
        [Category("Breed", "General")]
        [Required, GreaterThanValue(0), Proportion]
        public double PPrpM { get; set; } = 0.21;

        /// <summary>
        /// Oddy pPrpV
        /// </summary>
        [Description("Protein content fat-free viscera (Oddy pPrpV)")]
        [Category("Breed", "General")]
        [Required, GreaterThanValue(0), Proportion]
        public double PPrpV { get; set; } = 0.157;

        /// <summary>
        /// Oddy leanM
        /// </summary>
        [Description("Propotion empty body protein at maturity (Oddy leanM)")]
        [Category("Breed", "General")]
        [Required, GreaterThanValue(0), Proportion]
        public double LeanM { get; set; } = 0.75; //0.70

        /// <summary>
        /// Oddy shrink
        /// </summary>
        [Description("Ratio of EMW to LiveWeight at maturity (Oddy shrink)")]
        [Category("Breed", "General")]
        [Required, GreaterThanValue(0), Proportion]
        public double Shrink { get; set; } = 0.86;

        /// <summary>
        /// Oddy pMusc
        /// </summary>
        [Description("Proportion of mature body protein that is muscle (Oddy pMusc)")]
        [Category("Breed", "General")]
        [Required, GreaterThanValue(0), Proportion]
        public double PMusc { get; set; } = 0.85;

        /// <summary>
        /// Oddy pMusc at birth
        /// </summary>
        [Description("Proportion of body protein that is muscle at birth")]
        [Category("Breed", "General")]
        [Required, GreaterThanValue(0), Proportion]
        public double PMuscBirth { get; set; } = 0.85;

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
