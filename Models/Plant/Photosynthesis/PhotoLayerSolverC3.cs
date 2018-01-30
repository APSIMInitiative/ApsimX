using System;

namespace Models.PMF.Photosynthesis
{
    //public class PhotoLayerSolverC3 : PhotoLayerSolver
    //{
    //    //PhotosynthesisModel _PM;
    //    //int _layer;

    //    //--------------------------------------------------------------
    //    //public PhotoLayerSolverC3(int nPop, PhotosynthesisModel PM, int layer) :
    //    public PhotoLayerSolverC3(PhotosynthesisModel PM, int layer) :
    //        base(PM, layer)
    //    //base(4, nPop)
    //    //base(2, nPop) //Just 2 now shaded and sunlit Cc
    //    {

    //    }
    //    //    //--------------------------------------------------------------
    //    //    //public void Setup(DEStrategy _strategy, double _scale, double _probability)
    //    //    //{
    //    //    //    double[] _min = new double[2];
    //    //    //    double[] _max = new double[2];

    //    //    //    ////Temperature
    //    //    //    //for (int i = 0; i < 2; i++)
    //    //    //    //{
    //    //    //    //    _min[i] = _PM.envModel.getTemp(_PM.time) - 5;
    //    //    //    //    _max[i] = _PM.envModel.getTemp(_PM.time) + 5;
    //    //    //    //}
    //    //    //    //Ci
    //    //    //    for (int i = 0; i < 2; i++)
    //    //    //    {
    //    //    //        _min[i] = 0;
    //    //    //        _max[i] = 450;
    //    //    //    }

    //    //    //    base.Setup(_min, _max, _strategy, _scale, _probability);
    //    //    //}

    //    //    ////--------------------------------------------------------------
    //    //    //public override double EnergyFunction(double[] testSolution, bool bAtSolution)
    //    //    //{
    //    //    //    //return _PM.sunlit.calcPhotosynthesis(_PM, _layer, testSolution[2], testSolution[0]) +
    //    //    //    //    _PM.sunlit.calcLeafTemperature(_PM, _layer, testSolution[0]) +
    //    //    //    //    _PM.shaded.calcPhotosynthesis(_PM, _layer, testSolution[3], testSolution[1]) +
    //    //    //    //    _PM.shaded.calcLeafTemperature(_PM, _layer, testSolution[1]);

    //    //    //    return calcPhotosynthesis(_PM, _PM.sunlit, _layer, testSolution[0]) +
    //    //    //         calcPhotosynthesis(_PM, _PM.shaded, _layer, testSolution[1]);
    //    //    //}
    //    //    //--------------------------------------------------------------
    //    public override double calcPhotosynthesis(PhotosynthesisModel PM, SunlitShadedCanopy s, int layer, double _Cc)
    //    {
    //        return 0;
    //    }
    //    //    public override double calcPhotosynthesis(PhotosynthesisModel PM, SunlitShadedCanopy s, int layer, double _Cc)
    //    //    {
    //    //        LeafCanopy canopy = PM.canopy;

    //    //        s.calcPhotosynthesis(PM, layer);

    //    //        s.Oi[layer] = canopy.oxygenPartialPressure;

    //    //        //s.Om[layer] = canopy.oxygenPartialPressure;

    //    //        s.Oc[layer] = s.Oi[layer];

    //    //        s.r_[layer] = s.g_[layer] * s.Oc[layer];

    //    //        //This bit needs to be optimised

    //    //        s.Ac[layer] = calcAc(canopy, s, layer);
    //    //        s.Aj[layer] = calcAj(canopy, s, layer);

    //    //        if (s.Ac[layer] < 0 || double.IsNaN(s.Ac[layer]))
    //    //        {
    //    //            s.Ac[layer] = 0;
    //    //        }

    //    //        if (s.Aj[layer] < 0 || double.IsNaN(s.Aj[layer]))
    //    //        {
    //    //            s.Aj[layer] = 0;
    //    //        }

    //    //        //s.A[layer] = Math.Max(0, Math.Min(s.Aj[layer], s.Ac[layer]));
    //    //        s.A[layer] = Math.Min(s.Aj[layer], s.Ac[layer]);


    //    //        //s.gs_CO2[layer] = canopy.gs_0 * s.LAIS[layer] + (s.A[layer] + s.RdT[layer]) / 
    //    //        //    (_Cc - (s.r_[layer] - s.RdT[layer] / s.gm_CO2T[layer])) * canopy.a /
    //    //        //    (1 + s.VPD[layer] / canopy.D0);

    //    //        // s.gs_CO2[layer] = canopy.gs0_CO2 * s.LAIS[layer] + (s.A[layer] + s.RdT[layer]) /
    //    //        //     (_Cc - s.r_[layer]) * s.fVPD[layer];

