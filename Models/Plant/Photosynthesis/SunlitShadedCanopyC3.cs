//using System;
//using System.Linq;

//namespace Models.PMF.Photosynthesis
//{

//    public class SunlitShadedCanopyC3 : SunlitShadedCanopy
//    {
//        //---------------------------------------------------------------------------------------------------------
//        public SunlitShadedCanopyC3():base() { }
//        //---------------------------------------------------------------------------------------------------------
//        public SunlitShadedCanopyC3(int nLayers, SSType type) : base(nLayers, type)
//        {
//            //_nLayers = nLayers;
//            this.type = type;
//            initArrays(nLayers);

//        }
//        //---------------------------------------------------------------------------------------------------------
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="canopy"></param>
//        void calcPhotosynthesis(LeafCanopy canopy)
//        {
//            // for (int i = 1; i < _nLayers + 1; i++)
//            //{
//            //    calcPhotosynthesis(canopy, i);
//            //}
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="PM"></param>
//        /// <param name="canopy"></param>
//        public override void calcConductanceResistance(PhotosynthesisModel PM, LeafCanopy canopy)
//        {
//            for (int i = 0; i < canopy.nLayers; i++)
//            {
//                //gbh[i] = 0.01 * Math.Pow((canopy.us[i] / canopy.leafWidth), 0.5) *
//                //    (1 - Math.Exp(-1 * (0.5 * canopy.ku + canopy.kb) * canopy.LAI)) / (0.5 * canopy.ku + canopy.kb);

//                gbw[i] = gbh[i] / 0.93;

//                rbh[i] = 1 / gbh[i];

//                rbw[i] = 1 / gbw[i];

//                gbw_m[i] = PM.envModel.ATM * PM.canopy.rair * gbw[i];

//                gbCO2[i] = gbw_m[i] / 1.37;

//            }
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="PM"></param>
//        /// <param name="canopy"></param>
//        public override void calcLeafTemp(PhotosynthesisModel PM, LeafCanopy canopy)
//        {
//            base.calcLeafTemp(PM, canopy);
//            //for (int i = 0; i < canopy.nLayers; i++)
//            //{
//            //    gsCO2[i] = A[i] / (((1 - canopy.CPath.CiCaRatio) - A[i] * Math.Pow(gbCO2[i] * canopy.Ca, -1)) * canopy.Ca);

//            //    gsw[i] = gsCO2[i] * 1.6;

//            //    rsw[i] = canopy.rair / gsw[i] * PM.envModel.ATM;

//            //    VPD_la[i] = PM.envModel.calcSVP(leafTemp__[i]) - PM.envModel.calcSVP(PM.envModel.minT);

//            //    double totalAbsorbed = absorbedIrradiancePAR[i] + absorbedIrradianceNIR[i];

//            //    Rn[i] = totalAbsorbed - 2 * (canopy.sigma * Math.Pow(273 + leafTemp__[i], 4) - canopy.sigma * Math.Pow(273 + PM.envModel.getTemp(PM.time), 4));

//            //    Elambda_[i] = (canopy.s * Rn[i] + VPD_la[i] * canopy.rcp / rbh[i]) / (canopy.s + canopy.g * (rsw[i] + rbw[i]) / rbh[i]);

//            //    TDelta[i] = rbh[i] * (Rn[i] - Elambda_[i]) / canopy.rcp;

//            //    leafTemp[i] = PM.envModel.getTemp(PM.time) + TDelta[i];
//            //}
//        }
//        //---------------------------------------------------------------------------------------------------------
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="PM"></param>
//        /// <param name="useAirTemp"></param>
//        /// <param name="layer"></param>
//        /// <param name="leafTemperature"></param>
//        /// <param name="cm"></param>
//        /// <param name="mode"></param>
//        /// <param name="maxHourlyT"></param>
//        /// <param name="Tfraction"></param>
//        /// <returns></returns>
//        public override bool calcPhotosynthesis(PhotosynthesisModel PM, bool useAirTemp, int layer, double leafTemperature, double cm,
//            TranspirationMode mode, double maxHourlyT, double Tfraction)
//        {

//            LeafCanopy canopy = PM.canopy;

//            //calcPhotosynthesis(PM, layer);

//            Oi[layer] = canopy.oxygenPartialPressure;

//            //s.Om[layer] = canopy.oxygenPartialPressure;

//            Oc[layer] = Oi[layer];

//            r_[layer] = g_[layer] * Oc[layer];


