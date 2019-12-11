namespace APSIM.Shared.APSoil
{
    using System;

    /// <summary>A soil organic matter class.</summary>
    [Serializable]
    public class SoilOrganicMatter
    {
        /// <summary>Root C:N ratio</summary>
        public double RootCN { get; set; }

        /// <summary>Root Weight (kg/ha)</summary>
        public double RootWt { get; set; }

        /// <summary>Soil C:N ratio</summary>
        public double SoilCN { get; set; }

        /// <summary>Erosion enrichment coefficient A</summary>
        public double EnrACoeff { get; set; }

        /// <summary>Erosion enrichment coefficient B</summary>
        public double EnrBCoeff { get; set; }
        
        /// <summary>The thickness of each layer</summary>
        public double[] Thickness { get; set; }
        
        /// <summary>The organic carbon of each layer</summary>
        public double[] OC { get; set; }

        /// <summary>The organic carbon metadata.</summary>
        public string[] OCMetadata { get; set; }
        
        /// <summary>Gets or sets the fbiom.</summary>
        public double[] FBiom { get; set; }
        
        /// <summary>Gets or sets the finert.</summary>
        public double[] FInert { get; set; }

        /// <summary>Allowable units for OC</summary>
        public enum OCUnitsEnum 
        {
            /// <summary>total (%)</summary>
            Total,

            /// <summary>walkley black (%)</summary>
            WalkleyBlack 
        }

        /// <summary>Gets or sets the oc units.</summary>
        public OCUnitsEnum OCUnits { get; set; }
    }
}
