//using System;
//using System.Linq;

//namespace Models.PMF.Photosynthesis
//{
//    public abstract class SunlitShadedCanopyC4
//    {
//        //---------------------------------------------------------------------------------------------------------
//        public SunlitShadedCanopyC4() { }
//        //---------------------------------------------------------------------------------------------------------
//        public SunlitShadedCanopyC4(int nLayers, SSType type) : base(nLayers, type)
//        {
//            //_nLayers = nLayers;
//            this.type = type;
//            initArrays(nLayers);

//        }

//        //---------------------------------------------------------------------------------------------------------
//        public virtual void calcLAI(LeafCanopy canopy, SunlitShadedCanopyC4 counterpart) { }
//        public virtual void calcIncidentRadiation(EnvironmentModel EM, LeafCanopy canopy, SunlitShadedCanopyC4 counterpart) { }
//        public virtual void calcAbsorbedRadiation(EnvironmentModel EM, LeafCanopy canopy, SunlitShadedCanopyC4 counterpart) { }
//        public virtual void calcMaxRates(LeafCanopy canopy, SunlitShadedCanopyC4 counterpart, PhotosynthesisModel EM) { }
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
//        public virtual void calcLeafTemp(PhotosynthesisModel PM, LeafCanopy canopy)
//        {
//            for (int i = 0; i < canopy.nLayers; i++)
//            {
//                gsCO2[i] = A[i] / (((1 - canopy.CPath.CiCaRatio) - A[i] * Math.Pow(gbCO2[i] * canopy.Ca, -1)) * canopy.Ca);

//                gsw[i] = gsCO2[i] * 1.6;

//                rsw[i] = canopy.rair / gsw[i] * PM.envModel.ATM;

//                VPD_la[i] = PM.envModel.calcSVP(leafTemp__[i]) - PM.envModel.calcSVP(PM.envModel.minT);

//                double totalAbsorbed = absorbedIrradiancePAR[i] + absorbedIrradianceNIR[i];

//                Rn[i] = totalAbsorbed - 2 * (canopy.sigma * Math.Pow(273 + leafTemp__[i], 4) - canopy.sigma * Math.Pow(273 + PM.envModel.getTemp(PM.time), 4));

//                Elambda_[i] = (canopy.s * Rn[i] + VPD_la[i] * canopy.rcp / rbh[i]) / (canopy.s + canopy.g * (rsw[i] + rbw[i]) / rbh[i]);

//                TDelta[i] = rbh[i] * (Rn[i] - Elambda_[i]) / canopy.rcp;

//                leafTemp[i] = PM.envModel.getTemp(PM.time) + TDelta[i];
//            }
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
//                VPD[layer] = PM.envModel.getVPD(PM.time);

//                gm_CO2T[layer] = LAIS[layer] * TempFunctionNormal.val(leafTemp__[layer], canopy.CPath.gm_P25, canopy.CPath.gm_TOpt, canopy.CPath.gm_Omega);

//                Rm[layer] = RdT[layer] * 0.5;

//                canopy.z = (2 + canopy.fQ - canopy.CPath.fcyc) / (canopy.h * (1 - canopy.CPath.fcyc));

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

//                Cc[layer] = Cm[layer] + (xx - A[layer] - Rm[layer]) / gbs[layer];

//                if (Cc[layer] < 0 || double.IsNaN(Cc[layer]))
//                {
//                    Cc[layer] = 0;
//                }

//                F[layer] = gbs[layer] * (Cc[layer] - Cm[layer]) / xx;


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

//                Oc[layer] = canopy.alpha * A[layer] / (0.047 * gbs[layer]) + Om[layer];

//                r_[layer] = g_[layer] * Oc[layer];

//                Cc[layer] = Cm[layer] + (xx - A[layer] - Rm[layer]) / gbs[layer];

//                if (Cc[layer] < 0 || double.IsNaN(Cc[layer]))
//                {
//                    Cc[layer] = 0;
//                }

//                F[layer] = gbs[layer] * (Cc[layer] - Cm[layer]) / xx;

//            }

//            double airTemp = PM.envModel.getTemp(PM.time);

//            if (useAirTemp)
//            {
//                leafTemp[layer] = PM.envModel.getTemp(PM.time);
//            }

//            double diffCm = (type == SSType.AC1 ? Math.Abs(Cm__[layer] - Cm[layer]) : 0);
//            double diffTemp = leafTemp__[layer] - leafTemp[layer];


