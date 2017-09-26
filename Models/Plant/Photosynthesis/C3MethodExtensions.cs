using System;
using Models.PMF.Phenology;
using System.Linq;

namespace C3MethodExtensions
{

    public static class C3MethodExtensions
    {

        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PM"></param>
        /// <param name="useAirTemp"></param>
        /// <param name="layer"></param>
        /// <param name="leafTemperature"></param>
        /// <param name="cm"></param>
        /// <param name="mode"></param>
        /// <param name="maxHourlyT"></param>
        /// <param name="Tfraction"></param>
        /// <returns></returns>
        public static bool calcPhotosynthesis(this SunlitShadedCanopy s, PhotosynthesisModel PM, bool useAirTemp, int layer, double leafTemperature,
            TranspirationMode mode, double maxHourlyT, double Tfraction)
        { 

            LeafCanopy canopy = PM.canopy;

            //calcPhotosynthesis(PM, layer);

            s.Oi[layer] = canopy.oxygenPartialPressure;

            s.Om[layer] = canopy.oxygenPartialPressure;

            s.Oc[layer] = s.Oi[layer];

            s.leafTemp__[layer] = leafTemperature;

            if (useAirTemp)
            {
                s.leafTemp__[layer] = PM.envModel.getTemp(PM.time);
            }

            s.calcConductanceResistance(PM, canopy);

            s.VcMaxT[layer] = TempFunctionExp.val(s.leafTemp__[layer], s.VcMax25[layer], canopy.CPath.VcMax_c, canopy.CPath.VcMax_b);
            s.RdT[layer] = TempFunctionExp.val(s.leafTemp__[layer], s.Rd25[layer], canopy.CPath.Rd_c, canopy.CPath.Rd_b);
            s.JMaxT[layer] = TempFunctionNormal.val(s.leafTemp__[layer], s.JMax25[layer], canopy.CPath.JMax_TOpt, canopy.CPath.JMax_Omega);
            //s.VpMaxT[layer] = TempFunctionExp.val(s.leafTemp__[layer], s.VpMax25[layer], canopy.CPath.VpMax_c, canopy.CPath.VpMax_b);

            // s.Vpr[layer] = canopy.Vpr_l * s.LAIS[layer];///

            canopy.ja = (1 - canopy.f) / 2;

            s.J[layer] = (canopy.ja * s.absorbedIrradiance[layer] + s.JMaxT[layer] - Math.Pow(Math.Pow(canopy.ja * s.absorbedIrradiance[layer] + s.JMaxT[layer], 2) -
            4 * canopy.θ * s.JMaxT[layer] * canopy.ja * s.absorbedIrradiance[layer], 0.5)) / (2 * canopy.θ);

            s.Kc[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.Kc_P25, canopy.CPath.Kc_c, canopy.CPath.Kc_b);
            s.Ko[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.Ko_P25, canopy.CPath.Ko_c, canopy.CPath.Ko_b);
            s.VcVo[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.VcMax_VoMax_P25, canopy.CPath.VcMax_VoMax_c, canopy.CPath.VcMax_VoMax_b);

            s.ScO[layer] = s.Ko[layer] / s.Kc[layer] * s.VcVo[layer];

            s.g_[layer] = 0.5 / s.ScO[layer];

            s.r_[layer] = s.g_[layer] * s.Oc[layer];

            canopy.Sco = s.ScO[layer]; //For reporting ??? 

            s.gm_CO2T[layer] = s.LAIS[layer] * TempFunctionNormal.val(s.leafTemp__[layer], canopy.CPath.gm_P25, canopy.CPath.gm_TOpt, canopy.CPath.gm_Omega);


            if (mode == TranspirationMode.unlimited)
            {
                //Caculate A's
                if (s.type == SSType.AC1)
                {
                    s.A[layer] = calcAc(s, canopy, layer, TranspirationMode.unlimited);
                }
                else if (s.type == SSType.AJ)
                {
                    s.A[layer] = calcAj(s, canopy, layer, TranspirationMode.unlimited);
                }

                if (s.A[layer] < 0 || double.IsNaN(s.A[layer]))
                {
                    s.A[layer] = 0;
                }

                if (PM.conductanceModel == PhotosynthesisModel.ConductanceModel.DETAILED)
                {
                    s.Ci[layer] = canopy.Ca - s.A[layer] / s.gb_CO2[layer] - s.A[layer] / s.gs_CO2[layer];
                }
                else
                {
                    s.Ci[layer] = canopy.CPath.CiCaRatio * canopy.Ca;
                }

                s.Cc[layer] = s.Ci[layer] - s.A[layer] / s.gm_CO2T[layer];

                if (s.Cc[layer] < 0 || double.IsNaN(s.Cc[layer]))
                {
                    s.Cc[layer] = 0;
                }

                s.CiCaRatio[layer] = s.Ci[layer] / canopy.Ca;

                s.calcWaterUse(PM, canopy);

                s.TDelta[layer] = s.rbh[layer] * (s.Rn[layer] - s.Elambda_[layer]) / canopy.rcp;

                s.leafTemp[layer] = PM.envModel.getTemp(PM.time) + s.TDelta[layer];
            }

            else if (mode == TranspirationMode.limited)
            {
                double supplymmhr = maxHourlyT * Tfraction;

                s.Elambda[layer] = supplymmhr / (0.001 * 3600) * canopy.lambda / 1000;

                double totalAbsorbed = s.absorbedIrradiancePAR[layer] + s.absorbedIrradianceNIR[layer];
                s.Rn[layer] = totalAbsorbed - 2 * (canopy.sigma * Math.Pow(273 + s.leafTemp__[layer], 4) - canopy.sigma * Math.Pow(273 + PM.envModel.getTemp(PM.time), 4));

                s.leafTemp[layer] = s.rbh[layer] * (s.Rn[layer] - s.Elambda[layer]) / canopy.rcp + PM.envModel.getTemp(PM.time);

                s.VPD_la[layer] = PM.envModel.calcSVP(s.leafTemp__[layer]) - PM.envModel.calcSVP(PM.envModel.minT);

                s.rsw[layer] = ((canopy.s * s.Rn[layer] + s.VPD_la[layer] * canopy.rcp / s.rbh[layer]) / s.Elambda[layer] - canopy.s) *
                    s.rbh[layer] / canopy.g - s.rbw[layer];

                s.gsw[layer] = canopy.rair / s.rsw[layer] * PM.envModel.ATM;


                s.gsCO2[layer] = s.gsw[layer] / 1.6;

                s.gm_CO2T[layer] = s.LAIS[layer] * TempFunctionNormal.val(s.leafTemp__[layer], canopy.CPath.gm_P25, canopy.CPath.gm_TOpt, canopy.CPath.gm_Omega);

                //Caculate A's
                if (s.type == SSType.AC1)
                {
                    s.A[layer] = calcAc(s, canopy, layer, TranspirationMode.limited);
                }
                else if (s.type == SSType.AJ)
                {
                    s.A[layer] = calcAj(s, canopy, layer, TranspirationMode.limited);
                }

                s.Cb[layer] = canopy.Ca - s.A[layer] / s.gbCO2[layer];

                s.Ci[layer] = s.Cb[layer] - s.A[layer] / s.gsCO2[layer];

            }

            double airTemp = PM.envModel.getTemp(PM.time);

            if (useAirTemp)
            {
                s.leafTemp[layer] = PM.envModel.getTemp(PM.time);
            }

            double diffTemp = s.leafTemp__[layer] - s.leafTemp[layer];
              

            s.leafTemp[layer] = (s.leafTemp[layer] + s.leafTemp__[layer]) / 2;

            if ((Math.Abs(diffTemp) > s.leafTempTolerance) || double.IsNaN(s.leafTemp[layer]))
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="layer"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static double calcAj(SunlitShadedCanopy s, LeafCanopy canopy, int layer, TranspirationMode mode)
        {
            double assimilation = 0;
            double x_1 = s.J[layer] / 4;
            double x_2 = 2 * s.r_[layer];

            if (mode == TranspirationMode.unlimited)
            {
                assimilation = calcAssimilation(s, x_1, x_2, layer, canopy);
            }
            else
            {
                assimilation = calcAssimilationDiffusion(s, x_1, x_2, layer, canopy);
            }

            return assimilation;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x_1"></param>
        /// <param name="x_2"></param>
        /// <param name="layer"></param>
        /// <param name="canopy"></param>
        /// <returns></returns>
        public static double calcAssimilation(SunlitShadedCanopy s, double x_1, double x_2, int layer, LeafCanopy canopy)
        {
            double a, b, c, d;

            double g_m = s.gm_CO2T[layer];
            double R_d = s.RdT[layer];
            double C_a = canopy.Ca;
            double Γ_ = s.r_[layer];
            double x = canopy.CPath.CiCaRatio;
            double C_i = C_a * canopy.CPath.CiCaRatio;

            a = -C_i / C_a * C_a * g_m - g_m * x_2 + R_d - x_1;  // (A3)
            b = -C_i / C_a * C_a * g_m * R_d + C_i / C_a * C_a * g_m * x_1 - g_m * R_d * x_2 - g_m * Γ_ * x_1;  //   (A4)
            c = C_i / C_a * C_a * g_m + g_m * x_2 - R_d + x_1;   // (A5)
            d = 1;

            return s.solveQuadratic(a, b, c, d); //Eq (A55)
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x_1"></param>
        /// <param name="x_2"></param>
        /// <param name="layer"></param>
        /// <param name="canopy"></param>
        /// <returns></returns>
        public static double calcAssimilationDiffusion(SunlitShadedCanopy s, double x_1, double x_2, int layer, LeafCanopy canopy)
        {
            double a, b, c, d;

            double g_m = s.gm_CO2T[layer];
            double R_d = s.RdT[layer];
            double C_a = canopy.Ca;
            double g_s = s.gsCO2[layer];
            double g_b = s.gbCO2[layer];
            double Γ_ = s.r_[layer];

            a = -C_a * g_b * g_s * g_m + g_b * g_m * R_d - g_b * g_m * g_s * x_2 - g_b * g_m * x_1 + g_b * g_s * R_d - g_b * g_s * x_1 + g_m * g_s * R_d - g_m * g_s * x_1;
            b = (g_b * g_m + g_b * g_s + g_s * g_m) * (-C_a * g_b * g_s * g_m * R_d + C_a * g_b * g_s * g_m * x_1 - g_b * g_s * g_m * R_d * x_2 - g_b * g_s * g_m * Γ_ * x_1);
            c = C_a * g_b * g_s * g_m - g_b * g_m * R_d + g_b * g_m * g_s * x_2 + g_b * g_m * x_1 - g_b * g_s * R_d + g_b * g_s * x_1 - g_s * g_m * R_d + g_s * g_m * x_1;
            d = g_b * g_m + g_b * g_s + g_s * g_m;

            return s.solveQuadratic(a, b, c, d); //Eq (A55)
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="layer"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static double calcAc(SunlitShadedCanopy s, LeafCanopy canopy, int layer, TranspirationMode mode)
        {
            double assimilation;

            double x_1 = s.VcMaxT[layer];
            double x_2 = s.Kc[layer] * (1 + canopy.oxygenPartialPressure / s.Ko[layer]);

            if (mode == TranspirationMode.unlimited)
            {
                assimilation = calcAssimilation(s, x_1, x_2, layer, canopy);
            }
            else
            {
                assimilation = calcAssimilationDiffusion(s, x_1, x_2, layer, canopy);
            }

            return assimilation;
        }
    }
}
