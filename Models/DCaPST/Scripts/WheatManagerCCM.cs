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
    [Output] public double[] dcapst = new double[5];

    [Event] public event NullTypeDelegate IntervalStep;

    // Daily Outputs
    [Output] public double BIOtotalDAY;
    [Output] public double BIOshootDAY;
    [Output] public double RootShoot;
    [Output] public double EcanDemand;
    [Output] public double EcanSupply;
    [Output] public double RUE;
    [Output] public double TE;
    [Output] public double RadIntDcaps;
    [Output] public double BIOshootDAYPot;
    [Output] public double SoilWater;
    [Output] public double dsln;

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

    public double NLAITrigger = 1.0; // default = 1.0
    public double LAITrigger = 0.5;  // default = 0.5
    public double NLowLimit = 0.0;   // default = 0.5
    public double PsiFactor = 1.0;   // Psi reduction factor

    // DCaPST is disabled to begin with
    bool dcapstON = false;

    // The alternative photosystem model
    string PsModelName1;

    // The following event handler will be called once at the beginning of the simulation
    [EventHandler]
    public void OnInitialised()
    {
        /* Do NOT change the order of these values */
        CP = Classic.SetUpCanopy(
           CanopyType.CCM, // Canopy type
           370, // CO2 partial pressure
           0.7, // Empirical curvature factor
           0.047, // Diffusivity-solubility ratio (Used in CCM)
           210000, // O2 partial pressure
           0.78, // PAR diffuse extinction coefficient
           0.8, // NIR diffuse extinction coefficient
           0.036, // PAR diffuse reflection coefficient
           0.389, // NIR diffuse reflection coefficient
           60, // Leaf angle
           0.15, // PAR leaf scattering coefficient
           0.8, // NIR leaf scattering coefficient
           0.05, // Leaf width
           1.3, // SLN ratio at canopy top
           14, // Minimum structural nitrogen
           1.5, // Wind speed
           1.5); // Wind speed profile distribution coefficient

        PP = Classic.SetUpPathway(
           0, // Electron transport minimum temperature
           30.0, // Electron transport optimum temperature
           45.0, // Electron transport maximum temperature
           0.911017958600129, // Electron transport scaling constant
           1, // Electron transport Beta value

           0, // Mesophyll conductance minimum temperature
           29.2338417788683, // Mesophyll conductance optimum temperature
           45, // Mesophyll conductance maximum temperature
           0.875790608584141, // Mesophyll conductance scaling constant
           1, // Mesophyll conductance Beta value

           17.52 * 273.422964228666, // Kc25 - Michaelis Menten constant of Rubisco carboxylation at 25 degrees C (Changed in CCM)*********************
           93720, // KcFactor

           1.34 * 165824.064155384, // Ko25 - Michaelis Menten constant of Rubisco oxygenation at 25 degrees C (Changed in CCM)*********************
           33600, // KoFactor

           13.07 * 4.59217066521612, // VcVo25 - Rubisco carboxylation to oxygenation at 25 degrees C (Changed in CCM)*********************
           35713.19871277176, // VcVoFactor

           75, // Kp25 - Michaelis Menten constant of PEPc activity at 25 degrees C (Used in CCM)
           36300, // KpFactor (Used in CCM)

           65330, // VcFactor
           46390, // RdFactor
           57043.2677590512, // VpFactor (Used in CCM)

           400.0, // PEPc regeneration (Used in CCM)
           0.15, // Spectral correction factor
           0.1, // Photosystem II activity fraction (used in CCM)
           0.003, // Bundle sheath CO2 conductance (Used in CCM)********************
           1.1 * PsiFactor, // Max Rubisco activity to SLN ratio
           1.9484 * PsiFactor, // Max electron transport to SLN ratio (Changed in CCM)********************
           0.0 * PsiFactor, // Respiration to SLN ratio
           1 * PsiFactor, // Max PEPc activity to SLN ratio (Changed in CCM)*********************0.373684157583268
           0.00412 * PsiFactor, // Mesophyll CO2 conductance to SLN ratio
           0.75, // Extra ATP cost (Changed in CCM)*********************
           0.7); // Intercellular CO2 to air CO2 ratio         

        //Set the LAI trigger
        MyPaddock.Set("DCaPSTTriggerLAI", LAITrigger);
        MyPaddock.Get("PsModelName1", out PsModelName1);
    }

    // This routine is called when the plant model wants us to do the calculation
    //private bool lowLAI = false;
    private bool skipper = false;
    [EventHandler]
    public void Ondodcapst()
    {
        int DOY = 0;
        double latitude = 0;
        double maxT = 0;
        double minT = 0;
        double radn = 0;
        double RootShootRatio = 0;
        double sln = 0;
        double greenN = 0;
        double SWAvailable = 0;
        double lai = 0;

        MyPaddock.Get("DCAPSTDOY", out DOY);
        MyPaddock.Get("DCAPSTRootShootRatio", out RootShootRatio);
        MyPaddock.Get("DCAPSTswAvail", out SWAvailable);
        MyPaddock.Get("LeafGreenN", out greenN);
        MyPaddock.Get("latitude", out latitude);
        MyPaddock.Get("maxT", out maxT);
        MyPaddock.Get("minT", out minT);
        MyPaddock.Get("radn", out radn);
        MyPaddock.Get("lai", out lai);

        // Model the photosynthesis
        double rpar = 0.5;
        DCAPSTModel DM = Classic.SetUpModel(CP, PP, DOY, latitude, maxT, minT, radn, rpar);

        // OPTIONAL VALUES:      
        // Biological transpiration limit of the crop
        DM.Biolimit = 0;     // default = 0 (disabled)

        // Excess water reduction fraction for bio-limited transpiration 
        DM.Reduction = 0;    // default = 0 (disabled)      

        if (lai > LAITrigger && lai < NLAITrigger)
            sln = Math.Max(greenN, NLowLimit);
        else
            sln = greenN / lai;

        // Run the simulation
        DM.DailyRun(lai, sln, SWAvailable, RootShootRatio);

        // Daily outputs
        RootShoot = RootShootRatio;
        BIOshootDAY = dcapst[0] = DM.ActualBiomass;
        BIOtotalDAY = BIOshootDAY * (1 + RootShoot);
        EcanDemand = dcapst[1] = DM.WaterDemanded;
        EcanSupply = dcapst[2] = DM.WaterSupplied;
        RadIntDcaps = dcapst[3] = DM.InterceptedRadiation;
        RUE = (RadIntDcaps == 0 ? 0 : BIOshootDAY / RadIntDcaps);
        TE = (EcanSupply == 0 ? 0 : BIOshootDAY / EcanSupply);
        BIOshootDAYPot = dcapst[4] = DM.PotentialBiomass;
        SoilWater = SWAvailable;
        dsln = sln;

        // Interval outputs

        // For reasons unknown, wheat calls this method twice, but only the 2nd call is used/valid
        // This skipper just stops the bad data from showing up in the interval report
        skipper = !skipper;
        if (skipper)
        {
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
        RadIntDcaps = 0;
        RUE = 0;
        TE = 0;
        BIOshootDAYPot = 0;

        for (int i = 0; i < 5; i++) { dcapst[i] = -1.0f; }

        double lai = 0;
        MyPaddock.Get("lai", out lai);

        if (!dcapstON && lai >= LAITrigger)
        {
            dcapstON = true;
            MyPaddock.Set("PsModelName", "dcapst");
        }
    }

    // Make certain DCaPST is disabled at sowing
    [EventHandler]
    public void OnSowing()
    {
        dcapstON = false;
        MyPaddock.Set("PsModelName", PsModelName1);
    }
}