//            //Caculate A's
//            if (type == SSType.AC1)
//            {
//                A[layer] = calcAc(canopy, layer, TranspirationMode.unlimited);
//            }
//            else if (type == SSType.AJ)
//            {
//                A[layer] = calcAj(canopy, layer, TranspirationMode.unlimited);
//            }

//            if (A[layer] < 0 || double.IsNaN(A[layer]))
//            {
//                A[layer] = 0;
//            }

//            if (PM.conductanceModel == PhotosynthesisModel.ConductanceModel.DETAILED)
//            {
//                 Ci[layer] = canopy.Ca - A[layer] / gb_CO2[layer] - A[layer] / gs_CO2[layer];
//            }
//            else
//            {
//                Ci[layer] = canopy.CPath.CiCaRatio * canopy.Ca;
//            }

//            Cc[layer] = Ci[layer] - A[layer] / gm_CO2T[layer];

//            if (Cc[layer] < 0 || double.IsNaN(Cc[layer]))
//            {
//                Cc[layer] = 0;
//            }
          

//           CiCaRatio[layer] = Ci[layer] / canopy.Ca;

//         //Back to 



//           // LeafCanopy canopy = PM.canopy;

//            //leafTemp[layer] = PM.envModel.getTemp(PM.time);
//            leafTemp__[layer] = leafTemperature;

//            if (useAirTemp)
//            {
//                leafTemp__[layer] = PM.envModel.getTemp(PM.time);
//            }

//            Cm__[layer] = cm;

//            double vpd = PM.envModel.getVPD(PM.time);

//            VcMaxT[layer] = TempFunctionExp.val(leafTemp__[layer], VcMax25[layer], canopy.CPath.VcMax_c, canopy.CPath.VcMax_b);
//            RdT[layer] = TempFunctionExp.val(leafTemp__[layer], Rd25[layer], canopy.CPath.Rd_c, canopy.CPath.Rd_b);
//            JMaxT[layer] = TempFunctionNormal.val(leafTemp__[layer], JMax25[layer], canopy.CPath.JMax_TOpt, canopy.CPath.JMax_Omega);
//            VpMaxT[layer] = TempFunctionExp.val(leafTemp__[layer], VpMax25[layer], canopy.CPath.VpMax_c, canopy.CPath.VpMax_b);

//            //  J2[layer] = (canopy.a2 * absorbedIrradiance[layer] + J2MaxT[layer] - Math.Pow(Math.Pow(canopy.a2 * absorbedIrradiance[layer] + J2MaxT[layer], 2) -
//            //4 * canopy.θ2 * J2MaxT[layer] * canopy.a2 * absorbedIrradiance[layer], 0.5)) / (2 * canopy.θ2);

//            Vpr[layer] = canopy.Vpr_l * LAIS[layer];

//            canopy.ja = (1 - canopy.f) / 2;

//            J[layer] = (canopy.ja * absorbedIrradiance[layer] + JMaxT[layer] - Math.Pow(Math.Pow(canopy.ja * absorbedIrradiance[layer] + JMaxT[layer], 2) -
//            4 * canopy.θ * JMaxT[layer] * canopy.ja * absorbedIrradiance[layer], 0.5)) / (2 * canopy.θ);

//            Kc[layer] = TempFunctionExp.val(leafTemp__[layer], canopy.CPath.Kc_P25, canopy.CPath.Kc_c, canopy.CPath.Kc_b);
//            Ko[layer] = TempFunctionExp.val(leafTemp__[layer], canopy.CPath.Ko_P25, canopy.CPath.Ko_c, canopy.CPath.Ko_b);
//            VcVo[layer] = TempFunctionExp.val(leafTemp__[layer], canopy.CPath.VcMax_VoMax_P25, canopy.CPath.VcMax_VoMax_c, canopy.CPath.VcMax_VoMax_b);

//            ScO[layer] = Ko[layer] / Kc[layer] * VcVo[layer];

//            g_[layer] = 0.5 / ScO[layer];

//            canopy.Sco = ScO[layer]; //For reporting ??? 

//            K_[layer] = Kc[layer] * (1 + canopy.oxygenPartialPressure / Ko[layer]);

//            Kp[layer] = TempFunctionExp.val(leafTemp__[layer], canopy.CPath.Kp_P25, canopy.CPath.Kp_c, canopy.CPath.Kp_b);

//            gbs[layer] = canopy.gbs_CO2 * LAIS[layer];

//            Oi[layer] = canopy.oxygenPartialPressure;

//            Om[layer] = canopy.oxygenPartialPressure;

