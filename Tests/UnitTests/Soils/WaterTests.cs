using System.Collections.Generic;
using APSIM.Core;
using Models.Core;
using Models.Soils;
using NUnit.Framework;

namespace UnitTests.Soils
{
    /// <summary>Tests for the Water class when links have not been resolved (e.g. in the GUI).</summary>
    [TestFixture]
    public class WaterTests
    {
        private static Soil CreateSoil(string relativeTo)
        {
            var soil = new Soil
            {
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 100, 300 },
                        BD = new double[] { 1.36, 1.216 },
                        AirDry = new double[] { 0.135, 0.214 },
                        LL15 = new double[] { 0.27, 0.267 },
                        DUL = new double[] { 0.365, 0.461 },
                        SAT = new double[] { 0.400, 0.481 },
                        Children = new List<IModel>()
                        {
                            new SoilCrop
                            {
                                Name = "WheatSoil",
                                KL = new double[] { 0.06, 0.06 },
                                LL = new double[] { 0.28, 0.29 },
                                XF = new double[] { 1, 1 }
                            }
                        }
                    },
                    new Water
                    {
                        Thickness = new double[] { 100, 300 },
                        InitialValues = new double[] { 0.3, 0.3 },
                        RelativeTo = relativeTo
                    }
                }
            };
            Node.Create(soil);
            return soil;
        }

        [Test]
        public void RelativeToLLUsesLL15WithoutResolvedLinks()
        {
            var water = CreateSoil("LL15").Node.FindChild<Water>();
            Assert.That(water.RelativeToLL, Is.EqualTo(new double[] { 0.27, 0.267 }));
        }

        [Test]
        public void RelativeToLLUsesCropLLWithoutResolvedLinks()
        {
            var water = CreateSoil("Wheat").Node.FindChild<Water>();
            Assert.That(water.RelativeToLL, Is.EqualTo(new double[] { 0.28, 0.29 }));
        }

        [Test]
        public void RelativeToLLThrowsWhenCropMissing()
        {
            var water = CreateSoil("LL15").Node.FindChild<Water>();
            Assert.Throws<ApsimXException>(() => water.RelativeTo = "Maize");
        }

        [Test]
        public void RelativeToLLThrowsWhenPhysicalMissing()
        {
            var soil = new Soil
            {
                Children = new List<IModel>() { new Water { Thickness = new double[] { 100 }, RelativeTo = "LL15" } }
            };
            Node.Create(soil);
            var water = soil.Node.FindChild<Water>();
            Assert.Catch<ApsimXException>(() => { var _ = water.RelativeToLL; });
        }
    }
}
