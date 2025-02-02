using System;
using Models.Core;

namespace Models
{
    /// <summary>A class for holding a fertiliser type.</summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Fertiliser))]
    public class FertiliserType : Model
    {
        ///// <summary>The name of the fertiliser type.</summary>
        //public string Name { get; set; }

        /// <summary>A description of the fertiliser type.</summary>
        [Description("Description")]
        public string Description { get; set; }

        /// <summary>The fraction of no3.</summary>
        [Description("Fraction of NO3")]
        public double FractionNO3 { get; set; }

        /// <summary>The fraction of nh4.</summary>
        [Description("Fraction of NH4")]
        public double FractionNH4 { get; set; }

        /// <summary>The fraction of urea.</summary>
        [Description("Fraction of Urea")]
        public double FractionUrea { get; set; }

        /// <summary>The fraction of rock p.</summary>
        [Description("Fraction of rock P")]
        public double FractionRockP { get; set; }

        /// <summary>The fraction of banded p.</summary>
        [Description("Fraction of banded P")]
        public double FractionBandedP { get; set; }

        /// <summary>The fraction of labile p.</summary>
        [Description("Fraction of labile P")]
        public double FractionLabileP { get; set; }

        /// <summary>The fraction of ca.</summary>
        [Description("Fraction of calcium (Ca)")]
        public double FractionCa { get; set; }
    }
}