//            if (mode == TranspirationMode.unlimited)
//            {
//                //canopy.CPath.CiCaRatio = canopy.CPath.CiCaRatioSlope * vpd + canopy.CPath.CiCaRatioIntercept;

//                //es[layer] = 5.637E-7 * Math.Pow(leafTemp__[layer], 4) + 1.728E-5 * Math.Pow(leafTemp__[layer], 3) + 1.534E-3 *
//                //    Math.Pow(leafTemp__[layer], 2) + 4.424E-2 * leafTemp__[layer] + 6.095E-1;

//                ////VPD[layer] = es[layer] - canopy.Vair;

//                //canopy.airDensity = 1.295 * Math.Exp(-3.6E-3 * PM.envModel.getTemp(PM.time));

//                //canopy.ra = canopy.airDensity * 1000.0 / 28.966;


//                VPD[layer] = PM.envModel.getVPD(PM.time);

//                //fVPD[layer] = canopy.a / (1 + VPD[layer] / canopy.Do);

//                //gs[layer] = fVPD[layer];

//                gm_CO2T[layer] = LAIS[layer] * TempFunctionNormal.val(leafTemp__[layer], canopy.CPath.gm_P25, canopy.CPath.gm_TOpt, canopy.CPath.gm_Omega);

//                //gb_CO2[layer] = 1 / canopy.rb_CO2s[layer] * LAIS[layer] * canopy.ra;

//                Rm[layer] = RdT[layer] * 0.5;

//                canopy.z = (2 + canopy.fQ - canopy.CPath.fcyc) / (canopy.h * (1 - canopy.CPath.fcyc));

//                //gbs[layer] = canopy.gbs_CO2 * LAIS[layer];

//                //gs_CO2[layer] = canopy.gs_CO2 * LAIS[layer];

//                gbs[layer] = canopy.gbs_CO2 * LAIS[layer];

//                //Caculate A's
//                if (type == SSType.AC1)
//                {
//                    A[layer] = calcAc(canopy, layer, TranspirationMode.unlimited);
//                }
//                else if (type == SSType.AC2)
//                {
//                    A[layer] = calcAc(canopy, layer, TranspirationMode.unlimited);
//                }
//                else if (type == SSType.AJ)
//                {
//                    A[layer] = calcAj(canopy, layer, TranspirationMode.unlimited);
//                }


//                calcConductanceResistance(PM, canopy);

//                Ci[layer] = canopy.CPath.CiCaRatio * canopy.Ca;

//                Cm[layer] = Ci[layer] - A[layer] / gm_CO2T[layer];
//                //if (double.IsNaN(Cm[layer]))
//                //{
//                //    Cm[layer] = 1000;
//                //}

//                xx = Cm[layer] * x_4 + x_5;

//                if (type == SSType.AC1 || type == SSType.AC2)
//                {
//                    Vp[layer] = Math.Min(Cm[layer] * VpMaxT[layer] / (Cm[layer] + Kp[layer]), Vpr[layer]);
//                }
//                else if (type == SSType.AJ)
//                {
//                    Vp[layer] = canopy.CPath.x * J[layer] / 2;
//                }

//                Oc[layer] = canopy.alpha * A[layer] / (0.047 * gbs[layer]) + Om[layer];

//                r_[layer] = g_[layer] * Oc[layer];

//                //Cc[layer] = Cm[layer] + (Vp[layer] - A[layer] - Rm[layer]) / gbs[layer];

//                Cc[layer] = Cm[layer] + (xx - A[layer] - Rm[layer]) / gbs[layer];

//                if (Cc[layer] < 0 || double.IsNaN(Cc[layer]))
//                {
//                    Cc[layer] = 0;
//                }

//                F[layer] = gbs[layer] * (Cc[layer] - Cm[layer]) / xx;

//                //CiCaRatio[layer] = Ci[layer] / canopy.Ca;

//                //Calc leaf tepmperature
//                calcLeafTemp(PM, canopy);
//            }

//            else if (mode == TranspirationMode.limited)
//            {
//                double supplymmhr = maxHourlyT * Tfraction;

//                Elambda[layer] = supplymmhr / (0.001 * 3600) * canopy.lambda / 1000;

//                double totalAbsorbed = absorbedIrradiancePAR[layer] + absorbedIrradianceNIR[layer];
//                Rn[layer] = totalAbsorbed - 2 * (canopy.sigma * Math.Pow(273 + leafTemp__[layer], 4) - canopy.sigma * Math.Pow(273 + PM.envModel.getTemp(PM.time), 4));

