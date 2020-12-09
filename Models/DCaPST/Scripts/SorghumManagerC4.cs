using System;
using System.IO;
using ModelFramework;

using Models.DCAPST;
using Models.DCAPST.Interfaces;
using Models.DCAPST.Utilities;

public class Script
{
    [Link] public Simulation MySimulation;
    [Link] Paddock MyPaddock; // Can be used to dynamically get access to simulation structure and variables
    [Input] DateTime Today;   // Equates to the value of the current simulation date - value comes from CLOCK
    [Output] public double[] dcaps = new double[5];

    [Event] public event NullTypeDelegate IntervalStep;

    // Daily Outputs
    [Output] public double BIOtotalDAY;
    [Output] public double BIOshootDAY;
    [Output] public double RootShoot;
    [Output] public double EcanDemand;
    [Output] public double EcanSupply;
    [Output] public double RUE;
    [Output] public double TE;
    [Output] public double RadIntDcapst;
    [Output] public double BIOshootDAYPot;
    [Output] public double SoilWater;

    // Interval outputs
    [Output] public double Hour;
    [Output] public double SunlitTemperature;
    [Output] public double ShadedTemperature;
    [Output] public double SunlitAc1;
    [Output] public double SunlitAc2;
    [Output] public double SunlitAj;
    [Output] public double ShadedAc1;
    [Output] public double ShadedAc2;
    [Output] public double ShadedAj;

    public CanopyParameters CP;
    public PathwayParameters PP;
    public DCAPSTModel DM;

    public double LAITrigger = 0.5;
    public double PsiFactor = 0.4; //psiFactor-Psi Reduction Factor


    // The following event handler will be called once at the beginning of the simulation
    [EventHandler]
    public void OnInitialised()
    {
        CP = Classic.SetUpCanopy(
           CanopyType.C4, // Canopy type
           363, // CO2 partial pressure
           0.7, // Curvature factor
           0.047, // Diffusivity-solubility ratio
           210000, // O2 partial pressure
           0.78, // Diffuse extinction coefficient
           0.8, // Diffuse extinction coefficient NIR
           0.036, // Diffuse reflection coefficient
           0.389, // Diffuse reflection coefficient NIR
           60, // Leaf angle
           0.15, // Leaf scattering coefficient
           0.8, // Leaf scattering coefficient NIR
           0.15, // Leaf width
           1.3, // SLN ratio at canopy top
           14, // Minimum Nitrogen
           1.5, // Wind speed
           1.5);             // Wind speed extinction


        PP = Classic.SetUpPathway(
           0, // jTMin
           37.8649150880407, // jTOpt
           55, // jTMax
           0.711229539802063, // jC
           1, // jBeta
           0, // gTMin
           42, // gTOpt
           55, // gTMax
           0.462820450976839, // gC
           1, // gBeta
           1210, // KcAt25
           64200, // KcFactor
           292000, // KoAt25
           10500, // KoFactor
           5.51328906454566, // VcVoAt25
           21265.4029552906, // VcVoFactor
           75, // KpAt25
           36300, // KpFactor
           78000, // VcFactor
           46390, // RdFactor
           57043.2677590512, // VpFactor
           120, // pepRegeneration
           0.15, // spectralCorrectionFactor
           0.1, // ps2ActivityFraction
           0.003, // bundleSheathConductance
           0.465 * PsiFactor, // maxRubiscoActivitySLNRatio
           2.7 * PsiFactor, // maxElectronTransportSLNRatio
           0.0 * PsiFactor, // respirationSLNRatio
           1.55 * PsiFactor, // maxPEPcActivitySLNRatio
           0.0135 * PsiFactor, // mesophyllCO2ConductanceSLNRatio
           2, // extraATPCost
           0.45);   // intercellularToAirCO2Ratio         

        //Set the LAI trigger
        MyPaddock.Set("laiTrigger", LAITrigger);
    }

    // This routine is called when the plant model wants us to do the calculation
    [EventHandler]
    public void Ondodcaps()
    {
        int DOY = 0;
        double latitude = 0;
        double maxT = 0;
        double minT = 0;
        double radn = 0;
        double RootShootRatio = 0;
        double SLN = 0;
        double SWAvailable = 0;
        double lai = 0;

        MyPaddock.Get("DOY", out DOY);
        MyPaddock.Get("latitude", out latitude);
        MyPaddock.Get("maxT", out maxT);
        MyPaddock.Get("minT", out minT);
        MyPaddock.Get("radn", out radn);
        MyPaddock.Get("RootShootRatio", out RootShootRatio);
        MyPaddock.Get("SLN", out SLN);
        MyPaddock.Get("SWAvailable", out SWAvailable);
        MyPaddock.Get("lai", out lai);

        // Model the photosynthesis
        double rpar = 0.5;
        DCAPSTModel DM = Classic.SetUpModel(CP, PP, DOY, latitude, maxT, minT, radn, rpar);

        // Optional values 
        DM.PrintIntervalValues = false; // Switch to print extra data (default = false)
        DM.Biolimit = 0;     // Biological transpiration limit of the crop (0 disables mechanism)
        DM.Reduction = 0;    // Excess water reduction fraction for bio-limited transpiration (0 disables mechanism)

        // Run the simulation
        DM.DailyRun(lai, SLN, SWAvailable, RootShootRatio);

        // Daily Outputs
        RootShoot = RootShootRatio;
        BIOshootDAY = dcaps[0] = DM.ActualBiomass;
        BIOtotalDAY = BIOshootDAY * (1 + RootShoot);
        EcanDemand = dcaps[1] = DM.WaterDemanded;
        EcanSupply = dcaps[2] = DM.WaterSupplied;
        RadIntDcapst = dcaps[3] = DM.InterceptedRadiation;
        RUE = (RadIntDcapst == 0 ? 0 : BIOshootDAY / RadIntDcapst);
        TE = (EcanSupply == 0 ? 0 : BIOshootDAY / EcanSupply);
        BIOshootDAYPot = dcaps[4] = DM.PotentialBiomass;
        SoilWater = SWAvailable;

        // Interval outputs
        foreach (var interval in DM.Intervals)
        {
            Hour = interval.Time;
            SunlitTemperature = interval.Sunlit.Temperature;
            ShadedTemperature = interval.Shaded.Temperature;
            SunlitAc1 = interval.Sunlit.Ac1.Assimilation;
            SunlitAc2 = interval.Sunlit.Ac2.Assimilation;
            SunlitAj = interval.Sunlit.Aj.Assimilation;
            ShadedAc1 = interval.Shaded.Ac1.Assimilation;
            ShadedAc2 = interval.Shaded.Ac2.Assimilation;
            ShadedAj = interval.Shaded.Aj.Assimilation;

            if (IntervalStep != null) IntervalStep.Invoke();
        }
    }

    // Set its default value to garbage so that we find out quickly
    [EventHandler]
    public void OnPrepare()
    {
        RootShoot = 0;
        BIOshootDAY = 0;
        BIOtotalDAY = 0;
        EcanDemand = 0;
        EcanSupply = 0;
        RadIntDcapst = 0;
        RUE = 0;
        TE = 0;
        BIOshootDAYPot = 0;

        for (int i = 0; i < 5; i++) { dcaps[i] = -1.0f; }
    }
}