using Models.DCAPST;
using Models.DCAPST.Interfaces;

namespace UnitTests.DCaPST
{
    /// <summary>
    /// This is a snapshot of some of the code that is in classic. It allows us to compare
    /// the setup of the defaults from Classic with the setup with the defaults from Next Gen.
    /// This is purely for testing, there is no coupling with Classic in any way shape or form.
    /// </summary>
    public static class ClassicDCaPSTDefaultDataSetup
    {
        /// <summary>
        /// Sets up a Canopy in the same way that Classic did so that we can 
        /// test this against the defaults that we are using to ensure they are the same.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="airCO2"></param>
        /// <param name="curvatureFactor"></param>
        /// <param name="diffusivitySolubilityRatio"></param>
        /// <param name="airO2"></param>
        /// <param name="diffuseExtCoeff"></param>
        /// <param name="diffuseExtCoeffNIR"></param>
        /// <param name="diffuseReflectionCoeff"></param>
        /// <param name="diffuseReflectionCoeffNIR"></param>
        /// <param name="leafAngle"></param>
        /// <param name="leafScatteringCoeff"></param>
        /// <param name="leafScatteringCoeffNIR"></param>
        /// <param name="leafWidth"></param>
        /// <param name="slnRatioTop"></param>
        /// <param name="minimumN"></param>
        /// <param name="windspeed"></param>
        /// <param name="windSpeedExtinction"></param>
        /// <returns>The Canopy Parameters</returns>
        public static CanopyParameters SetUpCanopy(
            CanopyType type,
            double airCO2,
            double curvatureFactor,
            double diffusivitySolubilityRatio,
            double airO2,
            double diffuseExtCoeff,
            double diffuseExtCoeffNIR,
            double diffuseReflectionCoeff,
            double diffuseReflectionCoeffNIR,
            double leafAngle,
            double leafScatteringCoeff,
            double leafScatteringCoeffNIR,
            double leafWidth,
            double slnRatioTop,
            double minimumN,
            double windspeed,
            double windSpeedExtinction
        )
        {
            var CP = new CanopyParameters
            {
                Type = type,                
                CurvatureFactor = curvatureFactor,
                DiffusivitySolubilityRatio = diffusivitySolubilityRatio,
                AirO2 = airO2,
                DiffuseExtCoeff = diffuseExtCoeff,
                DiffuseExtCoeffNIR = diffuseExtCoeffNIR,
                DiffuseReflectionCoeff = diffuseReflectionCoeff,
                DiffuseReflectionCoeffNIR = diffuseReflectionCoeffNIR,
                LeafAngle = leafAngle,
                LeafScatteringCoeff = leafScatteringCoeff,
                LeafScatteringCoeffNIR = leafScatteringCoeffNIR,
                LeafWidth = leafWidth,
                SLNRatioTop = slnRatioTop,
                MinimumN = minimumN,
                Windspeed = windspeed,
                WindSpeedExtinction = windSpeedExtinction
            };

            return CP;
        }