//                calcConductanceResistance(PM, canopy);

//                leafTemp[layer] = rbh[layer] * (Rn[layer] - Elambda[layer]) / canopy.rcp + PM.envModel.getTemp(PM.time);

//                VPD_la[layer] = PM.envModel.calcSVP(leafTemp__[layer]) - PM.envModel.calcSVP(PM.envModel.minT);

//                rsw[layer] = ((canopy.s * Rn[layer] + VPD_la[layer] * canopy.rcp / rbh[layer]) / Elambda[layer] - canopy.s) *
//                    rbh[layer] / canopy.g - rbw[layer];

//                gsw[layer] = canopy.rair / rsw[layer] * PM.envModel.ATM;


//                gsCO2[layer] = gsw[layer] / 1.6;

//                gm_CO2T[layer] = LAIS[layer] * TempFunctionNormal.val(leafTemp__[layer], canopy.CPath.gm_P25, canopy.CPath.gm_TOpt, canopy.CPath.gm_Omega);

//                //Caculate A's
//                if (type == SSType.AC1)
//                {
//                    A[layer] = calcAc(canopy, layer, TranspirationMode.limited);
//                }
//                else if (type == SSType.AC2)
//                {
//                    A[layer] = calcAc(canopy, layer, TranspirationMode.limited);
//                }
//                else if (type == SSType.AJ)
//                {
//                    A[layer] = calcAj(canopy, layer, TranspirationMode.limited);
//                }

//                Cb[layer] = canopy.Ca - A[layer] / gbCO2[layer];

//                Ci[layer] = Cb[layer] - A[layer] / gsCO2[layer];

//                Cm[layer] = Ci[layer] - A[layer] / gm_CO2T[layer];

//                xx = Cm[layer] * x_4 + x_5;

//                //if (type == SSType.AC1 || type == SSType.AC2)
//                //{
//                //    Vp[layer] = Math.Min(Cm[layer] * VpMaxT[layer] / (Cm[layer] + Kp[layer]), Vpr[layer]);
//                //}
//                //else if (type == SSType.AJ)
//                //{
//                //    Vp[layer] = canopy.CPath.x * J[layer] / 2;
//                //}

//                Oc[layer] = canopy.alpha * A[layer] / (0.047 * gbs[layer]) + Om[layer];

//                r_[layer] = g_[layer] * Oc[layer];

//                //Cc[layer] = Cm[layer] + (Vp[layer] - A[layer] - Rm[layer]) / gbs[layer];

//                Cc[layer] = Cm[layer] + (xx - A[layer] - Rm[layer]) / gbs[layer];

//                if (Cc[layer] < 0 || double.IsNaN(Cc[layer]))
//                {
//                    Cc[layer] = 0;
//                }

//                F[layer] = gbs[layer] * (Cc[layer] - Cm[layer]) / xx;

//            }

//            double airTemp = PM.envModel.getTemp(PM.time);
//            //double tempThreshold = 5;

//            //leafTemp_1[layer] = airTemp;
//            //leafTemp_2[layer] = airTemp;

//            //leafTemp_1[layer] = leafTemp_2[layer];
//            //leafTemp_2[layer] = leafTemp[layer];

//            if (useAirTemp)
//            {
//                leafTemp[layer] = PM.envModel.getTemp(PM.time);
//            }

//            double diffCm = (type == SSType.AC1 ? Math.Abs(Cm__[layer] - Cm[layer]) : 0);
//            double diffTemp = leafTemp__[layer] - leafTemp[layer];


//            leafTemp[layer] = (leafTemp[layer] + leafTemp__[layer]) / 2;

//            //Cm_1[layer] = Cm_2[layer];
//            //Cm_2[layer] = Cm[layer];

//            Cm[layer] = (Cm[layer] + Cm__[layer]) / 2;

//            //leafTemp[layer] = leafTemp[layer] >= PM.envModel.getTemp(PM.time)? 
//            //    Math.Min(leafTemp[layer], airTemp + tempThreshold) : 
//            //    Math.Max(leafTemp[layer], airTemp - tempThreshold);


//            //diffTemp = 0;


//            if ((Math.Abs(diffCm) > CmTolerance) ||
//                (Math.Abs(diffTemp) > leafTempTolerance) ||
//                double.IsNaN(Cm[layer]) || double.IsNaN(leafTemp[layer]))
//            {
//                return false;
//            }
//            return true;

