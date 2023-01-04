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
            Assert.AreEqual(lhs.Rpar, rhs.Rpar);
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

            Assert.AreEqual(lhsSerialized, rhsSerialized);
        }

        /// <summary>
        /// Assert Canopies equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertCanopyValuesEqual(CanopyParameters lhs, CanopyParameters rhs)
        {            
            Assert.AreEqual(lhs.Type, rhs.Type);
            Assert.AreEqual(lhs.AirO2, rhs.AirO2);
            Assert.AreEqual(lhs.AirCO2, rhs.AirCO2);
            Assert.AreEqual(lhs.LeafAngle, rhs.LeafAngle);
            Assert.AreEqual(lhs.LeafWidth, rhs.LeafWidth);
            Assert.AreEqual(lhs.LeafScatteringCoeff, rhs.LeafScatteringCoeff);
            Assert.AreEqual(lhs.LeafScatteringCoeffNIR, rhs.LeafScatteringCoeffNIR);
            Assert.AreEqual(lhs.DiffuseExtCoeff, rhs.DiffuseExtCoeff);
            Assert.AreEqual(lhs.DiffuseExtCoeffNIR, rhs.DiffuseExtCoeffNIR);
            Assert.AreEqual(lhs.DiffuseReflectionCoeff, rhs.DiffuseReflectionCoeff);
            Assert.AreEqual(lhs.DiffuseReflectionCoeffNIR, rhs.DiffuseReflectionCoeffNIR);
            Assert.AreEqual(lhs.Windspeed, rhs.Windspeed);
            Assert.AreEqual(lhs.WindSpeedExtinction, rhs.WindSpeedExtinction);
            Assert.AreEqual(lhs.CurvatureFactor, rhs.CurvatureFactor);
            Assert.AreEqual(lhs.DiffusivitySolubilityRatio, rhs.DiffusivitySolubilityRatio);
            Assert.AreEqual(lhs.MinimumN, rhs.MinimumN);
            Assert.AreEqual(lhs.SLNRatioTop, rhs.SLNRatioTop);
        }

        /// <summary>
        /// Assert Pathways equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertPathwayValuesEqual(PathwayParameters lhs, PathwayParameters rhs)
        {            
            Assert.AreEqual(lhs.IntercellularToAirCO2Ratio, rhs.IntercellularToAirCO2Ratio);
            Assert.AreEqual(lhs.FractionOfCyclicElectronFlow, rhs.FractionOfCyclicElectronFlow);
            Assert.AreEqual(lhs.RespirationSLNRatio, rhs.RespirationSLNRatio);
            Assert.AreEqual(lhs.MaxRubiscoActivitySLNRatio, rhs.MaxRubiscoActivitySLNRatio);
            Assert.AreEqual(lhs.MaxElectronTransportSLNRatio, rhs.MaxElectronTransportSLNRatio);
            Assert.AreEqual(lhs.MaxPEPcActivitySLNRatio, rhs.MaxPEPcActivitySLNRatio);
            Assert.AreEqual(lhs.MesophyllCO2ConductanceSLNRatio, rhs.MesophyllCO2ConductanceSLNRatio);
            Assert.AreEqual(lhs.MesophyllElectronTransportFraction, rhs.MesophyllElectronTransportFraction);
            Assert.AreEqual(lhs.ATPProductionElectronTransportFactor, rhs.ATPProductionElectronTransportFactor);
            Assert.AreEqual(lhs.ExtraATPCost, rhs.ExtraATPCost);
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
            Assert.AreEqual(lhs.SpectralCorrectionFactor, rhs.SpectralCorrectionFactor);
            Assert.AreEqual(lhs.PS2ActivityFraction, rhs.PS2ActivityFraction);
            Assert.AreEqual(lhs.PEPRegeneration, rhs.PEPRegeneration);
            Assert.AreEqual(lhs.BundleSheathConductance, rhs.BundleSheathConductance);
        }

        /// <summary>
        /// Assert Temprature Response Values equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertTempratureResponseValuesEqual(TemperatureResponseValues lhs, TemperatureResponseValues rhs)
        {
            Assert.AreEqual(lhs.At25, rhs.At25);
            Assert.AreEqual(lhs.Factor, rhs.Factor);
        }

        /// <summary>
        /// Assert Leaf Temprature Response Values equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        private static void AssertLeafTemperatureParametersValuesEqual(LeafTemperatureParameters lhs, LeafTemperatureParameters rhs)
        {
            Assert.AreEqual(lhs.TMin, rhs.TMin);
            Assert.AreEqual(lhs.TOpt, rhs.TOpt);
            Assert.AreEqual(lhs.TMax, rhs.TMax);
            Assert.AreEqual(lhs.C, rhs.C);
            Assert.AreEqual(lhs.Beta, rhs.Beta);
        }
    }
}
