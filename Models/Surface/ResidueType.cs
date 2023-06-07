using System;

namespace Models.Surface
{

    /// <summary>
    /// Encapsulates a residue type for SurfaceOrganicMatter model
    /// </summary>
    [Serializable]
    public class ResidueType
    {
        /// <summary>Gets or sets the fom_type.</summary>
        public string fom_type { get; set; }

        /// <summary>Gets or sets the derived_from.</summary>
        public string derived_from { get; set; }

        /// <summary>Gets or sets the fraction_ c.</summary>
        public double fraction_C { get; set; }

        /// <summary>Gets or sets the po4ppm.</summary>
        public double po4ppm { get; set; }

        /// <summary>Gets or sets the NH4PPM.</summary>
        public double nh4ppm { get; set; }

        /// <summary>Gets or sets the no3ppm.</summary>
        public double no3ppm { get; set; }

        /// <summary>Gets or sets the specific_area.</summary>
        public double specific_area { get; set; }

        /// <summary>Gets or sets the cf_contrib.</summary>
        public int cf_contrib { get; set; }

        /// <summary>Gets or sets the pot_decomp_rate.</summary>
        public double pot_decomp_rate { get; set; }

        /// <summary>Gets or sets the FR_C.</summary>
        public double[] fr_c { get; set; }

        /// <summary>Gets or sets the FR_N.</summary>
        public double[] fr_n { get; set; }

        /// <summary>Gets or sets the FR_P.</summary>
        public double[] fr_p { get; set; }
    }
}
