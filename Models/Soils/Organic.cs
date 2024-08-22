using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>A model for capturing soil organic parameters</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Organic : Model
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
        [Display]
        [Summary]
        [Units("mm")]
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

        /// <summary>Root C:N Ratio</summary>
        [Summary]
        [Description("FOM C:N ratio (0-500)")]
        [Bounds(Lower = 0.0, Upper = 500.0)]
        public double FOMCNRatio { get; set; }

        /// <summary>Soil layer thickness for each layer (mm)</summary>
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Carbon concentration</summary>
        [Summary]
        [Bounds(Lower = 0.1, Upper = 10.0)]
        [Display(Format = "N3")]
        public double[] Carbon { get; set; }

        /// <summary>The units of organic carbon.</summary>
        public CarbonUnitsEnum CarbonUnits { get; set; }

        /// <summary>Carbon:nitrogen ratio.</summary>
        [Display(Format = "N3")]
        [Summary]
        [Units("g/g")]
        [Bounds(Lower = 5.0, Upper = 30.0)]
        public double[] SoilCNRatio { get; set; }

        /// <summary>F biom.</summary>
        [Summary]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Display(Format = "N3")]
        public double[] FBiom { get; set; }

        /// <summary>F inert.</summary>
        [Summary]
        [Units("0-1")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Display(Format = "N3")]
        public double[] FInert { get; set; }

        /// <summary>Fresh organic matter</summary>
        [Summary]
        [Units("kg/ha")]
        [Display(Format = "N1")]
        public double[] FOM { get; set; }

        /// <summary>Organic nitrogen. Units: %</summary>
        [Units("%")]
        public double[] Nitrogen { get { return MathUtilities.Divide(Carbon, SoilCNRatio); } }

        /// <summary>Organic carbon:nitrogen ratio</summary>
        [Units("%")]
        public double[] CNR { get { return MathUtilities.Divide(Carbon, Nitrogen); } }

        /// <summary>Carbon metadata</summary>
        public string[] CarbonMetadata { get; set; }

        /// <summary>FOM metadata</summary>
        public string[] FOMMetadata { get; set; }

        /// <summary>Gets the model ready for running in a simulation.</summary>
        /// <param name="targetThickness">Target thickness.</param>
        public void Standardise(double[] targetThickness)
        {
            SetThickness(targetThickness);
            if (CarbonUnits == CarbonUnitsEnum.WalkleyBlack)
            {
                Carbon = SoilUtilities.OCWalkleyBlackToTotal(Carbon);
                CarbonUnits = CarbonUnitsEnum.Total;
            }

            if (!MathUtilities.ValuesInArray(Carbon))
                Carbon = null;
            if (Carbon != null)
                Carbon = MathUtilities.FixArrayLength(Carbon, Thickness.Length);
        }

        /// <summary>Sets the soil organic matter thickness.</summary>
        /// <param name="targetThickness">Thickness to change soil water to.</param>
        private void SetThickness(double[] targetThickness)
        {
            if (!MathUtilities.AreEqual(targetThickness, Thickness))
            {
                FBiom = SoilUtilities.MapConcentration(FBiom, Thickness, targetThickness, MathUtilities.LastValue(FBiom));
                FInert = SoilUtilities.MapConcentration(FInert, Thickness, targetThickness, MathUtilities.LastValue(FInert));
                Carbon = SoilUtilities.MapConcentration(Carbon, Thickness, targetThickness, MathUtilities.LastValue(Carbon));
                SoilCNRatio = SoilUtilities.MapConcentration(SoilCNRatio, Thickness, targetThickness, MathUtilities.LastValue(SoilCNRatio));
                FOM = SoilUtilities.MapMass(FOM, Thickness, targetThickness, false);
                Thickness = targetThickness;
            }

            if (FBiom != null)
                MathUtilities.ReplaceMissingValues(FBiom, MathUtilities.LastValue(FBiom));
            if (FInert != null)
                MathUtilities.ReplaceMissingValues(FInert, MathUtilities.LastValue(FInert));
            if (Carbon != null)
                MathUtilities.ReplaceMissingValues(Carbon, MathUtilities.LastValue(Carbon));
        }
    }
}