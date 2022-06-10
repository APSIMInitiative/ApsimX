namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>A model for capturing physical soil parameters</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.NewGridView.glade")]
    [PresenterName("UserInterface.Presenters.NewGridPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Physical : Model, IPhysical, ITabularData
    {
        // Initial soil water when set by user as a layered variable (as opposed to an InitialWater node)
        private double[] sw;

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("mm")]
        [Summary]
        [JsonIgnore]
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

        /// <summary>Gets the depth mid points (mm).</summary>
        [Units("mm")]
        public double[] DepthMidPoints
        {
            get
            {
                var cumulativeThickness = MathUtilities.Cumulative(Thickness).ToArray();
                var midPoints = new double[cumulativeThickness.Length];
                for (int layer = 0; layer != cumulativeThickness.Length; layer++)
                {
                    if (layer == 0)
                        midPoints[layer] = cumulativeThickness[layer] / 2.0;
                    else
                        midPoints[layer] = (cumulativeThickness[layer] + cumulativeThickness[layer - 1]) / 2.0;
                }
                return midPoints;
            }
        }

        /// <summary>Return the soil layer cumulative thicknesses (mm)</summary>
        public double[] ThicknessCumulative { get { return MathUtilities.Cumulative(Thickness).ToArray(); } }

        /// <summary>The soil thickness (mm).</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Particle size clay.</summary>
        [Summary]
        [Description("Clay")]
        [Units("%")]
        public double[] ParticleSizeClay { get; set; }

        /// <summary>Particle size sand.</summary>
        [Summary]
        [Description("Sand")]
        [Units("%")]
        public double[] ParticleSizeSand { get; set; }

        /// <summary>Particle size silt.</summary>
        [Summary]
        [Description("Silt")]
        [Units("%")]
        public double[] ParticleSizeSilt { get; set; }

        /// <summary>Rocks.</summary>
        [Summary]
        [Description("Rocks")]
        [Units("%")]
        public double[] Rocks { get; set; }

        /// <summary>Texture.</summary>
        public string[] Texture { get; set; }

        /// <summary>Bulk density (g/cc).</summary>
        [Summary]
        [Description("BD")]
        [Units("g/cc")]
        [Display(Format = "N2")]
        public double[] BD { get; set; }

        /// <summary>Air dry - volumetric (mm/mm).</summary>
        [Summary]
        [Description("Air dry")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] AirDry { get; set; }

        /// <summary>Lower limit 15 bar (mm/mm).</summary>
        [Summary]
        [Description("LL15")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] LL15 { get; set; }

        /// <summary>Return lower limit limit at standard thickness. Units: mm</summary>
        [Units("mm")]
        public double[] LL15mm { get { return MathUtilities.Multiply(LL15, Thickness); } }

        /// <summary>Drained upper limit (mm/mm).</summary>
        [Summary]
        [Description("DUL")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] DUL { get; set; }

        /// <summary>Drained upper limit (mm).</summary>
        [Units("mm")]
        public double[] DULmm { get { return MathUtilities.Multiply(DUL, Thickness); } }

        /// <summary>Saturation (mm/mm).</summary>
        [Summary]
        [Description("SAT")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] SAT { get; set; }

        /// <summary>Saturation (mm).</summary>
        [Units("mm")]
        public double[] SATmm { get { return MathUtilities.Multiply(SAT, Thickness); } }

        /// <summary>Initial soil water (mm/mm).</summary>
        [Summary]
        [Description("SW")]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] SW
        {
            get
            {
                var initWater = FindChild<InitialWater>();
                if (initWater == null)
                    return sw;
                else
                    return initWater.SW;
            }
            set
            {
                sw = value;
            }
        }

        /// <summary>Initial soil water (mm).</summary>
        [Units("mm")]
        public double[] SWmm { get { return MathUtilities.Multiply(SW, Thickness); } }

        /// <summary>KS (mm/day).</summary>
        [Summary]
        [Description("KS")]
        [Units("mm/day")]
        [Display(Format = "N1")]
        public double[] KS { get; set; }

        /// <summary>Plant available water CAPACITY (DUL-LL15).</summary>
        [Units("mm/mm")]
        public double[] PAWC { get { return APSoilUtilities.CalcPAWC(Thickness, LL15, DUL, null); } }

        /// <summary>Plant available water CAPACITY (DUL-LL15).</summary>
        [Units("mm")]
        [Display(Format = "N0", ShowTotal = true)]
        public double[] PAWCmm { get { return MathUtilities.Multiply(PAWC, Thickness); } }

        /// <summary>Gets or sets the bd metadata.</summary>
        public string[] BDMetadata { get; set; }

        /// <summary>Gets or sets the air dry metadata.</summary>
        public string[] AirDryMetadata { get; set; }

        /// <summary>Gets or sets the l L15 metadata.</summary>
        public string[] LL15Metadata { get; set; }

        /// <summary>Gets or sets the dul metadata.</summary>
        public string[] DULMetadata { get; set; }

        /// <summary>Gets or sets the sat metadata.</summary>
        public string[] SATMetadata { get; set; }

        /// <summary>Gets or sets the ks metadata.</summary>
        public string[] KSMetadata { get; set; }

        /// <summary>Gets or sets the rocks metadata.</summary>
        public string[] RocksMetadata { get; set; }

        /// <summary>Gets or sets the texture metadata.</summary>
        public string[] TextureMetadata { get; set; }

        /// <summary>Particle size sand metadata.</summary>
        public string[] ParticleSizeSandMetadata { get; set; }

        /// <summary>Particle size silt metadata.</summary>
        public string[] ParticleSizeSiltMetadata { get; set; }

        /// <summary>Particle size clay metadata.</summary>
        public string[] ParticleSizeClayMetadata { get; set; }

        /// <summary>Tabular data. Called by GUI.</summary>
        public TabularData GetTabularData()
        {
            var columns = new List<TabularData.Column>();

            columns.Add(new TabularData.Column("Depth", new VariableProperty(this, GetType().GetProperty("Depth"))));
            columns.Add(new TabularData.Column("Sand", new VariableProperty(this, GetType().GetProperty("ParticleSizeSand"))));
            columns.Add(new TabularData.Column("Silt", new VariableProperty(this, GetType().GetProperty("ParticleSizeSilt"))));
            columns.Add(new TabularData.Column("Clay", new VariableProperty(this, GetType().GetProperty("ParticleSizeClay"))));
            columns.Add(new TabularData.Column("Rocks", new VariableProperty(this, GetType().GetProperty("Rocks"))));
            columns.Add(new TabularData.Column("BD", new VariableProperty(this, GetType().GetProperty("BD"))));
            columns.Add(new TabularData.Column("AirDry", new VariableProperty(this, GetType().GetProperty("AirDry"))));
            columns.Add(new TabularData.Column("LL15", new VariableProperty(this, GetType().GetProperty("LL15"))));
            columns.Add(new TabularData.Column("DUL", new VariableProperty(this, GetType().GetProperty("DUL"))));
            columns.Add(new TabularData.Column("SAT", new VariableProperty(this, GetType().GetProperty("SAT"))));
            columns.Add(new TabularData.Column("SW", new VariableProperty(this, GetType().GetProperty("SW")), readOnly: FindChild<InitialWater>() != null));
            columns.Add(new TabularData.Column("KS", new VariableProperty(this, GetType().GetProperty("KS"))));

            foreach (var soilCrop in FindAllChildren<SoilCrop>())
            {
                var cropName = soilCrop.Name.Replace("Soil", "");
                columns.Add(new TabularData.Column($"{cropName} LL", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("LL"))));
                columns.Add(new TabularData.Column($"{cropName} KL", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("KL"))));
                columns.Add(new TabularData.Column($"{cropName} XF", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("XF"))));
                columns.Add(new TabularData.Column($"{cropName} PAWC", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("PAWC")), units: $"{PAWCmm.Sum():F1} mm"));
            }

            return new TabularData(Name, columns);
        }
    }
}