        /// <summary>
        /// Sets up a Pathway in the same way that Classic did so that we can 
        /// test this against the defaults that we are using to ensure they are the same.
        /// </summary>
        /// <param name="jTMin"></param>
        /// <param name="jTOpt"></param>
        /// <param name="jTMax"></param>
        /// <param name="jC"></param>
        /// <param name="jBeta"></param>
        /// <param name="gmFactor"></param>
        /// <param name="KcAt25"></param>
        /// <param name="KcFactor"></param>
        /// <param name="KoAt25"></param>
        /// <param name="KoFactor"></param>
        /// <param name="VcVoAt25"></param>
        /// <param name="VcVoFactor"></param>
        /// <param name="KpAt25"></param>
        /// <param name="KpFactor"></param>
        /// <param name="VcFactor"></param>
        /// <param name="RdFactor"></param>
        /// <param name="VpFactor"></param>
        /// <param name="pepRegeneration"></param>
        /// <param name="spectralCorrectionFactor"></param>
        /// <param name="ps2ActivityFraction"></param>
        /// <param name="bundleSheathConductance"></param>
        /// <param name="maxRubiscoActivitySLNRatio"></param>
        /// <param name="maxElectronTransportSLNRatio"></param>
        /// <param name="respirationSLNRatio"></param>
        /// <param name="maxPEPcActivitySLNRatio"></param>
        /// <param name="mesophyllCO2ConductanceSLNRatio"></param>
        /// <param name="extraATPCost"></param>
        /// <param name="intercellularToAirCO2Ratio"></param>
        /// <returns>The PathwayParameters</returns>
        public static PathwayParameters SetUpPathway(
            double jTMin,
            double jTOpt,
            double jTMax,
            double jC,
            double jBeta,
            double gmFactor,
            //double ggTMin,
            //double ggTOpt,
            //double ggTMax,
            //double ggC,
            //double ggBeta,
            double KcAt25,
            double KcFactor,
            double KoAt25,
            double KoFactor,
            double VcVoAt25,
            double VcVoFactor,
            double KpAt25,
            double KpFactor,
            double VcFactor,
            double RdFactor,
            double VpFactor,
            double pepRegeneration,
            double spectralCorrectionFactor,
            double ps2ActivityFraction,
            double bundleSheathConductance,
            double maxRubiscoActivitySLNRatio,
            double maxElectronTransportSLNRatio,
            double respirationSLNRatio,
            double maxPEPcActivitySLNRatio,
            double mesophyllCO2ConductanceSLNRatio,
            double extraATPCost,
            double intercellularToAirCO2Ratio
        )
        {
            var j = new LeafTemperatureParameters
            {
                TMin = jTMin,
                TOpt = jTOpt,
                TMax = jTMax,
                C = jC,
                Beta = jBeta
            };

            var g = new TemperatureResponseValues
            {
                Factor = gmFactor
                //TMin = gTMin,
                //TOpt = gTOpt,
                //TMax = gTMax,
                //C = gC,
                //Beta = gBeta,
            };

            var Kc = new TemperatureResponseValues
            {
                At25 = KcAt25,
                Factor = KcFactor
            };

            var Ko = new TemperatureResponseValues
            {
                At25 = KoAt25,
                Factor = KoFactor
            };

            var VcVo = new TemperatureResponseValues
            {
                At25 = VcVoAt25,
                Factor = VcVoFactor
            };

            var Kp = new TemperatureResponseValues
            {
                At25 = KpAt25,
                Factor = KpFactor
            };

            var Vc = new TemperatureResponseValues
            {
                Factor = VcFactor
            };

            var Rd = new TemperatureResponseValues
            {
                Factor = RdFactor
            };

            var Vp = new TemperatureResponseValues
            {
                Factor = VpFactor
            };

            var PP = new PathwayParameters
            {
                PEPRegeneration = pepRegeneration,
                SpectralCorrectionFactor = spectralCorrectionFactor,
                PS2ActivityFraction = ps2ActivityFraction,
                BundleSheathConductance = bundleSheathConductance,
                MaxRubiscoActivitySLNRatio = maxRubiscoActivitySLNRatio,
                MaxElectronTransportSLNRatio = maxElectronTransportSLNRatio,
                RespirationSLNRatio = respirationSLNRatio,
                MaxPEPcActivitySLNRatio = maxPEPcActivitySLNRatio,
                MesophyllCO2ConductanceSLNRatio = mesophyllCO2ConductanceSLNRatio,
                ExtraATPCost = extraATPCost,
                IntercellularToAirCO2Ratio = intercellularToAirCO2Ratio,
                RubiscoCarboxylation = Kc,
                RubiscoOxygenation = Ko,
                RubiscoCarboxylationToOxygenation = VcVo,
                PEPc = Kp,
                RubiscoActivity = Vc,
                Respiration = Rd,
                PEPcActivity = Vp,
                ElectronTransportRateParams = j,
                MesophyllCO2ConductanceParams = g
            };

            PP.MesophyllElectronTransportFraction = PP.ExtraATPCost / (3.0 + PP.ExtraATPCost);
            PP.FractionOfCyclicElectronFlow = 0.25 * PP.ExtraATPCost;
            PP.ATPProductionElectronTransportFactor = (3.0 - PP.FractionOfCyclicElectronFlow) / (4.0 * (1.0 - PP.FractionOfCyclicElectronFlow));

            return PP;
        }
    }
}