//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="canopy"></param>
//        /// <param name="layer"></param>
//        /// <param name="mode"></param>
//        /// <returns></returns>
//        public override double calcAj(LeafCanopy canopy, int layer, TranspirationMode mode)
//        {
//            double assimilation = 0;
//            double x_1 = J[layer] / 4;
//            double x_2 = 2 * r_[layer];

//            if (mode == TranspirationMode.unlimited)
//            {
//                assimilation = calcAssimilation(x_1, x_2, layer, canopy);
//            }
//            else
//            {
//                assimilation = calcAssimilationDiffusion(x_1, x_2, layer, canopy);
//            }

//            return assimilation;
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="x_1"></param>
//        /// <param name="x_2"></param>
//        /// <param name="layer"></param>
//        /// <param name="canopy"></param>
//        /// <returns></returns>
//        public double calcAssimilation(double x_1, double x_2, int layer, LeafCanopy canopy)
//        {
//            double a, b, c, d;

//            double g_m = gm_CO2T[layer];
//            double g_bs = gbs[layer];
//            double α = canopy.alpha;
//            double R_d = RdT[layer];
//            double γ_ = g_[layer];
//            double O_m = Om[layer];
//            double R_m = Rm[layer];

//            double C_a = canopy.Ca;
//            double g_s = gsCO2[layer];
//            double g_b = gbCO2[layer];
//            double Γ_ = r_[layer];

//            double x = canopy.CPath.CiCaRatio;
//            double C_i = C_a * canopy.CPath.CiCaRatio;

//            a = -C_i / C_a * C_a * g_m - g_m * x_2 + R_d - x_1;  // (A3)
//            b = -C_i / C_a * C_a * g_m * R_d + C_i / C_a * C_a * g_m * x_1 - g_m * R_d * x_2 - g_m * Γ_ * x_1;  //   (A4)
//            c = C_i / C_a * C_a * g_m + g_m * x_2 - R_d + x_1;   // (A5)
//            d = 1;

//            return solveQuadratic(a, b, c, d); //Eq (A55)
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="x_1"></param>
//        /// <param name="x_2"></param>
//        /// <param name="layer"></param>
//        /// <param name="canopy"></param>
//        /// <returns></returns>
//        public double calcAssimilationDiffusion(double x_1, double x_2, int layer, LeafCanopy canopy)
//        {
//            double a, b, c, d;

//            double g_m = gm_CO2T[layer];
//            double g_bs = gbs[layer];
//            double α = canopy.alpha;
//            double R_d = RdT[layer];
//            double γ_ = g_[layer];
//            double O_m = Om[layer];
//            double R_m = Rm[layer];

//            double C_a = canopy.Ca;
//            double g_s = gsCO2[layer];
//            double g_b = gbCO2[layer];
//            double Γ_ = r_[layer];

//            a = -C_a * g_b * g_s * g_m + g_b * g_m * R_d - g_b * g_m * g_s * x_2 - g_b * g_m * g_s * x_1 + g_b * g_s * R_d - g_b * g_s * x_1 + g_m * g_s * R_d - g_m * g_s * x_1;
//            b = (g_b * g_m + g_b * g_s + g_s * g_m) * (-C_a * g_b * g_s * g_m * R_d + C_a * g_b * g_s * g_m * x_1 - g_b * g_s * g_m * R_d * x_2 - g_b * g_s * g_m * Γ_ * x_1);
//            c = C_a * g_b * g_s * g_m - g_b * g_m * R_d + g_b * g_s * g_m * x_2 + g_b * g_m * x_1 - g_b * g_s * R_d + g_b * g_s * x_1 - g_s * g_m * R_d + g_s * g_m * x_1;
//            d = g_b * g_m + g_b * g_s + g_s * g_m;

//            return solveQuadratic(a, b, c, d); //Eq (A55)
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="canopy"></param>
//        /// <param name="layer"></param>
//        /// <param name="mode"></param>
//        /// <returns></returns>
//        public override double calcAc(LeafCanopy canopy, int layer, TranspirationMode mode)
//        {
//            double assimilation;

//            double x_1 = VcMaxT[layer];
//            double x_2 = Kc[layer] * (1 + canopy.oxygenPartialPressure / Ko[layer]);

//            if (mode == TranspirationMode.unlimited)
//            {
//                assimilation = calcAssimilation(x_1, x_2, layer, canopy);
//            }
//            else
//            {
//                assimilation = calcAssimilationDiffusion(x_1, x_2, layer, canopy);
//            }

//            return assimilation;
//        }
//    }
//}
