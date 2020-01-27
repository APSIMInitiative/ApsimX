namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Soils.Standardiser;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>The class represents a soil sample.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Sample : Model
    {
        /// <summary>Constructor.</summary>
        public Sample() 
        { 
            this.Name = "Sample"; 
        }

        /// <summary>
        /// An enumeration for specifying soil water units
        /// </summary>
        public enum SWUnitsEnum
        {
            /// <summary>
            /// Volumetric mm/mm
            /// </summary>
            Volumetric,

            /// <summary>
            /// Gravimetric soil water
            /// </summary>
            Gravimetric,

            /// <summary>
            /// mm of water
            /// </summary>
            mm
        }

        /// <summary>
        /// An enumeration for specifying organic carbon units
        /// </summary>
        public enum OCSampleUnitsEnum
        {
            /// <summary>
            /// Organic carbon as total percent
            /// </summary>
            [Description("Total %")]
            Total,

            /// <summary>
            /// Organic carbon as walkley black percent
            /// </summary>
            [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
            [Description("Walkley Black %")]
            WalkleyBlack
        }

        /// <summary>
        /// An enumeration for specifying PH units
        /// </summary>
        public enum PHSampleUnitsEnum
        {
            /// <summary>
            /// PH as water method
            /// </summary>
            [Description("1:5 water")]
            Water,

            /// <summary>
            /// PH as Calcium chloride method
            /// </summary>
            [Description("CaCl2")]
            CaCl2
        }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("cm")]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Thickness</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Description("NO3N")]
        [Summary]
        [Units("kg/ha")]
        public double[] NO3N { get; set; }

        /// <summary>Ammonia NH4</summary>
        [Description("NH4N")]
        [Summary]
        [Units("kg/ha")]
        public double[] NH4N { get; set; }

        /// <summary>
        /// Gets or sets soil water. Units will be as specified by SWUnits
        /// </summary>
        [Description("SW")]
        [Summary]
        [Display(Format = "N3", ShowTotal = true)]
        public double[] SW { get; set; }

        /// <summary>
        /// Gets or sets organic carbon. Units will be as specified by OCUnits
        /// </summary>
        [Summary]
        [Description("OC")]
        [Display(Format = "N2", ShowTotal = true)]
        public double[] OC { get; set; }

        /// <summary>
        /// Gets or sets electrical conductivity (1:5 dS/m)
        /// </summary>
        [Summary]
        [Description("EC")]
        [Units("1:5 dS/m")]
        [Display(Format = "N3", ShowTotal = true)]
        public double[] EC { get; set; }

        /// <summary>
        /// Gets or sets chloride (mg/kg)
        /// </summary>
        [Summary]
        [Description("CL")]
        [Units("mg/kg")]
        [Display(Format = "N3", ShowTotal = true)]
        public double[] CL { get; set; }

        /// <summary>
        /// Gets or sets ESP (%)
        /// </summary>
        [Summary]
        [Description("ESP")]
        [Units("%")]
        [Display(Format = "N3", ShowTotal = true)]
        public double[] ESP { get; set; }

        /// <summary>
        /// Gets or sets PH. Units will be as specified by PHUnits
        /// </summary>
        [Summary]
        [Description("PH")]
        [Display(Format = "N1", ShowTotal = true)]
        public double[] PH { get; set; }

        /// <summary>
        /// Gets or sets the units of SW
        /// </summary>
        public SWUnitsEnum SWUnits { get; set; }

        /// <summary>
        /// Gets or sets the units of organic carbon
        /// </summary>
        public OCSampleUnitsEnum OCUnits { get; set; }

        /// <summary>
        /// Gets or sets the units of P
        /// </summary>
        public PHSampleUnitsEnum PHUnits { get; set; }

        /// <summary>
        /// Gets SW. Units: mm/mm.
        /// </summary>
        [Summary]
        [Display(Format = "N1", ShowTotal = true)]
        public double[] SWmm
        {
            get
            {
                if (this.Soil != null && this.SW != null)
                {
                    if (this.SWUnits == SWUnitsEnum.Volumetric)
                    {
                        return MathUtilities.Multiply(this.SW, this.Thickness);
                    }
                    else if (this.SWUnits == SWUnitsEnum.Gravimetric)
                    {
                        return MathUtilities.Multiply(MathUtilities.Multiply(this.SW, Layers.BDMapped(Soil, this.Thickness)), this.Thickness);
                    }
                    else
                    {
                        return this.SW;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets SW. Units: kg/kg.
        /// </summary>
        public double[] SWGravimetric
        {
            get
            {
                if (this.Soil != null && this.SW != null)
                {
                    if (this.SWUnits == SWUnitsEnum.Volumetric)
                    {
                        return MathUtilities.Divide(this.SW, Layers.BDMapped(Soil, this.Thickness));
                    }
                    else if (this.SWUnits == SWUnitsEnum.Gravimetric)
                    {
                        return this.SW;
                    }
                    else
                    {
                        return MathUtilities.Divide(MathUtilities.Divide(this.SW, Layers.BDMapped(Soil, this.Thickness)), this.Thickness);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets SW. Units: mm/mm.
        /// </summary>
        public double[] SWVolumetric
        {
            get
            {
                if (this.Soil != null && this.SW != null)
                {
                    if (this.SWUnits == SWUnitsEnum.Volumetric)
                    {
                        return this.SW;
                    }
                    else if (this.SWUnits == SWUnitsEnum.Gravimetric)
                    {
                        return MathUtilities.Multiply(this.SW, Layers.BDMapped(Soil, this.Thickness));
                    }
                    else
                    {
                        return MathUtilities.Divide(this.SW, this.Thickness);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets organic carbon. Units: Total %
        /// </summary>
        public double[] OCTotal
        {
            get
            {
                if (this.OCUnits == OCSampleUnitsEnum.WalkleyBlack && OC != null)
                {
                    return MathUtilities.Multiply_Value(this.OC, 1.3);
                }
                else
                {
                    return this.OC;
                }
            }
        }

        /// <summary>
        /// Gets organic carbon. Units: WalkleyBlack %
        /// </summary>
        public double[] OCWalkleyBlack
        {
            get
            {
                if (this.OCUnits == OCSampleUnitsEnum.Total && OC != null)
                {
                    return MathUtilities.Divide_Value(this.OC, 1.3);
                }
                else
                {
                    return this.OC;
                }
            }
        }

        /// <summary>
        /// Gets PH. Units: (1:5 water)
        /// </summary>
        public double[] PHWater
        {
            get
            {
                if (this.PHUnits == PHSampleUnitsEnum.CaCl2 && PH != null)
                {
                    // pH in water = (pH in CaCl X 1.1045) - 0.1375
                    return MathUtilities.Subtract_Value(MathUtilities.Multiply_Value(this.PH, 1.1045), 0.1375);
                }
                else
                {
                    return this.PH;
                }
            }
        }

        /// <summary>
        /// Gets PH. Units: (1:5 water)
        /// </summary>
        public double[] PHCaCl2
        {
            get
            {
                if (this.PHUnits == PHSampleUnitsEnum.Water && PH != null)
                {
                    // pH in CaCl = (pH in water + 0.1375) / 1.1045
                    return MathUtilities.Divide_Value(MathUtilities.AddValue(PH, 0.1375), 1.1045);
                }
                else
                {
                    return this.PH;
                }
            }
        }

        /// <summary>Organic nitrogen. Units: %</summary>
        [Units("%")]
        public double[] ON { get { return MathUtilities.Divide(OC, Soil.SoilCN); } }

        /// <summary>
        /// Gets the soil associated with this sample
        /// </summary>
        private Soil Soil
        {
            get
            {
                return Apsim.Parent(this, typeof(Soil)) as Soil;
            }
        }
    }
}