//            leafTemp[layer] = (leafTemp[layer] + leafTemp__[layer]) / 2;

//            Cm[layer] = (Cm[layer] + Cm__[layer]) / 2;

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
//            double x_1 = (1 - canopy.CPath.x) * J[layer] / 3.0;
//            double x_2 = 7.0 / 3.0 * g_[layer];
//            double x_3 = 0;
//            double x_4 = 0;
//            double x_5 = canopy.CPath.x * J[layer] / 2.0;

//            if (mode == TranspirationMode.unlimited)
//            {
//                assimilation = calcAssimilation(x_1, x_2, x_3, x_4, x_5, layer, canopy);
//            }
//            else
//            {
//                assimilation = calcAssimilationDiffusion(x_1, x_2, x_3, x_4, x_5, layer, canopy);
//            }

//            return assimilation;
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="x_1"></param>
//        /// <param name="x_2"></param>
//        /// <param name="x_3"></param>
//        /// <param name="x_4"></param>
//        /// <param name="x_5"></param>
//        /// <param name="layer"></param>
//        /// <param name="canopy"></param>
//        /// <returns></returns>
//        public double calcAssimilation(double x_1, double x_2, double x_3, double x_4, double x_5, int layer, LeafCanopy canopy)
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

//            double x = canopy.CPath.CiCaRatio;
//            double C_i = C_a * canopy.CPath.CiCaRatio;

//            a = -C_a * x * 0.047 * g_m * g_bs - C_a * x * 0.047 * g_m * x_4 - α * g_m * R_d * x_2 - α * g_m * γ_ * x_1 - O_m * 0.047 * g_m * g_bs * x_2 - 0.047 * g_m * g_bs * x_3 + 0.047 * g_m * R_m + 0.047 * g_m * R_d - 0.047 * g_m * x_1 - 0.047 * g_m * x_5 + 0.047 * g_bs * R_d - 0.047 * g_bs * x_1 + 0.047 * R_d * x_4 - 0.047 * x_1 * x_4; // Eq   (A56)
//            b = (-α * g_m * x_2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x_4) * (-C_a * x * 0.047 * g_m * g_bs * R_d + C_a * x * 0.047 * g_m * g_bs * x_1 - C_a * x * 0.047 * g_m * R_d * x_4 + C_a * x * 0.047 * g_m * x_1 * x_4 - O_m * 0.047 * g_m * g_bs * R_d * x_2 - 0.047 * g_m * g_bs * R_d * x_3 - O_m * 0.047 * g_m * g_bs * γ_ * x_1 + 0.047 * g_m * R_m - 0.047 * g_m * R_m * x_1 - 0.047 * g_m * R_d * x_5 + 0.047 * g_m * x_1 * x_5); // Eq	(A57)
//            c = C_a * x * 0.047 * g_m * g_bs + C_a * x * 0.047 * g_m * x_4 + α * g_m * R_d * x_2 + α * g_m * γ_ * x_1 + O_m * 0.047 * g_m * g_bs * x_2 + 0.047 * g_m * g_bs * x_3 - 0.047 * g_m * R_m - 0.047 * g_m * R_d + 0.047 * g_m * x_1 + 0.047 * g_m * x_5 - 0.047 * g_bs * R_d + 0.047 * g_bs * x_1 - 0.047 * R_d * x_4 + 0.047 * x_1 * x_4; // Eq(A58)
//            d = -α * g_m * x_2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x_4;  // Eq (A59)

//            return solveQuadratic(a, b, c, d); //Eq (A55)
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="x_1"></param>
//        /// <param name="x_2"></param>
//        /// <param name="x_3"></param>
//        /// <param name="x_4"></param>
//        /// <param name="x_5"></param>
//        /// <param name="layer"></param>
//        /// <param name="canopy"></param>
//        /// <returns></returns>
//        public double calcAssimilationDiffusion(double x_1, double x_2, double x_3, double x_4, double x_5, int layer, LeafCanopy canopy)
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

