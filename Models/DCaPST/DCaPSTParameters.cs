using Models.Core;

namespace Models.DCAPST
{
    /// <summary>
    /// Encapsulates all parameters passed to DCaPST.
    /// </summary>
    public class DCaPSTParameters
    {
        /// <summary>
        /// PAR energy fraction.
        /// </summary>
        [Description("PAR energy fraction")]
        public double Rpar { get; set; }

        /// <summary>
        /// Partial pressure of O2 in air.
        /// </summary>
        [Description("Partial pressure of O2 in air")]
        [Units("μbar")]
        public double AirO2 { get; set; }

        /// <summary>
        /// Local wind speed
        /// </summary>
        [Description("Local wind speed")]
        [Units("")]
        public double Windspeed { get; set; }

        /// <summary>
        /// Canopy parameters.
        /// </summary>
        [Description("Canopy Parameters")]
        [Display(Type = DisplayType.SubModel)]
        public CanopyParameters Canopy { get; set; }

        /// <summary>
        /// Pathway parameters.
        /// </summary>
        [Description("Pathway Parameters")]
        [Display(Type = DisplayType.SubModel)]
        public PathwayParameters Pathway { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DCaPSTParameters"/> struct.
        /// </summary>
        public DCaPSTParameters()
        {
            Rpar = 0.5;
            AirO2 = 210000;
            Windspeed = 1.5;
            Canopy = new();
            Pathway = new();
        }
    }
}
