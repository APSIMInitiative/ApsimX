using System;

using Models.DCAPST.Canopy;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Utilities
{
    /// <summary>
    /// Provides utility to simplify the interaction with Apsim Classic,
    /// by reducing the complexity of each manager script
    /// </summary>
    public static class Classic
    {
        /// <summary>
        /// 
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
        /// <returns></returns>
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
                AirCO2 = airCO2,
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
        /// 
        /// </summary>
        /// <param name="jTMin"></param>
        /// <param name="jTOpt"></param>
        /// <param name="jTMax"></param>
        /// <param name="jC"></param>
        /// <param name="jBeta"></param>
        /// <param name="gTMin"></param>
        /// <param name="gTOpt"></param>
        /// <param name="gTMax"></param>
        /// <param name="gC"></param>
        /// <param name="gBeta"></param>
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
        /// <returns></returns>
        public static PathwayParameters SetUpPathway(
            double jTMin,
            double jTOpt,
            double jTMax,
            double jC,
            double jBeta,
            double gTMin,
            double gTOpt,
            double gTMax,
            double gC,
            double gBeta,
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

            var g = new LeafTemperatureParameters
            {
                TMin = gTMin,
                TOpt = gTOpt,
                TMax = gTMax,
                C = gC,
                Beta = gBeta,
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="CP"></param>
        /// <param name="PP"></param>
        /// <param name="DOY"></param>
        /// <param name="latitude"></param>
        /// <param name="maxT"></param>
        /// <param name="minT"></param>
        /// <param name="radn"></param>
        /// <param name="rpar"></param>
        /// <returns></returns>
        public static DCAPSTModel SetUpModel(
            ICanopyParameters CP, 
            IPathwayParameters PP,
            int DOY, 
            double latitude, 
            double maxT, 
            double minT, 
            double radn,
            double rpar)
        {
            // Model the solar geometry
            var SG = new SolarGeometry
            {
                Latitude = latitude.ToRadians(),
                DayOfYear = DOY
            };

            // Model the solar radiation
            var SR = new SolarRadiation(SG)
            {
                Daily = radn,
                RPAR = rpar
            };

            // Model the environmental temperature
            var TM = new Temperature(SG)
            {
                MaxTemperature = maxT,
                MinTemperature = minT,
                AtmosphericPressure = 1.01325
            };            

            // Model the pathways
            var SunlitAc1 = new AssimilationPathway(CP, PP);
            var SunlitAc2 = new AssimilationPathway(CP, PP);
            var SunlitAj = new AssimilationPathway(CP, PP);

            var ShadedAc1 = new AssimilationPathway(CP, PP);
            var ShadedAc2 = new AssimilationPathway(CP, PP);
            var ShadedAj = new AssimilationPathway(CP, PP);

            // Model the canopy
            IAssimilation A;
            if (CP.Type == CanopyType.C3) A = new AssimilationC3(CP, PP);
            else if (CP.Type == CanopyType.C4) A = new AssimilationC4(CP, PP);
            else A = new AssimilationCCM(CP, PP);

            var sunlit = new AssimilationArea(SunlitAc1, SunlitAc2, SunlitAj, A);
            var shaded = new AssimilationArea(ShadedAc1, ShadedAc2, ShadedAj, A);
            var CA = new CanopyAttributes(CP, PP, sunlit, shaded);

            // Model the transpiration
            var WI = new WaterInteraction(TM);
            var TR = new TemperatureResponse(CP, PP);
            var TS = new Transpiration(CP, PP, WI, TR);

            // Model the photosynthesis
            var DM = new DCAPSTModel(SG, SR, TM, PP, CA, TS)
            {
                B = 0.409
            };

            return DM;
        }
    }
}
