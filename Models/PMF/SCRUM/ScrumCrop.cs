using Models.Core;
using System;
namespace Models.PMF.Scrum
{
    /// <summary>
    /// Data structure that contains information for a specific crop type in Scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Referee))]
    [ValidParent(ParentType = typeof(Plant))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumCrop: Model
    {
        /// <summary>Harvest Index</summary>
        [Description("Harvest Index (0-1)")]
        public double HarvestIndex { get; set; }

        /// <summary>Moisture percentage of product</summary>
        [Description("Moisture percentage of product (%)")]
        public double MoisturePc { get; set; }

        /// <summary>Root biomass proportion</summary>
        [Description("Root Biomass proportion (0-1)")]
        public double Proot { get; set; }

        /// <summary>Root depth at harvest (mm)</summary>
        [Description("Root depth at harvest (mm)")]
        public double MaxRD { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Maximum green cover (0-0.97)")]
        public double Acover { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Extinction coefficient (0-1)")]
        public double ExtinctCoeff { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Description("Root Nitrogen concentration (g/g)")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Stover Nitrogen concentration (g/g)")]
        public double StoverNConc { get; set; }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Product Nitrogen concentration (g/g)")]
        public double ProductNConc { get; set; }
        
        /// <summary>Is the crop a legume</summary>
        [Description("Is the crop a legume")]
        public bool Legume { get; set; }

        /// <summary>Base temperature for crop</summary>
        [Description("Base temperature for crop (oC)")]
        public double BaseT { get; set; }
        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for crop (oC)")]
        public double OptT { get; set; }
        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for crop (oC)")]
        public double MaxT { get; set; }
    }
}
