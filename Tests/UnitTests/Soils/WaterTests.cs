using System.Collections.Generic;
using APSIM.Core;
using Models.Core;
using Models.Soils;
using NUnit.Framework;

namespace UnitTests.Soils
{
    /// <summary>Tests for the Water class after links are resolved, as the GUI does.</summary>
    [TestFixture]
    public class WaterTests
    {
        private static Soil CreateSoil()
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
                        RelativeTo = "LL15"
                    }
                }
            };
            Node.Create(soil);
            new Links().Resolve(soil, true);
            return soil;
        }

        [Test]
        public void RelativeToLLUsesLL15AfterLinksResolved()
        {
            var water = CreateSoil().Node.FindChild<Water>();
            Assert.That(water.RelativeToLL, Is.EqualTo(new double[] { 0.27, 0.267 }));
        }

        [Test]
        public void RelativeToLLUsesCropLLAfterLinksResolved()
        {
            var water = CreateSoil().Node.FindChild<Water>();
            water.RelativeTo = "Wheat";
            Assert.That(water.RelativeToLL, Is.EqualTo(new double[] { 0.28, 0.29 }));
        }

        [Test]
        public void RelativeToLLThrowsWhenCropMissing()
        {
            var water = CreateSoil().Node.FindChild<Water>();
            Assert.Throws<ApsimXException>(() => water.RelativeTo = "Maize");
        }
    }
}
