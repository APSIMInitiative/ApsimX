using System;

namespace Models.PMF.Phenology
{
    //public class PhotoLayerSolverC4 : PhotoLayerSolver
    //{
    //    PhotosynthesisModel _PM;
    //    int _layer;

    //    //--------------------------------------------------------------
    //    //public PhotoLayerSolverC4(int nPop, PhotosynthesisModel PM, int layer) :
    //    public PhotoLayerSolverC4(PhotosynthesisModel PM, int layer) :
    //        base(PM, layer)
    //    //base(4, nPop)
    //    //base(2, nPop) //Just 2 now shaded and sunlit A
    //    {
    //        _PM = PM;
    //        _layer = layer;
    //    }
    //    //--------------------------------------------------------------
    //    //public void Setup(DEStrategy _strategy, double _scale, double _probability)
    //    //{
    //    //    double[] _min = new double[2];
    //    //    double[] _max = new double[2];

    //    //    //A
    //    //    for (int i = 0; i < 2; i++)
    //    //    {
    //    //        _min[i] = 0;
    //    //        _max[i] = 100;
    //    //    }

    //    //    base.Setup(_min, _max, _strategy, _scale, _probability);
    //    //}

    //    ////--------------------------------------------------------------
    //    //public override double EnergyFunction(double[] testSolution, bool bAtSolution)
    //    //{
    //    //    return calcPhotosynthesis(_PM, _PM.sunlit, _layer, testSolution[0]) +
    //    //        calcPhotosynthesis(_PM, _PM.shaded, _layer, testSolution[1]);

    //    //}
    //    //--------------------------------------------------------------
    //    public override double calcPhotosynthesis(PhotosynthesisModel PM, SunlitShadedCanopy s, int layer, double _A)
    //    {
    //        return 0;
    //    }
    //    //     public override double calcPhotosynthesis(PhotosynthesisModel PM, SunlitShadedCanopy s, int layer, double _A)
    //    //{
    //    //    LeafCanopy canopy = PM.canopy;

    //    //    s.calcPhotosynthesis(PM, layer);

    //    //    //This bit needs to be optimised
    //    //    s.Rm[layer] = s.RdT[layer] * 0.5;

    //    //    canopy.z = (2 + canopy.fQ - canopy.CPath.fcyc) / (canopy.h * (1 - canopy.CPath.fcyc));

    //    //    //canopy.g_ = 0.5 / s.ScO[layer];
    //    //    //s.g_[layer] = 0.5 / s.ScO[layer];
    //    //    //s.r_[layer] = s.g_[layer]

    //    //    s.gbs[layer] = canopy.gbs_CO2 * s.LAIS[layer];

    //    //    s.Oi[layer] = canopy.oxygenPartialPressure;

    //    //    s.Om[layer] = canopy.oxygenPartialPressure;

    //    //    s.gs_CO2[layer] = canopy.gs_CO2 * s.LAIS[layer];

    //    //    s.Kp[layer] = TempFunctionExp.val(s.leafTemp[layer], canopy.CPath.Kp_P25, canopy.CPath.Kp_c, canopy.CPath.Kp_b);
    //    //    //Caculate A's
    //    //    s.Aj[layer] = calcAj(canopy, s, layer);
    //    //    s.Ac[layer] = calcAc(canopy, s, layer);


    //    //    if (s.Ac[layer] < 0 || double.IsNaN(s.Ac[layer]))
    //    //    {
    //    //        s.Ac[layer] = 0;
    //    //    }

    //    //    if (s.Aj[layer] < 0 || double.IsNaN(s.Aj[layer]))
    //    //    {
    //    //        s.Aj[layer] = 0;
    //    //    }

    //    //    s.A[layer] = Math.Max(0, Math.Min(s.Aj[layer], s.Ac[layer]));

    //    //    if (PM.conductanceModel == PhotosynthesisModel.ConductanceModel.DETAILED)
    //    //    {
    //    //        s.Ci[layer] = canopy.Ca - s.A[layer] / s.gb_CO2[layer] - s.A[layer] / s.gs_CO2[layer];
    //    //    }
    //    //    else
    //    //    {
    //    //        s.Ci[layer] = canopy.CPath.CiCaRatio * canopy.Ca;
    //    //    }