//            a = -C_a * 0.047 * g_bs * g_s * g_m * g_b - C_a * 0.047 * x_4 * g_s * g_m * g_b - α * R_d * x_2 * g_s * g_m * g_b - α * γ_ * x_1 * g_s * g_m * g_b + 0.047 * g_bs * R_d * g_s * g_b + 0.047 * g_bs * R_d * g_m * g_b - 0.047 * g_bs * x_1 * g_s * g_b - 0.047 * g_bs * x_1 * g_m * g_b - O_m * 0.047 * g_bs * x_2 * g_s * g_m * g_b - 0.047 * g_bs * x_3 * g_s * g_m * g_b + 0.047 * R_m * g_s * g_m * g_b + 0.047 * R_d * x_4 * g_s * g_m + 0.047 * R_d * x_4 * g_s * g_b + 0.047 * R_d * x_4 * g_m * g_b + 0.047 * R_d * g_s * g_m * g_b - 0.047 * x_1 * x_4 * g_s * g_m - 0.047 * x_1 * x_4 * g_s * g_b - 0.047 * x_1 * x_4 * g_m * g_b - 0.047 * x_1 * g_s * g_m * g_b - 0.047 * x_5 * g_s * g_m * g_b;
//            b = (-α * x_2 * g_s * g_m * g_b + 0.047 * g_bs * g_s * g_b + 0.047 * g_bs * g_m * g_b + 0.047 * x_4 * g_s * g_m + 0.047 * x_4 * g_s * g_b + 0.047 * x_4 * g_m * g_b + 0.047 * g_s * g_m * g_b) * (-C_a * 0.047 * g_bs * R_d * g_s * g_m * g_b + C_a * 0.047 * g_bs * x_1 * g_s * g_m * g_b - C_a * 0.047 * R_d * x_4 * g_s * g_m * g_b + C_a * 0.047 * x_1 * x_4 * g_s * g_m * g_b - O_m * 0.047 * g_bs * R_d * x_2 * g_s * g_m * g_b - 0.047 * g_bs * R_d * x_3 * g_s * g_m * g_b - O_m * 0.047 * g_bs * γ_ * x_1 * g_s * g_m * g_b + 0.047 * R_m * R_d * g_s * g_m * g_b - 0.047 * R_m * x_1 * g_s * g_m * g_b - 0.047 * R_d * x_5 * g_s * g_m * g_b + 0.047 * x_1 * x_5 * g_s * g_m * g_b);
//            c = C_a * 0.047 * g_bs * g_s * g_m * g_b + C_a * 0.047 * x_4 * g_s * g_m * g_b + α * R_d * x_2 * g_s * g_m * g_b + α * γ_ * x_1 * g_s * g_m * g_b - 0.047 * g_bs * R_d * g_s * g_b - 0.047 * g_bs * R_d * g_m * g_b + 0.047 * g_bs * x_1 * g_s * g_b + 0.047 * g_bs * x_1 * g_m * g_b + O_m * 0.047 * g_bs * x_2 * g_s * g_m * g_b + 0.047 * g_bs * x_3 * g_s * g_m * g_b - 0.047 * R_m * g_s * g_m * g_b - 0.047 * R_d * x_4 * g_s * g_m - 0.047 * R_d * x_4 * g_s * g_b - 0.047 * R_d * x_4 * g_m * g_b - 0.047 * R_d * g_s * g_m * g_b + 0.047 * x_1 * x_4 * g_s * g_m + 0.047 * x_1 * x_4 * g_s * g_b + 0.047 * x_1 * x_4 * g_m * g_b + 0.047 * x_1 * g_s * g_m * g_b + 0.047 * x_5 * g_s * g_m * g_b;
//            d = -α * x_2 * g_s * g_m * g_b + 0.047 * g_bs * g_s * g_b + 0.047 * g_bs * g_m * g_b + 0.047 * x_4 * g_s * g_m + 0.047 * x_4 * g_s * g_b + 0.047 * x_4 * g_m * g_b + 0.047 * g_s * g_m * g_b;

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
//            double x_2 = Kc[layer] / Ko[layer];
//            double x_3 = Kc[layer];

//            double x_4 = 0;
//            double x_5 = 0;

//            if (type == SSType.AC1)
//            {
//                x_4 = VpMaxT[layer] / (Cm__[layer] + Kp[layer]); //Delta Eq (A56)
//                x_5 = 0;
//            }
//            else if (type == SSType.AC2)
//            {
//                x_4 = 0;
//                x_5 = Vpr[layer];
//            }

//            if (mode == TranspirationMode.unlimited)
//            {
//                assimilation = calcAssimilation(x_1, x_2, x_3, x_4, x_5, layer, canopy);
//            }
//            else
//            {
//                assimilation = calcAssimilationDiffusion(x_1, x_2, x_3, x_4, x_5, layer, canopy);
//            }

//            return assimilation;
//        }
//    }
//}
