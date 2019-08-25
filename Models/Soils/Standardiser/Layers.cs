namespace Models.Soils.Standardiser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Models.Core;

    /// <summary>Methods to standardise a soil ready for running in APSIM.</summary>
    [Serializable]
    public class Layers
    {
        /// <summary>Standardise the specified soil with a uniform thickness.</summary>
        /// <param name="soil">The soil.</param>
        /// <returns>A standardised soil.</returns>
        public static void Standardise(Soil soil)
        {
            var waterNode = Apsim.Child(soil, typeof(Physical)) as Physical;
            var analysisNode = Apsim.Child(soil, typeof(Chemical)) as Chemical;
            var layerStructure = Apsim.Child(soil, typeof(LayerStructure)) as LayerStructure;

            // Determine the target layer structure.
            var targetThickness = soil.Thickness;
            if (layerStructure != null)
                targetThickness = layerStructure.Thickness;

            foreach (Sample sample in Apsim.Children(soil, typeof(Sample)))
                SetSampleThickness(sample, targetThickness, soil);

            if (soil.SoilWater != null)
                SetSoilWaterThickness(soil.SoilWater as SoilWater, targetThickness);
            if (soil.Weirdo != null)
                soil.Weirdo.MapVariables(targetThickness);
            SetAnalysisThickness(analysisNode, targetThickness);
            SetSoilOrganicMatterThickness(soil.SoilOrganicMatter, targetThickness);
            SetWaterThickness(waterNode, targetThickness, soil);
        }

        /// <summary>Sets the water thickness.</summary>
        /// <param name="water">The water.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="soil">Soil</param>
        private static void SetWaterThickness(Physical water, double[] toThickness, Soil soil)
        {
            if (!MathUtilities.AreEqual(toThickness, soil.Thickness))
            {
                bool needToConstrainCropLL = false;
                if (soil.Crops != null)
                    foreach (var crop in soil.Crops)
                    {
                        crop.KL = MapConcentration(crop.KL, soil.Thickness, toThickness, MathUtilities.LastValue(crop.KL));
                        crop.XF = MapConcentration(crop.XF, soil.Thickness, toThickness, MathUtilities.LastValue(crop.XF));

                        if (crop is SoilCrop)
                        {
                            needToConstrainCropLL = true;

                            var soilCrop = crop as SoilCrop;
                            soilCrop.LL = MapConcentration(soilCrop.LL, soil.Thickness, toThickness, MathUtilities.LastValue(soilCrop.LL));
                        }
                    }

                soil.BD = MapConcentration(soil.BD, soil.Thickness, toThickness, MathUtilities.LastValue(soil.BD));
                soil.AirDry = MapConcentration(soil.AirDry, soil.Thickness, toThickness, MathUtilities.LastValue(soil.AirDry));
                soil.LL15 = MapConcentration(soil.LL15, soil.Thickness, toThickness, MathUtilities.LastValue(soil.LL15));
                soil.DUL = MapConcentration(soil.DUL, soil.Thickness, toThickness, MathUtilities.LastValue(soil.DUL));
                soil.SAT = MapConcentration(soil.SAT, soil.Thickness, toThickness, MathUtilities.LastValue(soil.SAT));
                soil.KS = MapConcentration(soil.KS, soil.Thickness, toThickness, MathUtilities.LastValue(soil.KS));
                soil.Thickness = toThickness;
                if (water != null)
                    if (water.ParticleSizeClay != null && water.ParticleSizeClay.Length == water.Thickness.Length)
                        water.ParticleSizeClay = MapConcentration(water.ParticleSizeClay, water.Thickness, toThickness, MathUtilities.LastValue(water.ParticleSizeClay));
                    else
                        water.ParticleSizeClay = null;
                
                if (needToConstrainCropLL)
                {
                    foreach (var crop in soil.Crops)
                    {
                        if (crop is SoilCrop)
                        {
                            var soilCrop = crop as SoilCrop;
                            // Ensure crop LL are between Airdry and DUL.
                            for (int i = 0; i < soilCrop.LL.Length; i++)
                                soilCrop.LL = MathUtilities.Constrain(soilCrop.LL, water.AirDry, water.DUL);
                        }
                    }
                }
            }
        }

        /// <summary>Sets the soil water thickness.</summary>
        /// <param name="soilWater">The soil water.</param>
        /// <param name="thickness">Thickness to change soil water to.</param>
        private static void SetSoilWaterThickness(SoilWater soilWater, double[] thickness)
        {
            if (soilWater != null)
            {
                if (!MathUtilities.AreEqual(thickness, soilWater.Thickness))
                {
                    soilWater.KLAT = MapConcentration(soilWater.KLAT, soilWater.Thickness, thickness, MathUtilities.LastValue(soilWater.KLAT));
                    soilWater.SWCON = MapConcentration(soilWater.SWCON, soilWater.Thickness, thickness, 0.0);

                    soilWater.Thickness = thickness;
                }

                MathUtilities.ReplaceMissingValues(soilWater.SWCON, 0.0);
            }
        }

        /// <summary>Sets the soil organic matter thickness.</summary>
        /// <param name="soilOrganicMatter">The soil organic matter.</param>
        /// <param name="thickness">Thickness to change soil water to.</param>
        private static void SetSoilOrganicMatterThickness(Organic soilOrganicMatter, double[] thickness)
        {
            if (soilOrganicMatter != null)
            {
                if (!MathUtilities.AreEqual(thickness, soilOrganicMatter.Thickness))
                {
                    soilOrganicMatter.FBiom = MapConcentration(soilOrganicMatter.FBiom, soilOrganicMatter.Thickness, thickness, MathUtilities.LastValue(soilOrganicMatter.FBiom));
                    soilOrganicMatter.FInert = MapConcentration(soilOrganicMatter.FInert, soilOrganicMatter.Thickness, thickness, MathUtilities.LastValue(soilOrganicMatter.FInert));
                    soilOrganicMatter.Carbon = MapConcentration(soilOrganicMatter.Carbon, soilOrganicMatter.Thickness, thickness, MathUtilities.LastValue(soilOrganicMatter.Carbon));
                    soilOrganicMatter.SoilCNRatio = MapConcentration(soilOrganicMatter.SoilCNRatio, soilOrganicMatter.Thickness, thickness, MathUtilities.LastValue(soilOrganicMatter.SoilCNRatio));
                    soilOrganicMatter.FOM = MapMass(soilOrganicMatter.FOM, soilOrganicMatter.Thickness, thickness, false);
                    soilOrganicMatter.Thickness = thickness;
                }

                if (soilOrganicMatter.FBiom != null)
                    MathUtilities.ReplaceMissingValues(soilOrganicMatter.FBiom, MathUtilities.LastValue(soilOrganicMatter.FBiom));
                if (soilOrganicMatter.FInert != null)
                    MathUtilities.ReplaceMissingValues(soilOrganicMatter.FInert, MathUtilities.LastValue(soilOrganicMatter.FInert));
                if (soilOrganicMatter.Carbon != null)
                    MathUtilities.ReplaceMissingValues(soilOrganicMatter.Carbon, MathUtilities.LastValue(soilOrganicMatter.Carbon));
            }
        }

        /// <summary>Sets the analysis thickness.</summary>
        /// <param name="analysis">The analysis.</param>
        /// <param name="thickness">The thickness to change the analysis to.</param>
        private static void SetAnalysisThickness(Chemical analysis, double[] thickness)
        {
            if (analysis != null && !MathUtilities.AreEqual(thickness, analysis.Thickness))
            {

                string[] metadata = StringUtilities.CreateStringArray("Mapped", thickness.Length);

                analysis.NO3N = MapConcentration(analysis.NO3N, analysis.Thickness, thickness, 1.0);
                analysis.NH4N = MapConcentration(analysis.NH4N, analysis.Thickness, thickness, 0.2);
                analysis.CL = MapConcentration(analysis.CL, analysis.Thickness, thickness, MathUtilities.LastValue(analysis.CL));
                analysis.EC = MapConcentration(analysis.EC, analysis.Thickness, thickness, MathUtilities.LastValue(analysis.EC));
                analysis.ESP = MapConcentration(analysis.ESP, analysis.Thickness, thickness, MathUtilities.LastValue(analysis.ESP));

                analysis.PH = MapConcentration(analysis.PH, analysis.Thickness, thickness, MathUtilities.LastValue(analysis.PH));
                analysis.Thickness = thickness;
            }
        }

        /// <summary>Sets the sample thickness.</summary>
        /// <param name="sample">The sample.</param>
        /// <param name="thickness">The thickness to change the sample to.</param>
        /// <param name="soil">The soil</param>
        private static void SetSampleThickness(Sample sample, double[] thickness, Soil soil)
        {
            if (!MathUtilities.AreEqual(thickness, sample.Thickness))
            {
                if (sample.SW != null)
                    sample.SW = MapSW(sample.SW, sample.Thickness, thickness, soil);
                if (sample.NH4N != null)
                    sample.NH4N = MapConcentration(sample.NH4N, sample.Thickness, thickness, 0.2);
                if (sample.NO3N != null)
                    sample.NO3N = MapConcentration(sample.NO3N, sample.Thickness, thickness, 1.0);

                // The elements below will be overlaid over other arrays of values so we want 
                // to have missing values (double.NaN) used at the bottom of the profile.

                if (sample.CL != null)
                    sample.CL = MapConcentration(sample.CL, sample.Thickness, thickness, double.NaN, allowMissingValues: true);
                if (sample.EC != null)
                    sample.EC = MapConcentration(sample.EC, sample.Thickness, thickness, double.NaN, allowMissingValues: true);
                if (sample.ESP != null)
                    sample.ESP = MapConcentration(sample.ESP, sample.Thickness, thickness, double.NaN, allowMissingValues: true);
                if (sample.OC != null)
                    sample.OC = MapConcentration(sample.OC, sample.Thickness, thickness, double.NaN, allowMissingValues: true);
                if (sample.PH != null)
                    sample.PH = MapConcentration(sample.PH, sample.Thickness, thickness, double.NaN, allowMissingValues: true);
                sample.Thickness = thickness;
            }
        }

        /// <summary>Convert the crop to the specified thickness. Ensures LL is between AirDry and DUL.</summary>
        /// <param name="crop">The crop to convert</param>
        /// <param name="thickness">The thicknesses to convert the crop to.</param>
        /// <param name="soil">The soil the crop belongs to.</param>
        private static void SetCropThickness(SoilCrop crop, double[] thickness, Soil soil)
        {
            if (!MathUtilities.AreEqual(thickness, soil.Thickness))
            {
                crop.LL = MapConcentration(crop.LL, soil.Thickness, thickness, MathUtilities.LastValue(crop.LL));
                crop.KL = MapConcentration(crop.KL, soil.Thickness, thickness, MathUtilities.LastValue(crop.KL));
                crop.XF = MapConcentration(crop.XF, soil.Thickness, thickness, MathUtilities.LastValue(crop.XF));

                crop.LL = MathUtilities.Constrain(crop.LL, AirDryMapped(soil, thickness), DULMapped(soil, thickness));
            }
        }

        /// <summary>Map soil variables (using concentration) from one layer structure to another.</summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="defaultValueForBelowProfile">The default value for below profile.</param>
        /// <param name="allowMissingValues">Tolerate missing values (double.NaN)?</param>
        /// <returns></returns>
        public static double[] MapConcentration(double[] fromValues, double[] fromThickness,
                                                  double[] toThickness,
                                                  double defaultValueForBelowProfile,
                                                  bool allowMissingValues = false)
        {
            if (fromValues != null)
            {
                if (fromValues.Length != fromThickness.Length)
                    throw new Exception("In MapConcentraction, the number of values doesn't match the number of thicknesses.");
                if (fromValues == null || fromThickness == null)
                    return null;

                // convert from values to a mass basis with a dummy bottom layer.
                List<double> values = new List<double>();
                List<double> thickness = new List<double>();
                for (int i = 0; i < fromValues.Length; i++)
                {
                    if (!allowMissingValues && double.IsNaN(fromValues[i]))
                        break;

                    values.Add(fromValues[i]);
                    thickness.Add(fromThickness[i]);
                }

                values.Add(defaultValueForBelowProfile);
                thickness.Add(3000);
                double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

                double[] newValues = MapMass(massValues, thickness.ToArray(), toThickness, allowMissingValues);

                // Convert mass back to concentration and return
                return MathUtilities.Divide(newValues, toThickness);
            }
            return null;
        }

        /// <summary>Map soil variables (using BD) from one layer structure to another.</summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="soil">The soil.</param>
        /// <param name="defaultValueForBelowProfile">The default value for below profile.</param>
        /// <returns></returns>
        private static double[] MapUsingBD(double[] fromValues, double[] fromThickness,
                                           double[] toThickness,
                                           Soil soil,
                                           double defaultValueForBelowProfile)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            // create an array of values with a dummy bottom layer.
            List<double> values = new List<double>();
            values.AddRange(fromValues);
            values.Add(defaultValueForBelowProfile);
            List<double> thickness = new List<double>();
            thickness.AddRange(fromThickness);
            thickness.Add(3000);

            // convert fromValues to a mass basis
            double[] BD = BDMapped(soil, fromThickness);
            for (int Layer = 0; Layer < values.Count; Layer++)
                values[Layer] = values[Layer] * BD[Layer] * fromThickness[Layer] / 100;

            // change layer structure
            double[] newValues = MapMass(values.ToArray(), thickness.ToArray(), toThickness);

            // convert newValues back to original units and return
            BD = BDMapped(soil, toThickness);
            for (int Layer = 0; Layer < newValues.Length; Layer++)
                newValues[Layer] = newValues[Layer] * 100.0 / BD[Layer] / toThickness[Layer];
            return newValues;
        }

        /// <summary>Map soil water from one layer structure to another.</summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="soil">The soil.</param>
        /// <returns></returns>
        public static double[] MapSW(double[] fromValues, double[] fromThickness, double[] toThickness, Soil soil)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            var waterNode = Apsim.Child(soil, typeof(Physical)) as Physical;

            // convert from values to a mass basis with a dummy bottom layer.
            List<double> values = new List<double>();
            values.AddRange(fromValues);
            values.Add(MathUtilities.LastValue(fromValues) * 0.8);
            values.Add(MathUtilities.LastValue(fromValues) * 0.4);
            values.Add(0.0);
            List<double> thickness = new List<double>();
            thickness.AddRange(fromThickness);
            thickness.Add(MathUtilities.LastValue(fromThickness));
            thickness.Add(MathUtilities.LastValue(fromThickness));
            thickness.Add(3000);

            // Get the first crop ll or ll15.
            double[] LowerBound;
            if (waterNode != null && soil.Crops.Count > 0)
                LowerBound = LLMapped(soil.Crops[0] as SoilCrop, thickness.ToArray());
            else
                LowerBound = LL15Mapped(soil, thickness.ToArray());
            if (LowerBound == null)
                throw new Exception("Cannot find crop lower limit or LL15 in soil");

            // Make sure all SW values below LastIndex don't go below CLL.
            int bottomLayer = fromThickness.Length - 1;
            for (int i = bottomLayer + 1; i < thickness.Count; i++)
                values[i] = Math.Max(values[i], LowerBound[i]);

            double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

            // Convert mass back to concentration and return
            double[] newValues = MathUtilities.Divide(MapMass(massValues, thickness.ToArray(), toThickness), toThickness);



            return newValues;
        }

        /// <summary>Map soil variables from one layer structure to another.</summary>
        /// <param name="fromValues">The f values.</param>
        /// <param name="fromThickness">The f thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="allowMissingValues">Tolerate missing values (double.NaN)?</param>
        /// <returns>The from values mapped to the specified thickness</returns>
        public static double[] MapMass(double[] fromValues, double[] fromThickness, double[] toThickness,
                                       bool allowMissingValues = false)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            double[] FromThickness = MathUtilities.RemoveMissingValuesFromBottom((double[])fromThickness.Clone());
            double[] FromValues = (double[])fromValues.Clone();

            if (FromValues == null)
                return null;

            if (!allowMissingValues)
            {
                // remove missing layers.
                for (int i = 0; i < FromValues.Length; i++)
                {
                    if (double.IsNaN(FromValues[i]) || i >= FromThickness.Length || double.IsNaN(FromThickness[i]))
                    {
                        FromValues[i] = double.NaN;
                        if (i == FromThickness.Length)
                            Array.Resize(ref FromThickness, i + 1);
                        FromThickness[i] = double.NaN;
                    }
                }
                FromValues = MathUtilities.RemoveMissingValuesFromBottom(FromValues);
                FromThickness = MathUtilities.RemoveMissingValuesFromBottom(FromThickness);
            }

            if (MathUtilities.AreEqual(FromThickness, toThickness))
                return FromValues;

            if (FromValues.Length != FromThickness.Length)
                return null;

            // Remapping is achieved by first constructing a map of
            // cumulative mass vs depth
            // The new values of mass per layer can be linearly
            // interpolated back from this shape taking into account
            // the rescaling of the profile.

            double[] CumDepth = new double[FromValues.Length + 1];
            double[] CumMass = new double[FromValues.Length + 1];
            CumDepth[0] = 0.0;
            CumMass[0] = 0.0;
            for (int Layer = 0; Layer < FromThickness.Length; Layer++)
            {
                CumDepth[Layer + 1] = CumDepth[Layer] + FromThickness[Layer];
                CumMass[Layer + 1] = CumMass[Layer] + FromValues[Layer];
            }

            //look up new mass from interpolation pairs
            double[] ToMass = new double[toThickness.Length];
            for (int Layer = 1; Layer <= toThickness.Length; Layer++)
            {
                double LayerBottom = MathUtilities.Sum(toThickness, 0, Layer, 0.0);
                double LayerTop = LayerBottom - toThickness[Layer - 1];
                bool DidInterpolate;
                double CumMassTop = MathUtilities.LinearInterpReal(LayerTop, CumDepth,
                    CumMass, out DidInterpolate);
                double CumMassBottom = MathUtilities.LinearInterpReal(LayerBottom, CumDepth,
                    CumMass, out DidInterpolate);
                ToMass[Layer - 1] = CumMassBottom - CumMassTop;
            }

            if (!allowMissingValues)
            {
                for (int i = 0; i < ToMass.Length; i++)
                    if (double.IsNaN(ToMass[i]))
                        ToMass[i] = 0.0;
            }

            return ToMass;
        }

        /// <summary>AirDry - mapped to the specified layer structure. Units: mm/mm        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] AirDryMapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.WaterNode.AirDry, soil.Thickness, ToThickness, soil.WaterNode.AirDry.Last());
        }

        /// <summary>Crop lower limit - mapped to the specified layer structure. Units: mm/mm        /// </summary>
        /// <param name="crop">The crop.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] LLMapped(SoilCrop crop, double[] ToThickness)
        {
            var waterNode = crop.Parent as Physical;
            return MapConcentration(crop.LL, waterNode.Thickness, ToThickness, MathUtilities.LastValue(crop.LL));
        }

        /// <summary>Bulk density - mapped to the specified layer structure. Units: mm/mm</summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public static double[] BDMapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.BD, soil.Thickness, ToThickness, soil.BD.Last());
        }

        /// <summary>Lower limit 15 bar - mapped to the specified layer structure. Units: mm/mm        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public static double[] LL15Mapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.LL15, soil.Thickness, ToThickness, soil.LL15.Last());
        }

        /// <summary>Drained upper limit - mapped to the specified layer structure. Units: mm/mm        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public static double[] DULMapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.WaterNode.DUL, soil.Thickness, ToThickness, soil.WaterNode.DUL.Last());
        }
    }
}
