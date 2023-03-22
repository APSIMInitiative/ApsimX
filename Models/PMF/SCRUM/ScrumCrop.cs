namespace Models.PMF
{
    using Models.Core;
    using System;

    /// <summary>
    /// Data structure that contains information for a specific crop type in Scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Referee))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumCrop
    {
        /// <summary>Harvest Index</summary>
        [Description("Harvest Index")]
        public double HarvestIndex { get; set; }

        /// <summary>Moisture percentage of product</summary>
        [Description("Moisture percentage of product")]
        public double MoisturePc { get; set; }

        /// <summary>Root biomass proportion</summary>
        [Description("Root Biomass proportion")]
        public double Proot { get; set; }

        /// <summary>Root depth at harvest</summary>
        [Description("Root depth at harvest")]
        public double MaxRD { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Maximum green cover")]
        public double Acover { get; set; }

        /// <summary>Cover rate </summary>
        [Description("Cover rate")]
        public double rCover { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Description("Root Nitrogen concentration")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Stover Nitrogen concentration")]
        public double StoverNConc { get; set; }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Product Nitrogen concentration")]
        public double ProductNConc { get; set; }
        
        /// <summary>Is the crop a legume</summary>
        [Description("Is the crop a legume")]
        public bool Legume { get; set; }

        /// <summary>Base temperature for crop</summary>
        [Description("Base temperature for crop")]
        public double BaseT { get; set; }
        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for crop")]
        public double OptT { get; set; }
        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for crop")]
        public double MaxT { get; set; }
    }

}