    //    //    s.Cm_ac[layer] = s.Ci[layer] - s.Ac[layer] / s.gm_CO2T[layer];
    //    //    s.Cm_aj[layer] = s.Ci[layer] - s.Aj[layer] / s.gm_CO2T[layer];

    //    //    double Vp_ac = Math.Min(s.Cm_ac[layer] * s.VpMaxT[layer] / (s.Cm_ac[layer] + s.Kp[layer]), s.Vpr[layer]);
    //    //    double Vp_aj = canopy.CPath.x * s.J[layer] / 2;

    //    //    s.Oc_ac[layer] = canopy.alpha * s.Ac[layer] / (0.047 * s.gbs[layer]) + s.Om[layer];
    //    //    s.Oc_aj[layer] = canopy.alpha * s.Aj[layer] / (0.047 * s.gbs[layer]) + s.Om[layer];

    //    //    // s.Oc[layer] = canopy.alpha * s.A[layer] / (0.047 * s.gbs[layer]) + s.Om[layer];

    //    //    s.r_ac[layer] = s.g_[layer] * s.Oc_ac[layer];
    //    //    s.r_aj[layer] = s.g_[layer] * s.Oc_aj[layer];

    //    //    s.Ccac[layer] = s.Cm_ac[layer] + (Vp_ac - s.Ac[layer] - s.Rm[layer]) / s.gbs[layer];
    //    //    s.Ccaj[layer] = s.Cm_aj[layer] + (Vp_aj - s.Aj[layer] - s.Rm[layer]) / s.gbs[layer];

    //    //    if (s.Ccac[layer] < 0 || double.IsNaN(s.Ccac[layer]))
    //    //    {
    //    //        s.Ccac[layer] = 0;
    //    //    }
    //    //    if (s.Ccaj[layer] < 0 || double.IsNaN(s.Ccaj[layer]))
    //    //    {
    //    //        s.Ccaj[layer] = 0;
    //    //    }

    //    //    double F_ac = s.gbs[layer] * (s.Ccac[layer] - s.Cm_ac[layer]) / Vp_ac;
    //    //    double F_aj = s.gbs[layer] * (s.Ccaj[layer] - s.Cm_aj[layer]) / Vp_aj;

    //    //    if (s.Ac[layer] < s.Aj[layer])
    //    //    {
    //    //        s.Vp[layer] = Vp_ac;
    //    //        s.Cc[layer] = s.Ccac[layer];
    //    //        s.Cm[layer] = s.Cm_ac[layer];
    //    //        s.Oc[layer] = s.Oc_ac[layer];
    //    //        s.r_[layer] = s.r_ac[layer];
    //    //        s.F[layer] = F_ac;
    //    //    }

    //    //    else
    //    //    {
    //    //        s.Vp[layer] = Vp_aj;
    //    //        s.Cc[layer] = s.Ccaj[layer];
    //    //        s.Cm[layer] = s.Cm_aj[layer];
    //    //        s.Oc[layer] = s.Oc_aj[layer];
    //    //        s.r_[layer] = s.r_aj[layer];
    //    //        s.F[layer] = F_aj;
    //    //    }

    //    //    // s.F[layer] = s.gbs[layer] * (s.Cc[layer] - s.Cm[layer]) / s.Vp[layer];

    //    //    s.CiCaRatio[layer] = s.Ci[layer] / canopy.Ca;

    //    //    //return Math.Pow(s.A[layer] - _A, 2);
    //    //    return 0;
    //    //}

    ////    public double calcAj(LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    ////    {

    ////        double x1 = (1 - canopy.CPath.x) * s.J[layer] / 3.0;
    ////        double x2 = 7.0 / 3.0 * s.g_[layer];
    ////        double x3 = 0;
    ////        double x4 = 0;
    ////        double x5 = canopy.CPath.x * s.J[layer] / 2.0;

    ////        double a, b, c, d;

