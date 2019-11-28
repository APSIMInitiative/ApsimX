namespace UnitTests.Soils
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils;
    using Models.WaterModel;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnitTests.Surface;
    using UnitTests.Weather;

    [TestFixture]
    public class SoilsTests
    {
        /// <summary>Test setup routine. Returns a soil properties that can be used for testing.</summary>
        public static Soil Setup()
        {
            var soil = new Soil
            {
                Children = new List<Model>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                        BD = new double[] { 1.36, 1.216, 1.24, 1.32, 1.372, 1.368 },
                        AirDry = new double[] { 0.135, 0.214, 0.261, 0.261, 0.261, 0.261 },
                        LL15 = new double[] { 0.27, 0.267, 0.261, 0.261, 0.261, 0.261 },
                        DUL = new double[] { 0.365, 0.461, 0.43, 0.412, 0.402, 0.404 },
                        SAT = new double[] { 0.400, 0.481, 0.45, 0.432, 0.422, 0.424 },

                        Children = new List<Model>()
                        {
                            new SoilCrop
                            {
                                Name = "Wheat",
                                KL = new double[] { 0.06, 0.060, 0.060, 0.060, 0.060, 0.060 },
                                LL = new double[] { 0.27, 0.267, 0.261, 0.315, 0.402, 0.402 }
                            }
                        }
                    },
                    new Organic
                    {
                        Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                        Carbon = new double[] { 2, 1, 0.5, 0.4, 0.3, 0.2 }
                    },
                    new Chemical
                    {
                        Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                        CL = new double[] { 38, double.NaN, 500, 490, 500, 500 }
                    },
                    new Sample
                    {
                        Thickness = new double[] { 100, 300, 300, 300 },
                        SW = new double[] { 0.103, 0.238, 0.253, 0.247 },
                        OC = new double[] { 1.35, double.NaN, double.NaN, double.NaN },
                        SWUnits = Sample.SWUnitsEnum.Gravimetric
                    },
                    new Sample
                    {
                        Thickness = new double[] { 100, 300 },
                        NO3N = new double[] { 23, 7 },
                        OC = new double[] { 1.35, 1.4 },
                        SWUnits = Sample.SWUnitsEnum.Volumetric
                    }
                }
            };

            return soil;
        }

        ///// <summary>Test soil water layer structure conversion and mapping.</summary>
        //[Test]
        //public void TestLayerStructure()
        //{
        //    APSIM.Shared.Soils.Soil soil = Setup();

        //    // convert sw from gravimetric to volumetric.
        //    APSIMReadySoil.Create(soil); 

        //    // Make sure the samples have been removed.
        //    Assert.AreEqual(soil.Samples.Count, 0);

        //    // Check the SW values have been converted and that the bottom two layers are at CLL (0.402)
        //    MathUtilities.AreEqual(soil.Water.SW, new double[] { 0.140, 0.289, 0.314, 0.326, 0.402, 0.402 });

        //    // Check the NO3 values haven't been converted and that the bottom layers have default values
        //    MathUtilities.AreEqual(soil.Nitrogen.NO3, new double[] { 23, 7, 2, 1, 0.01, 0.01 });

        //    // Check that the OC sample values have been put on top of the SoilOrganicMatter OC values.
        //    MathUtilities.AreEqual(soil.SoilOrganicMatter.OC, new double[] { 1.35, 1, 0.5, 0.4, 0.3, 0.2 });

        //    // Make sure the analysis missing value has been replaced with zero.
        //    MathUtilities.AreEqual(soil.Analysis.CL, new double[] { 38, 0.0, 500, 490, 500, 500 });

        //    // Make sure that no predicted crops have been added.
        //    string[] cropNames = soil.Water.Crops.Select(c => c.Name).ToArray();
        //    Assert.AreEqual(cropNames.Length, 1);
        //}

        ///// <summary>Test soil water layer structure conversion and mapping.</summary>
        //[Test]
        //public void TestPredictedCrops()
        //{
        //    APSIM.Shared.Soils.Soil soil = Setup();
        //    soil.SoilType = "Black vertosol";

        //    APSIMReadySoil.Create(soil);

        //    // Make sure that predicted crops have been added.
        //    Assert.AreEqual(soil.Water.Crops.Count, 3);
        //    Assert.AreEqual(soil.Water.Crops[0].Name, "Wheat");
        //    Assert.AreEqual(soil.Water.Crops[1].Name, "Sorghum");
        //    Assert.AreEqual(soil.Water.Crops[2].Name, "Cotton");

        //    // Change soil type to a grey vertosol and examine the predicted crops.

        //    soil.SoilType = "Grey vertosol";
        //    soil.Water.Crops.Clear();
        //    APSIMReadySoil.Create(soil);
        //    Assert.AreEqual(soil.Water.Crops.Count, 7);
        //    Assert.AreEqual(soil.Water.Crops[0].Name, "Wheat");
        //    Assert.AreEqual(soil.Water.Crops[1].Name, "Sorghum");
        //    Assert.AreEqual(soil.Water.Crops[2].Name, "Cotton");
        //    Assert.AreEqual(soil.Water.Crops[3].Name, "Barley");
        //    Assert.AreEqual(soil.Water.Crops[4].Name, "Chickpea");
        //    Assert.AreEqual(soil.Water.Crops[5].Name, "Fababean");
        //    Assert.AreEqual(soil.Water.Crops[6].Name, "Mungbean");
        //}

        ///// <summary>Test initial water removal.</summary>
        //[Test]
        //public void TestInitialWater()
        //{
        //    APSIM.Shared.Soils.Soil soil = Setup();
        //    soil.Samples[0].SW = null;
        //    soil.InitialWater = new InitialWater();
        //    soil.InitialWater.PercentMethod = InitialWater.PercentMethodEnum.FilledFromTop;
        //    soil.InitialWater.RelativeTo = "LL15";
        //    soil.InitialWater.FractionFull = 0.5;

        //    APSIMReadySoil.Create(soil);

        //    // Make sure the initial water has been removed.
        //    Assert.IsNull(soil.InitialWater);

        //    // Check the SW values.
        //    MathUtilities.AreEqual(soil.Water.SW, new double[] { 0.365, 0.461, 0.43, 0.281, 0.261, 0.261 });

        //    // check evenly distributed method.
        //    soil.InitialWater = new InitialWater();
        //    soil.InitialWater.PercentMethod = InitialWater.PercentMethodEnum.EvenlyDistributed;
        //    soil.InitialWater.RelativeTo = "LL15";
        //    soil.InitialWater.FractionFull = 0.5;
        //    APSIMReadySoil.Create(soil);
        //    MathUtilities.AreEqual(soil.Water.SW, new double[] { 0.318, 0.364, 0.345, 0.336, 0.332, 0.333 });
        //}

        ///// <summary>Check that a user specified layer structure works.</summary>
        //[Test]
        //public void CheckUserLayerStructure()
        //{
        //    APSIM.Shared.Soils.Soil soil = Setup();
        //    soil.LayerStructure = new LayerStructure();
        //    soil.LayerStructure.Thickness = new double[]  { 200, 200, 200, 200 };

        //    APSIMReadySoil.Create(soil);

        //    MathUtilities.AreEqual(soil.Water.Thickness, new double[] { 200, 200, 200, 200 });
        //}

        ///// <summary>Test that lateral flow works.</summary>
        //[Test]
        //public void TestLateralFlow()
        //{
        //    APSIM.Shared.Soils.Soil soilProperties = Setup();
        //    soilProperties.Samples.Clear();

        //    // Setup our objects with links.
        //    SoilModel soil = new SoilModel();
        //    LateralFlowModel lateralFlow = new LateralFlowModel();

        //    SetLink(soil, "properties", soilProperties);
        //    SetLink(soil, "lateralFlowModel", lateralFlow);
        //    SetLink(lateralFlow, "soil", soil);

        //    // Set initial water to full.
        //    soilProperties.InitialWater = new InitialWater();
        //    soilProperties.InitialWater.FractionFull = 1.0;
        //    APSIMReadySoil.Create(soilProperties);
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.SW, soilProperties.Water.Thickness);

        //    // No inflow, so there should be no outflow.
        //    lateralFlow.InFlow = null;
        //    Assert.AreEqual(lateralFlow.Values.Length, 0);

        //    // Profile is full so adding in flow will produce out flow.
        //    lateralFlow.InFlow = new double[] { 9, 9, 9, 9, 9, 9 };
        //    lateralFlow.KLAT = MathUtilities.CreateArrayOfValues(8.0, soilProperties.Water.Thickness.Length);
        //    Assert.IsTrue(MathUtilities.AreEqual(lateralFlow.Values, new double[] { 0.45999, 0.80498, 0.80498, 0.80498, 0.80498, 0.80498 }));

        //    // Set initial water to empty. Out flow should be zeros.
        //    soilProperties.InitialWater = new InitialWater();
        //    soilProperties.InitialWater.FractionFull = 0.0;
        //    APSIMReadySoil.Create(soilProperties);
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.SW, soilProperties.Water.Thickness);
        //    Assert.IsTrue(MathUtilities.AreEqual(lateralFlow.Values, new double[] { 0, 0, 0, 0, 0, 0 }));
        //}

        ///// <summary>Test that runoff is working.</summary>
        //[Test]
        //public void TestRunoff()
        //{
        //    SoilModel soil = new SoilModel();

        //    APSIM.Shared.Soils.Soil soilProperties = Setup();
        //    APSIMReadySoil.Create(soilProperties);

        //    MockWeather weather = new MockWeather();
        //    weather.Rain = 100;

        //    MockIrrigation irrigation = new MockIrrigation();
        //    irrigation.IrrigationApplied = 0;

        //    MockSurfaceOrganicMatter surfaceOrganicMatter = new MockSurfaceOrganicMatter();
        //    surfaceOrganicMatter.Cover = 0.1;

        //    CNReductionForCover reductionForCover = new CNReductionForCover();
        //    List<ICanopy> canopies = new List<ICanopy>();

        //    CNReductionForTillage reductionForTillage = new CNReductionForTillage();

        //    RunoffModel runoff = new RunoffModel();
        //    runoff.CN2Bare = 70;

        //    // setup links
        //    SetLink(soil, "properties", soilProperties);
        //    SetLink(soil, "runoffModel", runoff);
        //    SetLink(soil, "weather", weather);
        //    SetLink(soil, "irrigation", irrigation);
        //    SetLink(runoff, "soil", soil);
        //    SetLink(runoff, "reductionForCover", reductionForCover);
        //    SetLink(runoff, "reductionForTillage", reductionForTillage);
        //    SetLink(reductionForCover, "surfaceOrganicMatter", surfaceOrganicMatter);
        //    SetLink(reductionForCover, "canopies", canopies);
        //    SetLink(reductionForTillage, "weather", weather);

        //    // Empty profile.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.LL15, soilProperties.Water.Thickness);

        //    // Profile is empty - should be small amount of runoff.
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(runoff.Value(), 5.60815));

        //    // Full profile - should be a lot more runoff.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.DUL, soilProperties.Water.Thickness);
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(runoff.Value(), 58.23552));

        //    // Test CN reduction due to canopy. Tests the Curve Number vs Cover graph.
        //    // Cover is 10%, reduction is 2.5
        //    surfaceOrganicMatter.Cover = 0.1;
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(reductionForCover.Value(), 2.49999));

        //    // Cover is 80%, reduction is 20
        //    surfaceOrganicMatter.Cover = 0.8;
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(reductionForCover.Value(), 20.0));

        //    // Test Runoff vs Rainfall graph i.e. effect of different curve numbers.
        //    surfaceOrganicMatter.Cover = 0.0;
        //    runoff.CN2Bare = 60;
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(runoff.Value(), 48.18584));

        //    runoff.CN2Bare = 75;
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(runoff.Value(), 68.16430));

        //    runoff.CN2Bare = 85;
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(runoff.Value(), 81.15006));
        //}

        ///// <summary>Ensure saturated flow is working.</summary>
        //[Test]
        //public void TestSaturatedFlow()
        //{
        //    SoilModel soil = new SoilModel();

        //    APSIM.Shared.Soils.Soil soilProperties = Setup();
        //    APSIMReadySoil.Create(soilProperties);

        //    SaturatedFlowModel saturatedFlow = new SaturatedFlowModel();
        //    SetLink(soil, "properties", soilProperties);
        //    SetLink(saturatedFlow, "soil", soil);

        //    saturatedFlow.SWCON = new double[] { 0.3, 0.3, 0.3, 0.3, 0.3, 0.3 };

        //    // Profile at DUL.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.DUL, soilProperties.Water.Thickness);
        //    double[] flux = saturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flux, new double[] { 0, 0, 0, 0, 0, 0 }));

        //    // Profile at SAT.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.SAT, soilProperties.Water.Thickness);
        //    flux = saturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flux, new double[] { 1.05, 2.85, 4.64999, 6.45, 8.25, 10.05 }));

        //    // Use the KS method
        //    soilProperties.Water.KS = new double[] {1000, 300, 20, 100, 100, 100 };
        //    flux = saturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flux, new double[] { 1.05, 1.8000, 1.8000, 1.8000, 1.8000, 1.8000 }));
        //    Assert.AreEqual(saturatedFlow.backedUpSurface, 0);

        //    // Use the KS method, water above SAT.
        //    soilProperties.Water.KS = new double[] { 1000, 300, 20, 100, 100, 100 };
        //    MathUtilities.AddValue(soil.Water, 10); // add 5 mm of water into each layer.
        //    flux = saturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flux, new double[] { 1.05, 1.8000, 1.8000, 1.8000, 1.8000, 1.8000 }));

        //}

        ///// <summary>Ensure evaporation is working.</summary>
        //[Test]
        //public void TestEvaporation()
        //{
        //    MockSoil soil = new MockSoil();

        //    APSIM.Shared.Soils.Soil soilProperties = Setup();
        //    APSIMReadySoil.Create(soilProperties);

        //    MockClock clock = new MockClock();
        //    clock.Today = new DateTime(2015, 6, 1);

        //    MockWeather weather = new MockWeather();
        //    weather.MaxT = 30;
        //    weather.MinT = 10;
        //    weather.Rain = 100;
        //    weather.Radn = 25;

        //    MockSurfaceOrganicMatter surfaceOrganicMatter = new MockSurfaceOrganicMatter();
        //    surfaceOrganicMatter.Cover = 0.8;

        //    List<ICanopy> canopies = new List<ICanopy>();

        //    EvaporationModel evaporation = new EvaporationModel();
        //    SetLink(soil, "properties", soilProperties);
        //    SetLink(evaporation, "soil", soil);
        //    SetLink(evaporation, "clock", clock);
        //    SetLink(evaporation, "weather", weather);
        //    SetLink(evaporation, "canopies", canopies);
        //    SetLink(evaporation, "surfaceOrganicMatter", surfaceOrganicMatter);

        //    // Empty profile.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.LL15, soilProperties.Water.Thickness);

        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 3.00359));

        //    soil.Infiltration = 0;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 2.20072));

        //    soil.Infiltration = 0;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 1.57064));

        //    soil.Infiltration = 0;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 0.96006));

        //    soil.Infiltration = 0;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 0.75946));

        //    soil.Infiltration = 0;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 0.64851));

        //    soil.Infiltration = 100;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 3.00359));

        //    soil.Infiltration = 0;
        //    evaporation.Calculate();
        //    Assert.IsTrue(MathUtilities.FloatsAreEqual(evaporation.Es, 2.20072));

        //}

        ///// <summary>Ensure unsaturated flow is working.</summary>
        //[Test]
        //public void TestUnsaturatedFlow()
        //{
        //    SoilModel soil = new SoilModel();

        //    APSIM.Shared.Soils.Soil soilProperties = Setup();
        //    APSIMReadySoil.Create(soilProperties);

        //    UnsaturatedFlowModel unsaturatedFlow = new UnsaturatedFlowModel();
        //    SetLink(soil, "properties", soilProperties);
        //    SetLink(unsaturatedFlow, "soil", soil);

        //    unsaturatedFlow.DiffusConst = 88;
        //    unsaturatedFlow.DiffusSlope = 35.4;

        //    // Profile at DUL.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.DUL, soilProperties.Water.Thickness);
        //    double[] flow = unsaturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flow, new double[] { 0, 0, 0, 0, 0, 0 }));

        //    // Profile at SAT.
        //    soil.Water = MathUtilities.Multiply(soilProperties.Water.SAT, soilProperties.Water.Thickness);
        //    flow = unsaturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flow, new double[] { 0, 0, 0, 0, 0, 0 }));

        //    // Force some unsaturated flow by reducing the water to 0.8 of SAT.
        //    soil.Water = MathUtilities.Multiply_Value(soil.Water, 0.8);
        //    flow = unsaturatedFlow.Values;
        //    Assert.IsTrue(MathUtilities.AreEqual(flow, new double[] { 0.52148, -0.38359, -0.16771, -0.07481, 0, 0 }));

        //}

        ///// <summary>Ensure water table is working.</summary>
        //[Test]
        //public void TestWaterTable()
        //{
        //    SoilModel soil = new SoilModel();

        //    APSIM.Shared.Soils.Soil soilProperties = Setup();
        //    APSIMReadySoil.Create(soilProperties);

        //    WaterTableModel waterTable = new WaterTableModel();
        //    SetLink(soil, "properties", soilProperties);
        //    SetLink(waterTable, "soil", soil);

        //    double[] DUL = MathUtilities.Multiply(soilProperties.Water.DUL, soilProperties.Water.Thickness);
        //    double[] SAT = MathUtilities.Multiply(soilProperties.Water.SAT, soilProperties.Water.Thickness);

        //    // Profile at DUL. Essentially water table is below profile.
        //    soil.Water = DUL;
        //    Assert.AreEqual(waterTable.Value(), 1600);

        //    // Put a saturated layer at index 3.
        //    soil.Water[3] = SAT[3];
        //    Assert.AreEqual(waterTable.Value(), 700);

        //    // Put a saturated layer at index 3 and a drainable layer at index 2.
        //    soil.Water[2] = (DUL[2] + SAT[2]) / 2;
        //    soil.Water[3] = SAT[3];
        //    Assert.AreEqual(waterTable.Value(), 250);


        //}

    }
}
