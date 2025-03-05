using Models.DCAPST;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class WheatCropParameterGeneratorTests
    {
        [Test]
        public void Generate_DefaultParams_ReturnsDefaultsValue()
        {
            // Arrange & Act
            var cropParams = WheatCropParameterGenerator.Generate();

            // Assert
            Assert.That(cropParams.Rpar, Is.EqualTo(0.5));
            Assert.That(cropParams.AirO2, Is.EqualTo(210000));
            Assert.That(cropParams.Windspeed, Is.EqualTo(1.5));

            Assert.That(cropParams.Canopy.Type, Is.EqualTo(CanopyType.C3));
            Assert.That(cropParams.Canopy.LeafAngle, Is.EqualTo(60));
            Assert.That(cropParams.Canopy.LeafWidth, Is.EqualTo(0.05));
            Assert.That(cropParams.Canopy.LeafScatteringCoeff, Is.EqualTo(0.15));
            Assert.That(cropParams.Canopy.LeafScatteringCoeffNIR, Is.EqualTo(0.8));
            Assert.That(cropParams.Canopy.DiffuseExtCoeff, Is.EqualTo(0.78));
            Assert.That(cropParams.Canopy.ExtCoeffReductionSlope, Is.EqualTo(0.0288));
            Assert.That(cropParams.Canopy.ExtCoeffReductionIntercept, Is.EqualTo(0.5311));
            Assert.That(cropParams.Canopy.DiffuseExtCoeffNIR, Is.EqualTo(0.8));
            Assert.That(cropParams.Canopy.DiffuseReflectionCoeff, Is.EqualTo(0.036));
            Assert.That(cropParams.Canopy.DiffuseReflectionCoeffNIR, Is.EqualTo(0.389));
            Assert.That(cropParams.Canopy.WindSpeedExtinction, Is.EqualTo(1.5));
            Assert.That(cropParams.Canopy.CurvatureFactor, Is.EqualTo(0.7));
            Assert.That(cropParams.Canopy.DiffusivitySolubilityRatio, Is.EqualTo(0));
            Assert.That(cropParams.Canopy.MinimumN, Is.EqualTo(28.6));
            Assert.That(cropParams.Canopy.SLNRatioTop, Is.EqualTo(1.3));

            Assert.That(cropParams.Pathway.IntercellularToAirCO2Ratio, Is.EqualTo(0.7));
            Assert.That(cropParams.Pathway.FractionOfCyclicElectronFlow, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.RespirationSLNRatio, Is.EqualTo(0.0));
            Assert.That(cropParams.Pathway.MaxRubiscoActivitySLNRatio, Is.EqualTo(1.45));
            Assert.That(cropParams.Pathway.MaxElectronTransportSLNRatio, Is.EqualTo(2.4));
            Assert.That(cropParams.Pathway.MaxPEPcActivitySLNRatio, Is.EqualTo(1));
            Assert.That(cropParams.Pathway.MesophyllCO2ConductanceSLNRatio, Is.EqualTo(0.005));
            Assert.That(cropParams.Pathway.MesophyllElectronTransportFraction, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.ATPProductionElectronTransportFactor, Is.EqualTo(0.75));
            Assert.That(cropParams.Pathway.ExtraATPCost, Is.EqualTo(0));

            Assert.That(cropParams.Pathway.RubiscoActivityParams.TMin, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.RubiscoActivityParams.TOpt, Is.EqualTo(39.241));
            Assert.That(cropParams.Pathway.RubiscoActivityParams.TMax, Is.EqualTo(50));
            Assert.That(cropParams.Pathway.RubiscoActivityParams.C, Is.EqualTo(0.744604));
            Assert.That(cropParams.Pathway.RubiscoActivityParams.Beta, Is.EqualTo(0.396));

            Assert.That(cropParams.Pathway.PEPcActivityParams.TMin, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.PEPcActivityParams.TOpt, Is.EqualTo(45.964));
            Assert.That(cropParams.Pathway.PEPcActivityParams.TMax, Is.EqualTo(50));
            Assert.That(cropParams.Pathway.PEPcActivityParams.C, Is.EqualTo(0.304367));
            Assert.That(cropParams.Pathway.PEPcActivityParams.Beta, Is.EqualTo(0.275));

            Assert.That(cropParams.Pathway.ElectronTransportRateParams.TMin, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.ElectronTransportRateParams.TOpt, Is.EqualTo(30));
            Assert.That(cropParams.Pathway.ElectronTransportRateParams.TMax, Is.EqualTo(45));
            Assert.That(cropParams.Pathway.ElectronTransportRateParams.C, Is.EqualTo(0.911017958600129));
            Assert.That(cropParams.Pathway.ElectronTransportRateParams.Beta, Is.EqualTo(1));

            Assert.That(cropParams.Pathway.RespirationParams.TMin, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.RespirationParams.TOpt, Is.EqualTo(38.888));
            Assert.That(cropParams.Pathway.RespirationParams.TMax, Is.EqualTo(50));
            Assert.That(cropParams.Pathway.RespirationParams.C, Is.EqualTo(0.626654));
            Assert.That(cropParams.Pathway.RespirationParams.Beta, Is.EqualTo(0.682));

            Assert.That(cropParams.Pathway.RubiscoCarboxylation.At25, Is.EqualTo(273.422964228666));
            Assert.That(cropParams.Pathway.RubiscoCarboxylation.Factor, Is.EqualTo(93720));
            Assert.That(cropParams.Pathway.RubiscoOxygenation.At25, Is.EqualTo(165824.064155384));
            Assert.That(cropParams.Pathway.RubiscoOxygenation.Factor, Is.EqualTo(33600));
            Assert.That(cropParams.Pathway.RubiscoCarboxylationToOxygenation.At25, Is.EqualTo(4.59217066521612));
            Assert.That(cropParams.Pathway.RubiscoCarboxylationToOxygenation.Factor, Is.EqualTo(35713.19871277176));
            Assert.That(cropParams.Pathway.PEPc.At25, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.PEPc.Factor, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.RubiscoActivity.At25, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.RubiscoActivity.Factor, Is.EqualTo(65330));

            Assert.That(cropParams.Pathway.SpectralCorrectionFactor, Is.EqualTo(0.15));
            Assert.That(cropParams.Pathway.PS2ActivityFraction, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.PEPRegeneration, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.BundleSheathConductance, Is.EqualTo(0));
            Assert.That(cropParams.Pathway.Epsilon, Is.EqualTo(0.3));
            Assert.That(cropParams.Pathway.EpsilonAt25C, Is.EqualTo(0.2));
        }
    }
}
