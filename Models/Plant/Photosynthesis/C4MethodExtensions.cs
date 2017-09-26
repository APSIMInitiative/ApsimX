using System;
using Models.PMF.Phenology;
using System.Linq;

namespace C4MethodExtensions
{
    public static class C4MethodExtensions
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
        public static bool calcPhotosynthesis(this SunlitShadedCanopy s,PhotosynthesisModel PM, bool useAirTemp, int layer, double leafTemperature, double cm,
            TranspirationMode mode, double maxHourlyT, double Tfraction)
        {
            LeafCanopy canopy = PM.canopy;

            //leafTemp[layer] = PM.envModel.getTemp(PM.time);
            s.leafTemp__[layer] = leafTemperature;

            if (useAirTemp)
            {
                s.leafTemp__[layer] = PM.envModel.getTemp(PM.time);
            }

            s.Cm__[layer] = cm;

            double vpd = PM.envModel.getVPD(PM.time);

            s.VcMaxT[layer] = TempFunctionExp.val(s.leafTemp__[layer], s.VcMax25[layer], canopy.CPath.VcMax_c, canopy.CPath.VcMax_b);
            s.RdT[layer] = TempFunctionExp.val(s.leafTemp__[layer], s.Rd25[layer], canopy.CPath.Rd_c, canopy.CPath.Rd_b);
            s.JMaxT[layer] = TempFunctionNormal.val(s.leafTemp__[layer], s.JMax25[layer], canopy.CPath.JMax_TOpt, canopy.CPath.JMax_Omega);
            s.VpMaxT[layer] = TempFunctionExp.val(s.leafTemp__[layer], s.VpMax25[layer], canopy.CPath.VpMax_c, canopy.CPath.VpMax_b);

            s.Vpr[layer] = canopy.Vpr_l * s.LAIS[layer];

            canopy.ja = (1 - canopy.f) / 2;

            s.J[layer] = (canopy.ja * s.absorbedIrradiance[layer] + s.JMaxT[layer] - Math.Pow(Math.Pow(canopy.ja * s.absorbedIrradiance[layer] + s.JMaxT[layer], 2) -
            4 * canopy.θ * s.JMaxT[layer] * canopy.ja * s.absorbedIrradiance[layer], 0.5)) / (2 * canopy.θ);

            s.Kc[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.Kc_P25, canopy.CPath.Kc_c, canopy.CPath.Kc_b);
            s.Ko[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.Ko_P25, canopy.CPath.Ko_c, canopy.CPath.Ko_b);
            s.VcVo[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.VcMax_VoMax_P25, canopy.CPath.VcMax_VoMax_c, canopy.CPath.VcMax_VoMax_b);

            s.ScO[layer] = s.Ko[layer] / s.Kc[layer] * s.VcVo[layer];

            s.g_[layer] = 0.5 / s.ScO[layer];

            canopy.Sco = s.ScO[layer]; //For reporting ??? 

            s.K_[layer] = s.Kc[layer] * (1 + canopy.oxygenPartialPressure / s.Ko[layer]);

            s.Kp[layer] = TempFunctionExp.val(s.leafTemp__[layer], canopy.CPath.Kp_P25, canopy.CPath.Kp_c, canopy.CPath.Kp_b);

            s.gbs[layer] = canopy.gbs_CO2 * s.LAIS[layer];

            s.Oi[layer] = canopy.oxygenPartialPressure;

            s.Om[layer] = canopy.oxygenPartialPressure;

            if (mode == TranspirationMode.unlimited)
            {
                s.VPD[layer] = PM.envModel.getVPD(PM.time);

                s.gm_CO2T[layer] = s.LAIS[layer] * TempFunctionNormal.val(s.leafTemp__[layer], canopy.CPath.gm_P25, canopy.CPath.gm_TOpt, canopy.CPath.gm_Omega);

                s.Rm[layer] = s.RdT[layer] * 0.5;

                canopy.z = (2 + canopy.fQ - canopy.CPath.fcyc) / (canopy.h * (1 - canopy.CPath.fcyc));

                s.gbs[layer] = canopy.gbs_CO2 * s.LAIS[layer];

                //Caculate A's
                if (s.type == SSType.AC1)
                {
                    s.A[layer] = calcAc(s, canopy, layer, TranspirationMode.unlimited);
                }
                else if (s.type == SSType.AC2)
                {
                    s.A[layer] = calcAc(s, canopy, layer, TranspirationMode.unlimited);
                }
                else if (s.type == SSType.AJ)
                {
                    s.A[layer] = calcAj(s, canopy, layer, TranspirationMode.unlimited);
                }


                s.calcConductanceResistance(PM, canopy);

                s.Ci[layer] = canopy.CPath.CiCaRatio * canopy.Ca;

                s.Cm[layer] = s.Ci[layer] - s.A[layer] / s.gm_CO2T[layer];

                s.xx = s.Cm[layer] * s.x_4 + s.x_5;

                if (s.type == SSType.AC1 || s.type == SSType.AC2)
                {
                    s.Vp[layer] = Math.Min(s.Cm[layer] * s.VpMaxT[layer] / (s.Cm[layer] + s.Kp[layer]), s.Vpr[layer]);
                }
                else if (s.type == SSType.AJ)
                {
                    s.Vp[layer] = canopy.CPath.x * s.J[layer] / 2;
                }

                s.Oc[layer] = canopy.alpha * s.A[layer] / (0.047 * s.gbs[layer]) + s.Om[layer];

                s.r_[layer] = s.g_[layer] * s.Oc[layer];

                s.Cc[layer] = s.Cm[layer] + (s.xx - s.A[layer] - s.Rm[layer]) / s.gbs[layer];

                if (s.Cc[layer] < 0 || double.IsNaN(s.Cc[layer]))
                {
                    s.Cc[layer] = 0;
                }

                s.F[layer] = s.gbs[layer] * (s.Cc[layer] - s.Cm[layer]) / s.xx;

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

                s.calcConductanceResistance(PM, canopy);

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
                else if (s.type == SSType.AC2)
                {
                    s.A[layer] = calcAc(s, canopy, layer, TranspirationMode.limited);
                }
                else if (s.type == SSType.AJ)
                {
                    s.A[layer] = calcAj(s, canopy, layer, TranspirationMode.limited);
                }

                s.Cb[layer] = canopy.Ca - s.A[layer] / s.gbCO2[layer];

                s.Ci[layer] = s.Cb[layer] - s.A[layer] / s.gsCO2[layer];

                s.Cm[layer] = s.Ci[layer] - s.A[layer] / s.gm_CO2T[layer];

                s.xx = s.Cm[layer] * s.x_4 + s. x_5;

                s.Oc[layer] = canopy.alpha * s.A[layer] / (0.047 * s.gbs[layer]) + s.Om[layer];

                s.r_[layer] = s.g_[layer] * s.Oc[layer];

                s.Cc[layer] = s.Cm[layer] + (s.xx - s.A[layer] - s.Rm[layer]) / s.gbs[layer];

                if (s.Cc[layer] < 0 || double.IsNaN(s.Cc[layer]))
                {
                    s.Cc[layer] = 0;
                }

                s.F[layer] = s.gbs[layer] * (s.Cc[layer] - s.Cm[layer]) / s.xx;

            }

            double airTemp = PM.envModel.getTemp(PM.time);

            if (useAirTemp)
            {
                s.leafTemp[layer] = PM.envModel.getTemp(PM.time);
            }

            double diffCm = (s.type == SSType.AC1 ? Math.Abs(s.Cm__[layer] - s.Cm[layer]) : 0);
            double diffTemp = s.leafTemp__[layer] - s.leafTemp[layer];


            s.leafTemp[layer] = (s.leafTemp[layer] + s.leafTemp__[layer]) / 2;

            s.Cm[layer] = (s.Cm[layer] + s.Cm__[layer]) / 2;

            if ((Math.Abs(diffCm) > s.CmTolerance) ||
                (Math.Abs(diffTemp) > s.leafTempTolerance) ||
                double.IsNaN(s.Cm[layer]) || double.IsNaN(s.leafTemp[layer]))
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
            double x_1 = (1 - canopy.CPath.x) * s.J[layer] / 3.0;
            double x_2 = 7.0 / 3.0 * s.g_[layer];
            double x_3 = 0;
            double x_4 = 0;
            double x_5 = canopy.CPath.x * s.J[layer] / 2.0;

            if (mode == TranspirationMode.unlimited)
            {
                assimilation = calcAssimilation(s, x_1, x_2, x_3, x_4, x_5, layer, canopy);
            }
            else
            {
                assimilation = calcAssimilationDiffusion(s, x_1, x_2, x_3, x_4, x_5, layer, canopy);
            }

            return assimilation;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x_1"></param>
        /// <param name="x_2"></param>
        /// <param name="x_3"></param>
        /// <param name="x_4"></param>
        /// <param name="x_5"></param>
        /// <param name="layer"></param>
        /// <param name="canopy"></param>
        /// <returns></returns>
        public static double calcAssimilation(SunlitShadedCanopy s, double x_1, double x_2, double x_3, double x_4, double x_5, int layer, LeafCanopy canopy)
        {
            double a, b, c, d;

            double g_m = s.gm_CO2T[layer];
            double g_bs = s.gbs[layer];
            double α = canopy.alpha;
            double R_d = s.RdT[layer];
            double γ_ = s.g_[layer];
            double O_m = s.Om[layer];
            double R_m = s.Rm[layer];

            double C_a = canopy.Ca;
            double g_s = s.gsCO2[layer];
            double g_b = s.gbCO2[layer];

            double x = canopy.CPath.CiCaRatio;
            double C_i = C_a * canopy.CPath.CiCaRatio;

            a = -C_a * x * 0.047 * g_m * g_bs - C_a * x * 0.047 * g_m * x_4 - α * g_m * R_d * x_2 - α * g_m * γ_ * x_1 - O_m * 0.047 * g_m * g_bs * x_2 - 0.047 * g_m * g_bs * x_3 + 0.047 * g_m * R_m + 0.047 * g_m * R_d - 0.047 * g_m * x_1 - 0.047 * g_m * x_5 + 0.047 * g_bs * R_d - 0.047 * g_bs * x_1 + 0.047 * R_d * x_4 - 0.047 * x_1 * x_4; // Eq   (A56)
            b = (-α * g_m * x_2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x_4) * (-C_a * x * 0.047 * g_m * g_bs * R_d + C_a * x * 0.047 * g_m * g_bs * x_1 - C_a * x * 0.047 * g_m * R_d * x_4 + C_a * x * 0.047 * g_m * x_1 * x_4 - O_m * 0.047 * g_m * g_bs * R_d * x_2 - 0.047 * g_m * g_bs * R_d * x_3 - O_m * 0.047 * g_m * g_bs * γ_ * x_1 + 0.047 * g_m * R_m - 0.047 * g_m * R_m * x_1 - 0.047 * g_m * R_d * x_5 + 0.047 * g_m * x_1 * x_5); // Eq	(A57)
            c = C_a * x * 0.047 * g_m * g_bs + C_a * x * 0.047 * g_m * x_4 + α * g_m * R_d * x_2 + α * g_m * γ_ * x_1 + O_m * 0.047 * g_m * g_bs * x_2 + 0.047 * g_m * g_bs * x_3 - 0.047 * g_m * R_m - 0.047 * g_m * R_d + 0.047 * g_m * x_1 + 0.047 * g_m * x_5 - 0.047 * g_bs * R_d + 0.047 * g_bs * x_1 - 0.047 * R_d * x_4 + 0.047 * x_1 * x_4; // Eq(A58)
            d = -α * g_m * x_2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x_4;  // Eq (A59)

            return s.solveQuadratic(a, b, c, d); //Eq (A55)
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x_1"></param>
        /// <param name="x_2"></param>
        /// <param name="x_3"></param>
        /// <param name="x_4"></param>
        /// <param name="x_5"></param>
        /// <param name="layer"></param>
        /// <param name="canopy"></param>
        /// <returns></returns>
        public static double calcAssimilationDiffusion(SunlitShadedCanopy s, double x_1, double x_2, double x_3, double x_4, double x_5, int layer, LeafCanopy canopy)
        {

            double a, b, c, d;

            double g_m = s.gm_CO2T[layer];
            double g_bs = s.gbs[layer];
            double α = canopy.alpha;
            double R_d = s.RdT[layer];
            double γ_ = s.g_[layer];
            double O_m = s.Om[layer];
            double R_m = s.Rm[layer];

            double C_a = canopy.Ca;
            double g_s = s.gsCO2[layer];
            double g_b = s.gbCO2[layer];

            a = -C_a * 0.047 * g_bs * g_s * g_m * g_b - C_a * 0.047 * x_4 * g_s * g_m * g_b - α * R_d * x_2 * g_s * g_m * g_b - α * γ_ * x_1 * g_s * g_m * g_b + 0.047 * g_bs * R_d * g_s * g_b + 0.047 * g_bs * R_d * g_m * g_b - 0.047 * g_bs * x_1 * g_s * g_b - 0.047 * g_bs * x_1 * g_m * g_b - O_m * 0.047 * g_bs * x_2 * g_s * g_m * g_b - 0.047 * g_bs * x_3 * g_s * g_m * g_b + 0.047 * R_m * g_s * g_m * g_b + 0.047 * R_d * x_4 * g_s * g_m + 0.047 * R_d * x_4 * g_s * g_b + 0.047 * R_d * x_4 * g_m * g_b + 0.047 * R_d * g_s * g_m * g_b - 0.047 * x_1 * x_4 * g_s * g_m - 0.047 * x_1 * x_4 * g_s * g_b - 0.047 * x_1 * x_4 * g_m * g_b - 0.047 * x_1 * g_s * g_m * g_b - 0.047 * x_5 * g_s * g_m * g_b;
            b = (-α * x_2 * g_s * g_m * g_b + 0.047 * g_bs * g_s * g_b + 0.047 * g_bs * g_m * g_b + 0.047 * x_4 * g_s * g_m + 0.047 * x_4 * g_s * g_b + 0.047 * x_4 * g_m * g_b + 0.047 * g_s * g_m * g_b) * (-C_a * 0.047 * g_bs * R_d * g_s * g_m * g_b + C_a * 0.047 * g_bs * x_1 * g_s * g_m * g_b - C_a * 0.047 * R_d * x_4 * g_s * g_m * g_b + C_a * 0.047 * x_1 * x_4 * g_s * g_m * g_b - O_m * 0.047 * g_bs * R_d * x_2 * g_s * g_m * g_b - 0.047 * g_bs * R_d * x_3 * g_s * g_m * g_b - O_m * 0.047 * g_bs * γ_ * x_1 * g_s * g_m * g_b + 0.047 * R_m * R_d * g_s * g_m * g_b - 0.047 * R_m * x_1 * g_s * g_m * g_b - 0.047 * R_d * x_5 * g_s * g_m * g_b + 0.047 * x_1 * x_5 * g_s * g_m * g_b);
            c = C_a * 0.047 * g_bs * g_s * g_m * g_b + C_a * 0.047 * x_4 * g_s * g_m * g_b + α * R_d * x_2 * g_s * g_m * g_b + α * γ_ * x_1 * g_s * g_m * g_b - 0.047 * g_bs * R_d * g_s * g_b - 0.047 * g_bs * R_d * g_m * g_b + 0.047 * g_bs * x_1 * g_s * g_b + 0.047 * g_bs * x_1 * g_m * g_b + O_m * 0.047 * g_bs * x_2 * g_s * g_m * g_b + 0.047 * g_bs * x_3 * g_s * g_m * g_b - 0.047 * R_m * g_s * g_m * g_b - 0.047 * R_d * x_4 * g_s * g_m - 0.047 * R_d * x_4 * g_s * g_b - 0.047 * R_d * x_4 * g_m * g_b - 0.047 * R_d * g_s * g_m * g_b + 0.047 * x_1 * x_4 * g_s * g_m + 0.047 * x_1 * x_4 * g_s * g_b + 0.047 * x_1 * x_4 * g_m * g_b + 0.047 * x_1 * g_s * g_m * g_b + 0.047 * x_5 * g_s * g_m * g_b;
            d = -α * x_2 * g_s * g_m * g_b + 0.047 * g_bs * g_s * g_b + 0.047 * g_bs * g_m * g_b + 0.047 * x_4 * g_s * g_m + 0.047 * x_4 * g_s * g_b + 0.047 * x_4 * g_m * g_b + 0.047 * g_s * g_m * g_b;

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
            double x_2 = s.Kc[layer] / s.Ko[layer];
            double x_3 = s.Kc[layer];

            double x_4 = 0;
            double x_5 = 0;

            if (s.type == SSType.AC1)
            {
                x_4 = s.VpMaxT[layer] / (s.Cm__[layer] + s.Kp[layer]); //Delta Eq (A56)
                x_5 = 0;
            }
            else if (s.type == SSType.AC2)
            {
                x_4 = 0;
                x_5 = s.Vpr[layer];
            }

            if (mode == TranspirationMode.unlimited)
            {
                assimilation = calcAssimilation(s, x_1, x_2, x_3, x_4, x_5, layer, canopy);
            }
            else
            {
                assimilation = calcAssimilationDiffusion(s, x_1, x_2, x_3, x_4, x_5, layer, canopy);
            }

            return assimilation;
        }
    }
}
