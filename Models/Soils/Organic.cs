namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using Newtonsoft.Json;
    using Models.Interfaces;

    /// <summary>A model for capturing soil organic parameters</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Organic : Model, ITabularData
    {
        /// <summary>
        /// An enumeration for specifying organic carbon units
        /// </summary>
        public enum CarbonUnitsEnum
        {
            /// <summary>Organic carbon as total percent.</summary>
            [Description("Total %")]
            Total,

            /// <summary>Organic carbon as walkley black percent.</summary>
            [Description("Walkley Black %")]
            WalkleyBlack
        }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Units("cm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStringsCM(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThicknessCM(value);
            }
        }

        /// <summary>Root C:N Ratio</summary>
        [Summary]
        [Description("FOM C:N ratio (0-500)")]
        [Bounds(Lower = 0.0, Upper = 500.0)]
        public double FOMCNRatio { get; set; }

        /// <summary>Soil layer thickness for each layer (mm)</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Carbon concentration</summary>
        [Summary]
        [Bounds(Lower = 0.1, Upper = 10.0)]
        [Display(Format = "N2")]
        public double[] Carbon { get; set; }

        /// <summary>The units of organic carbon.</summary>
        public CarbonUnitsEnum CarbonUnits { get; set; }

        /// <summary>Carbon:nitrogen ratio.</summary>
        [Summary]
        [Units("g/g")]
        [Bounds(Lower = 5.0, Upper = 30.0)]
        public double[] SoilCNRatio { get; set; }

        /// <summary>F biom.</summary>
        [Summary]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] FBiom { get; set; }

        /// <summary>F inert.</summary>
        [Summary]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double[] FInert { get; set; }

        /// <summary>Fresh organic matter</summary>
        [Summary]
        [Units("kg/ha")]
        [Display(Format = "N1")]
        public double[] FOM { get; set; }

        /// <summary>Carbon metadata</summary>
        public string[] CarbonMetadata { get; set; }

        /// <summary>FOM metadata</summary>
        public string[] FOMMetadata { get; set; }

        /// <summary>Tabular data. Called by GUI.</summary>
        public TabularData GetTabularData()
        {
            return new TabularData(Name, new TabularData.Column[]
            {
                new TabularData.Column("Depth", new VariableProperty(this, GetType().GetProperty("Depth"))),
                new TabularData.Column("Carbon", new VariableProperty(this, GetType().GetProperty("Carbon"))),
                new TabularData.Column("SoilCNRatio", new VariableProperty(this, GetType().GetProperty("SoilCNRatio"))),
                new TabularData.Column("FBiom", new VariableProperty(this, GetType().GetProperty("FBiom"))),
                new TabularData.Column("FInert", new VariableProperty(this, GetType().GetProperty("FInert"))),
                new TabularData.Column("FOM", new VariableProperty(this, GetType().GetProperty("FOM")))
            });
        }
    }
}