    //    //        if (PM.conductanceModel == PhotosynthesisModel.ConductanceModel.DETAILED)
    //    //        {
    //    //            // s.Ci[layer] = canopy.Ca - s.A[layer] / s.gb_CO2[layer] - s.A[layer] / s.gs_CO2[layer];
    //    //        }
    //    //        else
    //    //        {
    //    //            s.Ci[layer] = canopy.CPath.CiCaRatio * canopy.Ca;
    //    //        }

    //    //        s.Ccac[layer] = s.Ci[layer] - s.Ac[layer] / s.gm_CO2T[layer];

    //    //        s.Ccaj[layer] = s.Ci[layer] - s.Aj[layer] / s.gm_CO2T[layer];

    //    //        if (s.Ccac[layer] < 0 || double.IsNaN(s.Ccac[layer]))
    //    //        {
    //    //            s.Ccac[layer] = 0;
    //    //        }
    //    //        if (s.Ccaj[layer] < 0 || double.IsNaN(s.Ccaj[layer]))
    //    //        {
    //    //            s.Ccaj[layer] = 0;
    //    //        }

    //    //        if (s.Ac[layer] < s.Aj[layer])
    //    //        {
    //    //            s.Cc[layer] = s.Ac[layer];
    //    //        }
    //    //        else
    //    //        {
    //    //            s.Cc[layer] = s.Aj[layer];
    //    //        }

    //    //        s.Cc[layer] = s.Ci[layer] - s.A[layer] / s.gm_CO2T[layer];
    //    //        if (s.Cc[layer] < 0 || double.IsNaN(s.Cc[layer]))
    //    //        {
    //    //            s.Cc[layer] = 0;
    //    //        }
    //    //        // s.Ccac[layer] = s.Ci[layer] - s.Ac[layer] / s.gm_CO2T[layer];
    //    //        // s.Ccaj[layer] = s.Ci[layer] - s.Aj[layer] / s.gm_CO2T[layer];


    //    //        s.CiCaRatio[layer] = s.Ci[layer] / canopy.Ca;

    //    //        return Math.Pow(s.Cc[layer] - _Cc, 2);
    //    //    }
    //    //    //---------------------------------------------------------------------------------------------------------
    //    //    public double calcAc(LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    //    //    {
    //    //        double x1 = s.VcMaxT[layer];
    //    //        double x2 = s.Kc[layer] * (1 + canopy.oxygenPartialPressure / s.Ko[layer]);


    //    //        double a, b, c, d;

    //    //        a = -1 * canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] - s.gm_CO2T[layer] * x2 + s.RdT[layer] - x1; //Eq	(A56)
    //    //        b = -1 * canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] * s.RdT[layer] + canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] * x1 -
    //    //            s.gm_CO2T[layer] * s.RdT[layer] * x2 - s.gm_CO2T[layer] * s.r_[layer] * x1; // Eq	(A57)
    //    //        c = canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] + s.gm_CO2T[layer] * x2 - s.RdT[layer] + x1; // Eq (A58)
    //    //        d = 1;


    //    //        return (-1 * Math.Pow((Math.Pow(a, 2) - 4 * b), 0.5) + c) / (2 * d); //Eq (A55)
    //    //    }

    //    //    public override double calcAc(double Cc, LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    //    //    {
    //    //        return (Cc - s.r_[layer]) * s.VcMaxT[layer] / (Cc + s.Kc[layer] * (1 + canopy.oxygenPartialPressure / s.Ko[layer])) - s.RdT[layer];
    //    //    }
    //    //    //---------------------------------------------------------------------------------------------------------
    //    //    public double calcAj(LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    //    //    {
    //    //        double x1 = s.J[layer] / 4;
    //    //        double x2 = 2 * s.r_[layer];

    //    //        double a, b, c, d;

    //    //        a = -1 * canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] - s.gm_CO2T[layer] * x2 + s.RdT[layer] - x1; //Eq	(A56)
    //    //        b = -1 * canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] * s.RdT[layer] + canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] * x1 -
    //    //            s.gm_CO2T[layer] * s.RdT[layer] * x2 - s.gm_CO2T[layer] * s.r_[layer] * x1; // Eq	(A57)
    //    //        c = canopy.CPath.CiCaRatio * canopy.Ca * s.gm_CO2T[layer] + s.gm_CO2T[layer] * x2 - s.RdT[layer] + x1; // Eq (A58)
    //    //        d = 1;


    //    //        return (-1 * Math.Pow((Math.Pow(a, 2) - 4 * b), 0.5) + c) / (2 * d); //Eq (A55)
    //    //    }

    //    //    public override double calcAj(double Cc, LeafCanopy canopy, SunlitShadedCanopy s, int layer)
    //    //    {
    //    //        return (Cc - s.r_[layer]) * s.J[layer] / (4 * (Cc + 2 * s.r_[layer])) - s.RdT[layer];
    //    //    }
    //    //}
    }
