namespace UnitTests.Soils
{
    using Models.Core;
    using Models.Soils;
    using Models.Soils.Standardiser;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class StandardiserTests
    {
        /// <summary>Ensure layer mapping works.</summary>
        [Test]
        public void LayerMappingWorks()
        {
            var soil = new Soil
            {
                Children = new List<Model>()
                {
                    new Water()
                    {
                        Thickness = new double[] { 100, 300, 300 },
                        BD = new double[] { 1.36, 1.216, 1.24 },
                        AirDry = new double[] { 0.135, 0.214, 0.261 },
                        LL15 = new double[] { 0.27, 0.267, 0.261 },
                        DUL = new double[] { 0.365, 0.461, 0.43 },
                        SAT = new double[] { 0.400, 0.481, 0.45 },

                        Children = new List<Model>()
                        {
                            new SoilCrop
                            {
                                Name = "Wheat",
                                KL = new double[] { 0.06, 0.060, 0.060 },
                                LL = new double[] { 0.27, 0.267, 0.261 }
                            }
                        }
                    },
                    new SoilOrganicMatter
                    {
                        Thickness = new double[] { 100, 300 },
                        OC = new double[] { 2, 1 }
                    },
                    new Analysis
                    {
                        Thickness = new double[] { 100, 200 },
                        CL = new double[] { 38, double.NaN }
                    },
                    new Sample
                    {
                        Thickness = new double[] { 500 },
                        SW = new double[] { 0.103 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Gravimetric
                    },
                    new Sample
                    {
                        Thickness = new double[] { 1000 },
                        NO3 = new double[] { 27 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Volumetric
                    }
                }
            };
            Apsim.ParentAllChildren(soil);
            soil.OnCreated();

            SoilStandardiser.Standardise(soil);

            var water = soil.Children[0] as Water;
            var soilOrganicMatter = soil.Children[1] as SoilOrganicMatter;
            var analysis = soil.Children[2] as Analysis;
            var sample = soil.Children[3] as Sample;

            // Make sure layer structures have been standardised.
            var targetThickness = new double[] { 100, 300, 300 };
            Assert.AreEqual(water.Thickness, targetThickness);
            Assert.AreEqual(soilOrganicMatter.Thickness, targetThickness);
            Assert.AreEqual(analysis.Thickness, targetThickness);
            Assert.AreEqual(sample.Thickness, targetThickness);

            // Make sure sample units are volumetric.
            Assert.AreEqual(sample.SWUnits, Sample.SWUnitsEnum.Volumetric);
        }

        /// <summary>Ensure a LayerStructure is used for mapping.</summary>
        [Test]
        public void LayerStructureIsUsedForMapping()
        {
            var soil = new Soil
            {
                Children = new List<Model>()
                {
                    new Water()
                    {
                        Thickness = new double[] { 100, 300, 300 },
                        BD = new double[] { 1.36, 1.216, 1.24 },
                        AirDry = new double[] { 0.135, 0.214, 0.261 },
                        LL15 = new double[] { 0.27, 0.267, 0.261 },
                        DUL = new double[] { 0.365, 0.461, 0.43 },
                        SAT = new double[] { 0.400, 0.481, 0.45 },

                        Children = new List<Model>()
                        {
                            new SoilCrop
                            {
                                Name = "Wheat",
                                KL = new double[] { 0.06, 0.060, 0.060 },
                                LL = new double[] { 0.27, 0.267, 0.261 }
                            }
                        }
                    },
                    new SoilOrganicMatter
                    {
                        Thickness = new double[] { 100, 300 },
                        OC = new double[] { 2, 1 }
                    },
                    new Analysis
                    {
                        Thickness = new double[] { 100, 200 },
                        CL = new double[] { 38, double.NaN }
                    },
                    new Sample
                    {
                        Thickness = new double[] { 500 },
                        SW = new double[] { 0.103 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Gravimetric
                    },
                    new Sample
                    {
                        Thickness = new double[] { 1000 },
                        NO3 = new double[] { 27 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Volumetric
                    },
                    new LayerStructure
                    {
                        Thickness = new double[] { 100, 300 }
                    }
                }
            };
            Apsim.ParentAllChildren(soil);
            soil.OnCreated();

            SoilStandardiser.Standardise(soil);

            var water = soil.Children[0] as Water;
            var soilOrganicMatter = soil.Children[1] as SoilOrganicMatter;
            var analysis = soil.Children[2] as Analysis;
            var sample = soil.Children[3] as Sample;

            // Make sure layer structures have been standardised.
            var targetThickness = new double[] { 100, 300 };
            Assert.AreEqual(water.Thickness, targetThickness);
            Assert.AreEqual(soilOrganicMatter.Thickness, targetThickness);
            Assert.AreEqual(analysis.Thickness, targetThickness);
            Assert.AreEqual(sample.Thickness, targetThickness);

            // Make sure sample units are volumetric.
            Assert.AreEqual(sample.SWUnits, Sample.SWUnitsEnum.Volumetric);
        }
    }
}
