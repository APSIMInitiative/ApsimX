using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using APSIM.Shared.Soils;
using APSIM.Shared.Utilities;

namespace Tests
{
    [TestFixture]
    public class Soils
    {
        /// <summary>Test setup routine.</summary>
        /// <returns>A soil that can be used for testing.</returns>
        private static Soil Setup()
        {
            Soil soil = new Soil();
            soil.Water = new Water();
            soil.Water.Thickness = new double[] { 100, 300, 300, 300, 300, 300 };
            soil.Water.BD = new double[] { 1.36, 1.216, 1.24, 1.32, 1.372, 1.368 };
            soil.Water.AirDry = new double[] { 0.135, 0.214, 0.261, 0.261, 0.261, 0.261 };
            soil.Water.LL15 = new double[] { 0.27, 0.267, 0.261, 0.261, 0.261, 0.261 };
            soil.Water.DUL = new double[] { 0.365, 0.461, 0.43, 0.412, 0.402, 0.404 };

            // Add a wheat crop.
            SoilCrop crop = new SoilCrop();
            crop.Thickness = soil.Water.Thickness;
            crop.Name = "Wheat";
            crop.KL = new double[] { 0.06, 0.060, 0.060, 0.060, 0.060, 0.060 };
            crop.LL = new double[] { 0.27, 0.267, 0.261, 0.315, 0.402, 0.402 };
            soil.Water.Crops = new List<SoilCrop>();
            soil.Water.Crops.Add(crop);

            // Add OC values into SoilOrganicMatter.
            soil.SoilOrganicMatter = new SoilOrganicMatter();
            soil.SoilOrganicMatter.Thickness = soil.Water.Thickness;
            soil.SoilOrganicMatter.OC = new double[] { 2, 1, 0.5, 0.4, 0.3, 0.2 };

            // Add in CL into analysis.
            soil.Analysis = new Analysis();
            soil.Analysis.Thickness = soil.Water.Thickness;
            soil.Analysis.CL = new double[] { 38, double.NaN, 500, 490, 500, 500 };

            // Add a sample.
            Sample sample = new Sample();
            sample.Thickness = new double[] { 100, 300, 300, 300 };
            sample.SW = new double[] { 0.103, 0.238, 0.253, 0.247 };
            sample.NO3 = new double[] { 23, 7, 2, 1 };
            sample.OC = new double[] { 1.35, double.NaN, double.NaN, double.NaN };
            sample.SWUnits = Sample.SWUnitsEnum.Gravimetric;
            soil.Samples = new List<Sample>();
            soil.Samples.Add(sample);
            return soil;
        }

        /// <summary>Test soil water layer structure conversion and mapping.</summary>
        [Test]
        public void TestLayerStructure()
        {
            Soil soil = Setup();

            // convert sw from gravimetric to volumetric.
            APSIMReadySoil.Create(soil); 

            // Make sure the samples have been removed.
            Assert.AreEqual(soil.Samples.Count, 0);

            // Check the SW values have been converted and that the bottom two layers are at CLL (0.402)
            MathUtilities.AreEqual(soil.Water.SW, new double[] { 0.140, 0.289, 0.314, 0.326, 0.402, 0.402 });

            // Check the NO3 values haven't been converted and that the bottom layers have default values
            MathUtilities.AreEqual(soil.Nitrogen.NO3, new double[] { 23, 7, 2, 1, 0.01, 0.01 });

            // Check that the OC sample values have been put on top of the SoilOrganicMatter OC values.
            MathUtilities.AreEqual(soil.SoilOrganicMatter.OC, new double[] { 1.35, 1, 0.5, 0.4, 0.3, 0.2 });

            // Make sure the analysis missing value has been replaced with zero.
            MathUtilities.AreEqual(soil.Analysis.CL, new double[] { 38, 0.0, 500, 490, 500, 500 });

            // Make sure that no predicted crops have been added.
            string[] cropNames = soil.Water.Crops.Select(c => c.Name).ToArray();
            Assert.AreEqual(cropNames.Length, 1);
        }
         
        /// <summary>Test soil water layer structure conversion and mapping.</summary>
        [Test]
        public void TestPredictedCrops()
        {
            Soil soil = Setup();
            soil.SoilType = "Black vertosol";

            APSIMReadySoil.Create(soil);

            // Make sure that predicted crops have been added.
            Assert.AreEqual(soil.Water.Crops.Count, 3);
            Assert.AreEqual(soil.Water.Crops[0].Name, "Wheat");
            Assert.AreEqual(soil.Water.Crops[1].Name, "Sorghum");
            Assert.AreEqual(soil.Water.Crops[2].Name, "Cotton");

            // Change soil type to a grey vertosol and examine the predicted crops.

            soil.SoilType = "Grey vertosol";
            soil.Water.Crops.Clear();
            APSIMReadySoil.Create(soil);
            Assert.AreEqual(soil.Water.Crops.Count, 7);
            Assert.AreEqual(soil.Water.Crops[0].Name, "Wheat");
            Assert.AreEqual(soil.Water.Crops[1].Name, "Sorghum");
            Assert.AreEqual(soil.Water.Crops[2].Name, "Cotton");
            Assert.AreEqual(soil.Water.Crops[3].Name, "Barley");
            Assert.AreEqual(soil.Water.Crops[4].Name, "Chickpea");
            Assert.AreEqual(soil.Water.Crops[5].Name, "Fababean");
            Assert.AreEqual(soil.Water.Crops[6].Name, "Mungbean");
        }

        /// <summary>Test initial water removal.</summary>
        [Test]
        public void TestInitialWater()
        {
            Soil soil = Setup();
            soil.Samples[0].SW = null;
            soil.InitialWater = new InitialWater();
            soil.InitialWater.PercentMethod = InitialWater.PercentMethodEnum.FilledFromTop;
            soil.InitialWater.RelativeTo = "LL15";
            soil.InitialWater.FractionFull = 0.5;

            APSIMReadySoil.Create(soil);

            // Make sure the initial water has been removed.
            Assert.IsNull(soil.InitialWater);

            // Check the SW values.
            MathUtilities.AreEqual(soil.Water.SW, new double[] { 0.365, 0.461, 0.43, 0.281, 0.261, 0.261 });

            // check evenly distributed method.
            soil.InitialWater = new InitialWater();
            soil.InitialWater.PercentMethod = InitialWater.PercentMethodEnum.EvenlyDistributed;
            soil.InitialWater.RelativeTo = "LL15";
            soil.InitialWater.FractionFull = 0.5;
            APSIMReadySoil.Create(soil);
            MathUtilities.AreEqual(soil.Water.SW, new double[] { 0.318, 0.364, 0.345, 0.336, 0.332, 0.333 });
        }

        /// <summary>Check that a user specified layer structure works.</summary>
        [Test]
        public void CheckUserLayerStructure()
        {
            Soil soil = Setup();
            soil.LayerStructure = new LayerStructure();
            soil.LayerStructure.Thickness = new double[]  { 200, 200, 200, 200 };

            APSIMReadySoil.Create(soil);

            MathUtilities.AreEqual(soil.Water.Thickness, new double[] { 200, 200, 200, 200 });
        }
    }
}
