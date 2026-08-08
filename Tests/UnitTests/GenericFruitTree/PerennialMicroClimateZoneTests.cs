using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models;
using Models.Core;
using Models.Interfaces;
using NUnit.Framework;

namespace UnitTests.Agroforestry
{
    [TestFixture]
    public class PerennialMicroClimateZoneTests
    {
        [Test]
        public void DoCanopyCompartments_UsesPreloadedLightProfileWhenAvailable()
        {
            TestCanopy targetCanopy = new TestCanopy
            {
                Height = 3000.0,
                Depth = 3000.0,
                LAI = 6.0,
                LAITotal = 7.5,
                CoverGreen = 0.7,
                CoverTotal = 0.8,
                LightProfile = new[]
                {
                    new CanopyEnergyBalanceInterceptionlayerType { thickness = 1.0, AmountOnGreen = 1.0, AmountOnDead = 0.5 },
                    new CanopyEnergyBalanceInterceptionlayerType { thickness = 1.0, AmountOnGreen = 2.0, AmountOnDead = 0.5 },
                    new CanopyEnergyBalanceInterceptionlayerType { thickness = 1.0, AmountOnGreen = 3.0, AmountOnDead = 0.5 }
                }
            };
            TestCanopy nodeCanopy = CreateLayerBoundaryCanopy();
            PerennialMicroClimateZone zone = CreateZone(targetCanopy, nodeCanopy);

            zone.DoCanopyCompartments();

            Assert.Multiple(() =>
            {
                Assert.That(zone.numLayers, Is.EqualTo(3));
                Assert.That(zone.DeltaZ, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }).Within(1e-12));
                Assert.That(zone.Canopies[0].LAI, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }).Within(1e-12));
                Assert.That(zone.Canopies[0].LAItot, Is.EqualTo(new[] { 1.5, 2.5, 3.5 }).Within(1e-12));
                Assert.That(zone.LAItotsum, Is.EqualTo(new[] { 1.5, 2.5, 3.5 }).Within(1e-12));
                Assert.That(zone.Canopies[0].Ftot, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }).Within(1e-12));
            });
        }

        [Test]
        public void DoCanopyCompartments_MissingProfileFallsBackToUniformDepthAllocation()
        {
            TestCanopy targetCanopy = new TestCanopy
            {
                Height = 3000.0,
                Depth = 3000.0,
                LAI = 1.5,
                LAITotal = 3.0,
                CoverGreen = 0.5,
                CoverTotal = 0.7
            };
            PerennialMicroClimateZone zone = CreateZone(targetCanopy, CreateLayerBoundaryCanopy());

            zone.DoCanopyCompartments();

            Assert.Multiple(() =>
            {
                Assert.That(zone.Canopies[0].LAI, Is.EqualTo(new[] { 0.5, 0.5, 0.5 }).Within(1e-12));
                Assert.That(zone.Canopies[0].LAItot, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }).Within(1e-12));
                Assert.That(zone.LAItotsum, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }).Within(1e-12));
                Assert.That(zone.Canopies[0].Ftot, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }).Within(1e-12));
            });
        }

        [Test]
        public void DoCanopyCompartments_InvalidProfileFallsBackToUniformDepthAllocation()
        {
            TestCanopy targetCanopy = new TestCanopy
            {
                Height = 3000.0,
                Depth = 3000.0,
                LAI = 0.9,
                LAITotal = 1.8,
                CoverGreen = 0.4,
                CoverTotal = 0.6,
                LightProfile = new[]
                {
                    new CanopyEnergyBalanceInterceptionlayerType { thickness = 0.0, AmountOnGreen = 10.0, AmountOnDead = 0.0 }
                }
            };
            PerennialMicroClimateZone zone = CreateZone(targetCanopy, CreateLayerBoundaryCanopy());

            zone.DoCanopyCompartments();

            Assert.Multiple(() =>
            {
                Assert.That(zone.Canopies[0].LAI, Is.EqualTo(new[] { 0.3, 0.3, 0.3 }).Within(1e-12));
                Assert.That(zone.Canopies[0].LAItot, Is.EqualTo(new[] { 0.6, 0.6, 0.6 }).Within(1e-12));
                Assert.That(zone.LAItotsum, Is.EqualTo(new[] { 0.6, 0.6, 0.6 }).Within(1e-12));
            });
        }

        [Test]
        public void DoCanopyCompartments_ComputesLayerExtinctionFromCoverDerivedKtot()
        {
            TestCanopy targetCanopy = new TestCanopy
            {
                Height = 3000.0,
                Depth = 3000.0,
                LAI = 1.5,
                LAITotal = 3.0,
                CoverGreen = 0.5,
                CoverTotal = 0.7
            };
            PerennialMicroClimateZone zone = CreateZone(targetCanopy, CreateLayerBoundaryCanopy());

            zone.DoCanopyCompartments();

            double expectedK = -Math.Log(1.0 - targetCanopy.CoverGreen) / targetCanopy.LAI;
            double expectedKtot = -Math.Log(1.0 - targetCanopy.CoverTotal) / targetCanopy.LAITotal;

            Assert.Multiple(() =>
            {
                Assert.That(zone.Canopies[0].K, Is.EqualTo(expectedK).Within(1e-12));
                Assert.That(zone.Canopies[0].Ktot, Is.EqualTo(expectedKtot).Within(1e-12));
                Assert.That(zone.layerKtot, Is.EqualTo(new[] { expectedKtot, expectedKtot, expectedKtot }).Within(1e-12));
                Assert.That(zone.Canopies.Select(c => c.Ftot.Sum()).First(), Is.EqualTo(3.0).Within(1e-12));
            });
        }

        private static PerennialMicroClimateZone CreateZone(params ICanopy[] canopies)
        {
            Zone rawZone = new Zone { Name = "TestZone", Area = 1.0 };
            Simulation simulation = new Simulation
            {
                Children = new List<IModel> { rawZone }
            };
            Node.Create(simulation);

            PerennialMicroClimateZone zone = new PerennialMicroClimateZone(
                new TestClock { Today = new DateTime(2024, 1, 15) },
                rawZone,
                simulation.Node,
                minHeightDiffForNewLayer: 0.01);

            zone.Canopies.Clear();
            foreach (ICanopy canopy in canopies)
                zone.Canopies.Add(new MicroClimateCanopy(canopy));

            return zone;
        }

        private static TestCanopy CreateLayerBoundaryCanopy()
        {
            return new TestCanopy
            {
                Height = 2000.0,
                Depth = 1000.0,
                LAI = 0.0,
                LAITotal = 0.0,
                CoverGreen = 0.0,
                CoverTotal = 0.0
            };
        }

        private class TestCanopy : ICanopy
        {
            public string CanopyType { get; set; } = "Test";
            public double Albedo { get; set; }
            public double Gsmax { get; set; }
            public double R50 { get; set; }
            public double LAI { get; set; }
            public double LAITotal { get; set; }
            public double CoverGreen { get; set; }
            public double CoverTotal { get; set; }
            public double Height { get; set; }
            public double Depth { get; set; }
            public double Width { get; set; }
            public double PotentialEP { get; set; }
            public double WaterDemand { get; set; }
            public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        }
    }
}
