namespace Models.Soils.Standardiser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;

    /// <summary>Methods to standardise a soil ready for running in APSIM.</summary>
    [Serializable]
    public class Layers
    {
        /// <summary>Standardise the specified soil with a uniform thickness.</summary>
        /// <param name="soil">The soil.</param>
        /// <returns>A standardised soil.</returns>
        public static void Standardise(Soil soil)
        {
            var physicalNode = soil.FindChild<IPhysical>();
            var analysisNode = soil.FindChild<Chemical>();
            var layerStructure = soil.FindChild<LayerStructure>();
            var weirdo = soil.FindChild<WEIRDO>();
            var organicNode = soil.FindChild<Organic>();
            var waterBalance = soil.FindChild<ISoilWater>();

            // Determine the target layer structure.
            var targetThickness = physicalNode.Thickness;
            if (layerStructure != null)
                targetThickness = layerStructure.Thickness;

            foreach (Sample sample in soil.FindAllChildren<Sample>())
                SetSampleThickness(sample, targetThickness, soil);

            if (waterBalance is WaterModel.WaterBalance)
                SetSoilWaterThickness(waterBalance as WaterModel.WaterBalance, targetThickness);
            if (weirdo != null)
                weirdo.SetLayerThickness(targetThickness);

            SetAnalysisThickness(analysisNode, targetThickness);
            SetSoilOrganicMatterThickness(organicNode, targetThickness);
            SetPhysicalPropertiesThickness(physicalNode, targetThickness, soil);
        }

        /// <summary>Sets the water thickness.</summary>
        /// <param name="physical">The water.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="soil">Soil</param>
        private static void SetPhysicalPropertiesThickness(IPhysical physical, double[] toThickness, Soil soil)
        {
            if (!MathUtilities.AreEqual(toThickness, physical.Thickness))
            {
                var crops = (physical as IModel).FindAllChildren<SoilCrop>();
                foreach (var crop in crops)
                {
                    crop.KL = MapConcentration(crop.KL, physical.Thickness, toThickness, MathUtilities.LastValue(crop.KL));
                    crop.XF = MapConcentration(crop.XF, physical.Thickness, toThickness, MathUtilities.LastValue(crop.XF));
                    crop.LL = MapConcentration(crop.LL, physical.Thickness, toThickness, MathUtilities.LastValue(crop.LL));
                }

                physical.BD = MapConcentration(physical.BD, physical.Thickness, toThickness, MathUtilities.LastValue(physical.BD));
                physical.AirDry = MapConcentration(physical.AirDry, physical.Thickness, toThickness, MathUtilities.LastValue(physical.AirDry));
                physical.LL15 = MapConcentration(physical.LL15, physical.Thickness, toThickness, MathUtilities.LastValue(physical.LL15));
                physical.DUL = MapConcentration(physical.DUL, physical.Thickness, toThickness, MathUtilities.LastValue(physical.DUL));
                physical.SAT = MapConcentration(physical.SAT, physical.Thickness, toThickness, MathUtilities.LastValue(physical.SAT));
                physical.KS = MapConcentration(physical.KS, physical.Thickness, toThickness, MathUtilities.LastValue(physical.KS));
                if (physical != null)
                    if (physical.ParticleSizeClay != null && physical.ParticleSizeClay.Length == physical.Thickness.Length)
                        physical.ParticleSizeClay = MapConcentration(physical.ParticleSizeClay, physical.Thickness, toThickness, MathUtilities.LastValue(physical.ParticleSizeClay));
                    else
                        physical.ParticleSizeClay = null;
                physical.Thickness = toThickness;

                foreach (var crop in crops)
                {
                    var soilCrop = crop as SoilCrop;
                    // Ensure crop LL are between Airdry and DUL.
                    for (int i = 0; i < soilCrop.LL.Length; i++)
                        soilCrop.LL = MathUtilities.Constrain(soilCrop.LL, physical.AirDry, physical.DUL);
                }
            }
        }

        /// <summary>Sets the soil water thickness.</summary>
        /// <param name="soilWater">The soil water.</param>
        /// <param name="thickness">Thickness to change soil water to.</param>
        private static void SetSoilWaterThickness(WaterModel.WaterBalance soilWater, double[] thickness)
        {
            if (soilWater != null)
            {
                if (!MathUtilities.AreEqual(thickness, soilWater.Thickness))
                {
                    soilWater.KLAT = MapConcentration(soilWater.KLAT, soilWater.Thickness, thickness, MathUtilities.LastValue(soilWater.KLAT));
                    soilWater.SWCON = MapConcentration(soilWater.SWCON, soilWater.Thickness, thickness, 0.0);

                    soilWater.Thickness = thickness;
                }
                if (soilWater.SWCON == null)
                    soilWater.SWCON = MathUtilities.CreateArrayOfValues(0.3, soilWater.Thickness.Length);
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
                if (sample.NH4 != null)
                    sample.NH4 = MapConcentration(sample.NH4, sample.Thickness, thickness, 0.2);
                if (sample.NO3 != null)
                    sample.NO3 = MapConcentration(sample.NO3, sample.Thickness, thickness, 1.0);

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
        /// <param name="physical">The soil the crop belongs to.</param>
        private static void SetCropThickness(SoilCrop crop, double[] thickness, IPhysical physical)
        {
            if (!MathUtilities.AreEqual(thickness, physical.Thickness))
            {
                crop.LL = MapConcentration(crop.LL, physical.Thickness, thickness, MathUtilities.LastValue(crop.LL));
                crop.KL = MapConcentration(crop.KL, physical.Thickness, thickness, MathUtilities.LastValue(crop.KL));
                crop.XF = MapConcentration(crop.XF, physical.Thickness, thickness, MathUtilities.LastValue(crop.XF));

                crop.LL = MathUtilities.Constrain(crop.LL, AirDryMapped(physical, thickness), DULMapped(physical, thickness));
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
                    throw new Exception($"In MapConcentration, the number of values ({fromValues.Length}) doesn't match the number of thicknesses ({fromThickness.Length}).");
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
                thickness.Add(30000);
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
            var soilPhysical = soil.FindChild<Soils.IPhysical>();

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
            double[] BD = BDMapped(soilPhysical, fromThickness);
            for (int Layer = 0; Layer < values.Count; Layer++)
                values[Layer] = values[Layer] * BD[Layer] * fromThickness[Layer] / 100;

            // change layer structure
            double[] newValues = MapMass(values.ToArray(), thickness.ToArray(), toThickness);

            // convert newValues back to original units and return
            BD = BDMapped(soilPhysical, toThickness);
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

            var physical = soil.FindChild<IPhysical>();

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
            var firstCrop = soil.FindChild<SoilCrop>();
            double[] LowerBound;
            if (physical != null && firstCrop != null)
                LowerBound = LLMapped(firstCrop, thickness.ToArray());
            else
                LowerBound = LL15Mapped(physical, thickness.ToArray());
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
        /// <param name="physical">The soil physical properties.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] AirDryMapped(IPhysical physical, double[] ToThickness)
        {
            return MapConcentration(physical.AirDry, physical.Thickness, ToThickness, physical.AirDry.Last());
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
        /// <param name="physical">The soil physical properties.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public static double[] BDMapped(IPhysical physical, double[] ToThickness)
        {
            return MapConcentration(physical.BD, physical.Thickness, ToThickness, physical.BD.Last());
        }

        /// <summary>Lower limit 15 bar - mapped to the specified layer structure. Units: mm/mm        /// </summary>
        /// <param name="physical">The soil physical properties.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public static double[] LL15Mapped(IPhysical physical, double[] ToThickness)
        {
            return MapConcentration(physical.LL15, physical.Thickness, ToThickness, physical.LL15.Last());
        }

        /// <summary>Drained upper limit - mapped to the specified layer structure. Units: mm/mm        /// </summary>
        /// <param name="physical">The soil physical properties.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public static double[] DULMapped(IPhysical physical, double[] ToThickness)
        {
            return MapConcentration(physical.DUL, physical.Thickness, ToThickness, physical.DUL.Last());
        }
    }
}
