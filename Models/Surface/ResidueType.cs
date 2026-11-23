using System;
using Models.Core;

namespace Models.Surface
{
    /// <summary>
    /// Encapsulates a residue type for SurfaceOrganicMatter model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ResidueType: Model
    {
        /// <summary>Gets or sets the fom_type.</summary>
        public string fom_type { get { return Name; } }

        /// <summary>Gets or sets the derived_from.</summary>
        [Description("derived_from")]
        [Display(Type = DisplayType.ResidueName)]
        public string derived_from { get; set; }

        /// <summary>Gets or sets the fraction_ c.</summary>
        [Description("fraction_C")]
        public double fraction_C { get; set; }

        /// <summary>Gets or sets the po4ppm.</summary>
        [Description("po4ppm")]
        public double po4ppm { get; set; }

        /// <summary>Gets or sets the NH4PPM.</summary>
        [Description("nh4ppm")]
        public double nh4ppm { get; set; }

        /// <summary>Gets or sets the no3ppm.</summary>
        [Description("no3ppm")]
        public double no3ppm { get; set; }

        /// <summary>Gets or sets the specific_area.</summary>
        [Description("specific_area")]
        public double specific_area { get; set; }

        /// <summary>Gets or sets the cf_contrib.</summary>
        [Description("cf_contrib")]
        public int cf_contrib { get; set; }

        /// <summary>Gets or sets the pot_decomp_rate.</summary>
        [Description("pot_decomp_rate")]
        public double pot_decomp_rate { get; set; }

        /// <summary>Gets or sets the FR_C.</summary>
        [Description("fr_c")]
        public double[] fr_c { get; set; }

        /// <summary>Gets or sets the FR_N.</summary>
        [Description("fr_n")]
        public double[] fr_n { get; set; }

        /// <summary>Gets or sets the FR_P.</summary>
        [Description("fr_p")]
        public double[] fr_p { get; set; }
    }
}