    ////        double Ci = canopy.CPath.CiCaRatio * canopy.Ca;

    ////        double g_m = s.gm_CO2T[layer];
    ////        double g_bs = s.gbs[layer];
    ////        double α = canopy.alpha;
    ////        double R_d = s.RdT[layer];
    ////        double γ_ = s.g_[layer];
    ////        double O_m = s.Om[layer];
    ////        double R_m = s.Rm[layer];

    ////        a = -Ci * 0.047 * g_m * g_bs - Ci * 0.047 * g_m * x4 - α * g_m * R_d * x2 - α * g_m * γ_ * x1 - O_m * 0.047 * g_m * g_bs * x2 - 0.047 * g_m * g_bs * x3 +
    ////            0.047 * g_m * R_m + 0.047 * g_m * R_d - 0.047 * g_m * x1 - 0.047 * g_m * x5 + 0.047 * g_bs * R_d -
    ////            0.047 * g_bs * x1 + 0.047 * R_d * x4 - 0.047 * x1 * x4; // Eq   (A56)
    ////        b = (-α * g_m * x2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x4) *
    ////        (-Ci * 0.047 * g_m * g_bs * R_d + Ci * 0.047 * g_m * g_bs * x1 - Ci * 0.047 * g_m * R_d * x4 + Ci * 0.047 * g_m * x1 * x4 -
    ////            O_m * 0.047 * g_m * g_bs * R_d * x2 - 0.047 * g_m * g_bs * R_d * x3 - O_m * 0.047 * g_m * g_bs * γ_ * x1 +
    ////            0.047 * g_m * R_m - 0.047 * g_m * R_m * x1 - 0.047 * g_m * R_d * x5 + 0.047 * g_m * x1 * x5); // Eq	(A57)
    ////        c = Ci * 0.047 * g_m * g_bs + Ci * 0.047 * g_m * x4 + α * g_m * R_d * x2 + α * g_m * γ_ * x1 + O_m * 0.047 * g_m * g_bs * x2 +
    ////                        0.047 * g_m * g_bs * x3 - 0.047 * g_m * R_m - 0.047 * g_m * R_d + 0.047 * g_m * x1 + 0.047 * g_m * x5 - 0.047 * g_bs * R_d +
    ////                        0.047 * g_bs * x1 - 0.047 * R_d * x4 + 0.047 * x1 * x4; // Eq(A58)
    ////        d = -α * g_m * x2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x4;  // Eq (A59)


    ////        return (-1 * Math.Pow((Math.Pow(a, 2) - 4 * b), 0.5) + c) / (2 * d); //Eq (A55)
    ////    }

    ////    public override double calcAj(double Cc, LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    ////    {
    ////        // return ((1 - s.r_[layer] / Cc) * (1 - canopy.x) *
    ////        //     s.J[layer] * canopy.z / (3 * (1 + 7 * s.r_[layer] / (3 * Cc))) -
    ////        //     s.RdT[layer]);
    ////        if (Cc == 0)
    ////        {
    ////            return 0;
    ////        }
    ////        // return ((1 - s.r_[layer] / Cc) * (1 - canopy.x) *
    ////        //     s.J[layer] * canopy.z / (3 * (1 + 7 * s.r_[layer] / (3 * Cc))) -
    ////        //     s.RdT[layer]);
    ////        return ((1 - s.r_aj[layer] / Cc) * (1 - canopy.x) *
    ////           s.J[layer] / (3 * (1 + 7 * s.r_[layer] / (3 * Cc))) -
    ////           s.RdT[layer]);
    ////    }


    ////    public double calcAc(LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    ////    {
    ////        int iteration = 1;
    ////        double cm_ = 160;
    ////        while (iteration <= 4)
    ////        {
    ////            cm_ = calcAc(canopy, s, layer, cm_, false, true);
    ////            iteration++;
    ////        }

    ////        s.Cm_ac[layer] = cm_;

    ////        double Ac1 = calcAc(canopy, s, layer, cm_, true, true);
    ////        double Ac2 = calcAc(canopy, s, layer, cm_, true, false);

