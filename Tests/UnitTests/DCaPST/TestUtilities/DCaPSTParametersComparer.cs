using Models.DCAPST;
using Newtonsoft.Json;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    /// <summary>
    /// A Test class for comparing DCaPSTParameters
    /// </summary>
    public static class DCaPSTParametersComparer
    {
        /// <summary>
        /// Assert the DCaPSTParameters equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static void AssertDCaPSTParametersValuesEqual(DCaPSTParameters lhs, DCaPSTParameters rhs)
        {
            // Assert
            Assert.That(lhs.Rpar, Is.EqualTo(rhs.Rpar));
            // Canopy params
            AssertCanopyValuesEqual(lhs.Canopy, rhs.Canopy);
            // Pathway params
            AssertPathwayValuesEqual(lhs.Pathway, rhs.Pathway);

            // JSON Convert for good measure.
            AssertDCaPSTParametersValuesEqualJsonCompare(lhs, rhs);
        }

        /// <summary>
        /// Assert the DCaPSTParameters equal, according to a JSON compare.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertDCaPSTParametersValuesEqualJsonCompare(DCaPSTParameters lhs, DCaPSTParameters rhs)
        {
            var lhsSerialized = JsonConvert.SerializeObject(lhs);
            var rhsSerialized = JsonConvert.SerializeObject(rhs);

            Assert.That(lhsSerialized, Is.EqualTo(rhsSerialized));
       }

        /// <summary>
        /// Assert Canopies equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertCanopyValuesEqual(CanopyParameters lhs, CanopyParameters rhs)
        {            
            Assert.That(lhs.Type, Is.EqualTo(rhs.Type));
            Assert.That(lhs.AirO2, Is.EqualTo(rhs.AirO2));
            Assert.That(lhs.LeafAngle, Is.EqualTo(rhs.LeafAngle));
            Assert.That(lhs.LeafWidth, Is.EqualTo(rhs.LeafWidth));
            Assert.That(lhs.LeafScatteringCoeff, Is.EqualTo(rhs.LeafScatteringCoeff));
            Assert.That(lhs.LeafScatteringCoeffNIR, Is.EqualTo(rhs.LeafScatteringCoeffNIR));
            Assert.That(lhs.DiffuseExtCoeff, Is.EqualTo(rhs.DiffuseExtCoeff));
            Assert.That(lhs.DiffuseExtCoeffNIR, Is.EqualTo(rhs.DiffuseExtCoeffNIR));
            Assert.That(lhs.DiffuseReflectionCoeff, Is.EqualTo(rhs.DiffuseReflectionCoeff));
            Assert.That(lhs.DiffuseReflectionCoeffNIR, Is.EqualTo(rhs.DiffuseReflectionCoeffNIR));
            Assert.That(lhs.Windspeed, Is.EqualTo(rhs.Windspeed));
            Assert.That(lhs.WindSpeedExtinction, Is.EqualTo(rhs.WindSpeedExtinction));
            Assert.That(lhs.CurvatureFactor, Is.EqualTo(rhs.CurvatureFactor));
            Assert.That(lhs.DiffusivitySolubilityRatio, Is.EqualTo(rhs.DiffusivitySolubilityRatio));
            Assert.That(lhs.MinimumN, Is.EqualTo(rhs.MinimumN));
            Assert.That(lhs.SLNRatioTop, Is.EqualTo(rhs.SLNRatioTop));
        }

        /// <summary>
        /// Assert Pathways equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertPathwayValuesEqual(PathwayParameters lhs, PathwayParameters rhs)
        {            
            Assert.That(lhs.IntercellularToAirCO2Ratio, Is.EqualTo(rhs.IntercellularToAirCO2Ratio));
            Assert.That(lhs.FractionOfCyclicElectronFlow, Is.EqualTo(rhs.FractionOfCyclicElectronFlow));
            Assert.That(lhs.RespirationSLNRatio, Is.EqualTo(rhs.RespirationSLNRatio));
            Assert.That(lhs.MaxRubiscoActivitySLNRatio, Is.EqualTo(rhs.MaxRubiscoActivitySLNRatio));
            Assert.That(lhs.MaxElectronTransportSLNRatio, Is.EqualTo(rhs.MaxElectronTransportSLNRatio));
            Assert.That(lhs.MaxPEPcActivitySLNRatio, Is.EqualTo(rhs.MaxPEPcActivitySLNRatio));
            Assert.That(lhs.MesophyllCO2ConductanceSLNRatio, Is.EqualTo(rhs.MesophyllCO2ConductanceSLNRatio));
            Assert.That(lhs.MesophyllElectronTransportFraction, Is.EqualTo(rhs.MesophyllElectronTransportFraction));
            Assert.That(lhs.ATPProductionElectronTransportFactor, Is.EqualTo(rhs.ATPProductionElectronTransportFactor));
            Assert.That(lhs.ExtraATPCost, Is.EqualTo(rhs.ExtraATPCost));
            AssertTempratureResponseValuesEqual(lhs.RubiscoCarboxylation, rhs.RubiscoCarboxylation);
            AssertTempratureResponseValuesEqual(lhs.RubiscoOxygenation, rhs.RubiscoOxygenation);
            AssertTempratureResponseValuesEqual(lhs.RubiscoCarboxylationToOxygenation, rhs.RubiscoCarboxylationToOxygenation);
            AssertTempratureResponseValuesEqual(lhs.RubiscoActivity, rhs.RubiscoActivity);
            AssertTempratureResponseValuesEqual(lhs.PEPc, rhs.PEPc);
            AssertTempratureResponseValuesEqual(lhs.PEPcActivity, rhs.PEPcActivity);
            AssertTempratureResponseValuesEqual(lhs.Respiration, rhs.Respiration);
            AssertTempratureResponseValuesEqual(lhs.Respiration, rhs.Respiration);
            AssertLeafTemperatureParametersValuesEqual(lhs.ElectronTransportRateParams, rhs.ElectronTransportRateParams);
            AssertTempratureResponseValuesEqual(lhs.MesophyllCO2ConductanceParams, rhs.MesophyllCO2ConductanceParams);
            Assert.That(lhs.SpectralCorrectionFactor, Is.EqualTo(rhs.SpectralCorrectionFactor));
            Assert.That(lhs.PS2ActivityFraction, Is.EqualTo(rhs.PS2ActivityFraction));
            Assert.That(lhs.PEPRegeneration, Is.EqualTo(rhs.PEPRegeneration));
            Assert.That(lhs.BundleSheathConductance, Is.EqualTo(rhs.BundleSheathConductance));
        }

        /// <summary>
        /// Assert Temprature Response Values equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertTempratureResponseValuesEqual(TemperatureResponseValues lhs, TemperatureResponseValues rhs)
        {
            Assert.That(lhs.At25, Is.EqualTo(rhs.At25));
            Assert.That(lhs.Factor, Is.EqualTo(rhs.Factor));
        }

        /// <summary>
        /// Assert Leaf Temprature Response Values equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertLeafTemperatureParametersValuesEqual(LeafTemperatureParameters lhs, LeafTemperatureParameters rhs)
        {
            Assert.That(lhs.TMin, Is.EqualTo(rhs.TMin));
            Assert.That(lhs.TOpt, Is.EqualTo(rhs.TOpt));
            Assert.That(lhs.TMax, Is.EqualTo(rhs.TMax));
            Assert.That(lhs.C, Is.EqualTo(rhs.C));
            Assert.That(lhs.Beta, Is.EqualTo(rhs.Beta));
        }
    }
}
