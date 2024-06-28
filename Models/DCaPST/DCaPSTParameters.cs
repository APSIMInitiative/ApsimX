using System;
using Models.Core;

namespace Models.DCAPST
{
    /// <summary>
    /// Encapsulates all parameters passed to DCaPST.
    /// </summary>
    [Serializable]
    public class DCaPSTParameters
    {
        /// <summary>
        /// PAR energy fraction
        /// </summary>
        [Description("PAR energy fraction")]
        public double Rpar { get; set; }

        /// <summary>
        /// Canopy parameters.
        /// </summary>
        [Description("Canopy Parameters")]
        [Display(Type = DisplayType.SubModel)]
        public CanopyParameters Canopy { get; set; } = new CanopyParameters();

        /// <summary>
        /// Pathway parameters.
        /// </summary>
        [Description("Pathway Parameters")]
        [Display(Type = DisplayType.SubModel)]
        public PathwayParameters Pathway { get; set; } = new PathwayParameters();
}
}