    ////        return Math.Min(Ac1, Ac2);
    ////    }
    ////    public double calcAc(LeafCanopy canopy, SunlitShadedCanopy s, int layer, double cm_, bool assimilation, bool AC1)
    ////    {

    ////        double x1 = s.VcMaxT[layer];
    ////        double x2 = s.Kc[layer] / s.Ko[layer];
    ////        double x3 = s.Kc[layer];

    ////        double x4, x5;

    ////        if (AC1)
    ////        {
    ////            x4 = s.VpMaxT[layer] / (cm_ + s.Kp[layer]); //Delta Eq (A56)
    ////            x5 = 0;
    ////        }
    ////        else
    ////        {
    ////            x4 = 0;
    ////            x5 = s.Vpr[layer];
    ////        }

    ////        double a, b, c, d;

    ////        double Ci = canopy.CPath.CiCaRatio * canopy.Ca;

    ////        double g_m = s.gm_CO2T[layer];
    ////        double g_bs = s.gbs[layer];
    ////        double α = canopy.alpha;
    ////        double R_d = s.RdT[layer];
    ////        double γ_ = s.g_[layer];
    ////        double O_m = s.Om[layer];
    ////        double R_m = s.Rm[layer];

    ////        double value;

    ////        a = -Ci * 0.047 * g_m * g_bs - Ci * 0.047 * g_m * x4 - α * g_m * R_d * x2 - α * g_m * γ_ * x1 - O_m * 0.047 * g_m * g_bs * x2 - 0.047 * g_m * g_bs * x3 +
    ////             0.047 * g_m * R_m + 0.047 * g_m * R_d - 0.047 * g_m * x1 - 0.047 * g_m * x5 + 0.047 * g_bs * R_d -
    ////             0.047 * g_bs * x1 + 0.047 * R_d * x4 - 0.047 * x1 * x4; // Eq   (A56)
    ////        b = (-α * g_m * x2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x4) *
    ////        (-Ci * 0.047 * g_m * g_bs * R_d + Ci * 0.047 * g_m * g_bs * x1 - Ci * 0.047 * g_m * R_d * x4 + Ci * 0.047 * g_m * x1 * x4 -
    ////            O_m * 0.047 * g_m * g_bs * R_d * x2 - 0.047 * g_m * g_bs * R_d * x3 - O_m * 0.047 * g_m * g_bs * γ_ * x1 +
    ////            0.047 * g_m * R_m - 0.047 * g_m * R_m * x1 - 0.047 * g_m * R_d * x5 + 0.047 * g_m * x1 * x5); // Eq	(A57)
    ////        c = Ci * 0.047 * g_m * g_bs + Ci * 0.047 * g_m * x4 + α * g_m * R_d * x2 + α * g_m * γ_ * x1 + O_m * 0.047 * g_m * g_bs * x2 +
    ////                        0.047 * g_m * g_bs * x3 - 0.047 * g_m * R_m - 0.047 * g_m * R_d + 0.047 * g_m * x1 + 0.047 * g_m * x5 - 0.047 * g_bs * R_d +
    ////                        0.047 * g_bs * x1 - 0.047 * R_d * x4 + 0.047 * x1 * x4; // Eq(A58)
    ////        d = -α * g_m * x2 + 0.047 * g_m + 0.047 * g_bs + 0.047 * x4;  // Eq (A59)

    ////        double ass = (-1 * Math.Pow((Math.Pow(a, 2) - 4 * b), 0.5) + c) / (2 * d); //Eq (A55)

    ////        if (assimilation)
    ////        {
    ////            value = ass;
    ////        }
    ////        else
    ////        {
    ////            value = Ci - ass / g_m; //Eq 43
    ////        }
    ////        return value;
    ////    }

    ////    public override double calcAc(double Cc, LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    ////    {
    ////        return ((Cc - s.r_ac[layer]) * s.VcMaxT[layer] / (Cc + s.Kc[layer] *
    ////            (1 + s.Oc_ac[layer] / s.Ko[layer])) - s.RdT[layer]);
    ////    }
    ////}
}
