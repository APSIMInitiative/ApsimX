namespace APSIM.Shared.APSoil
{
    using System;

    /// <summary>A specification of soil water model constants and parameters.</summary>
    [Serializable]
    public class SoilWater
    {
        /// <summary>Gets or sets the summer cona.</summary>
        public double SummerCona { get; set; }

        /// <summary>Gets or sets the summer u.</summary>
        public double SummerU { get; set; }

        /// <summary>Gets or sets the summer date.</summary>
        public string SummerDate { get; set; }

        /// <summary>Gets or sets the winter cona.</summary>
        public double WinterCona { get; set; }

        /// <summary>Gets or sets the winter u.</summary>
        public double WinterU { get; set; }

        /// <summary>Gets or sets the winter date.</summary>
        public string WinterDate { get; set; }

        /// <summary>Gets or sets the diffus constant.</summary>
        public double DiffusConst { get; set; }

        /// <summary>Gets or sets the diffus slope.</summary>
        public double DiffusSlope { get; set; }

        /// <summary>Gets or sets the salb.</summary>
        public double Salb { get; set; }

        /// <summary>Gets or sets the c n2 bare.</summary>
        public double CN2Bare { get; set; }

        /// <summary>Gets or sets the cn red.</summary>
        public double CNRed { get; set; }

        /// <summary>Gets or sets the cn cov.</summary>
        public double CNCov { get; set; }

        /// <summary>Gets or sets the slope.</summary>
        public double Slope { get; set; }

        /// <summary>Gets or sets the width of the discharge.</summary>
        public double DischargeWidth { get; set; }

        /// <summary>Gets or sets the catchment area.</summary>
        public double CatchmentArea { get; set; }

        /// <summary>Gets or sets the maximum pond.</summary>
        public double MaxPond { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        public double[] Thickness { get; set; }

        /// <summary>Gets or sets the swcon.</summary>
        public double[] SWCON { get; set; }

        /// <summary>Gets or sets the mwcon.</summary>
        public double[] MWCON { get; set; }

        /// <summary>Gets or sets the klat.</summary>
        public double[] KLAT { get; set; }
    }
}
