﻿using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.APSoil;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>A model for capturing physical soil parameters</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Physical : Model, IPhysical, ITabularData
    {
        // Water node.
        private Water waterNode = null;

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
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
        [Units("%")]
        public double[] ParticleSizeClay { get; set; }

        /// <summary>Particle size sand.</summary>
        [Summary]
        [Units("%")]
        public double[] ParticleSizeSand { get; set; }

        /// <summary>Particle size silt.</summary>
        [Summary]
        [Units("%")]
        public double[] ParticleSizeSilt { get; set; }

        /// <summary>Rocks.</summary>
        [Summary]
        [Units("%")]
        public double[] Rocks { get; set; }

        /// <summary>Texture.</summary>
        public string[] Texture { get; set; }

        /// <summary>Bulk density (g/cc).</summary>
        [Summary]
        [Units("g/cc")]
        [Display(Format = "N2")]
        public double[] BD { get; set; }

        /// <summary>Air dry - volumetric (mm/mm).</summary>
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] AirDry { get; set; }

        /// <summary>Lower limit 15 bar (mm/mm).</summary>
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] LL15 { get; set; }

        /// <summary>Return lower limit limit at standard thickness. Units: mm</summary>
        [Units("mm")]
        public double[] LL15mm { get { return MathUtilities.Multiply(LL15, Thickness); } }

        /// <summary>Drained upper limit (mm/mm).</summary>
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] DUL { get; set; }

        /// <summary>Drained upper limit (mm).</summary>
        [Units("mm")]
        public double[] DULmm { get { return MathUtilities.Multiply(DUL, Thickness); } }

        /// <summary>Saturation (mm/mm).</summary>
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        public double[] SAT { get; set; }

        /// <summary>Saturation (mm).</summary>
        [Units("mm")]
        public double[] SATmm { get { return MathUtilities.Multiply(SAT, Thickness); } }

        /// <summary>Initial soil water (mm/mm).</summary>
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N2")]
        [JsonIgnore]
        public double[] SW
        {
            get
            {
                return SoilUtilities.MapConcentration(WaterNode.InitialValues, WaterNode.Thickness, Thickness, 0);
            }
            set
            {
                if (IsSWSameLayerStructure)
                    WaterNode.InitialValues = value;
            }
        }

        /// <summary>Initial soil water (mm).</summary>
        [Units("mm")]
        public double[] SWmm { get { return MathUtilities.Multiply(SW, Thickness); } }

        /// <summary>KS (mm/day).</summary>
        [Summary]
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
            bool waterNodePresent = FindInScope<Water>() != null;
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
            if (waterNodePresent)
                columns.Add(new TabularData.Column("SW", new VariableProperty(this, GetType().GetProperty("SW")), readOnly: !IsSWSameLayerStructure));
            columns.Add(new TabularData.Column("KS", new VariableProperty(this, GetType().GetProperty("KS"))));

            foreach (var soilCrop in FindAllChildren<SoilCrop>())
            {
                var cropName = soilCrop.Name.Replace("Soil", "");
                columns.Add(new TabularData.Column($"{cropName} LL", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("LL"))));
                columns.Add(new TabularData.Column($"{cropName} KL", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("KL"))));
                columns.Add(new TabularData.Column($"{cropName} XF", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("XF"))));
                columns.Add(new TabularData.Column($"{cropName} PAWC", new VariableProperty(soilCrop, soilCrop.GetType().GetProperty("PAWCmm")), units: $"{soilCrop.PAWCmm.Sum():F1} mm"));
            }

            return new TabularData(Name, columns);
        }

        private Water WaterNode
        {
            get
            {
                if (waterNode == null)
                    waterNode = FindInScope<Water>();
                if (waterNode == null)
                    throw new Exception("Cannot find water node in simulation");
                return waterNode;
            }
        }

        /// <summary>Is SW from the water node in the same layer structure?</summary>
        private bool IsSWSameLayerStructure => MathUtilities.AreEqual(Thickness, WaterNode.Thickness);

        /// <summary>Gets the model ready for running in a simulation.</summary>
        /// <param name="targetThickness"></param>
        public void Standardise(double[] targetThickness)
        {
            SetThickness(targetThickness);
            AddPredictedCrops();
            foreach (var crop in FindAllChildren<SoilCrop>())
            {
                if (crop.XF == null)
                {
                    crop.XF = MathUtilities.CreateArrayOfValues(1.0, Thickness.Length);
                    crop.XFMetadata = StringUtilities.CreateStringArray("Estimated", Thickness.Length);
                }
                if (crop.KL == null)
                    FillInKLForCrop(crop);

                CheckCropForMissingValues(crop);

                // Modify wheat crop for sub soil constraints.
                if (crop.Name.Equals("wheat", StringComparison.InvariantCultureIgnoreCase))
                    ModifyKLForSubSoilConstraints(crop);
            }

            // Make sure there are the correct number of KS values.
            if (KS != null && KS.Length > 0)
                KS = MathUtilities.FillMissingValues(KS, Thickness.Length, 0.0);
        }

        /// <summary>Sets the water thickness.</summary>
        /// <param name="targetThickness">To thickness.</param>
        private void SetThickness(double[] targetThickness)
        {
            if (!MathUtilities.AreEqual(targetThickness, Thickness))
            {
                foreach (var crop in FindAllChildren<SoilCrop>())
                {
                    crop.KL = SoilUtilities.MapConcentration(crop.KL, Thickness, targetThickness, MathUtilities.LastValue(crop.KL));
                    crop.XF = SoilUtilities.MapConcentration(crop.XF, Thickness, targetThickness, MathUtilities.LastValue(crop.XF));
                    crop.LL = SoilUtilities.MapConcentration(crop.LL, Thickness, targetThickness, MathUtilities.LastValue(crop.LL));
                }

                BD = SoilUtilities.MapConcentration(BD, Thickness, targetThickness, MathUtilities.LastValue(BD));
                AirDry = SoilUtilities.MapConcentration(AirDry, Thickness, targetThickness, MathUtilities.LastValue(AirDry));
                LL15 = SoilUtilities.MapConcentration(LL15, Thickness, targetThickness, MathUtilities.LastValue(LL15));
                DUL = SoilUtilities.MapConcentration(DUL, Thickness, targetThickness, MathUtilities.LastValue(DUL));
                SAT = SoilUtilities.MapConcentration(SAT, Thickness, targetThickness, MathUtilities.LastValue(SAT));
                KS = SoilUtilities.MapConcentration(KS, Thickness, targetThickness, MathUtilities.LastValue(KS));
                if (ParticleSizeClay != null && ParticleSizeClay.Length > 0 && ParticleSizeClay.Length != targetThickness.Length)
                    ParticleSizeClay = SoilUtilities.MapConcentration(ParticleSizeClay, Thickness, targetThickness, MathUtilities.LastValue(ParticleSizeClay));
                if (ParticleSizeSand != null && ParticleSizeSand.Length > 0 && ParticleSizeSand.Length != targetThickness.Length)
                    ParticleSizeSand = SoilUtilities.MapConcentration(ParticleSizeSand, Thickness, targetThickness, MathUtilities.LastValue(ParticleSizeSand));
                if (ParticleSizeSilt != null && ParticleSizeSilt.Length > 0 && ParticleSizeSilt.Length != targetThickness.Length)
                    ParticleSizeSilt = SoilUtilities.MapConcentration(ParticleSizeSilt, Thickness, targetThickness, MathUtilities.LastValue(ParticleSizeSilt));
                if (Rocks != null && Rocks.Length > 0 && Rocks.Length != targetThickness.Length)
                    Rocks = SoilUtilities.MapConcentration(Rocks, Thickness, targetThickness, MathUtilities.LastValue(Rocks));
                Thickness = targetThickness;

                foreach (var crop in FindAllChildren<SoilCrop>())
                {
                    var soilCrop = crop as SoilCrop;
                    // Ensure crop LL are between Airdry and DUL.
                    for (int i = 0; i < soilCrop.LL.Length; i++)
                        soilCrop.LL = MathUtilities.Constrain(soilCrop.LL, AirDry, DUL);
                }
            }
        }

        /// <summary>Fills in KL for crop.</summary>
        /// <param name="crop">The crop.</param>
        private static void FillInKLForCrop(SoilCrop crop)
        {
            if (crop.Name == null)
                throw new Exception("Crop has no name");
            int i = StringUtilities.IndexOfCaseInsensitive(cropNames, crop.Name);
            if (i != -1)
            {
                var water = crop.Parent as Physical;

                double[] KLs = GetRowOfArray(defaultKLs, i);

                double[] cumThickness = SoilUtilities.ToCumThickness(water.Thickness);
                crop.KL = new double[water.Thickness.Length];
                for (int l = 0; l < water.Thickness.Length; l++)
                {
                    bool didInterpolate;
                    crop.KL[l] = MathUtilities.LinearInterpReal(cumThickness[l], defaultKLThickness, KLs, out didInterpolate);
                }
            }
        }

        /// <summary>Gets the row of a 2 dimensional array.</summary>
        /// <param name="array">The array.</param>
        /// <param name="row">The row index</param>
        /// <returns>The values in the specified row.</returns>
        private static double[] GetRowOfArray(double[,] array, int row)
        {
            List<double> values = new List<double>();
            for (int col = 0; col < array.GetLength(1); col++)
                values.Add(array[row, col]);

            return values.ToArray();
        }

        /// <summary>Checks the crop for missing values.</summary>
        /// <param name="crop">The crop.</param>
        private void CheckCropForMissingValues(SoilCrop crop)
        {
            for (int i = 0; i < Thickness.Length; i++)
            {
                if (crop.LL != null && double.IsNaN(crop.LL[i]))
                    crop.LL[i] = LL15[i];
                if (crop.KL != null && double.IsNaN(crop.KL[i]))
                    crop.KL[i] = 0.06;
                if (crop.XF != null && double.IsNaN(crop.XF[i]))
                    crop.XF[i] = 1;
            }
        }

        private static string[] cropNames = {"Wheat", "Oats",
                                             "Sorghum", "Barley", "Chickpea", "Mungbean", "Cotton", "Canola",
                                             "PigeonPea", "Maize", "Cowpea", "Sunflower", "Fababean", "Lucerne",
                                             "Lupin", "Lentil", "Triticale", "Millet", "Soybean" };

        private static double[] defaultKLThickness = new double[] { 150, 300, 600, 900, 1200, 1500, 1800 };
        private static double[,] defaultKLs =  {{0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.07,   0.07,   0.07,   0.05,   0.05,   0.04,   0.03},
                                                {0.07,   0.07,   0.07,   0.05,   0.05,   0.03,   0.02},
                                                {0.06,   0.06,   0.06,   0.06,   0.06,   0.06,   0.06},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.00,   0.00},
                                                {0.10,   0.10,   0.10,   0.10,   0.09,   0.07,   0.05},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.05,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.10,   0.10,   0.08,   0.06,   0.04,   0.02,   0.01},
                                                {0.08,   0.08,   0.08,   0.08,   0.06,   0.04,   0.03},
                                                {0.10,   0.10,   0.10,   0.10,   0.09,   0.09,   0.09},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.07,   0.07,   0.07,   0.04,   0.02,   0.01,   0.01},
                                                {0.07,   0.07,   0.07,   0.05,   0.05,   0.04,   0.03},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01}};

        /// <summary>
        /// The black vertosol crop list
        /// </summary>
        private static string[] BlackVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton" };
        /// <summary>
        /// The grey vertosol crop list
        /// </summary>
        private static string[] GreyVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton", "Barley", "Chickpea", "Fababean", "Mungbean" };
        /// <summary>
        /// The predicted thickness
        /// </summary>
        private static double[] PredictedThickness = new double[] { 150, 150, 300, 300, 300, 300, 300 };
        /// <summary>
        /// The predicted xf
        /// </summary>
        private static double[] PredictedXF = new double[] { 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00 };
        /// <summary>
        /// The wheat kl
        /// </summary>
        private static double[] WheatKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The sorghum kl
        /// </summary>
        private static double[] SorghumKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.04, 0.03 };
        /// <summary>
        /// The barley kl
        /// </summary>
        private static double[] BarleyKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.03, 0.02 };
        /// <summary>
        /// The chickpea kl
        /// </summary>
        private static double[] ChickpeaKL = new double[] { 0.06, 0.06, 0.06, 0.06, 0.06, 0.06, 0.06 };
        /// <summary>
        /// The mungbean kl
        /// </summary>
        private static double[] MungbeanKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.00, 0.00 };
        /// <summary>
        /// The cotton kl
        /// </summary>
        private static double[] CottonKL = new double[] { 0.10, 0.10, 0.10, 0.10, 0.09, 0.07, 0.05 };
        /// <summary>
        /// The canola kl
        /// </summary>
        /// private static double[] CanolaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The pigeon pea kl
        /// </summary>
        /// private static double[] PigeonPeaKL = new double[] { 0.06, 0.06, 0.06, 0.05, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The maize kl
        /// </summary>
        /// private static double[] MaizeKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The cowpea kl
        /// </summary>
        /// private static double[] CowpeaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The sunflower kl
        /// </summary>
        /// private static double[] SunflowerKL = new double[] { 0.01, 0.01, 0.08, 0.06, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The fababean kl
        /// </summary>
        private static double[] FababeanKL = new double[] { 0.08, 0.08, 0.08, 0.08, 0.06, 0.04, 0.03 };
        /// <summary>
        /// The lucerne kl
        /// </summary>
        /// private static double[] LucerneKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.09, 0.09 };
        /// <summary>
        /// The perennial kl
        /// </summary>
        /// private static double[] PerennialKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.07, 0.05 };

        /// <summary>
        /// 
        /// </summary>
        private class BlackVertosol
        {
            /// <summary>
            /// The cotton a
            /// </summary>
            internal static double[] CottonA = new double[] { 0.832, 0.868, 0.951, 0.988, 1.043, 1.095, 1.151 };
            /// <summary>
            /// The sorghum a
            /// </summary>
            internal static double[] SorghumA = new double[] { 0.699, 0.802, 0.853, 0.907, 0.954, 1.003, 1.035 };
            /// <summary>
            /// The wheat a
            /// </summary>
            internal static double[] WheatA = new double[] { 0.124, 0.049, 0.024, 0.029, 0.146, 0.246, 0.406 };

            /// <summary>
            /// The cotton b
            /// </summary>
            internal static double CottonB = -0.0070;
            /// <summary>
            /// The sorghum b
            /// </summary>
            internal static double SorghumB = -0.0038;
            /// <summary>
            /// The wheat b
            /// </summary>
            internal static double WheatB = 0.0116;

        }
        /// <summary>
        /// 
        /// </summary>
        private class GreyVertosol
        {
            /// <summary>
            /// The cotton a
            /// </summary>
            internal static double[] CottonA = new double[] { 0.853, 0.851, 0.883, 0.953, 1.022, 1.125, 1.186 };
            /// <summary>
            /// The sorghum a
            /// </summary>
            internal static double[] SorghumA = new double[] { 0.818, 0.864, 0.882, 0.938, 1.103, 1.096, 1.172 };
            /// <summary>
            /// The wheat a
            /// </summary>
            internal static double[] WheatA = new double[] { 0.660, 0.655, 0.701, 0.745, 0.845, 0.933, 1.084 };
            /// <summary>
            /// The barley a
            /// </summary>
            internal static double[] BarleyA = new double[] { 0.847, 0.866, 0.835, 0.872, 0.981, 1.036, 1.152 };
            /// <summary>
            /// The chickpea a
            /// </summary>
            internal static double[] ChickpeaA = new double[] { 0.435, 0.452, 0.481, 0.595, 0.668, 0.737, 0.875 };
            /// <summary>
            /// The fababean a
            /// </summary>
            internal static double[] FababeanA = new double[] { 0.467, 0.451, 0.396, 0.336, 0.190, 0.134, 0.084 };
            /// <summary>
            /// The mungbean a
            /// </summary>
            internal static double[] MungbeanA = new double[] { 0.779, 0.770, 0.834, 0.990, 1.008, 1.144, 1.150 };
            /// <summary>
            /// The cotton b
            /// </summary>
            internal static double CottonB = -0.0082;
            /// <summary>
            /// The sorghum b
            /// </summary>
            internal static double SorghumB = -0.007;
            /// <summary>
            /// The wheat b
            /// </summary>
            internal static double WheatB = -0.0032;
            /// <summary>
            /// The barley b
            /// </summary>
            internal static double BarleyB = -0.0051;
            /// <summary>
            /// The chickpea b
            /// </summary>
            internal static double ChickpeaB = 0.0029;
            /// <summary>
            /// The fababean b
            /// </summary>
            internal static double FababeanB = 0.02455;
            /// <summary>
            /// The mungbean b
            /// </summary>
            internal static double MungbeanB = -0.0034;
        }

        /// <summary>
        /// Return a list of predicted crop names or an empty string[] if none found.
        /// </summary>
        /// <returns></returns>
        private void AddPredictedCrops()
        {
            var soil = Parent as Soil;
            if (soil.SoilType != null)
            {
                string[] predictedCropNames = null;
                if (soil.SoilType.Equals("Black Vertosol", StringComparison.CurrentCultureIgnoreCase))
                    predictedCropNames = BlackVertosolCropList;
                else if (soil.SoilType.Equals("Grey Vertosol", StringComparison.CurrentCultureIgnoreCase))
                    predictedCropNames = GreyVertosolCropList;

                if (predictedCropNames != null)
                {
                    var water = soil.FindChild<Physical>();
                    var crops = water.FindAllChildren<SoilCrop>().ToList();

                    foreach (string cropName in predictedCropNames)
                    {
                        // if a crop parameterisation already exists for this crop then don't add a predicted one.
                        if (crops.Find(c => c.Name.Equals(cropName, StringComparison.InvariantCultureIgnoreCase)) == null)
                            crops.Add(PredictedCrop(soil, cropName));
                    }
                }
            }
        }

        /// <summary>
        /// Return a predicted SoilCrop for the specified crop name or null if not found.
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="CropName">Name of the crop.</param>
        /// <returns></returns>
        private static SoilCrop PredictedCrop(Soil soil, string CropName)
        {
            double[] A = null;
            double B = double.NaN;
            double[] KL = null;

            if (soil.SoilType == null)
                return null;

            if (soil.SoilType.Equals("Black Vertosol", StringComparison.CurrentCultureIgnoreCase))
            {
                if (CropName.Equals("Cotton", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = BlackVertosol.CottonA;
                    B = BlackVertosol.CottonB;
                    KL = CottonKL;
                }
                else if (CropName.Equals("Sorghum", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = BlackVertosol.SorghumA;
                    B = BlackVertosol.SorghumB;
                    KL = SorghumKL;
                }
                else if (CropName.Equals("Wheat", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = BlackVertosol.WheatA;
                    B = BlackVertosol.WheatB;
                    KL = WheatKL;
                }
            }
            else if (soil.SoilType.Equals("Grey Vertosol", StringComparison.CurrentCultureIgnoreCase))
            {
                if (CropName.Equals("Cotton", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.CottonA;
                    B = GreyVertosol.CottonB;
                    KL = CottonKL;
                }
                else if (CropName.Equals("Sorghum", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.SorghumA;
                    B = GreyVertosol.SorghumB;
                    KL = SorghumKL;
                }
                else if (CropName.Equals("Wheat", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.WheatA;
                    B = GreyVertosol.WheatB;
                    KL = WheatKL;
                }
                else if (CropName.Equals("Barley", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.BarleyA;
                    B = GreyVertosol.BarleyB;
                    KL = BarleyKL;
                }
                else if (CropName.Equals("Chickpea", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.ChickpeaA;
                    B = GreyVertosol.ChickpeaB;
                    KL = ChickpeaKL;
                }
                else if (CropName.Equals("Fababean", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.FababeanA;
                    B = GreyVertosol.FababeanB;
                    KL = FababeanKL;
                }
                else if (CropName.Equals("Mungbean", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.MungbeanA;
                    B = GreyVertosol.MungbeanB;
                    KL = MungbeanKL;
                }
            }


            if (A == null)
                return null;

            var physical = soil.FindChild<IPhysical>();
            double[] LL = PredictedLL(physical, A, B);
            LL = SoilUtilities.MapConcentration(LL, PredictedThickness, physical.Thickness, LL.Last());
            KL = SoilUtilities.MapConcentration(KL, PredictedThickness, physical.Thickness, KL.Last());
            double[] XF = SoilUtilities.MapConcentration(PredictedXF, PredictedThickness, physical.Thickness, PredictedXF.Last());
            string[] Metadata = StringUtilities.CreateStringArray("Estimated", physical.Thickness.Length);

            return new SoilCrop()
            {
                Name = CropName,
                LL = LL,
                LLMetadata = Metadata,
                KL = KL,
                KLMetadata = Metadata,
                XF = XF,
                XFMetadata = Metadata
            };
        }

        /// <summary>
        /// Calculate and return a predicted LL from the specified A and B values.
        /// </summary>
        /// <param name="physical">The soil physical properties.</param>
        /// <param name="A">a.</param>
        /// <param name="B">The b.</param>
        /// <returns></returns>
        private static double[] PredictedLL(IPhysical physical, double[] A, double B)
        {
            double[] LL15 = SoilUtilities.MapConcentration(physical.LL15, physical.Thickness, PredictedThickness, physical.LL15.Last());
            double[] DUL = SoilUtilities.MapConcentration(physical.DUL, physical.Thickness, PredictedThickness, physical.DUL.Last());
            double[] LL = new double[PredictedThickness.Length];
            for (int i = 0; i != PredictedThickness.Length; i++)
            {
                double DULPercent = DUL[i] * 100.0;
                LL[i] = DULPercent * (A[i] + B * DULPercent);
                LL[i] /= 100.0;

                // Bound the predicted LL values.
                LL[i] = Math.Max(LL[i], LL15[i]);
                LL[i] = Math.Min(LL[i], DUL[i]);
            }

            //  make the top 3 layers the same as the top 3 layers of LL15
            if (LL.Length >= 3)
            {
                LL[0] = LL15[0];
                LL[1] = LL15[1];
                LL[2] = LL15[2];
            }
            return LL;
        }


        /// <summary>Standard thicknesses</summary>
        private static readonly double[] StandardThickness = new double[] { 100, 100, 200, 200, 200, 200, 200 };
        /// <summary>Standard Kls</summary>
        private static readonly double[] StandardKL = new double[] { 0.06, 0.06, 0.04, 0.04, 0.04, 0.04, 0.02 };

        /// <summary>
        /// Modify the KL values for subsoil constraints.
        /// </summary>
        /// <remarks>
        /// From:
        /// Hochman, Z., Dang, Y.P., Schwenke, G.D., Dalgliesh, N.P., Routley, R., McDonald, M., 
        ///     Daniells, I.G., Manning, W., Poulton, P.L., 2007. 
        ///     Simulating the effects of saline and sodic subsoils on wheat crops 
        ///     growing on Vertosols. Australian Journal of Agricultural Research 58, 802–810. doi:10.1071/ar06365
        /// </remarks>
        /// <param name="crop"></param>
        private void ModifyKLForSubSoilConstraints(SoilCrop crop)
        {
            var clNode = FindInScope<Solute>("CL");
            var chemical = FindInScope<Chemical>();
            double[] cl = clNode.InitialValues;
            if (MathUtilities.ValuesInArray(cl))
            {
                cl = SoilUtilities.MapConcentration(cl, clNode.Thickness, Thickness, 0);
                crop.KL = SoilUtilities.MapConcentration(StandardKL, StandardThickness, Thickness, StandardKL.Last());
                for (int i = 0; i < Thickness.Length; i++)
                    crop.KL[i] *= Math.Min(1.0, 4.0 * Math.Exp(-0.005 * cl[i]));
            }
            else
            {
                double[] esp = chemical.ESP;
                if (MathUtilities.ValuesInArray(esp))
                {
                    crop.KL = SoilUtilities.MapConcentration(StandardKL, StandardThickness, Thickness, StandardKL.Last());
                    for (int i = 0; i < Thickness.Length; i++)
                        crop.KL[i] *= Math.Min(1.0, 10.0 * Math.Exp(-0.15 * esp[i]));
                }
                else
                {
                    double[] ec = chemical.EC;
                    if (MathUtilities.ValuesInArray(ec))
                    {
                        crop.KL = SoilUtilities.MapConcentration(StandardKL, StandardThickness, Thickness, StandardKL.Last());
                        for (int i = 0; i < Thickness.Length; i++)
                            crop.KL[i] *= Math.Min(1.0, 3.0 * Math.Exp(-1.3 * ec[i]));
                    }
                }
            }
        }
    }
}
