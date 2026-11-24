using System;
using APSIM.Core;
using Models.Core;

namespace Models.Surface
{
    /// <summary>
    /// Encapsulates a residue type for SurfaceOrganicMatter model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResidueTypes))]
    public class ResidueType: Model, IOutOfScope
    {
        /// <summary>Gets or sets the fom_type.</summary>
        public string FomType { get { return Name; } }

        /// <summary>Gets or sets the derived_from.</summary>
        [Description("DerivedFrom")]
        [Display(Type = DisplayType.ResidueName)]
        public string DerivedFrom { get; set; }

        /// <summary>Gets or sets the fraction_ c.</summary>
        [Description("FractionC")]
        public double FractionC { get; set; }

        /// <summary>Gets or sets the po4ppm.</summary>
        [Description("PO4ppm")]
        public double PO4ppm { get; set; }

        /// <summary>Gets or sets the NH4PPM.</summary>
        [Description("NH4ppm")]
        public double NH4ppm { get; set; }

        /// <summary>Gets or sets the no3ppm.</summary>
        [Description("NO3ppm")]
        public double NO3ppm { get; set; }

        /// <summary>Gets or sets the specific_area.</summary>
        [Description("SpecificArea")]
        public double SpecificArea { get; set; }

        /// <summary>Gets or sets the cf_contrib.</summary>
        [Description("CfContrib")]
        public int CfContrib { get; set; }

        /// <summary>Gets or sets the pot_decomp_rate.</summary>
        [Description("PotDecompRate")]
        public double PotDecompRate { get; set; }

        /// <summary>Gets or sets the FR_C.</summary>
        [Description("FR_C")]
        public double[] FR_C { get; set; }

        /// <summary>Gets or sets the FR_N.</summary>
        [Description("FR_N")]
        public double[] FR_N { get; set; }

        /// <summary>Gets or sets the FR_P.</summary>
        [Description("FR_P")]
        public double[] FR_P { get; set; }
    }
}
