namespace Models.Soils
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using System.Xml.Serialization;
    using APSIM.Shared.Utilities;
    using System.Linq;

    /// <summary>
    /// Represents the simulation initial water status. There are multiple ways
    /// of specifying the starting water; 1) by a fraction of a full profile, 2) by depth of
    /// wet soil or 3) a single value of plant available water.
    /// </summary>
    [ViewName("UserInterface.Views.InitialWaterView")]
    [PresenterName("UserInterface.Presenters.InitialWaterPresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    [Serializable]
    public class InitialWater : Model
    {
        /// <summary>
        /// Gets the parent soil model.
        /// </summary>
        private Soil Soil
        {
            get
            {
                return Apsim.Parent(this, typeof(Soil)) as Soil;
            }
        }

        /// <summary>
        /// The fraction of a full profile.
        /// </summary>
        private double fractionFull = double.NaN;

        /// <summary>
        /// The depth of wet soil.
        /// </summary>
        private double depthOfWetSoil = double.NaN;

        /// <summary>
        /// An enumeration for soil water distribution used by the percent full
        /// method.
        /// </summary>
        public enum PercentMethodEnum 
        { 
            /// <summary>
            /// Represents filled from the top of the profile
            /// </summary>
            FilledFromTop, 

            /// <summary>
            /// Represents evenly distribution down the profile.
            /// </summary>
            EvenlyDistributed 
        }

        /// <summary>
        /// Gets or sets the distribution method for the percent full method.
        /// </summary>
        public PercentMethodEnum PercentMethod { get; set; }

        /// <summary>
        /// Gets or sets the fraction of a full profile. If NaN is returned then
        /// the depth of wet soil is the specified method.
        /// </summary>
        [Summary]
        [Description("Fraction full")]
        public double FractionFull
        {
            get
            {
                if (Soil == null)
                    return 0;
                else if (double.IsNaN(this.fractionFull))
                {
                    // Get the plant available water (mm/mm)
                    double[] pawc;
                    if (this.RelativeTo == "LL15" || this.RelativeTo == null)
                    {
                        pawc = this.Soil.PAWC;
                    }
                    else
                    {
                        pawc = this.PAWCCrop(this.RelativeTo);
                    }

                    // Convert from mm/mm to mm and sum over the profile.
                    pawc = MathUtilities.Multiply(pawc, this.Soil.Thickness);
                    double totalPAWC = MathUtilities.Sum(pawc);

                    // Convert from total to a fraction.
                    if (totalPAWC > 0)
                    {
                        return MathUtilities.Bound(PAW / totalPAWC, 0, 100);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return this.fractionFull;
                }
            }

            set
            {
                if (!double.IsNaN(value))
                    this.depthOfWetSoil = double.NaN;
                else if (!double.IsNaN(this.fractionFull))
                    this.depthOfWetSoil = TotalSoilDepth() * this.fractionFull;
                this.fractionFull = value;
            }
        }

        /// <summary>
        /// Gets or sets the depth of wet soil (mm). If NaN is returned then
        /// fraction full is the specified method.
        /// </summary>
        [Summary]
        [Description("Depth of wet soil")]
        public double DepthWetSoil
        {
            get
            {
                return this.depthOfWetSoil;
            }

            set
            {
                if (!double.IsNaN(value))
                    this.fractionFull = double.NaN;
                else if (!double.IsNaN(this.depthOfWetSoil))
                    this.fractionFull = this.FractionFull;
                this.depthOfWetSoil = value;
            }
        }

        //[XmlIgnore]
        //[Units("cm")]
        //[Description("Depth")]
        //public string[] Depth
        //{
        //    get
        //    {
        //        return Soil.ToDepthStrings(Soil.Thickness);
        //    }
        //}

        /// <summary>
        /// Gets or sets the plant available water content
        /// </summary>
        [Summary]
        [XmlIgnore]
        [Description("Plant available water")]
        [Units("mm")]
        public double PAW
        {
            get
            {
                // Get the correct lower limits and xf values to use in the calculation of PAW
                double[] ll;
                double[] xf;
                if (this.RelativeTo == "LL15" || this.RelativeTo == null)
                {
                    ll = this.Soil.LL15;
                    xf = null;
                }
                else
                {
                    var soilCrop = Soil.Crop(RelativeTo);
                    ll = soilCrop.LL;
                    xf = soilCrop.XF;
                }

                // Get the soil water values for each layer.
                double[] sw = this.SW(this.Soil.Thickness, ll, this.Soil.DUL, xf);

                // Calculate the plant available water (mm/mm)
                double[] pawVolumetric = MathUtilities.Subtract(sw, ll);

                // Convert from mm/mm to mm and return
                double[] paw = MathUtilities.Multiply(pawVolumetric, this.Soil.Thickness);
                return MathUtilities.Sum(paw);
            }

            set
            {
                // Get the plant available water (mm/mm)
                double[] pawc;
                if (this.RelativeTo == "LL15" || this.RelativeTo == null)
                {
                    pawc = this.Soil.PAWC;
                }
                else
                {
                    pawc = this.PAWCCrop(this.RelativeTo);
                }

                // Convert from mm/mm to mm and sum over the profile.
                pawc = MathUtilities.Multiply(pawc, this.Soil.Thickness);
                double totalPAWC = MathUtilities.Sum(pawc);

                // Convert from total to a fraction.
                if (totalPAWC > 0)
                {
                    this.fractionFull = MathUtilities.Bound(value / totalPAWC, 0, 100);
                }
                else
                {
                    this.fractionFull = 0;
                }
            }
        }

        /// <summary>
        /// Return the plant available water CAPACITY. Units: mm/mm
        /// </summary>
        public double[] PAWCCrop(string CropName)
        {
            var soilCrop = Soil.Crop(CropName);
            if (soilCrop != null)
                return Soil.CalcPAWC(Soil.Thickness,
                                     soilCrop.LL,
                                     Soil.DUL,
                                     soilCrop.XF);
            else
                return new double[0];
        }

        /// <summary>
        /// Gets or sets the crop that starting plant available water is relative to.
        /// </summary>
        [Summary]
        [Description("Relative to")]
        public string RelativeTo { get; set; }

        /// <summary>
        /// Gets the crop names that are permissible in the 'RelativeTo' property.
        /// </summary>
        public string[] RelativeToCrops
        {
            get
            {
                List<string> crops = new List<string>();
                crops.Add("LL15");
                crops.AddRange(this.Soil.Crops.Select(crop => crop.Name));
                return crops.ToArray();
            }
        }

        /// <summary>
        /// Calculate a layered soil water. Units: mm/mm
        /// </summary>
        /// <param name="thickness">Thickness of each layer</param>
        /// <param name="ll">Lower limit</param>
        /// <param name="dul">Drained upper limit</param>
        /// <param name="xf">Exploratory factor</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        internal double[] SW(double[] thickness, double[] ll, double[] dul, double[] xf)
        {
            if (double.IsNaN(this.DepthWetSoil))
            {
                if (this.PercentMethod == InitialWater.PercentMethodEnum.FilledFromTop)
                {
                    return this.SWFilledFromTop(thickness, ll, dul, xf);
                }
                else
                {
                    return this.SWEvenlyDistributed(ll, dul);
                }
            }
            else
            {
                return this.SWDepthWetSoil(thickness, ll, dul);
            }
        }

        /// <summary>
        /// Calculate a layered soil water using a FractionFull and filled from the top
        /// </summary>
        /// <param name="thickness">Thickness of each layer</param>
        /// <param name="ll">Lower limit</param>
        /// <param name="dul">Drained upper limit</param>
        /// <param name="xf">Exploratory factor</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        private double[] SWFilledFromTop(double[] thickness, double[] ll, double[] dul, double[] xf)
        {
            double[] sw = new double[thickness.Length];
            if (thickness.Length != ll.Length ||
                thickness.Length != dul.Length)
            {
                return sw;
            }

            double[] pawcmm = MathUtilities.Multiply(MathUtilities.Subtract(dul, ll), thickness);

            double amountWater = MathUtilities.Sum(pawcmm) * this.FractionFull;
            for (int layer = 0; layer < ll.Length; layer++)
            {
                if (amountWater >= 0 && xf != null && xf[layer] == 0)
                {
                    sw[layer] = ll[layer];
                }
                else if (amountWater >= pawcmm[layer])
                {
                    sw[layer] = dul[layer];
                    amountWater = amountWater - pawcmm[layer];
                }
                else
                {
                    double prop = amountWater / pawcmm[layer];
                    sw[layer] = (prop * (dul[layer] - ll[layer])) + ll[layer];
                    amountWater = 0;
                }
            }

            return sw;
        }

        /// <summary>
        /// Calculate a layered soil water using a FractionFull and evenly distributed. Units: mm/mm
        /// </summary>
        /// <param name="ll">Lower limit</param>
        /// <param name="dul">Drained upper limit</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        private double[] SWEvenlyDistributed(double[] ll, double[] dul)
        {
            double[] sw = new double[ll.Length];
            for (int layer = 0; layer < ll.Length; layer++)
            {
                sw[layer] = (this.FractionFull * (dul[layer] - ll[layer])) + ll[layer];
            }

            return sw;
        }

        /// <summary>
        /// Calculate a layered soil water using a depth of wet soil. Units: mm/mm
        /// </summary>
        /// <param name="thickness">Thickness of each layer</param>
        /// <param name="ll">Lower limit</param>
        /// <param name="dul">Drained upper limit</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        private double[] SWDepthWetSoil(double[] thickness, double[] ll, double[] dul)
        {
            double[] sw = new double[ll.Length];
            double depthSoFar = 0;
            for (int layer = 0; layer < thickness.Length; layer++)
            {
                if (this.DepthWetSoil > depthSoFar + thickness[layer])
                {
                    sw[layer] = dul[layer];
                }
                else
                {
                    double prop = Math.Max(this.DepthWetSoil - depthSoFar, 0) / thickness[layer];
                    sw[layer] = (prop * (dul[layer] - ll[layer])) + ll[layer];
                }

                depthSoFar += thickness[layer];
            }

            return sw;
        }

        /// <summary>
        /// Returns the total depth of the soil, in mm
        /// </summary>
        /// <returns>Total soil depth</returns>
        public double TotalSoilDepth()
        {
            return MathUtilities.Sum(Soil.Thickness);
        }
    }
}