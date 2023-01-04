using Models.DCAPST;
using Newtonsoft.Json;

namespace UnitTests.DCaPST
{
    /// <summary>
    /// A Test class for comparing DCaPSTParameters
    /// </summary>
    public static class DCaPSTParametersComparer
    {
        /// <summary>
        /// Are the DCaPSTParameters equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if equal</returns>
        public static bool AreDCaPSTParametersValuesEqual(DCaPSTParameters lhs, DCaPSTParameters rhs)
        {
            // Assert
            return
                lhs.Rpar == rhs.Rpar &&
                // Canopy params
                AreCanopyValuesEqual(lhs.Canopy, rhs.Canopy) &&
                // Pathway params
                ArePathwayValuesEqual(lhs.Pathway, rhs.Pathway);
        }

        /// <summary>
        /// Are the DCaPSTParameters equal, according to a JSON compare.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if equal</returns>
        public static bool AreDCaPSTParametersValuesEqualJsonCompare(DCaPSTParameters lhs, DCaPSTParameters rhs)
        {
            var lhsSerialized = JsonConvert.SerializeObject(lhs);
            var rhsSerialized = JsonConvert.SerializeObject(rhs);

            return lhsSerialized == rhsSerialized;
        }

        /// <summary>
        /// Are Canopies equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if equal</returns>
        private static bool AreCanopyValuesEqual(CanopyParameters lhs, CanopyParameters rhs)
        {
            return
                lhs.Type == rhs.Type &&
                lhs.AirO2 == rhs.AirO2 &&
                lhs.AirCO2 == rhs.AirCO2 &&
                lhs.LeafAngle == rhs.LeafAngle &&
                lhs.LeafWidth == rhs.LeafWidth &&
                lhs.LeafScatteringCoeff == rhs.LeafScatteringCoeff &&
                lhs.LeafScatteringCoeffNIR == rhs.LeafScatteringCoeffNIR &&
                lhs.DiffuseExtCoeff == rhs.DiffuseExtCoeff &&
                lhs.DiffuseExtCoeffNIR == rhs.DiffuseExtCoeffNIR &&
                lhs.DiffuseReflectionCoeff == rhs.DiffuseReflectionCoeff &&
                lhs.DiffuseReflectionCoeffNIR == rhs.DiffuseReflectionCoeffNIR &&
                lhs.Windspeed == rhs.Windspeed &&
                lhs.WindSpeedExtinction == rhs.WindSpeedExtinction &&
                lhs.CurvatureFactor == rhs.CurvatureFactor &&
                lhs.DiffusivitySolubilityRatio == rhs.DiffusivitySolubilityRatio &&
                lhs.MinimumN == rhs.MinimumN &&
                lhs.SLNRatioTop == rhs.SLNRatioTop;
        }

        /// <summary>
        /// Are Pathways equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if equal</returns>
        private static bool ArePathwayValuesEqual(PathwayParameters lhs, PathwayParameters rhs)
        {
            return
                lhs.IntercellularToAirCO2Ratio == rhs.IntercellularToAirCO2Ratio &&
                lhs.FractionOfCyclicElectronFlow == rhs.FractionOfCyclicElectronFlow &&
                lhs.RespirationSLNRatio == rhs.RespirationSLNRatio &&
                lhs.MaxRubiscoActivitySLNRatio == rhs.MaxRubiscoActivitySLNRatio &&
                lhs.MaxElectronTransportSLNRatio == rhs.MaxElectronTransportSLNRatio &&
                lhs.MaxPEPcActivitySLNRatio == rhs.MaxPEPcActivitySLNRatio &&
                lhs.MesophyllCO2ConductanceSLNRatio == rhs.MesophyllCO2ConductanceSLNRatio &&
                lhs.MesophyllElectronTransportFraction == rhs.MesophyllElectronTransportFraction &&
                lhs.ATPProductionElectronTransportFactor == rhs.ATPProductionElectronTransportFactor &&
                lhs.ExtraATPCost == rhs.ExtraATPCost &&
                AreTempratureResponseValuesEqual(lhs.RubiscoCarboxylation, rhs.RubiscoCarboxylation) &&
                AreTempratureResponseValuesEqual(lhs.RubiscoOxygenation, rhs.RubiscoOxygenation) &&
                AreTempratureResponseValuesEqual(lhs.RubiscoCarboxylationToOxygenation, rhs.RubiscoCarboxylationToOxygenation) &&
                AreTempratureResponseValuesEqual(lhs.RubiscoActivity, rhs.RubiscoActivity) &&
                AreTempratureResponseValuesEqual(lhs.PEPc, rhs.PEPc) &&
                AreTempratureResponseValuesEqual(lhs.PEPcActivity, rhs.PEPcActivity) &&
                AreTempratureResponseValuesEqual(lhs.Respiration, rhs.Respiration) &&
                AreTempratureResponseValuesEqual(lhs.Respiration, rhs.Respiration) &&
                AreLeafTemperatureParametersValuesEqual(lhs.ElectronTransportRateParams, rhs.ElectronTransportRateParams) &&
                AreTempratureResponseValuesEqual(lhs.MesophyllCO2ConductanceParams, rhs.MesophyllCO2ConductanceParams) &&
                lhs.SpectralCorrectionFactor == rhs.SpectralCorrectionFactor &&
                lhs.PS2ActivityFraction == rhs.PS2ActivityFraction &&
                lhs.PEPRegeneration == rhs.PEPRegeneration &&
                lhs.BundleSheathConductance == rhs.BundleSheathConductance;
        }

        /// <summary>
        /// Are Temprature Response Values equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if equal</returns>
        private static bool AreTempratureResponseValuesEqual(TemperatureResponseValues lhs, TemperatureResponseValues rhs)
        {
            return
                lhs.At25 == rhs.At25 &&
                lhs.Factor == rhs.Factor;
        }

        /// <summary>
        /// Are Leaf Temprature Response Values equal
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if equal</returns>
        private static bool AreLeafTemperatureParametersValuesEqual(LeafTemperatureParameters lhs, LeafTemperatureParameters rhs)
        {
            return
                lhs.TMin == rhs.TMin &&
                lhs.TOpt == rhs.TOpt &&
                lhs.TMax == rhs.TMax &&
                lhs.C == rhs.C &&
                lhs.Beta == rhs.Beta;
        }
    }
}
