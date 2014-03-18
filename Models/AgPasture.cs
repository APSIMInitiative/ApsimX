using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Models;
using Models.Core;

namespace Models
{
    //------ RemoveCropBiomassdm ------
    [Serializable]
    public class RemoveCropBiomassdmType
    {
        public string pool = "";
        public string[] part;
        public double[] dlt;
    }

    //------ RemoveCropBiomass ------
    [Serializable]
    public class RemoveCropBiomassType
    {
        public RemoveCropBiomassdmType[] dm;
    }

    //------ Sow ------
    public class SowType
    {
        public string Cultivar = "";
        public double plants;
        public double sowing_depth;
        public double row_spacing;
        public double SkipRow;
        public double SkipPlant;
        public string Establishment = "";
        public string crop_class = "";
        public string tiller_no_fertile = "";
        public string Skip = "";
        public double plants_pm;
        public int Ratoon;
        public int sbdur;
        public double nplh;
        public double nh;
        public double nplsb;
        public double nplds;
    }

    //------ Graze ------
    [Serializable]
    public class GrazeType
    {
        public string sender = "";
        public double amount;
        public string type = "";
    }

    //------ KillCrop ------
    [Serializable]
    public class KillCropType
    {
        public double KillFraction;
    }

    /// <summary>
    /// A multi-species pasture model 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AgPasture : Model
    {
        [Link]
        private Clock clock = null;

        [Link]
        private Soils.SoilNitrogen SoilN = null;

        [Link]
        private Soils.SoilWater SoilWat = null;

        [Link]
        private WeatherFile MetData = null;

        [Link]
        private ISummary Summary = null;
        private int Debug_Level = 0;
        public Species[] SP = new Species[3] { 
            new Species { 
                speciesName = "ryegrass", 
                micrometType = "grass",
                isAnnual         = false,
                isLegume         = false,
                photoPath        = 3,
                dayEmerg         = 0,
                monEmerg         = 0,
                dayAnth          = 0,
                monAnth          = 0,
                daysToMature     = 0,
                dRootDepth       = 50,
                maxRootDepth     = 900,
                rootDepth        = 900,
                growthTmin       = 2,
                growthTmax       = 32,
                growthTopt       = 20,
                growthTq         = 2,
                massFluxTmin     = 2,
                massFluxTopt     = 20,
                massFluxW0       = 2,
                massFluxWopt     = 0.5,
                heatOnsetT       = 60,
                heatFullT        = 70,
                heatSumT         = 50,
                coldOnsetT       = -20,
                coldFullT        = -30,
                coldSumT         = 20,
                Pm               = 1,
                maintRespiration = 3,
                growthEfficiency = 0.75,
                SLA              = 20,
                lightExtCoeff    = 0.5,
                rue              = 1.65,
                maxAssimiRate    = 330,
                rateLive2Dead    = 0.05,
                rateDead2Litter  = 0.11,
                rateRootSen      = 0.02,
                stockParameter   = 0.05,
                maxSRratio       = (1.0 - 0.25)/0.25,
                allocationSeasonF= 0.8,
                leafRate         = 3,
                fLeaf            = 0.7,
                fStolon          = 0.0,
                digestLive       = 0.6,
                digestDead       = 0.2,
                dmtotal          = 1500,
                dmroot           = 450,
                dmlitter         = 200,
                dmgreenmin       = 500,
                CO2PmaxScale     = 700,
                CO2NScale        = 600,
                CO2NMin          = 0.7,
                CO2NCurvature    = 2,
                NcleafOpt        = 4 * 0.01,
                NcleafMax        = 5 * 0.01,
                NcleafMin        = 1.2 * 0.01,
                NcstemFr         = 0.5,
                NcstolFr         = 0.5,
                NcrootFr         = 0.5,
                MinFix           = 0,
                MaxFix           = 0,
                NdilutCoeff      = 0.5,
                Frgr             = 1,
                waterStressFactor= 1,
                soilSatFactor    = 0.1
            },

            new Species { 
                speciesName      = "whiteclover", 
                micrometType     = "grass",
                isAnnual         = false,
                isLegume         = true,
                photoPath        = 3,
                dayEmerg         = 1,
                monEmerg         = 3,
                dayAnth          = 1,
                monAnth          = 11,
                daysToMature     = 0,
                dRootDepth       = 50,
                maxRootDepth     = 300,
                rootDepth        = 300,
                growthTmin       = 4,
                growthTmax       = 32,
                growthTopt       = 20,
                growthTq         = 2,
                massFluxTmin     = 2,
                massFluxTopt     = 20,
                massFluxW0       = 2,
                massFluxWopt     = 0.5,
                heatOnsetT       = 60,
                heatFullT        = 70,
                heatSumT         = 50,
                coldOnsetT       = -20,
                coldFullT        = -30,
                coldSumT         = 20,
                Pm               = 1,
                maintRespiration = 3,
                growthEfficiency = 0.75,
                SLA              = 20,
                lightExtCoeff    = 0.8,
                rue              = 1.65,
                maxAssimiRate    = 330,
                rateLive2Dead    = 0.05,
                rateDead2Litter  = 0.11,
                rateRootSen      = 0.02,
                stockParameter   = 0.05,
                maxSRratio       = (1.0 - 0.25)/0.25,
                allocationSeasonF= 0.8,
                leafRate         = 3,
                fLeaf            = 0.7,
                fStolon          = 0.2,
                digestLive       = 0.6,
                digestDead       = 0.2,
                dmtotal          = 350,
                dmroot           = 120,
                dmlitter         = 50,
                dmgreenmin       = 50,
                CO2PmaxScale     = 700,
                CO2NScale        = 600,
                CO2NMin          = 0.7,
                CO2NCurvature    = 2,
                NcleafOpt        = 4.5 * 0.01,
                NcleafMax        = 5.5 * 0.01,
                NcleafMin        = 1.2 * 0.01,
                NcstemFr         = 0.5,
                NcstolFr         = 0.5,
                NcrootFr         = 0.5,
                MinFix           = 0.2,
                MaxFix           = 0.6,
                NdilutCoeff      = 1.0,
                Frgr             = 1,
                waterStressFactor= 1,
                soilSatFactor    = 0.1
            },

            new Species { 
                speciesName      = "paspalum", 
                micrometType     = "grass",
                isAnnual         = false,
                isLegume         = false,
                photoPath        = 4,
                dayEmerg         = 1,
                monEmerg         = 3,
                dayAnth          = 1,
                monAnth          = 1,
                daysToMature     = 0,
                dRootDepth       = 50,
                maxRootDepth     = 900,
                rootDepth        = 900,
                growthTmin       = 10,
                growthTmax       = 40,
                growthTopt       = 20,
                growthTq         = 1.2,
                massFluxTmin     = 5,
                massFluxTopt     = 20,
                massFluxW0       = 2,
                massFluxWopt     = 0.5,
                heatOnsetT       = 60,
                heatFullT        = 70,
                heatSumT         = 50,
                coldOnsetT       = -20,
                coldFullT        = -30,
                coldSumT         = 50,
                Pm               = 1.2,
                maintRespiration = 3,
                growthEfficiency = 0.75,
                SLA              = 20,
                lightExtCoeff    = 0.6,
                rue              = 1.65,
                maxAssimiRate    = 330,
                rateLive2Dead    = 0.05,
                rateDead2Litter  = 0.11,
                rateRootSen      = 0.02,
                stockParameter   = 0.05,
                maxSRratio       = (1.0 - 0.25)/0.25,
                allocationSeasonF= 0.8,
                leafRate         = 3.5,
                fLeaf            = 0.7,
                fStolon          = 0.0,
                digestLive       = 0.6,
                digestDead       = 0.2,
                dmtotal          = 100,
                dmroot           = 30,
                dmlitter         = 10,
                dmgreenmin       = 20,
                CO2PmaxScale     = 150,
                CO2NScale        = 600,
                CO2NMin          = 0.7,
                CO2NCurvature    = 2,
                NcleafOpt        = 3 * 0.01,
                NcleafMax        = 3.5 * 0.01,
                NcleafMin        = 0.8 * 0.01,
                NcstemFr         = 0.5,
                NcstolFr         = 0,
                NcrootFr         = 0.5,
                MaxFix           = 0,
                MinFix           = 0,
                NdilutCoeff      = 0.5,
                Frgr             = 1,
                waterStressFactor= 1,
                soilSatFactor    = 0.1
            }
                                                             
        };
        private Species[] pSP = new Species[3];

        //component constant
        private const double SVPfrac = 0.66;
        //private WeatherFile.NewMetType MetData = new WeatherFile.NewMetType();  // Daily Met Data
        private double _IntRadn;    // Intercepted Radn   

        [Units("MJ")]
        public double IntRadn { get { return _IntRadn; }}

        //Parameters that get initial values from the .xml    
        private int _Nsp = 3;
        [Description("Number of species")]
        public int Nsp { get { return _Nsp; } set { _Nsp = value; } }                //can't read in integer directly 
        //public double Nspecies { get; set; }         //Number of species in pasture

        private double[] ftArray;                     //used for returning species values

        private string _thisCropName = "ryegrass_clover_paspalum";
        [Description("Crop name shown as on the simulation tree")]
        public string thisCropName { get { return _thisCropName; } set { _thisCropName = value; } }

        public double[] allocationSeasonF;	    //growth allocation (shoot/root) season factor,
        //made this accessable from xml 
        public double[] NdilutCoeff;           //N dilution coefficient, making it accessible on 23 Mar 2012

        private double[] myRootDepth;          //int:current root depth (mm)
        public double[] rootDepth
        {
            get
            {
                return myRootDepth;
            }
            set
            {
                myRootDepth = value;
                p_rootFrontier = 0;
                foreach (double x in myRootDepth)
                {
                    p_rootFrontier = (x > p_rootFrontier) ? x : p_rootFrontier;
                }
            }
        }
        private double[] rootDist;         //Broken stick parameter [0-1]
        public double[] rootFnType;	        //int:Root function 0=default_1=Ritchie_2=power_law_3=proportional_depth

        // not used till Mar2010
        //    [Param]
        //    private double CO2ambient = 380; //ambient [CO2]
        private double CO2ambient = 380; //expected to be updated from MET  
        private double co2 = 380; //expected to be updated from MAE and ClimateControl 

        //Senescence rate is affected by min(gf-N, gf_water)
        public LinearInterpolation FVPDFunction = new LinearInterpolation
        {
            X = new double[3] { 0.0, 10.0, 50.0 },
            Y = new double[3] { 1.0, 1.0, 1.0 }
        };
        public LinearInterpolation HeightMassFN = new LinearInterpolation
        {
            X = new double[5] { 0, 1000, 2000, 3000, 4000 },
            Y = new double[5] { 0, 25, 75, 150, 250 }
        };

        //Soil & roots 
        public double[] rlvp = new double[7] { 0.05, 0.05, 0.03, 0.02, 0.01, 0.00, 0.00 }; //Root Length Density proportion (relative)  
        public double[] kl = new double[7] { 0.10, 0.06, 0.06, 0.06, 0.06, 0.06, 0.06 };   //SW uptake parameter (/day)
        public double[] ll = new double[7] { 0.20, 0.23, 0.23, 0.23, 0.23, 0.23, 0.23 };         //Crop Lower Limit (mm/mm)
        public double[] xf = new double[7] { 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 0.80 };      //effects of X-factors on root growth(fraction)

        //for SWIM options
        private string _WaterUptakeSource = "calc";
        [Description("Water uptake done by AgPasture (calc) or by apsim?")]
        public string WaterUptakeSource { get { return _WaterUptakeSource; } set { _WaterUptakeSource = value; } }
        public string NUptakeSource = "calc";

        public string alt_N_uptake = "no";

//        public double[] dlayer;   //Soil Layer Thickness (mm)
//        private double[] sw_dep;  //soil water by layer    
//        private double[] SAT;     //saturation point
//        private double[] DUL;     //draina upper limit (fioeld capacity);
//        private double[] no3;     //SNO3dep = new double[dlayer.Length];
//        private double[] nh4;     //SNH4dep = new double[dlayer.Length];

        private double day_length = 12;
//        private double latitude;
        private int day_of_month;
        private int month;
        private int year;

        public int UseRootProportion = 0;

        //** Aggregated pasture parameters of all species (wiht a prefix 'p_')    
        //p_d... variables are daily changes (delta) 
        private double p_dGrowthPot;      //daily growth potential    
        private double p_dGrowthW;        //daily growth with water-deficit incoprporated  
        private double p_dGrowth;         //daily growth  
        private double p_dHerbage;        //daily herbage (total standing DM) increae  
        private double p_dLitter;         //daily litter formation  
        private double p_dRootSen;        //daily root senescence
        private double p_dNLitter;        //daily litter formation
        private double p_dNRootSen;       //daily root senescence

        //p_... variables are pasture states at a given time (day)
        private double p_fShoot;	      //actual fraction of dGrowth to shoot
        private double p_height;          // Canopy height (mm)
        private double p_greenLAI;
        private double p_deadLAI;
        private double p_totalLAI;
        private double p_lightExtCoeff;
        private double p_greenDM;         //green is the live aboveground herbage mass, kgDM/ha
        private double p_deadDM;
        private double p_totalDM;

        private double p_rootMass;        //total root mass
        private double p_rootFrontier;    //depth of root frontier

        //soil        
        private double p_bottomRootLayer;   //the soil layer just below root zone
        private double p_soilNdemand;       //plant N demand (shoot + root) for daily growth from soil (excludingfixation and remob)
        // private double p_soilNdemandMax;    //plant N demand with luxury uptake    
        private double p_soilNavailable;    //Plant available N in soil kgN/ha, at the present day
        private double p_soilNuptake;       //Plant N uptake, daily  
        private double[] SNSupply;
        private double[] SNUptake;
        private double p_Nfix = 0;
        private double p_gfn;               // = effect of p_Nstress on growth 

        private double p_waterDemand;   // Daily Soil Water Demand (mm)
        private double p_waterUptake;   // Daily Soil Water uptake (mm)
        private double p_waterSupply;   // plant extractable soil moisture (mm)
        private double[] SWSupply;
        private double[] SWUptake;
        private double p_gfwater;       // = effects of water stress on growth
        private double p_gftemp;

        private double p_harvestDM;              //daily harvested dm
        private double p_harvestN;               //daily harvested n
        private double p_harvestDigest;
        //private double p_herbageDigest;
        private bool p_Live = true;              //flag signialling crop is live (not killed)

        //Events
        public delegate void NewCropDelegate(PMF.NewCropType Data);
        public event NewCropDelegate NewCrop;

        public delegate void NewCanopyDelegate(NewCanopyType Data);
        public event NewCanopyDelegate NewCanopy;

        public delegate void NewPotentialGrowthDelegate(NewPotentialGrowthType Data);
        public event NewPotentialGrowthDelegate NewPotentialGrowth;

        public delegate void FOMLayerDelegate(Soils.FOMLayerType Data);
        public event FOMLayerDelegate IncorpFOM;

        public delegate void BiomassRemovedDelegate(PMF.BiomassRemovedType Data);
        public event BiomassRemovedDelegate BiomassRemoved;

        public delegate void WaterChangedDelegate(PMF.WaterChangedType Data);
        public event WaterChangedDelegate WaterChanged;

        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        public event NitrogenChangedDelegate NitrogenChanged;

        //temporary testing, will be removed later when IL1 can be get from micromet
        private int canopiesNum = 1;            //number of canpy including this one
        private double[] canopiesRadn = null;   //Radn intercepted by canopies

        //following was for testing 
        [Units("")]
        public double[] spIL1  //for testing only
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].IL1;
                return ftArray;
            }
        }
        [Units("")]
        public double[] spPgross
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].Pgross;
                return ftArray;
            }
        }
        [Units("")]
        private double[] spResp_m
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].Resp_m;
                return ftArray;
            }
        }

        //----------------------------------------------------------------
        /// <summary>
        /// Initialise parameters
        /// </summary>
        private void InitParameters()
        {
            //Rainss 20110711 - Introduced broken stick root distribution 
            // This hack uses rlvp as interface to pass distribution parameters and then recalc rlvp
            // This is not what I would call good pratice due to the parameters technically changing meaning during
            // initilisation, but it will do in the interim.
            Nsp = SP.Length;
            if (rlvp.Length == Nsp)
            {
                //Console.Out.WriteLine("Using two parameter root distribution model (root depth + distribution parameter)");
                UseRootProportion = 2;
                rootDist = new double[rlvp.Length];
                rlvp.CopyTo(rootDist, 0); //Store distribution parameter values
                // Calculate actual rlvp values
                rlvp = new double[SoilWat.dlayer.Length];
                for (int layer = 0; layer < SoilWat.dlayer.Length; layer++)
                {
                    rlvp[layer] = RootProportion(layer, p_rootFrontier);
                }
            }
            else if (rlvp.Length == SoilWat.dlayer.Length)
            {
                //Console.Out.WriteLine("Using real rlvp data values - original method");
                UseRootProportion = 1; //using corrected depth calculation method
            }
            else
            {
                Array.Resize(ref rlvp, SoilWat.dlayer.Length);
                Array.Resize(ref ll, SoilWat.dlayer.Length);
                Array.Resize(ref kl, SoilWat.dlayer.Length);
                Array.Resize(ref xf, SoilWat.dlayer.Length);
                //throw new Exception("AgPasture: Incorrect number of values passed to RLVP exception");
            }

            //init
            p_dGrowthPot = 0.0;
            p_dGrowthW = 0.0;
            p_dGrowth = 0.0;
            p_dHerbage = 0.0;
            p_height = 0.0;

            p_dLitter = 0.0;         //daily litter formation  
            p_dRootSen = 0.0;        //daily root senescence
            p_dNLitter = 0.0;        //daily litter formation N
            p_dNRootSen = 0.0;       //daily root senescence N
            p_bottomRootLayer = 0;

            //Parameters for environmental factors 
            p_soilNdemand = 0;
            p_soilNavailable = 0;
            p_soilNuptake = 0;
            p_gfn = 0;
            p_Nfix = 0.0;
            p_gftemp = 0.0;
            p_gfwater = 0.0;
            p_harvestN = 0.0;

            p_waterSupply = 0;
            p_waterDemand = 0;
            p_waterUptake = 0;
            p_gfwater = 0;
            _IntRadn = 0.0;
            actualUptake = 0.0;

            month = clock.Today.Month;
            year = clock.Today.Year;
            day_of_month = clock.Today.Day;

            //Create and initialise each species  
            //Nsp = (int)Nspecies;
            ftArray = new double[Nsp];

            //SP = new Species[Nsp];         //species of the pasture
            pSP = new Species[Nsp];        //For storing species status at previous day     
            for (int s = 0; s < Nsp; s++)
            {
                //SP[s] = new Species();
                //pSP[s] = new Species();
                InitSpeciesValues(s);
            }

            //Initialising the aggregated pasture parameters from initial valuses of each species  
            p_rootFrontier = 0.0;
            p_rootMass = 0.0;
            p_greenLAI = 0.0;
            p_deadLAI = 0.0;
            p_greenDM = 0.0;
            p_deadDM = 0.0;
            double sum_fShoot = 0.0;
            double sum_lightExtCoeff = 0.0;

            for (int s = 0; s < Nsp; s++)
            {
                //accumulate LAI of all species
                p_greenLAI += SP[s].greenLAI;
                p_deadLAI += SP[s].deadLAI;

                p_greenDM += SP[s].dmgreen;
                p_deadDM += SP[s].dmdead;

                //accumulate the sum for weighted average
                sum_fShoot += SP[s].fShoot * SP[s].dmshoot;
                sum_lightExtCoeff += SP[s].lightExtCoeff * SP[s].totalLAI;

                //Set the deepest root frontier
                if (SP[s].rootDepth > p_rootFrontier)
                    p_rootFrontier = SP[s].rootDepth;

                p_rootMass += SP[s].dmroot;

            }
            p_totalLAI = p_greenLAI + p_deadLAI;
            p_totalDM = p_greenDM + p_deadDM;

            if (p_totalDM == 0) { p_fShoot = 0; }
            else { p_fShoot = sum_fShoot / p_totalDM; }

            if (p_totalLAI == 0) { p_lightExtCoeff = 0.5; }
            else { p_lightExtCoeff = sum_lightExtCoeff / p_totalLAI; }

            //if (rlvp.Length != dlayer.Length)
            //{
            //    String msg = "Warning: Number of layers specified for root length density (rlvp) is different ";
            //    msg += "\nfrom the number of soil layers.The simulation will run using the minimum of the two.";
            //    Console.WriteLine(msg);
            //}
        }
        //----------------------------------------------------------
        /// <summary>
        /// Set parameter valuse that each species need to know
        /// - from pasture to species
        /// </summary>
        /// <param name="s"></param>
        private void InitSpeciesValues(int s)
        {
            Species.CO2ambient = CO2ambient;
            Species.thisCropName = thisCropName;

            SP[s].InitValues();

            SWSupply = new double[SoilWat.dlayer.Length];
            SWUptake = new double[SoilWat.dlayer.Length];
            SNSupply = new double[SoilWat.dlayer.Length];
            SNUptake = new double[SoilWat.dlayer.Length];
        }

        //---------------------------------------------------------------------------
        /// <summary>
        /// Let species know weather conditions
        /// </summary>
        /// <returns></returns>
        private bool SetSpeciesMetData()
        {
            //pass metData & day_length to species (same to all species)
            Species.dayLength = day_length = MetData.DayLength;
            Species.latitude = MetData.Latitude;
            Species.MetData = MetData.MetData;
            Species.day_of_month = day_of_month;
            Species.month = month;
            Species.year = year;
            Species.CO2 = co2;
            Species.PIntRadn = _IntRadn;
            Species.PCoverGreen = Cover_green;
            Species.PLightExtCoeff = p_lightExtCoeff;
            Species.Pdmshoot = AboveGroundWt;   //dm_shoot;
            Species.coverRF = coverRF();

            //partition the MetData to species 
            double sumRadnIntercept = 0.0;   //Intercepted Fraction of the solar Radn available to a species  
            for (int s = 0; s < Nsp; s++)
            {
                sumRadnIntercept += SP[s].coverGreen;
            }
            //update available Radn for each species at current day
            //IntRadn - the total intecepted radn by whole canopy of mixed species
            for (int s = 0; s < Nsp; s++)
            {
                if (sumRadnIntercept == 0)
                {
                    SP[s].intRadnFrac = 0;
                    SP[s].intRadn = 0;
                }
                else
                {
                    SP[s].intRadnFrac = SP[s].coverGreen / sumRadnIntercept;
                    SP[s].intRadn = _IntRadn * SP[s].intRadnFrac;
                }
            }

            //testing SNF decline by factor df 
            // double dFrac = 1.0;
            if (co2 == 475)
            {
                for (int s = 0; s < Nsp; s++)
                {
                    SP[s].MaxFix = 0.5;// dFrac;
                    SP[s].MinFix = 0.2;// dFrac;
                }
            }


            return true;
        }


        //--------------------------------------------------------------------------
        /// <summary>
        /// Set drought stress factor to each species
        /// Worth more efforts in this area 
        /// </summary>
        private void SetSpeciesLimitingFactors()
        {

            if (p_waterDemand == 0)
            {
                p_gfwater = 1.0;
                for (int s = 0; s < Nsp; s++)
                    SP[s].gfwater = p_gfwater;
                return;                                 //case (1) return
            }
            if (p_waterDemand > 0 && p_waterUptake == 0)
            {
                p_gfwater = 0.0;
                for (int s = 0; s < Nsp; s++)
                    SP[s].gfwater = p_gfwater;
                return;                                 //case (2) return
            }

            p_gfwater = p_waterUptake / p_waterDemand;
            double spDepth = 0;              // soil profile depth
            if (p_gfwater > 0.999)  //possible saturation
            {
                // calculate soil moisture content in root zone
                double SW = 0;      //soil water content        
                double Sat = 0;     //water content at saturation
                double FC = 0;      //water contenct at field capacity      

                for (int layer = 0; layer < SoilWat.dlayer.Length; layer++)
                {
                    spDepth += SoilWat.dlayer[layer];
                    if (spDepth <= p_rootFrontier)
                    {
                        SW += SoilWat.sw_dep[layer];
                        Sat += SoilWat.sat_dep[layer];
                        FC += SoilWat.dul_dep[layer];
                    }
                }
                if (SW > FC) //if saturated 
                {
                    //    double soilSatFactor = 0.2;         //gfwater = 1-0.2 (when SW == Sat)    
                    //    p_gfwater = 1 - soilSatFactor * (SW - FC) / (Sat - FC);
                    //    if (p_gfwater > 1.0) p_gfwater = 1.0;
                    //}
                    //    for (int s=0; s<Nsp; s++)
                    //        SP[s].gfwater = p_gfwater;

                    double accum_gfwater = 0;
                    p_greenLAI = 0;     //update p_greenLAI before using it.
                    for (int s = 0; s < Nsp; s++)
                    {
                        SP[s].gfwater = 1 - SP[s].soilSatFactor * (SW - FC) / (Sat - FC);
                        accum_gfwater += SP[s].gfwater * SP[s].greenLAI;   //weighted by greenLAI 
                        p_greenLAI += SP[s].greenLAI;                      //FLi 19 Sept 2011 for avoiding error of an unupdated  
                    }                                                      //p_greenLAI when using SWIM for waterUptake    
                    if (p_greenLAI > 0)
                        p_gfwater = accum_gfwater / p_greenLAI;
                    else
                        p_gfwater = 1.0;
                    return;                         //case (3) return
                }
                //Reaching here is possible (SW < FC) even with a p_gfwater ==1     //FLi 20 Oct 2012
                //not return, but go though to the case (4) below                                               
            }

            //Original block Set specieS.gfwater = p_gfwater, to distinguish them later    
            for (int s = 0; s < Nsp; s++)
            {
                SP[s].gfwater = p_gfwater;
            }
            //Console.Out.WriteLine("gfwater4: " + p_gfwater);
            return;                                     //case (4) return


            /*/////
              int  dep = SWUptake.Length;
              double[] transpLAI = new double[dep];       //partitioning soil water in each layer according to species demand (LAI)
                                                          //This process sounds more reasonable, but not giving better results, possible 
                                                          //due to that compensation of moisture among layers are important between species
              spDepth = 0;                         //Soil profile depth                                            
              for (int layer = 0; layer < dep; layer++)
              {
                  spDepth += dlayer[layer];                      
                  for (int s = 0; s < Nsp; s++)
                  {                        
                      if (spDepth <= SP[s].rootDepth)  
                          transpLAI[layer] += SP[s].greenLAI;     //totalLAI which has root ToString this layer
                  }                                
              }

              //species soil water demand
              for (int s = 0; s < Nsp; s++)
              {
                  SP[s].swuptake = 0; //init
                  SP[s].swdemandFrac = 0;             
                  if (p_greenLAI > 0)
                      SP[s].swdemandFrac =  SP[s].greenLAI / p_greenLAI;
              }

              //soil water uptake (assumed)
              spDepth = 0;
              for (int layer = 0; layer < dep; layer++)
              {
                  spDepth += dlayer[layer];
                  for (int s = 0; s < Nsp; s++)
                  {
                      if (SP[s].rootDepth >= spDepth && SP[s].greenLAI > 0 && transpLAI[layer] > 0)
                          SP[s].swuptake += SWUptake[layer] * SP[s].greenLAI /transpLAI[layer];
                  }  
              }

              for (int s = 0; s < Nsp; s++)
              {
                  if (SP[s].swdemandFrac > 0 && p_waterUptake > 0)
                      SP[s].gfwater = SP[s].swuptake / (SP[s].swdemandFrac * p_waterUptake);    
                  
                  if (SP[s].gfwater > 1.0) 
                      SP[s].gfwater = 1.0;                
              }         
        
              return;                                         //case (4) return
             */

        }


        //--------------------------------------------------------------------------
        /// <summary>
        /// plant growth and partitioning and tissue turnover 
        /// </summary>
        private void GrowthAndPartition()
        {
            p_greenLAI = 0;
            p_deadLAI = 0;

            p_greenDM = 0.0;
            p_deadDM = 0.0;

            p_dHerbage = 0.0;
            p_rootMass = 0.0;

            p_dLitter = 0;
            p_dNLitter = 0;

            p_dRootSen = 0;
            p_dNRootSen = 0;

            for (int s = 0; s < Nsp; s++)
            {
                SP[s].PartitionTurnover();

                p_greenLAI += SP[s].greenLAI;
                p_deadLAI += SP[s].deadLAI;

                p_greenDM += SP[s].dmgreen;
                p_deadDM += SP[s].dmdead;
                p_rootMass += SP[s].dmroot;

                p_dHerbage += (SP[s].dmtotal - SP[s].pS.dmtotal);
                //p_dHerbage += SP[s].dGrowth - SP[s].dLitter;  

                p_dLitter += SP[s].dLitter;
                p_dNLitter += SP[s].dNLitter;

                p_dRootSen += SP[s].dRootSen;
                p_dNRootSen += SP[s].dNrootSen;
            }

            p_totalLAI = p_greenLAI + p_deadLAI;
            p_totalDM = p_greenDM + p_deadDM;



            //litter return to surface OM completely (frac = 1.0)
            DoSurfaceOMReturn(p_dLitter, p_dNLitter, 1.0);

            //Root FOM return 
            DoIncorpFomEvent(p_dRootSen, p_dNRootSen);

        }


        //==============================

        # region "EventSenders"

        //--------------------------------------------------------------------------------------------
        /// <summary>
        /// Event publication - new crop
        /// </summary>
        private void DoNewCropEvent()
        {
            if (NewCrop != null)
            {
                // Send out New Crop Event to tell other modules who I am and what I am
                PMF.NewCropType EventData = new PMF.NewCropType();
                EventData.crop_type = SP[0].micrometType;  // need to separate crop type for micromet & canopy name !!  
                EventData.sender = Name;        // 
                NewCrop.Invoke(EventData);
            }

        }

        //----------------------------------------------------------------
        /// <summary>
        /// Event publication - new canopy 
        /// </summary>
        private void DoNewCanopyEvent()
        {
            if (NewCanopy != null)
            {
                NewCanopyType canopy = new NewCanopyType();
                canopy.sender = Name;
                canopy.lai = p_greenLAI;
                canopy.lai_tot = p_totalLAI;
                p_height = HeightfromDM;
                canopy.height = (int)p_height;             // height effect, mm 
                canopy.depth = (int)p_height;              // canopy depth 
                canopy.cover = Cover_green;
                canopy.cover_tot = Cover_tot;


                NewCanopy.Invoke(canopy);
            }
        }

        //----------------------------------------------------------------
        /// <summary>
        /// Send out plant growth limiting factor for other module calculating potential transp.
        /// </summary>
        private void DoNewPotentialGrowthEvent()
        {
            if (NewPotentialGrowth != null)
            {
                NewPotentialGrowthType EventData = new NewPotentialGrowthType();
                EventData.sender = Name;
                p_gftemp = 0;     //weighted average   


                double Tday = 0.75 * MetData.MaxT + 0.25 * MetData.MinT; //Tday
                for (int s = 0; s < Nsp; s++)
                {
                    double prop = 1.0 / Nsp;
                    if (p_greenDM != 0.0)
                    {
                        prop = SP[s].dmgreen / AboveGroundLiveWt;   // dm_green;       
                    }
                    p_gftemp += SP[s].GFTemperature(Tday) * prop;
                }

                double gft = 1;
                if (Tday < 20) gft = Math.Sqrt(p_gftemp);
                else gft = p_gftemp;
                // Note: p_gftemp is for gross photosysthsis.
                // This is different from that for net production as used in other APSIM crop models, and is 
                // assumesd in calculation of temperature effect on transpiration (in micromet). 
                // Here we passed it as sqrt - (Doing so by a comparison of p_gftemp and that
                // used in wheat). Temperature effects on NET produciton of forage species in other models 
                // (e.g., grassgro) are not so significant for T = 10-20 degrees(C)     

                //Also, have tested the consequences of passing p_Ncfactor in (different concept for gfwater), 
                //coulnd't see any differnece for results
                EventData.frgr = Math.Min(FVPD, gft);
                // RCichota, Jan/2014: removed AgPasture's Frgr from here, it is considered at the same level as nitrogen etc...
                NewPotentialGrowth.Invoke(EventData);
            }
        }

        #endregion //EventSender
        //======================================================================

        #region "EventHandlers"
        /// <summary>
        /// Eventhandeler - initialisation
        /// </summary>
        [EventSubscribe("Initialised")]
        private void Initialise() //overrides Sub init2()
        {
            InitParameters();            // Init parameters after reading the data           

            if (MetData.StartDate != new DateTime(0) || MetData.ReadFirstDay())
              SetSpeciesMetData();         // This is needed for the first day after knowing the number of species 

            DoNewCropEvent();            // Tell other modules that I exist
            DoNewCanopyEvent();          // Tell other modules about my canopy
            DoNewPotentialGrowthEvent(); // Tell other modules about my current growth status

            alt_N_uptake = alt_N_uptake.ToLower();
            if (alt_N_uptake == "yes")
                if (Nsp > 1)
                    throw new Exception("When working with multiple species, 'ValsMode' must ALWAYS be 'none'");
        }

        public override void OnCommencing()
        {
            Initialise();
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// EventHandeler - preparation befor the main process 
        /// </summary>
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            //  p_harvestDM = 0.0;      // impartant to have this reset because  
            //  p_harvestN = 0.0;       // they are used to DM & N returns
            //  p_harvestDigest = 0.0;

            month = clock.Today.Month;
            year = clock.Today.Year;
            day_of_month = clock.Today.Day;

            DoNewCanopyEvent();
            DoNewPotentialGrowthEvent();
        }

/*        //---------------------------------------------------------------------
        /// <summary>
        /// Get new meteo- data
        /// </summary>
        /// <param name="NewMetData"></param>
        [EventSubscribe("NewMet")]
        private void OnNewMet(WeatherFile.NewMetType NewMetData)
        {
            MetData = NewMetData;
        }
 */

        //---------------------------------------------------------------------
        /// <summary>
        /// Get plant potential transpiration
        /// </summary>
        /// <param name="CWB"></param>
        [EventSubscribe("Canopy_Water_Balance")]
        private void OnCanopy_Water_Balance(CanopyWaterBalanceType CWB)
        {
            for (int i = 0; i < CWB.Canopy.Length; i++)
            {
                if (CWB.Canopy[i].name.ToUpper() == thisCropName.ToUpper())
                {
                    p_waterDemand = (double)CWB.Canopy[i].PotentialEp;
                }
            }
        }

        //---------------------------------------------------------------------
        [EventSubscribe("Canopy_Energy_Balance")]
        private void OnCanopy_Energy_Balance(CanopyEnergyBalanceType LP)
        {
            canopiesNum = LP.Interception.Length;
            canopiesRadn = new double[canopiesNum];

            for (int i = 0; i < canopiesNum; i++)
            {
                if (LP.Interception[i].name.ToUpper() == thisCropName.ToUpper())  //TO: species by species, and get the total?
                {
                    _IntRadn = 0;
                    for (int j = 0; j < LP.Interception[i].layer.Length; j++)
                    {
                        _IntRadn += LP.Interception[i].layer[j].amount;
                    }
                    canopiesRadn[i] = _IntRadn;
                }
                else //Radn intercepted possibly by other canopies used for a rough IL estimation,
                {    //potenital use when species were specified separately in pasture. (not used of now.11Mar10 )                    
                    double otherRadn = 0;
                    for (int j = 0; j < LP.Interception[i].layer.Length; j++)
                    {
                        otherRadn += LP.Interception[i].layer[j].amount;
                    }
                    canopiesRadn[i] = otherRadn;
                }
            }
        }


        //---------------------------------------------------------------------
        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {
            if (!p_Live)
                return;

            //**Remember last status, and update root depth frontier (root depth mainly for annuals)            
            for (int s = 0; s < Nsp; s++)
            {
                pSP[s] = SP[s];       //Species state yesterday is rememberd                  
                SP[s].SetPrevPools(); //pool values yesterday is also retained in current state 
                SP[s].DailyRefresh();

                double spRootDepth = SP[s].rootGrowth();    //update root depth 
                if (p_rootFrontier < spRootDepth)
                    p_rootFrontier = spRootDepth;
            }

            //Console.WriteLine("Warning message");
            //throw new Exception("throw ...");

            //**To partition Radn to different species 
            SetSpeciesMetData();

            //** advance phenology
            int anyEmerged = 0;
            for (int s = 0; s < Nsp; s++)
            {
                anyEmerged += SP[s].Phenology();
            }

            //**Potential growth
            p_dGrowthPot = 0;
            for (int s = 0; s < Nsp; s++)
            {
                //p_dGrowthPot += SP[s].DailyGrowthPot();   // alternative way for calclating potential growth
                p_dGrowthPot += SP[s].DailyEMGrowthPot();   //pot here incorporated [N] effects 
            }


            //**Calculate soil N available in root zone                          
            p_soilNavailable = calcPlantAvailableN();
            //p_soilNavailable = calcPlantExtractableN();   //need to do more validation/calibration for activating this
            //**Water supply & uptake        
            if (WaterUptakeSource == "calc")
            {
                p_waterUptake = SWUptakeProcess();      //actual uptake by all species
            }
            else
            {
                //Water uptake be calculated by other modules (e.g., SWIM) and got by [EventHandler]
            }
            SetSpeciesLimitingFactors();  // * root competition for water when SM is deficit: species-specific ?   

            //**add drought effects (before considering other nutrient limitation)
            p_dGrowthW = 0;
            for (int s = 0; s < Nsp; s++)
            {
                p_dGrowthW += SP[s].DailyGrowthW();
            }
            double nuptake = NBudgetAndUptake();

            //**actual daily growth
            p_dGrowth = 0;
            for (int s = 0; s < Nsp; s++)
            {
                p_dGrowth += SP[s].DailyGrowthAct();
            }

            /*trick species for specified clover% 
            DateTime d97J1 = new DateTime(1997, 7, 1, 0, 0, 0);
            DateTime d98J1 = new DateTime(1998, 7, 1, 0, 0, 0);
            DateTime d99J1 = new DateTime(1999, 7, 1, 0, 0, 0);
            DateTime d00J1 = new DateTime(2000, 7, 1, 0, 0, 0);
            DateTime d01J1 = new DateTime(2001, 7, 1, 0, 0, 0);
            DateTime d02J1 = new DateTime(2002, 7, 1, 0, 0, 0);
            DateTime d03J1 = new DateTime(2003, 7, 1, 0, 0, 0);
            DateTime d04J1 = new DateTime(2004, 7, 1, 0, 0, 0);
            DateTime d05J1 = new DateTime(2005, 7, 1, 0, 0, 0);
            DateTime d06J1 = new DateTime(2006, 7, 1, 0, 0, 0);
            DateTime d07J1 = new DateTime(2007, 7, 1, 0, 0, 0);
            DateTime d08J1 = new DateTime(2008, 7, 1, 0, 0, 0);
            double legumeF = 0.10;                                                                      //ElevObs  //AmbObs     
            if (DateTime.Compare(Today, d97J1) >= 0 && DateTime.Compare(Today, d98J1) < 0) legumeF = 0.03;//0.05;//0.03; 
            else if (DateTime.Compare(Today, d98J1) >= 0 && DateTime.Compare(Today, d99J1) < 0) legumeF = 0.06;//0.19;//0.06; 
            else if (DateTime.Compare(Today, d99J1) >= 0 && DateTime.Compare(Today, d00J1) < 0) legumeF = 0.17;//0.31;//0.17;
            else if (DateTime.Compare(Today, d00J1) >= 0 && DateTime.Compare(Today, d01J1) < 0) legumeF = 0.21;//0.34;//0.21;
            else if (DateTime.Compare(Today, d01J1) >= 0 && DateTime.Compare(Today, d02J1) < 0) legumeF = 0.03;//0.04;//0.03; 
            else if (DateTime.Compare(Today, d02J1) >= 0 && DateTime.Compare(Today, d03J1) < 0) legumeF = 0.03;//0.07;//0.03; 
            else if (DateTime.Compare(Today, d03J1) >= 0 && DateTime.Compare(Today, d04J1) < 0) legumeF = 0.09;//0.06;//0.09;
            else if (DateTime.Compare(Today, d04J1) >= 0 && DateTime.Compare(Today, d05J1) < 0) legumeF = 0.10;//0.22;//0.10;
            else if (DateTime.Compare(Today, d05J1) >= 0 && DateTime.Compare(Today, d06J1) < 0) legumeF = 0.11;//0.07;//0.11;
            else if (DateTime.Compare(Today, d06J1) >= 0 && DateTime.Compare(Today, d07J1) < 0) legumeF = 0.02;//0.05;//0.02;
            else if (DateTime.Compare(Today, d07J1) >= 0 && DateTime.Compare(Today, d08J1) < 0) legumeF = 0.05;//0.06;//0.05; 
        
            SP[0].dGrowth = p_dGrowth * (1 - legumeF);
            SP[1].dGrowth = p_dGrowth * legumeF;
            Console.WriteLine(" legumeF = " + legumeF); 
            //end of trick#
            */


            //**partitioning & turnover   
            GrowthAndPartition();       // litter returns to surfaceOM; Root returns to soil FOM dead in this turnover routines                                                                  

            /* if (!p_HarvestDay)
             {
                 p_harvestDM = 0.0;      // impartant to have this reset because  
                 p_harvestN = 0.0;       // they are used to DM & N returns
                 p_harvestDigest = 0.0;
             }            
             p_HarvestDay = false;    //reset the  
          */
        }


        //----------------------------------------------------------------------
        [EventSubscribe("RemoveCropBiomass")]
        private void Onremove_crop_biomass(RemoveCropBiomassType rm)
        {
            //Note: It is resposibility of the calling module to check the
            // amount of herbage in each pools of AbovegroundBiomassWt and set the
            // the correct amount in 'rm'. 
            // No checking if the removing amount passed in are too much here

            const double gm2ha = 10;   // constant for conversion of g/m^2 to kg/ha, 
            // rm.dm.dlt should be in g/m^2

            double dm_leaf_green = LeafLiveWt;
            double dm_stem_green = StemLiveWt;
            double dm_leaf_dead = LeafDeadWt;
            double dm_stem_dead = StemDeadWt;

            for (int s = 0; s < Nsp; s++)     // for accumulating the total DM & N removal of species from verious pools
            {
                SP[s].dmdefoliated = 0.0;
                SP[s].Ndefoliated = 0.0;
            }

            for (int i = 0; i < rm.dm.Length; i++)              //for each pool
            {
                for (int j = 0; j < rm.dm[i].dlt.Length; j++)   //for each part
                {
                    if (rm.dm[i].pool == "green" && rm.dm[i].part[j] == "leaf")
                    {
                        for (int s = 0; s < Nsp; s++)           //for each species
                        {
                            if (dm_leaf_green != 0)             //resposibility of other modules to check the amount
                            {
                                double rm_leaf = gm2ha * rm.dm[i].dlt[j] * SP[s].dmleaf_green / dm_leaf_green;
                                double rm_leaf1 = rm_leaf * SP[s].dmleaf1 / SP[s].dmleaf_green;
                                double rm_leaf2 = rm_leaf * SP[s].dmleaf2 / SP[s].dmleaf_green;
                                double rm_leaf3 = rm_leaf * SP[s].dmleaf3 / SP[s].dmleaf_green;
                                SP[s].dmleaf1 -= rm_leaf1;
                                SP[s].dmleaf2 -= rm_leaf2;
                                SP[s].dmleaf3 -= rm_leaf3;
                                SP[s].dmdefoliated += rm_leaf1 + rm_leaf2 + rm_leaf3;

                                SP[s].Nleaf1 -= SP[s].Ncleaf1 * rm_leaf1;
                                SP[s].Nleaf2 -= SP[s].Ncleaf2 * rm_leaf2;
                                SP[s].Nleaf3 -= SP[s].Ncleaf3 * rm_leaf3;
                                SP[s].Ndefoliated += SP[s].Ncleaf1 * rm_leaf1 + SP[s].Ncleaf2 * rm_leaf2 + SP[s].Ncleaf3 * rm_leaf3;
                            }
                        }
                    }
                    else if (rm.dm[i].pool == "green" && rm.dm[i].part[j] == "stem")
                    {
                        for (int s = 0; s < Nsp; s++)
                        {
                            if (dm_stem_green != 0)  //resposibility of other modules to check the amount
                            {
                                double rm_stem = gm2ha * rm.dm[i].dlt[j] * SP[s].dmstem_green / dm_stem_green;
                                double rm_stem1 = rm_stem * SP[s].dmstem1 / SP[s].dmstem_green;
                                double rm_stem2 = rm_stem * SP[s].dmstem2 / SP[s].dmstem_green;
                                double rm_stem3 = rm_stem * SP[s].dmstem3 / SP[s].dmstem_green;
                                SP[s].dmstem1 -= rm_stem1;
                                SP[s].dmstem2 -= rm_stem2;
                                SP[s].dmstem3 -= rm_stem3;
                                SP[s].dmdefoliated += rm_stem1 + rm_stem2 + rm_stem3;

                                SP[s].Nstem1 -= SP[s].Ncstem1 * rm_stem1;
                                SP[s].Nstem2 -= SP[s].Ncstem2 * rm_stem2;
                                SP[s].Nstem3 -= SP[s].Ncstem3 * rm_stem3;
                                SP[s].Ndefoliated += SP[s].Ncstem1 * rm_stem1 + SP[s].Ncstem2 * rm_stem2 + SP[s].Ncstem3 * rm_stem3;
                            }
                        }
                    }
                    else if (rm.dm[i].pool == "dead" && rm.dm[i].part[j] == "leaf")
                    {
                        for (int s = 0; s < Nsp; s++)
                        {
                            if (dm_leaf_dead != 0)  //resposibility of other modules to check the amount
                            {
                                double rm_leaf4 = gm2ha * rm.dm[i].dlt[j] * SP[s].dmleaf4 / dm_leaf_dead;
                                SP[s].dmleaf4 -= rm_leaf4;
                                SP[s].dmdefoliated += rm_leaf4;

                                SP[s].Ndefoliated += SP[s].Ncleaf4 * rm_leaf4;
                                SP[s].Nleaf4 -= SP[s].Ncleaf4 * rm_leaf4;
                            }
                        }
                    }
                    else if (rm.dm[i].pool == "dead" && rm.dm[i].part[j] == "stem")
                    {
                        for (int s = 0; s < Nsp; s++)
                        {
                            if (dm_stem_dead != 0)  //resposibility of other modules to check the amount
                            {
                                double rm_stem4 = gm2ha * rm.dm[i].dlt[j] * SP[s].dmstem4 / dm_stem_dead;
                                SP[s].dmstem4 -= rm_stem4;
                                SP[s].dmdefoliated += rm_stem4;

                                SP[s].Nstem4 -= SP[s].Ncstem4 * rm_stem4;
                                SP[s].Ndefoliated += SP[s].Ncstem4 * rm_stem4;
                            }
                        }
                    }
                }
            }

            p_harvestDM = 0;
            p_harvestN = 0;
            for (int s = 0; s < Nsp; s++)
            {
                p_harvestDM += SP[s].dmdefoliated;
                p_harvestN += SP[s].Ndefoliated;
                SP[s].updateAggregated();
            }

            //In this routine of no selection among species, the removed tissue from different species 
            //will be in proportion with exisisting mass of each species.
            //The digetibility below is an approximation (= that of pasture swards). 
            //It is more reasonable to calculate it organ-by-organ for each species, then put them together.            
            p_harvestDigest = HerbageDigestibility;

        }

        //----------------------------------------------------------------------
        public void Harvest(String type, double amount)  //Being called not by Event
        {
            GrazeType GZ = new GrazeType();
            GZ.amount = amount;
            GZ.type = type;
            OnGraze(GZ);
        }

        public void Graze(string type, double amount)
        {
            if ((!p_Live) || p_totalDM == 0)
                return;

            double herbage_mass = StemWt + LeafWt;  // dm_stem + dm_leaf;
            double min_residue = 200;               // kg/ha assumed
            double residue_amt = min_residue;
            double remove_amt = 0;

            //case 1: remove untill the residue reaches the specified amount
            if (type == "residue")
            {
                residue_amt = amount;
                if (herbage_mass > residue_amt)
                {
                    remove_amt = herbage_mass - residue_amt;
                }
                else
                {
                    remove_amt = 0;
                }
            }
            //case 2: remove the specified amount
            else if (type == "removal")
            {
                remove_amt = amount;

                if (herbage_mass > min_residue)
                {
                    if (herbage_mass > (remove_amt + min_residue))
                    {
                        residue_amt = herbage_mass - remove_amt;
                    }
                    else
                    {
                        residue_amt = min_residue;
                        remove_amt = herbage_mass - min_residue;
                    }
                }
                else
                {
                    remove_amt = 0;
                }
            }

            p_harvestDM = remove_amt;

            //remove DM & N species by species
            p_harvestDigest = 0;
            for (int s = 0; s < Nsp; s++)
            {
                double amt = 0;
                if (herbage_mass != 0)
                {
                    amt = remove_amt * (SP[s].dmstem + SP[s].dmleaf) / herbage_mass;
                }
                p_harvestN += SP[s].Remove(amt);

                //calc digestibility
                if (remove_amt > 0)
                    p_harvestDigest += SP[s].digestDefoliated * amt / remove_amt;
            }
        }


        //----------------------------------------------------------------------
        [EventSubscribe("Graze")]
        private void OnGraze(GrazeType GZ)
        {
            Summary.WriteMessage(FullPath, "Agpasture.OnGraze");
            //Console.WriteLine("");
            Graze(GZ.type, GZ.amount);
        }

        //----------------------------------------------------------
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
        {
            // Gets the water uptake for each layer as calculated by an external module (SWIM)
            p_waterUptake = 0;
            for (int i_Crop = 0; i_Crop != SoilWater.Uptakes.Length; i_Crop++)
            {
                string MyName = SoilWater.Uptakes[i_Crop].Name;
                if (MyName == thisCropName)
                {
                    int length = SoilWater.Uptakes[i_Crop].Amount.Length;
                    for (int layer = 0; layer < length; layer++)
                    {
                        SWUptake[layer] = SoilWater.Uptakes[i_Crop].Amount[layer];
                        p_waterUptake += SoilWater.Uptakes[i_Crop].Amount[layer];
                    }
                }
            }
        }

        //----------------------------------------------------------------------
        [EventSubscribe("Sow")]
        private void OnSow(SowType PSow)
        {
            /*SowType is our type and is defined like this:
            <type name="Sow">
              <field name="Cultivar" kind="string" />
              <field name="Population" kind="double" />
              <field name="Depth" kind="double" />
              <field name="MaxCover" kind="double" />
              <field name="BudNumber" kind="double" />
            </type>
            */

            p_Live = true;
            ResetZero();
            for (int s = 0; s < Nsp; s++)
                SP[s].SetInGermination();

        }

        //----------------------------------------------------------------------
        [EventSubscribe("KillCrop")]
        private void OnKillCrop(KillCropType PKill)
        {
            double frac = PKill.KillFraction;
            //always complete kill for pasture, ignore fraction

            //Above_ground part returns to surface OM comletey (frac = 1.0)
            DoSurfaceOMReturn(p_totalDM, AboveGroundN, 1.0);    //n_shoot

            //Incorporate root mass in soil fresh organic matter
            DoIncorpFomEvent(p_rootMass, BelowGroundN);         //n_root);

            ResetZero();

            p_Live = false;
        }

        //-----------------------------------------------------------------------
        private void ResetZero()
        {
            //shoot
            p_greenLAI = 0;
            p_deadLAI = 0;
            p_totalLAI = 0;
            p_greenDM = 0;
            p_deadDM = 0;
            p_totalDM = 0;
            p_height = 0;

            //root
            p_rootMass = 0;
            p_rootFrontier = 0;

            //daily changes
            p_dGrowthPot = p_dGrowthW = p_dGrowth = p_dHerbage = 0;   //daily DM increase  
            p_dLitter = p_dNLitter = 0;
            p_dRootSen = p_dNRootSen = 0;

            p_waterDemand = p_waterUptake = 0;
            p_soilNdemand = p_soilNuptake = 0;

            //species (ignore fraction)
            for (int s = 0; s < Nsp; s++)
                SP[s].ResetZero();

        }
        //-----------------------------------------------------------------------
        private double calcPlantAvailableN()
        {
            p_soilNavailable = 0;
            double spDepth = 0;         // depth before next soil layer
            int sLayer = 0;
            for (sLayer = 0; sLayer < SoilWat.dlayer.Length; sLayer++)
            {
                if (spDepth <= p_rootFrontier)
                {
                    /* an approach for controlling N uptake
                    const float KNO3 = 0.1F;
                    const float KNH4 = 0.1F;
                    double swaf = 1.0;
                    swaf = (sw_dep[sLayer] - ll[sLayer]) / (DUL[sLayer] - ll[sLayer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));                                
                    p_soilNavailable += (no3[sLayer] * KNO3 + nh4[sLayer] * KNH4 ) * swaf;
                    SNSupply[sLayer] = (no3[sLayer] * KNO3 + nh4[sLayer] * KNH4 ) * (float)swaf;
                    */
                    //original below
                    p_soilNavailable += (SoilN.no3[sLayer] + SoilN.nh4[sLayer]);
                    SNSupply[sLayer] = (SoilN.no3[sLayer] + SoilN.nh4[sLayer]);
                }
                else
                {
                    p_bottomRootLayer = sLayer;
                    break;
                }

                spDepth += SoilWat.dlayer[sLayer];

            }

            if (p_bottomRootLayer == 0 && sLayer > 0)
                p_bottomRootLayer = sLayer - 1;

            return p_soilNavailable;
        }

        //-----------------------------------------------------------------------
        private double calcPlantExtractableN()    // not all minN is extractable
        {
            p_soilNavailable = 0;
            double spDepth = 0;         // depth before next soil layer
            int sLayer = 0;
            for (sLayer = 0; sLayer < SoilWat.dlayer.Length; sLayer++)
            {
                if (spDepth <= p_rootFrontier)
                {
                    //an approach for controlling N uptake
                    const double KNO3 = 0.1;
                    const double KNH4 = 0.1;
                    double swaf = 1.0;
                    swaf = (SoilWat.sw_dep[sLayer] - ll[sLayer]) / (SoilWat.dul[sLayer] - ll[sLayer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                    p_soilNavailable += (SoilN.no3[sLayer] * KNO3 + SoilN.nh4[sLayer] * KNH4) * Math.Pow(swaf, 0.25);
                    SNSupply[sLayer] = (SoilN.no3[sLayer] * KNO3 + SoilN.nh4[sLayer] * KNH4) * Math.Pow(swaf, 0.25);

                    //original below
                    //p_soilNavailable += (no3[sLayer] + nh4[sLayer]);
                    //SNSupply[sLayer] = (no3[sLayer] + nh4[sLayer]);
                }
                else
                {
                    p_bottomRootLayer = sLayer;
                    break;
                }

                spDepth += SoilWat.dlayer[sLayer];

            }

            if (p_bottomRootLayer == 0 && sLayer > 0)
                p_bottomRootLayer = sLayer - 1;

            return p_soilNavailable;
        }
        //-------------------------------------------------------
        private double NBudgetAndUptake()
        {
            //1)Calculate soil N demand (species by species)        
            p_Nfix = 0;
            p_soilNdemand = 0;

            double p_Ndemand = 0;
            double p_NdemandOpt = 0;

            for (int s = 0; s < Nsp; s++)
            {
                p_Nfix += SP[s].CalcNdemand();      //Also, default SP[s].Nfix is set assuming soil N supply is sufficient                    
                p_NdemandOpt += SP[s].NdemandOpt;   //demand to optimum [N]
                p_Ndemand += SP[s].Ndemand;         //for luxury uptake
            }

            //2)Update Nfix of legume species under N stress            
            double Nstress = 1.0;
            if (p_Ndemand > 0.0 && (p_Ndemand > p_soilNavailable + p_Nfix))
                Nstress = p_soilNavailable / (p_Ndemand - p_Nfix);

            for (int s = 0; s < Nsp; s++)          //Pasture N demand 
            {
                if (!SP[s].isLegume)
                {
                    if (SP[s].Ndemand <= SP[s].Nremob)
                    {
                        SP[s].soilNdemand = 0;
                        SP[s].remob2NewGrowth = SP[s].Ndemand;
                        SP[s].Nremob -= SP[s].Ndemand;
                    }
                    else
                    {
                        SP[s].soilNdemand = SP[s].Ndemand - SP[s].Nremob;
                        SP[s].remob2NewGrowth = SP[s].Nremob;
                        SP[s].Nremob = 0;
                    }
                }
                else
                {
                    if (Nstress < 0.99)  //more fixation under Nstress
                    {
                        double newNfix = (SP[s].MaxFix - (SP[s].MaxFix - SP[s].MinFix) * Nstress) * SP[s].Ndemand;
                        double moreNfix = Math.Max(0.0, (newNfix - SP[s].Nfix));
                        SP[s].Nfix = newNfix;
                        p_Nfix += moreNfix;
                    }

                    if (SP[s].Ndemand <= SP[s].Nremob + SP[s].Nfix)
                    {
                        SP[s].remob2NewGrowth = SP[s].Ndemand - SP[s].Nfix;
                        SP[s].Nremob -= SP[s].remob2NewGrowth;
                        SP[s].soilNdemand = 0;
                    }
                    else
                    {
                        SP[s].remob2NewGrowth = SP[s].Nremob;
                        SP[s].soilNdemand = SP[s].Ndemand - SP[s].Nfix - SP[s].Nremob;
                        SP[s].Nremob = 0;
                    }
                }
                p_soilNdemand += SP[s].soilNdemand;
            }

            //3) soil N uptake & N limiation factor
            p_soilNuptake = 0;
            p_gfn = 0;
            for (int s = 0; s < Nsp; s++)          //Pasture N demand and uptake
            {
                if (Nstress < 0.99)
                {
                    //partitioning available N among species according to their N demand%               
                    if (p_soilNdemand == 0)
                    {
                        SP[s].soilNuptake = 0;
                        SP[s].gfn = 1.0;
                        SP[s].newGrowthN = SP[s].remob2NewGrowth + SP[s].Nfix; //Nfix = 0 for non-legume
                    }
                    else
                    {
                        SP[s].soilNuptake = p_soilNavailable * SP[s].soilNdemand / p_soilNdemand;
                        if (SP[s].Ndemand == 0)
                        {
                            SP[s].gfn = 1.0;
                            SP[s].newGrowthN = 0;
                        }
                        else
                        {
                            if (SP[s].isLegume)
                            {
                                SP[s].newGrowthN = SP[s].soilNuptake + SP[s].remob2NewGrowth + SP[s].Nfix;
                                SP[s].gfn = SP[s].newGrowthN / SP[s].NdemandOpt;
                            }
                            else
                            {
                                SP[s].newGrowthN = SP[s].soilNuptake + SP[s].remob2NewGrowth;
                                SP[s].gfn = SP[s].newGrowthN / SP[s].NdemandOpt;
                            }

                            if (SP[s].gfn > 1.0) SP[s].gfn = 1.0;
                            if (SP[s].gfn < 0.0) SP[s].gfn = 0.0;
                        }
                    }
                }
                else
                {
                    SP[s].soilNuptake = SP[s].soilNdemand;
                    SP[s].gfn = 1.0;
                    SP[s].newGrowthN = SP[s].soilNuptake + SP[s].remob2NewGrowth + SP[s].Nfix; //Nfix = 0 for non-legume
                }
                p_soilNuptake += SP[s].soilNuptake;

                if (p_dGrowthW == 0)
                { p_gfn = 1; }
                else
                {
                    p_gfn += SP[s].gfn * SP[s].dGrowthW / p_dGrowthW;   //weighted average of species gfn
                }
            }
            //4) Go and uptake
            double soilNremoved = 0;
            if (NUptakeSource == "calc")
            {
                soilNremoved = SNUptakeProcess();               //N remove from soil                 
            }
            else
            {
                // N uptake calculated by other modules (e.g., SWIM)  
                String msg = "\nInforamtion: AgPasture calculates N uptake. No other approach is available now.";
                msg += "\n             Please specify N uptake source as default \"calc\".";
                Console.WriteLine(msg);

            }

            return soilNremoved;


        }


        #endregion //Eventhandlers
        //--------------------------------------------------------------------------------------

        //==================================================================

        #region "Properties"
        [Units("")]
        public String Crop_type         //  useful for SWIM 
        {
            get { return thisCropName; }
        }

        [Units("")]
        public String plant_status
        {
            get
            {
                if (p_Live) return "alive";
                else return "out";
            }
        }
        [Units("")]
        public int Stage
        {
            //An approximate of teh stages corresponding to that of other arable crops for management application settings. 
            //Phenostage of the first species (ryegrass) is used for this approximation
            get
            {
                int cropStage = 0; //default as "phase out"
                if (p_Live)
                {
                    if (SP[0].phenoStage == 0)
                        cropStage = 1;    //"sowing & germination"; 
                    if (SP[0].phenoStage == 1)
                        cropStage = 3;    //"emergence";                                   
                }
                return cropStage;
            }
        }
        [Units("")]
        public String StageName
        {
            get
            {
                String name = "out";
                if (p_Live)
                {
                    if (SP[0].phenoStage == 0)
                        name = "sowing";    //cropStage = 1 & 2
                    if (SP[0].phenoStage == 1)
                        name = "emergence"; // cropStage = 3    
                }
                return name;
            }
        }


        [Units("kg/ha")]
        public double TotalC            // total C in plant, kg/ha
        {
            get { return 0.4 * (p_totalDM + p_rootMass); }
        }

        [Units("kg/ha")]
        public double TotalPlantWt      //dm_plant          // total dm in plant
        {
            get { return (AboveGroundWt + BelowGroundWt); }    //dm_shoot+dm_root);}
        }

        [Units("kg/ha")]
        public double Biomass
        {
            get { return AboveGroundWt; }
        }

        [Units("kg/ha")]
        public double AboveGroundWt     //dm_shoot          // total dm in shoot (above ground)
        {
            get
            {  // return p_totalDM; } 
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmshoot;
                return dm;
            }
        }
        [Units("kg/ha")]
        public double BelowGroundWt      //dm_root          // total dm in root
        {
            get { return p_rootMass; }
        }

        [Units("kg/ha")]
        public double AboveGroundLiveWt //dm_green          // total dm in green shoot
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmgreen;
                return dm;
            }
        }

        [Units("kg/ha")]
        public double AboveGroundDeadWt //dm_dead          // total dm in standing dead
        {
            get { return p_deadDM; }
        }
        [Units("kg/ha")]
        public double LeafWt            //dm_leaf         
        {
            get
            {
                double dmleaf = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dmleaf += SP[s].dmleaf;
                return dmleaf;
            }
        }
        [Units("kg/ha")]
        public double LeafLiveWt        //dm_leaf_green          // total dm in root
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmleaf_green;
                return dm;
            }
        }
        [Units("kg/ha")]
        public double LeafDeadWt        //dm_leaf_dead          // total dm in root
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmleaf4;
                return dm;
            }
        }

        [Units("kg/ha")]
        public double StemDeadWt        //dm_stem_dead          // total dm in root
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmstem4;
                return dm;
            }
        }
        [Units("kg/ha")]
        public double StemLiveWt        //dm_stem_green          // total dm in root
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmstem_green;
                return dm;
            }
        }

        [Units("kg/ha")]
        public double StemWt            //dm_stem          // total dm in root
        {
            get
            {
                double dmstem = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dmstem += SP[s].dmstem;
                return dmstem;
            }
        }

        [Units("kg/ha")]
        public double StolonWt          //dm_stolon          // total dm in root
        {
            get
            {
                double dmstol = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dmstol += SP[s].dmstol;
                return dmstol;
            }
        }
        //for consistantly passing varibles in Onremove_crop_biomass()with other plant modules 
        [Units("g/m^2")]
        public double leafgreenwt { get { return LeafLiveWt / 10; } }
        [Units("g/m^2")]
        public double stemgreenwt { get { return StemLiveWt / 10; } }
        [Units("g/m^2")]
        public double leafsenescedwt { get { return LeafDeadWt / 10; } }
        [Units("g/m^2")]
        public double stemsenescedwt { get { return StemDeadWt / 10; } }

        [Units("kg/ha")]
        public double PlantPotentialGrowthWt //dm_daily_pot_growth
        {
            get { return p_dGrowthPot; }
        }
        [Units("kg/ha")]
        public double PlantGrowthNoNLimit
        {
            get { return p_dGrowthW; }
        }
        [Units("kg/ha")]
        public double GrossGrowthWt        //dm_daily_growth
        {                                 //including roots & before littering   
            get { return p_dGrowth; }
        }

        [Units("kg/ha")]
        public double HerbageGrowthWt     //dm_daily_herbage
        {
            get { return p_dHerbage; }
        }

        [Units("kg/ha")]
        public double LitterDepositionWt     //dm_daily_litter
        {
            get { return p_dLitter; }
        }

        [Units("kg/ha")]
        public double RootSenescenceWt       //dm_daily_rootSen
        {
            get { return p_dRootSen; }
        }

        [Units("kg/ha")]
        public double HarvestWt      //dm_daily_harvest
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmdefoliated;
                return dm;
            }
        }

        //**LAI & Cover
        [Units("")]
        public double LAI_green
        {
            get { return p_greenLAI; }
        }

        [Units("")]
        public double LAI_dead
        {
            get { return p_deadLAI; }
        }

        [Units("")]
        public double LAI_total
        {
            get { return p_totalLAI; }
        }

        [Units("%")]
        public double Cover_green
        {
            get
            {
                if (p_greenLAI == 0) return 0;
                return (1.0 - Math.Exp(-p_lightExtCoeff * p_greenLAI));
            }

        }

        [Units("%")]
        public double Cover_dead
        {
            get
            {
                if (p_deadLAI == 0) return 0;
                return (1.0 - Math.Exp(-p_lightExtCoeff * p_deadLAI));
            }
        }

        [Units("%")]
        public double Cover_tot
        {
            get
            {
                if (p_totalLAI == 0) return 0;
                return (1.0 - (Math.Exp(-p_lightExtCoeff * p_totalLAI)));
            }
        }

        //** Nitrogen
        [Units("kg/ha")]
        public double TotalPlantN          // total N in plant
        {
            get { return (AboveGroundN + BelowGroundN); }   //n_shoot + n_root); }
        }

        [Units("kg/ha")]
        public double AboveGroundN      //n_shoot          // total N above ground
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Nshoot;       //remoblised N is reported in stem
                return N;
            }
        }
        [Units("%")]
        public double AboveGroundNPct   //n_shoot_pct          // total [N] above ground   
        {
            get
            {
                double pct = 0;
                if (AboveGroundWt != 0)                      //dm_shoot != 0)
                    pct = 100 * AboveGroundN / AboveGroundWt;    // n_shoot/dm_shoot; 
                return pct;
            }
        }

        [Units("kg/ha")]
        public double BelowGroundN      //n_root          // total N below ground
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Nroot;
                return N;
            }
        }

        [Units("kg/ha")]
        public double AboveGroundLiveN  //n_green          // total N above ground green
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Ngreen;
                return N;
            }
        }
        [Units("kg/ha")]
        public double AboveGroundDeadN  //n_dead          // total N above ground dead
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Ndead;
                return N;
            }
        }

        [Units("kg/ha")]
        public double LeafN             //n_leaf          // leaf total N
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Nleaf;
                return N;
            }
        }

        [Units("kg/ha")]
        public double StemN             //n_stem          // stem total N 
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Nstem;
                return N;
            }
        }

        [Units("kg/ha")]
        public double StolonN           //n_stolon          // stolon total N 
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Nstolon;
                return N;
            }
        }

        [Units("kg/ha")]
        public double PlantFixedN       //n_daily_fix     
        {
            get { return p_Nfix; }
        }

        [Units("kg/ha")]
        public double HarvestN          //n_daily_harvest
        {
            get
            {
                //return (float)p_harvestN; }
                double n = 0.0;
                if (HarvestWt > 0.0)
                {
                    for (int s = 0; s < Nsp; s++)
                        n += SP[s].Ndefoliated;
                }
                return n;
            }
        }
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get
            {
                if (!p_Live || (StemWt + LeafWt) <= 0)    // (dm_stem + dm_leaf) <= 0)
                    return 0;

                double digest = 0.0;
                for (int s = 0; s < Nsp; s++)
                    digest += SP[s].digestHerbage * (SP[s].dmstem + SP[s].dmleaf) / (StemWt + LeafWt);  //(dm_stem + dm_leaf);
                return digest;
            }
        }
        [Units("0-1")]
        public double DefoliatedDigestibility
        {
            get { return p_harvestDigest; }
        }
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get
            {
                double me = 16 * HerbageDigestibility * (StemWt + LeafWt); //(dm_stem + dm_leaf);
                return me;
            }
        }
        [Units("kg/ha")]
        public double LitterDepositionN  //n_daily_litter
        {
            get { return p_dNLitter; }
        }

        [Units("kg/ha")]
        public double RootSenescenceN    //n_daily_rootSen
        {
            get { return p_dNRootSen; }
        }

        [Units("kg/ha")]
        public double NitrogenDemand    //n_demand          //demand from soil
        {
            get { return p_soilNdemand; }
        }

        [Units("kg/ha")]
        public double NitrogenSupply    //n_supply
        {
            get { return p_soilNavailable; }
        }

        [Units("kg/ha")]
        public double[] NitrogenSupplyLayers //n_supply_layer
        {
            get { return SNSupply; }
        }

        [Units("kg/ha")]
        public double NitrogenUptake        //n_uptake
        {
            get { return p_soilNuptake; }
        }

        [Units("kg/ha")]
        public double[] NitrogenUptakeLayers //n_uptake_layer
        {
            get { return SNUptake; }
        }

        [Units("0-1")]      //Calculate growth factor of nitrogen deficit
        public double GLFn                      //gf_n
        {
            get { return p_gfn; }
        }
        [Units("0-1")]      //Calculate growth factor of nitrogen deficit
        public double GLFnConcentration         //Ncfactor
        {
            get
            {
                double f = 0.0;
                for (int s = 0; s < Nsp; s++)
                    f += SP[s].Ncfactor * SP[s].dmshoot;
                return (f / AboveGroundWt);     //dm_shoot);
            }
        }
        [Units("kg/ha")]
        public double ToRootDM
        {
            get
            {
                double drDM = 0.0;
                for (int s = 0; s < Nsp; s++)
                {
                    drDM += (1 - SP[s].fShoot) * SP[s].dGrowth;
                }
                return drDM;
            }
        }

        [Units("0-1")]      //Calculate growth factor of nitrogen deficit
        public double ToRoot
        {
            get
            {
                double f = 0.0;
                for (int s = 0; s < Nsp; s++)
                {
                    f += SP[s].fShoot * SP[s].dGrowth;
                }
                if (p_dGrowth <= 0) return 0;       //p_dGrowth = dm_daily_growth
                else return (1 - f / p_dGrowth);    //dm_daily_growth);
            }
        }

        //** water related
        [Units("mm/mm^3")]
        public double[] rlv         //Root Length Density (mm/mm3)  
        {
            get
            {
                double[] rlv = new Double[rlvp.Length];
                double sum_rlvp = 0;            // note: rlvp is the root length proportion over the soil profile
                double p_srl = 75000;           // specific root length (mm root/g root)
                //Compute the root length, total over the whole profile
                double Total_Rlength = p_rootMass * p_srl * 0.0000001;  //(mm root/mm2 soil)
                for (int layer = 0; layer < rlvp.Length; layer++)
                {
                    //Sum the rlvp values up to the deepest layer with roots           
                    sum_rlvp += rlvp[layer];
                }
                for (int layer = 0; layer < rlv.Length; layer++)
                {
                    //Recalculate the rlv values           
                    rlv[layer] = rlvp[layer] * Total_Rlength / (sum_rlvp * SoilWat.dlayer[layer]);
                }
                return rlv;
            }
        }

        [Units("mm")]
        public double WaterDemand   //sw_demand          // Daily Soil Water Demand (mm)
        {
            get { return p_waterDemand; }
        }

        private double actualUptake;

        [Units("mm")]
        public double ep   //sw_demand          // Daily Soil Water Demand (mm)
        {
            get { return actualUptake; }
        }

        [Units("mm")]
        public double WaterSupply   //sw_supply          // Daily Soil Water Demand (mm)
        {
            get { return p_waterSupply; }
        }

        [Units("mm")]
        public double[] WaterSupplyLayers    //sw_supply_layer
        {
            get { return SWSupply; }
        }

        [Units("mm")]
        public double WaterUptake     //sw_uptake          // Daily Soil Water Demand (mm)
        {
            get { return p_waterUptake; }
        }

        [Units("mm")]
        public double[] WaterUptakeLayers     //sw_uptake_layer
        {
            get { return SWUptake; }
        }

        [Units("0-1")]
        public double GLFwater              //gf_water
        {
            get { return p_gfwater; }
        }

        //**Stress factors
        [Units("0-1")]
        public double GLFtemp               //gf_temp
        {
            get { return p_gftemp; }
        }

        [Units("0-1")]
        public double GLFrgr                 //gf_rgr
        {
            get
            {
                double p_Frgr = 0; //weighted value
                for (int s = 0; s < Nsp; s++)
                {
                    double prop = 1.0 / Nsp;
                    if (p_greenDM != 0.0)
                    {
                        prop = SP[s].dmgreen / AboveGroundLiveWt;
                    }
                    p_Frgr += SP[s].Frgr * prop;
                }
                return p_Frgr;
            }
        }

        [Units("mm")]
        public double Height                 //needed by micromet
        {
            get { return p_height; }
        }

        //testing purpose
        [Units("kg/ha")]
        public double dm1          // just for testing, kg/ha   
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmleaf1 + SP[s].dmstem1 + SP[s].dmstol1;
                return dm;
            }
        }

        [Units("kg/ha")]
        public double dm2          // just for testing, kg/ha 
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmleaf2 + SP[s].dmstem2 + SP[s].dmstol2;
                return dm;
            }
        }
        [Units("kg/ha")]
        public double dm3          // just for testing, kg/ha  
        {
            get
            {
                double dm = 0.0;
                for (int s = 0; s < Nsp; s++)
                    dm += SP[s].dmleaf3 + SP[s].dmstem3 + SP[s].dmstol3;
                return dm;
            }
        }
        [Units("kg/ha")]
        public double N1          // just for testing, kg/ha   
        {
            get
            {
                double n = 0.0;
                for (int s = 0; s < Nsp; s++)
                    n += SP[s].Nleaf1 + SP[s].Nstem1 + SP[s].Nstol1;
                return n;
            }
        }
        [Units("kg/ha")]
        public double N2          // just for testing, kg/ha    
        {
            get
            {
                double n = 0.0;
                for (int s = 0; s < Nsp; s++)
                    n += SP[s].Nleaf2 + SP[s].Nstem2 + SP[s].Nstol2;
                return n;
            }
        }
        [Units("kg/ha")]
        public double N3          // just for testing, kg/ha    
        {
            get
            {
                double n = 0.0;
                for (int s = 0; s < Nsp; s++)
                    n += SP[s].Nleaf3 + SP[s].Nstem3 + SP[s].Nstol3;
                return n;
            }
        }
        [Units("kg/ha")]
        public double PlantRemobilisedN     //n_remob          // just for testing, kg/ha
        {
            get
            {
                double N = 0.0;
                for (int s = 0; s < Nsp; s++)
                    N += SP[s].Nremob;
                return N;
            }
        }
        [Units("kg/ha")]
        public double C_remob          // just for testing, kg/ha
        {
            get
            {
                double C = 0.0;
                for (int s = 0; s < Nsp; s++)
                    C += SP[s].Cremob;
                return C;
            }
        }

        private double HeightfromDM        // height calculation from DM, not output
        {
            get
            {
                double ht = HeightMassFN.Value(p_greenDM + p_deadDM);
                if (ht < 20.0) ht = 20.0F;      // minimum = 20mm
                return ht;
            }

        }
        [Units("")]
        public double VPD_out              // VPD effect on Growth Interpolation Set
        {
            get { return VPD(); }
        }

        [Units("")]
        public double FVPD              // VPD effect on Growth Interpolation Set 
        {                               // mostly = 1 for crop/grass/forage
            get { return FVPDFunction.Value(VPD()); }
        }

        //Following are species values (arrays) 
        [Units("m^2/m^2")]
        public double[] SpeciesGreenLAI  //spGreenLAI
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].greenLAI;
                return ftArray;
            }
        }
        [Units("kg/ha")]
        public double[] SpeciesLiveWt  //spGreenDM
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].dmgreen;
                return ftArray;
            }
        }
        [Units("kg/ha")]
        public double[] SpeciesTotalWt
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].dmtotal;
                return ftArray;
            }
        }

        [Units("%")]
        public double[] SpeciesHarvestPct  //proportion in harvested biomass, not on teh land 
        {
            get
            {
                double totalHarvestable = 0;  //for excluding stolon
                for (int s = 0; s < Nsp; s++)
                    totalHarvestable += SP[s].dmstem + SP[s].dmleaf;
                for (int s = 0; s < Nsp; s++)
                {
                    if (totalHarvestable == 0.0) ftArray[s] = 0.0F;
                    else ftArray[s] = (SP[s].dmstem + SP[s].dmleaf) * 100 / totalHarvestable;
                }
                return ftArray;
            }
        }
        [Units("kgDM/ha")]
        public double[] SpeciesHarvestWt  //species N 
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].dmdefoliated;
                return ftArray;
            }
        }

        [Units("kgN/ha")]
        public double[] SpeciesHarvestN  //species N 
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].Ndefoliated;
                return ftArray;
            }
        }

        [Units("0-1")]      //Calculate growth factor of nitrogen deficit
        public double[] spGFN
        {
            get
            {
                for (int s = 0; s < Nsp; s++)
                    ftArray[s] = SP[s].gfn;
                return ftArray;
            }
        }

        #endregion
        //=================================================================


        //=================================================================
        #region "Functions"

        //===============================================    
        /// <summary>
        /// water uptake processes ...
        /// Rainss Notes 20010707
        ///  - Should this be done per species? Is using the root frontier an acceptable solution?
        ///  - Plant2 breaks this into two parts: WaterSupply and DoWaterUptake
        /// 
        /// </summary>
        /// <returns></returns>
        private double SWUptakeProcess()
        {

            //find out soil available water
            p_waterSupply = 0;
            for (int layer = 0; layer < SoilWat.dlayer.Length; layer++)
            {
                SWSupply[layer] = Math.Max(0.0,
                                  kl[layer] * (SoilWat.sw_dep[layer] - ll[layer] * (SoilWat.dlayer[layer]))) * RootProportion(layer, p_rootFrontier);

                if (layer < p_bottomRootLayer)
                    p_waterSupply += SWSupply[layer];
            }

            //uptake in proportion
            PMF.WaterChangedType WaterUptake = new PMF.WaterChangedType();
            WaterUptake.DeltaWater = new double[SoilWat.dlayer.Length];
            //float[] SWUptake = new float[dlayer.Length];
            double Fraction = Math.Min(1.0, p_waterDemand / p_waterSupply);
            actualUptake = 0.0;
            for (int layer = 0; layer < p_bottomRootLayer; layer++)
            {   //water are taken up only in top layers that root can reach.               
                SWUptake[layer] = SWSupply[layer] * Fraction;
                actualUptake += SWUptake[layer];
                WaterUptake.DeltaWater[layer] = -SWUptake[layer];
            }

            if (WaterChanged != null)
                WaterChanged.Invoke(WaterUptake);

            return actualUptake;
        }

        private double RootProportion(int layer, double root_depth)
        {
            switch (UseRootProportion)
            {
                case 1:
                    { // "Linear" code taken directly from Plant2
                        double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
                        double depth_to_layer_top = 0;      // depth to top of layer (mm)
                        double depth_to_root = 0;           // depth to root in layer (mm)
                        double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
                        // Implementation Section ----------------------------------
                        for (int i = 0; i <= layer; i++)
                            depth_to_layer_bottom += SoilWat.dlayer[i];
                        depth_to_layer_top = depth_to_layer_bottom - SoilWat.dlayer[layer];
                        depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
                        depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

                        return depth_of_root_in_layer / SoilWat.dlayer[layer];
                    }
                case 2:
                    { // "broken stick"
                        double depth_to_layer_bottom = 0;
                        for (int i = 0; i <= layer; i++)
                        {
                            depth_to_layer_bottom += SoilWat.dlayer[i];
                        }
                        double depth_to_layer_top = depth_to_layer_bottom - SoilWat.dlayer[layer];

                        double dX0 = rootDist[0] * root_depth; //TODO allow this to be set via Root Distribution Parameter
                        double dX1 = root_depth;

                        if (Debug_Level > 1)
                        {
                            Console.Out.WriteLine();
                            Console.Out.WriteLine("Root Depth 0 = " + dX0);
                            Console.Out.WriteLine("Root Depth 1 = " + dX1);
                            Console.Out.WriteLine("Layer = " + (layer + 1));
                            Console.Out.WriteLine("   depth_to_layer_top = " + depth_to_layer_top);
                            Console.Out.WriteLine("   depth_to_layer_bottom = " + depth_to_layer_bottom);
                        }
                        if (depth_to_layer_bottom <= dX0)
                        {
                            if (Debug_Level > 1)
                                Console.Out.WriteLine("   1.0 =        fully in the Square Zone");
                            return 1.0; // fully in the "Square Zone"
                        }
                        if (depth_to_layer_top >= dX1)
                        {
                            if (Debug_Level > 1)
                                Console.Out.WriteLine("   0.0 =        fully below the root zone");
                            return 0.0; // fully below the root zone
                        }

                        //Includes special cases of includes poviot point and/or layer partially below the root zone
                        double rootUpper = (dX0 > depth_to_layer_top) ? dX0 - depth_to_layer_top : 0;
                        double rootLower = 0;
                        if (dX0 < dX1)
                        {
                            double A = dX1 - dX0; // depth of "triangle"
                            double B = 1;
                            double a0 = A - (Math.Max(depth_to_layer_top, dX0) - dX0);
                            double b0 = B * (a0 / A);
                            double a1 = A - (Math.Min(depth_to_layer_bottom, dX1) - dX0);
                            double b1 = B * (a1 / A);
                            double prop = (b0 + b1) / 2;
                            double depth = Math.Min(depth_to_layer_bottom, dX1) - Math.Max(depth_to_layer_top, dX0);
                            rootLower = prop * depth;
                        }
                        double result = (rootUpper + rootLower) / (depth_to_layer_bottom - depth_to_layer_top);
                        if (Debug_Level > 1)
                        {
                            Console.Out.WriteLine("   " + rootUpper + "        rootUpper");
                            Console.Out.WriteLine("   " + rootLower + "        rootLower");
                            Console.Out.WriteLine("   " + result + "        result");
                        }
                        return result;
                    }

                default: return 1; //this is equivilant to the original implementation (depreciated)
            }
        }

        //================================================
        /// <summary>
        /// Nitrogen uptake process
        /// </summary>
        /// <returns></returns>
        private double SNUptakeProcess()
        {
            //Uptake from the root_zone
            Soils.NitrogenChangedType NUptake = new Soils.NitrogenChangedType();
            NUptake.Sender = Name;
            NUptake.SenderType = "Plant";
            NUptake.DeltaNO3 = new double[SoilWat.dlayer.Length];
            NUptake.DeltaNH4 = new double[SoilWat.dlayer.Length];

            double Fraction = 0;
            if (p_soilNavailable > 0)
            {
                Fraction = Math.Min(1.0, p_soilNuptake / p_soilNavailable);
            }

            double n_uptake = 0;

            if (alt_N_uptake == "yes")
            {
                double
                    uptake_multiplier = double.MaxValue,
                    totSWUptake = SWUptake.Sum();

                double[]
                    availableNH4_bylayer = new double[SoilWat.dlayer.Length],
                    availableNO3_bylayer = new double[SoilWat.dlayer.Length],
                    diffNH4_bylayer = new double[SoilWat.dlayer.Length],
                    diffNO3_bylayer = new double[SoilWat.dlayer.Length];

                for (int sLayer = 0; sLayer < SoilWat.dlayer.Length; sLayer++)
                {
                    double
                        totN = SoilN.nh4[sLayer] + SoilN.no3[sLayer],
                        fracH2O = SWUptake[sLayer] / totSWUptake;

                    if (totN > 0)
                    {
                        availableNH4_bylayer[sLayer] = fracH2O * SoilN.nh4[sLayer] / totN;
                        availableNO3_bylayer[sLayer] = fracH2O * SoilN.no3[sLayer] / totN;

                        //if we have no3 and nh4 in this layer then calculate our uptake multiplier, otherwise set it to 0
                        //the idea behind the multiplier is that it allows us to calculate the max amount of N we can extract
                        //without forcing any of the layers below 0 AND STILL MAINTAINING THE RATIO as calculated with fracH2O
                        //NOTE: it doesn't matter whether we use nh4 or no3 for this calculation, we will get the same answer regardless
                        uptake_multiplier = SoilN.nh4[sLayer] * SoilN.no3[sLayer] > 0 ? Math.Min(uptake_multiplier, SoilN.nh4[sLayer] / availableNH4_bylayer[sLayer]) : 0;
                    }
                    else
                    {
                        availableNH4_bylayer[sLayer] = 0;
                        availableNO3_bylayer[sLayer] = 0;
                    }
                }

                //adjust availability values with the multiplier we just calculated
                availableNH4_bylayer = availableNH4_bylayer.Select(x => x * uptake_multiplier).ToArray();
                availableNO3_bylayer = availableNO3_bylayer.Select(x => x * uptake_multiplier).ToArray();

                //calculate how much no3/nh4 will be left in the soil layers (diff_nxx[layer] = nxx[layer] - availableNH4_bylayer[layer])
                diffNH4_bylayer = SoilN.nh4.Select((x, sLayer) => Math.Max(0, x - availableNH4_bylayer[sLayer])).ToArray();
                diffNO3_bylayer = SoilN.no3.Select((x, sLayer) => Math.Max(0, x - availableNO3_bylayer[sLayer])).ToArray();

                //adjust this by the sum of all leftover so we get a ratio we can use later
                double sum_diff = diffNH4_bylayer.Sum() + diffNO3_bylayer.Sum();
                diffNH4_bylayer = diffNH4_bylayer.Select(x => x / sum_diff).ToArray();
                diffNO3_bylayer = diffNO3_bylayer.Select(x => x / sum_diff).ToArray();

                double
                    //available N from our 'withwater' calcs (still some left in the 'diff' arrays if this isn't enough)
                    avail_withwater = availableNH4_bylayer.Sum() + availableNO3_bylayer.Sum(),
                    //if not enough N was available via the 'withwater' calcs this will be positive and will require more from the 'diffs' we calculated
                    shortfall_withwater = p_soilNuptake - avail_withwater;

                if (shortfall_withwater > 0)
                {
                    //this cap should not be needed because shortfall is already capped via the math.min in the scaled_demand calcs (leave it here though)
                    double scaled_diff = Math.Min(shortfall_withwater / avail_withwater, 1);

                    availableNH4_bylayer = availableNH4_bylayer.Select((x, sLayer) => x + shortfall_withwater * diffNH4_bylayer[sLayer]).ToArray();
                    availableNO3_bylayer = availableNO3_bylayer.Select((x, sLayer) => x + shortfall_withwater * diffNO3_bylayer[sLayer]).ToArray();
                }

                NUptake.DeltaNH4 = availableNH4_bylayer.Select(x => x * -1).ToArray();
                NUptake.DeltaNO3 = availableNO3_bylayer.Select(x => x * -1).ToArray();

                for (int layer = 0; layer < p_bottomRootLayer; layer++)
                    n_uptake += SNUptake[layer] = (NUptake.DeltaNH4[layer] + NUptake.DeltaNO3[layer]) * -1;

                double[] diffs = NUptake.DeltaNO3.Select((x, i) => Math.Max(SoilN.no3[i] + x + 0.00000001, 0)).ToArray();
                if (diffs.Any(x => x == 0))
                    throw new Exception();

            }

            /*if (ValsMode == "withwater")
            {
                NUptake.DeltaNO3 = SP[0].availableNO3_bylayer.Select(x => x * -1).ToArray();
                NUptake.DeltaNH4 = SP[0].availableNH4_bylayer.Select(x => x * -1).ToArray();

                for (int layer = 0; layer < p_bottomRootLayer; layer++)
                    SNUptake[layer] = SP[0].availableNO3_bylayer[layer] + SP[0].availableNH4_bylayer[layer];
                n_uptake = SNUptake.Sum();
            }*/
            else
            {
                for (int layer = 0; layer < p_bottomRootLayer; layer++)
                {   //N are taken up only in top layers that root can reach (including buffer Zone).                               
                    n_uptake += (SoilN.no3[layer] + SoilN.nh4[layer]) * Fraction;
                    SNUptake[layer] = (SoilN.no3[layer] + SoilN.nh4[layer]) * Fraction;

                    NUptake.DeltaNO3[layer] = -SoilN.no3[layer] * Fraction;
                    NUptake.DeltaNH4[layer] = -SoilN.nh4[layer] * Fraction;
                }
            }

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptake);
            return n_uptake;
        }


        //-------------------------------------------------------
        /// <summary>
        /// return plant litter to surface organic matter poor
        /// </summary>
        /// <param name="amtDM"></param>
        /// <param name="amtN"></param>
        /// <param name="frac"></param>
        private void DoSurfaceOMReturn(Double amtDM, Double amtN, Double frac)
        {
            if (BiomassRemoved != null)
            {
                Single dDM = (Single)amtDM;

                PMF.BiomassRemovedType BR = new PMF.BiomassRemovedType();
                String[] type = new String[1];
                Single[] dltdm = new Single[1];
                Single[] dltn = new Single[1];
                Single[] dltp = new Single[1];
                Single[] fraction = new Single[1];

                type[0] = "grass";
                dltdm[0] = dDM;                 // kg/ha 
                dltn[0] = (Single)amtN;         // dDM * (Single)dead_nconc;
                dltp[0] = dltn[0] * 0.3F;       //just a stub here, no P budgeting process in this module
                fraction[0] = (Single)frac;

                BR.crop_type = "grass";
                BR.dm_type = type;
                BR.dlt_crop_dm = dltdm;
                BR.dlt_dm_n = dltn;
                BR.dlt_dm_p = dltp;
                BR.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(BR);
            }

        }



        //--------------------------------------------------------------------------------
        /// <summary>
        /// return scenescent roots into fresh organic matter pool in soil
        /// </summary>
        /// <param name="rootSen"></param>
        /// <param name="NinRootSen"></param>
        private void DoIncorpFomEvent(double rootSen, double NinRootSen)
        {
            Soils.FOMLayerLayerType[] fomLL = new Soils.FOMLayerLayerType[SoilWat.dlayer.Length];

            //considering root_zone only or minimum root
            int minLayer = rlvp.Length;
            if (minLayer > SoilWat.dlayer.Length)
                minLayer = SoilWat.dlayer.Length;

            //Console.WriteLine("rlvp layers should match the number of soil layers.");
            //throw new Exception("..");

            //the amount could be according to the root length density            
            double sum_rlvp = 0.0;     //weighted sum of root length density, as defined by following formula
            for (int i = 0; i < minLayer; i++)
            {
                if (i >= p_bottomRootLayer)
                    break;

                sum_rlvp += rlvp[i] * SoilWat.dlayer[i];
            }

            double dAmtLayer = 0.0; //amount of root litter in a layer
            double dNLayer = 0.0;
            for (int i = 0; i < SoilWat.dlayer.Length; i++)
            {
                if (sum_rlvp == 0.0 || (i >= minLayer)) //no root distribution if (i > minLayer)
                {
                    dAmtLayer = dNLayer = 0.0;
                }
                else
                {
                    dAmtLayer = rootSen * rlvp[i] * SoilWat.dlayer[i] / sum_rlvp;
                    dNLayer = NinRootSen * rlvp[i] * SoilWat.dlayer[i] / sum_rlvp;
                }
                double amt = dAmtLayer;

                Soils.FOMType fom = new Soils.FOMType();
                fom.amount = amt;
                fom.N = dNLayer;// 0.03F * amt;    // N in dead root                
                fom.C = 0.40F * amt;    //40% of OM is C. Actually, 'C' is not used, as shown in DataTypes.xml
                fom.P = 0;              //to consider later
                fom.AshAlk = 0;         //to consider later

                Soils.FOMLayerLayerType Layer = new Soils.FOMLayerLayerType();
                Layer.FOM = fom;
                Layer.CNR = 0;       //not used
                Layer.LabileP = 0;   //not used

                fomLL[i] = Layer;
            }

            if (IncorpFOM != null)
            {
                Soils.FOMLayerType FomLayer = new Soils.FOMLayerType();
                FomLayer.Type = "agpasture";
                FomLayer.Layer = fomLL;
                IncorpFOM.Invoke(FomLayer);
            }
        }


        [Units("0-1")]
        public double outcoverRF
        {
            get { return coverRF(); }
        }

        /// <summary>
        ///  Temporary for estimating IL reduction factor when considering other possible canopies. 
        ///  Only useful for species reporting to micromet as separate pasture canopies and having
        ///  similar growth forms (height). 
        ///  Need to be cautious when using it, and this be removed we may get IL of a species  
        ///  from multiple canopies (of separate species) in other module    
        /// </summary>
        /// <returns></returns>
        public double coverRF()
        {
            if (canopiesNum == 1)
                return 1;

            double totR = 0;
            for (int i = 0; i < canopiesNum; i++)
                totR += canopiesRadn[i];

            double sumCov = 0;
            double sumLAI = 0;
            for (int i = 0; i < canopiesNum; i++)
            {
                double cover = Cover_green * canopiesRadn[i] / _IntRadn;
                sumCov += cover;
                sumLAI += -Math.Log(1 - cover) / p_lightExtCoeff;

            }
            double totCov = 1 - Math.Exp(-p_lightExtCoeff * sumLAI);
            return totCov / sumCov;
        }
        #endregion //Funcitons


        #region "Utilities"
        //-----------------------------------------------------------------
        /// <summary>
        /// The following helper functions [VDP and svp] are for calculating Fvdp
        /// </summary>
        /// <returns></returns>
        private double VPD()
        {
            double VPDmint = svp(MetData.MinT) - MetData.vp;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = svp(MetData.MaxT) - MetData.vp;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            double vdp = SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            return vdp;
        }
        private double svp(double temp)  // from Growth.for documented in MicroMet
        {
            return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
        }

        #endregion //Utility
    }


    //------------------------------------------------------------------------------

    [Serializable]
    public class LinearInterpolation
    {
        //public string[] XYs;

        public double[] X;
        public double[] Y;

        /*
        [EventSubscribe("Initialised")]
        public void OnInitialised()
        {

            X = new double[XYs.Length];
            Y = new double[XYs.Length];
            for (int i = 0; i < XYs.Length; i++)
            {
                string[] XYBits = XYs[i].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (XYBits.Length != 2)
                    throw new Exception("Invalid XY coordinate for function. Value: " + XYs[i]);
                X[i] = Convert.ToDouble(XYBits[0]);
                Y[i] = Convert.ToDouble(XYBits[1]);
            }
        } */
        public double Value(double dX)
        {
            bool DidInterpolate = false;
            return Utility.Math.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }

    //================================================================================
    // One species
    //
    //================================================================================
    [Serializable]
    public class Species
    {

        internal DMPools pS;                //for remember the status of previous day
        //constants                        
        const double CD2C = 12.0 / 44.0;    //convert CO2 into C 
        const double C2DM = 2.5;            //C to DM convertion
        const double DM2C = 0.4;            //DM to C converion
        const double N2Protein = 6.25;      //this is for plants... (higher amino acids)
        const double C2N_protein = 3.5;     //C:N in remobilised material 
        //const double growthTref = 20.0;      //reference temperature

        //static variables for common parameters among species    
        internal static WeatherFile.NewMetType MetData = new WeatherFile.NewMetType();    //climate data applied to all species    
        internal static double latitude;
        internal static double dayLength;                         //day length
        internal static double CO2 = 380;                         //CO2 concentration
        internal static double CO2ambient = 380;                  //ambient CO2 concentration 
        internal static int day_of_month;
        internal static int month;
        internal static int year;
        internal static double PIntRadn;                          //total Radn intecepted by pasture
        internal static double PCoverGreen;
        internal static double PLightExtCoeff;                    //k of mixed pasture
        internal static String thisCropName;
        internal static double Pdmshoot;

        internal double intRadnFrac;     //fraction of Radn intercepted by this species = intRadn/Radn 
        internal double intRadn;         //Intercepted Radn by this species 

        public String speciesName;
        public String micrometType;

        public bool isAnnual;        //Species type (1=annual,0=perennial)
        public bool isLegume;	    //Legume (0=no,1=yes)
        public int photoPath;       //Phtosynthesis pathways: 3=C3, 4=C4; //no consideration for CAM(=3)

        //annual species parameters
        public int dayEmerg; 	    //Earlist day of emergence (for annuals only)
        public int monEmerg;	    //Earlist month of emergence (for annuals only)
        public int dayAnth;	        //Earlist day of anthesis (for annuals only)
        public int monAnth;	        //Earlist month of anthesis (for annuals only)
        public int daysToMature;	//Days from anthesis to maturity (for annuals only)
        internal int daysEmgToAnth;   //Days from emergence to Anthesis (calculated, annual only)
        internal int phenoStage = 1;  //pheno stages: 0 - pre_emergence, 1 - vegetative, 2 - reproductive
        internal double phenoFactor = 1;
        internal int daysfromEmergence = 0;   //days 
        internal int daysfromAnthesis = 0;    //days

        internal bool bSown = false;
        internal double DDSfromSowing = 0;
        // For later use
        //private double DDSfromEmergence = 0;
        //private double DDSfromAnthesis = 0;

        //**public double cropFactor;	//Crop Factor
        //**public double maxResidCover;//Maximum Residue Cover (0-1) (added to ccov to define cover)
        public int dRootDepth;	    //Daily root growth (mm)
        public int maxRootDepth;	//Maximum root depth (mm)
        public double allocationSeasonF; //factor for different biomass allocation among seasons 
        public double NdilutCoeff;
        public int rootDepth;       //current root depth (mm)
        //**public int rootFnType;	    //Root function 0=default 1=Ritchie 2=power_law 3=proportional_depth

        public double growthTmin;   //Minimum temperature (grtmin) - originally 0
        public double growthTmax;   //Maximum temperature (grtmax) - originally 30
        public double growthTopt;   //Optimum temperature (grtopt) - originally 20
        public double growthTq;	    //Temperature n (grtemn) --fyl: q curvature coefficient, 1.5 for c3 & 2 for c4 in IJ

        public double heatOnsetT;	        //onset tempeature for heat effects
        public double heatFullT;	        //full temperature for heat effects
        public double heatSumT;	        //temperature sum for recovery - sum of (25-mean)
        public double coldOnsetT;          //onset tempeature for cold effects
        public double coldFullT;	        //full tempeature for cold effects
        public double coldSumT;	        //temperature sum for recovery - sum of means
        public double Pm;	                //reference leaf co2 g/m^2/s maximum
        public double maintRespiration;    //in % 
        public double growthEfficiency;


        private double highTempEffect = 1;  //fraction of growth rate due to high temp. effect
        private double lowTempEffect = 1;   //fraction of growth rate due to low temp. effect
        private double accumT = 0;          //accumulated temperature from previous heat strike = sum of '25-MeanT'(>0)
        private double accumTLow = 0;       //accumulated temperature from previous cold strike = sum of MeanT (>0) 

        public double massFluxTmin;	        //grfxt1	Mass flux minimum temperature
        public double massFluxTopt;	        //grfxt2	Mass flux optimum temperature
        public double massFluxW0;	        //grfw1	    Mass flux scale factor at GLFwater=0 (must be > 1)
        public double massFluxWopt;         //grfw2	    Mass flux optimum temperature

        //**public double satRadn;     	//Saturated canopy radiation Pnet (MJ/m^2/day)
        public double SLA;	            //Specific leaf area (m2/kg dwt)
        public double lightExtCoeff;    //Light extinction coefficient
        private double lightExtCoeff_ref;
        public double rue;              //radiaiton use efficiency 
        public double maxAssimiRate;    //Maximum Assimulation rate at reference temp & daylength (20C & 12Hrs) 
        public double rateLive2Dead;    //Decay coefficient between live and dead
        public double rateDead2Litter;	//Decay coefficient between dead and litter
        public double rateRootSen;      //Decay reference root senescence rate (%/day)
        public double stockParameter;   //Stock influence parameter
        public double maxSRratio;       //Shoot-Root ratio maximum
        public double leafRate;         //reference leaf appearance rate without stress
        public double fLeaf;	        //Fixed growth partition to leaf (0-1)
        public double fStolon;	        //Fixed growth partition to stolon (0-1)

        public double digestLive;   //Digestibility of live plant material (0-1)
        public double digestDead;   //Digestibility of dead plant material (0-1)

        internal double dmleaf1;	//leaf 1 (kg/ha)
        internal double dmleaf2;	//leaf 2 (kg/ha)
        internal double dmleaf3;	//leaf 3 (kg/ha)
        internal double dmleaf4;	//leaf dead (kg/ha)
        internal double dmstem1;	//sheath and stem 1 (kg/ha)
        internal double dmstem2;	//sheath and stem 2 (kg/ha)
        internal double dmstem3;	//sheath and stem 3 (kg/ha)
        internal double dmstem4;	//sheath and stem dead (kg/ha)
        internal double dmstol1;	//stolon 1 (kg/ha)
        internal double dmstol2;	//stolon 2 (kg/ha)
        internal double dmstol3;	//stolon 3 (kg/ha)
        internal double dmroot;	    //root (kg/ha)
        internal double dmlitter;	//Litter pool (kg/ha)

        public double dmrootInit;   // Intial value for dmroot
        public double dmlitterInit;
        public double dmtotalInit;
        public double dmgreenmin;   // minimum grenn dm
        public double Frgr;


        //CO2
        public double CO2PmaxScale;
        public double CO2NScale;
        public double CO2NMin;
        public double CO2NCurvature;

        //water
        //private double swuptake;
        //private double swdemandFrac;
        public double waterStressFactor;
        public double soilSatFactor;

        //Nc - N concentration    
        public double NcstemFr;   //stem Nc as % of leaf Nc    
        public double NcstolFr;   //stolon Nc as % of leaf Nc  
        public double NcrootFr;   //root Nc as % of leaf Nc  

        //current   
        internal double Ncleaf1;	//leaf 1  (critical N %)    
        internal double Ncleaf2;	//leaf 2 
        internal double Ncleaf3;	//leaf 3 
        internal double Ncleaf4;	//leaf dead 
        internal double Ncstem1;	//sheath and stem 1
        internal double Ncstem2;	//sheath and stem 2
        internal double Ncstem3;	//sheath and stem 3
        internal double Ncstem4;	//sheath and stem dead 
        internal double Ncstol1;	//stolon 1 
        internal double Ncstol2;	//stolon 2 
        internal double Ncstol3;	//stolon 3 
        internal double Ncroot;	    //root 
        internal double Nclitter;	//Litter pool 

        //Max, Min & Opt = critical N
        public double NcleafOpt;	//leaf   (critical N %)   
        internal double NcstemOpt;	//sheath and stem    
        internal double NcstolOpt;	//stolon     
        internal double NcrootOpt;	//root   
        public double NcleafMax;	//leaf  (critical N %)  
        internal double NcstemMax;	//sheath and stem    
        internal double NcstolMax;	//stolon   
        internal double NcrootMax;	//root    
        public double NcleafMin;
        internal double NcstemMin;
        internal double NcstolMin;
        internal double NcrootMin;
        public double MaxFix;   //N-fix fraction when no soil N available, read in later   
        public double MinFix;   //N-fix fraction when soil N sufficient 

        //N in each pool (calculated as dm * Nc)
        internal double Nleaf1 = 0;	//leaf 1 (kg/ha)
        internal double Nleaf2 = 0;	//leaf 2 (kg/ha)
        internal double Nleaf3 = 0;	//leaf 3 (kg/ha)
        internal double Nleaf4 = 0;	//leaf dead (kg/ha)
        internal double Nstem1 = 0;	//sheath and stem 1 (kg/ha)
        internal double Nstem2 = 0;	//sheath and stem 2 (kg/ha)
        internal double Nstem3 = 0;	//sheath and stem 3 (kg/ha)
        internal double Nstem4 = 0;	//sheath and stem dead (kg/ha)
        internal double Nstol1 = 0;	//stolon 1 (kg/ha)
        internal double Nstol2 = 0;	//stolon 2 (kg/ha)
        internal double Nstol3 = 0;	//stolon 3 (kg/ha)
        internal double Nroot = 0;	//root (kg/ha)
        internal double Nlitter = 0;	//Litter pool (kg/ha)

        //calculated 
        //DM
        internal double dmtotal;      //=dmgreen + dmdead
        internal double dmgreen;
        internal double dmdead;
        internal double dmleaf;
        internal double dmstem;
        internal double dmleaf_green;
        internal double dmstem_green;
        internal double dmstol_green;
        internal double dmstol;
        internal double dmshoot;

        internal double dmdefoliated;
        internal double Ndefoliated;
        internal double digestHerbage;
        internal double digestDefoliated;
        //LAI
        internal double greenLAI; //sum of 3 pools
        internal double deadLAI;  //pool dmleaf4
        internal double totalLAI;
        //N plant    
        internal double Nshoot;	//above-ground total N (kg/ha)
        internal double Nleaf;	//leaf N
        internal double Nstem;	//stem N
        internal double Ngreen;	//live N
        internal double Ndead;	//in standing dead (kg/ha)
        internal double Nstolon;	//stolon  

        //internal double NremobMax;  //maximum N remob of the day    
        internal double Nremob = 0;       //N remobiliesd N during senesing
        internal double Cremob = 0;
        internal double Nleaf3Remob = 0;
        internal double Nstem3Remob = 0;
        internal double Nstol3Remob = 0;
        internal double NrootRemob = 0;
        internal double remob2NewGrowth = 0;
        internal double newGrowthN = 0;    //N plant-soil 
        internal double Ndemand;      //N demand for new growth 
        internal double NdemandOpt;
        //internal double NdemandMax;   //luxury N demand for new growth      
        internal double Nfix;         //N fixed by legumes     

        //internal double soilNAvail;   //N available to this species
        internal double soilNdemand;  //N demand from soil (=Ndemand-Nremob-Nfixed)
        //internal double soilNdemandMax;   //N demand for luxury uptake   
        internal double soilNuptake;  //N uptake of the day

        //growth limiting factors
        internal double gfwater;  //from water stress
        internal double gftemp;   //from temperature
        internal double gfn;      //from N deficit
        internal double Ncfactor;
        //internal double fNavail2Max; //demand/Luxruy uptake

        //calculated, species delta 
        internal double dGrowthPot;    //daily growth potential 
        internal double dGrowthW;      //daily growth with water-deficit incorporated
        internal double dGrowth;       //daily growth
        internal double dGrowthRoot;   //daily root growth

        internal double dLitter;       //daily litter production
        internal double dNLitter;      //N in dLitter
        internal double dRootSen;      //daily root sennesce
        internal double dNrootSen;     //N in dRootSen

        internal double fShoot;	     //actual fraction of dGrowth to shoot
        //internal int dayCounter;
        //internal double sumGFW;

        internal double leafPref = 1;    //leaf preference    
        // internal double accumtotalnewG = 0;
        // internal double accumtotalnewN = 0;
        internal double IL1;
        internal double Pgross;
        internal double Resp_m;
        //internal double Resp_root;
        internal static double coverRF = 1;    //temp. for reduce IL considering other canopies

        //Species ------------------------------------------------------------
        public Species()
        {
            //Initialise parameters (for those not set from parameter readings)
            Nremob = 0.0;
            Cremob = 0;
            Nfix = 0.0;
            Ndemand = 0.0;
            soilNdemand = 0.0;
            soilNuptake = 0.0;
            dmdefoliated = 0.0;
            Ndefoliated = 0;
            digestHerbage = 0;
            digestDefoliated = 0;

        }

        public void InitValues()
        {
            pS = new DMPools();
            Nremob = 0.0;
            Cremob = 0;
            Nfix = 0.0;
            Ncfactor = 0.0;
            Ndemand = 0.0;
            soilNdemand = 0.0;
            soilNuptake = 0.0;
            dmdefoliated = 0.0;
            Ndefoliated = 0;
            digestHerbage = 0;
            digestDefoliated = 0;
            dLitter = 0.0;
            dNLitter = 0.0;
            dRootSen = 0.0;
            dNrootSen = 0.0;
            gfn = 0.0;
            gftemp = 0.0;
            gfwater = 0.0;
            phenoFactor = 1.0;
            intRadn = 0.0;
            intRadnFrac = 0.0;
            newGrowthN = 0.0;
            Pgross = 0.0;
            NdemandOpt = 0.0;
            remob2NewGrowth = 0.0;
            Resp_m = 0.0;
            NrootRemob = 0.0;
            IL1 = 0.0;

            if (isAnnual) //calulate days from Emg to Antheis        
                CalcDaysEmgToAnth();

            lightExtCoeff_ref = lightExtCoeff;

            leafPref = 1;
            if (isLegume) leafPref = 1.5;        //Init DM (is partitioned to different pools)

            dmtotal = dmtotalInit;
            dmroot = dmrootInit;
            dmlitter = dmlitterInit;

            if (dmtotal == 0.0) phenoStage = 0;
            else phenoStage = 1;

            if (!isLegume)
            {
                dmleaf1 = 0.15 * dmtotal;
                dmleaf2 = 0.25 * dmtotal;
                dmleaf3 = 0.25 * dmtotal;
                dmleaf4 = 0.05 * dmtotal;
                dmstem1 = 0.05 * dmtotal;
                dmstem2 = 0.10 * dmtotal;
                dmstem3 = 0.10 * dmtotal;
                dmstem4 = 0.05 * dmtotal;
                dmstol1 = dmstol2 = dmstol3 = 0;
            }
            else //legume
            {
                dmleaf1 = 0.20 * dmtotal;
                dmleaf2 = 0.25 * dmtotal;
                dmleaf3 = 0.25 * dmtotal;
                dmleaf4 = 0.00;
                dmstem1 = 0.02 * dmtotal;  //added small % of stem for legume
                dmstem2 = 0.04 * dmtotal;
                dmstem3 = 0.04 * dmtotal;
                dmstem4 = 0.00;
                dmstol1 = 0.06 * dmtotal;
                dmstol2 = 0.12 * dmtotal;
                dmstol3 = 0.12 * dmtotal;
            }
            dmroot = dmtotal / maxSRratio;

            //init N
            // double Fn = =SP[s].NCO2Effects() //Delay teh [co2] effect to calculating N demand.                      
            //0.01 is for conversion of % to fraction [i.e., 4% ->0.04]    
            NcstemOpt = NcleafOpt * NcstemFr; 	//stem 
            NcstolOpt = NcleafOpt * NcstolFr; 	//stolon  
            NcrootOpt = NcleafOpt * NcrootFr; 	//root 

            NcstemMax = NcleafMax * NcstemFr; //sheath and stem         
            NcstolMax = NcleafMax * NcstolFr;	//stolon         
            NcrootMax = NcleafMax * NcrootFr;	//root         

            NcstemMin = NcleafMin * NcstemFr;
            NcstolMin = NcleafMin * NcstolFr;
            NcrootMin = NcleafMin * NcrootFr;

            //init as optimum        
            Ncleaf1 = NcleafOpt;
            Ncleaf2 = NcleafOpt; //optimum now is the optimum of green leaf [N]     
            Ncleaf3 = NcleafOpt;
            Ncleaf4 = NcleafMin; //this could become much small depending on [N] in green tisssue       

            Ncstem1 = NcstemOpt; //stem [N] is 50% of the leaf [N]       
            Ncstem2 = NcstemOpt;
            Ncstem3 = NcstemOpt;
            Ncstem4 = NcstemMin;

            Ncstol1 = NcstolOpt;
            Ncstol2 = NcstolOpt;
            Ncstol3 = NcstolOpt;

            Ncroot = NcrootOpt;
            Nclitter = NcleafMin;  //init as same [N]

            //Init total N in each pool
            Nleaf1 = dmleaf1 * Ncleaf1; //convert % to fraction [i.e., 4% ->0.02]
            Nleaf2 = dmleaf2 * Ncleaf2;
            Nleaf3 = dmleaf3 * Ncleaf3;
            Nleaf4 = dmleaf4 * Ncleaf4;
            Nstem1 = dmstem1 * Ncstem1;
            Nstem2 = dmstem2 * Ncstem2;
            Nstem3 = dmstem3 * Ncstem3;
            Nstem4 = dmstem4 * Ncstem4;
            Nstol1 = dmstol1 * Ncstol1;
            Nstol2 = dmstol2 * Ncstol2;
            Nstol3 = dmstol3 * Ncstol3;
            Nroot = dmroot * Ncroot;
            Nlitter = dmlitter * Nclitter;

            //calculated, DM and LAI,  species-specific 
            updateAggregated();   // agregated properties, such as p_totalLAI            

            dGrowthPot = 0;       // daily growth potential
            dGrowthW = 0;          // daily growth actual
            dGrowth = 0;          // daily growth actual
            dGrowthRoot = 0;      // daily root growth
            fShoot = 1;	        // actual fraction of dGrowth allocated to shoot              

        }

        //Species -----------------------
        public void DailyRefresh()
        {
            dmdefoliated = 0;
            Ndefoliated = 0;
            digestHerbage = 0;
            digestDefoliated = 0;
        }

        //Species -----------------------------
        public double Remove(double amt)
        {
            //double pRest = 1 - (amt/dmtotal);
            double pRest = 1 - (amt / (dmstem + dmleaf));
            if (pRest < 0)
                return 0;

            dmdefoliated = amt;

            // Mar2011: If removing the specified 'amt' would result in a 'dmgreen' less than specified 'dmgreenmin',
            // then less green tissue (pool1-3 of leaf+stem) and more standing dead (pool4), will be removed 
            // This is especially necessaery for semi-arid grassland
            double pRest_green = pRest;
            double pRest_dead = pRest;
            if (pRest * (dmleaf_green + dmstem_green) + dmstol_green < dmgreenmin)
            {
                pRest_green = (dmgreenmin - dmstol_green) / (dmleaf_green + dmstem_green);
                double amt_dead_remove = amt - (1 - pRest_green) * (dmleaf_green + dmstem_green);
                pRest_dead = (dmstem4 + dmleaf4 - amt_dead_remove) / (dmstem4 + dmleaf4);
                if (pRest_dead < 0.0) pRest_dead = 0.0;   //this is impossible
            }

            //double standingDead =dmleaf4 + dmstem4;
            //double deadFrac = standingDead /(dmleaf+dmstem);
            //digestDefoliated = (1-deadFrac) * digestLive + deadFrac * digestDead;
            digestDefoliated = calcDigestability(); //because the defoliateion of different parts is in proportion to biomass 

            //Amount is removed from stem or leaf              
            /* 3) This part explicitly remove leaf/stem with consideration of preference
             * double pRemove = 0;        
            if (amt !=0 || dmleaf + dmstem > 0)            
                pRemove = amt / (dmleaf + dmstem); 
            else 
            {
                updateAggregated();          
                Ndefoliated = 0;
                dmdefoliated = 0;// amt;
                return 0;   
            }
 
            leafPref = 1.0;   
            if (isLegume)
                leafPref = 1.5;
            //DM remove                 
            double rm_dmleaf1 = Math.Min(dmleaf1, pRemove * leafPref * dmleaf1);        
            double rm_dmleaf2 = Math.Min(dmleaf2, pRemove * leafPref * dmleaf2);        
            double rm_dmleaf3 = Math.Min(dmleaf3, pRemove * leafPref * dmleaf3);        
            double rm_dmleaf4 = Math.Min(dmleaf4, pRemove * leafPref * dmleaf4);        
            double rm_dmleaf  = rm_dmleaf1 + rm_dmleaf2 + rm_dmleaf3 + rm_dmleaf4;                
        
            double rm_dmstem  = Math.Max(0, amt - rm_dmleaf);        
            double rm_dmstem1 = Math.Min(dmstem1, rm_dmstem * dmstem1/dmstem);        
            double rm_dmstem2 = Math.Min(dmstem2, rm_dmstem * dmstem2/dmstem);        
            double rm_dmstem3 = Math.Min(dmstem3, rm_dmstem * dmstem3/dmstem);        
            double rm_dmstem4 = Math.Min(dmstem4, rm_dmstem * dmstem4/dmstem);        
            dmleaf1 -= rm_dmleaf1;        
            dmleaf2 -= rm_dmleaf2;        
            dmleaf3 -= rm_dmleaf3;        
            dmleaf4 -= rm_dmleaf4;        
            dmstem1 -= rm_dmstem1;        
            dmstem2 -= rm_dmstem2;        
            dmstem3 -= rm_dmstem3;        
            dmstem4 -= rm_dmstem4;        
        
            //remove N             
            double preNshoot = Nshoot; 
            Nleaf1 -= rm_dmleaf1 * Ncleaf1;
            Nleaf2 -= rm_dmleaf2 * Ncleaf2;
            Nleaf3 -= rm_dmleaf3 * Ncleaf3;
            Nleaf4 -= rm_dmleaf4 * Ncleaf4;
            Nstem1 -= rm_dmstem1 * Ncstem1;
            Nstem2 -= rm_dmstem2 * Ncstem2;
            Nstem3 -= rm_dmstem3 * Ncstem3;
            Nstem4 -= rm_dmstem4 * Ncstem4;
            */

            /*
                    //2) Remove more standing dead and scenescent dm
                    //   will result in a slight higher yield and less litter, but 
                    //   affact little on the difference of litter formation between different rotational periods  
                    double pRemove = 1 - pRest;
                    double dm1 = dmleaf1 + dmstem1;
                    double dm2 = dmleaf2 + dmstem2;
                    double dm3 = dmleaf3 + dmstem3;        
                    double dm4 = dmleaf4 + dmstem4;

                    double dm1Remove = dm1 * pRemove;  //in proportion
                    double dm2Remove = dm2 * pRemove;
                    double dm3Remove = dm3 * pRemove;
                    double dm4Remove = dm4 * pRemove;

                    double dm4MoreR = 0.5 * (dm4 - dm4Remove);
                    double dm3MoreR = 0.25 * (dm3 - dm3Remove);
                    double dm2MoreR = 0;
                    double dm1MoreR = 0;
                    if (dm3MoreR + dm4MoreR  < dm1 - dm1Remove + dm2 - dm2Remove )  
                    {
                        dm2MoreR = - (dm3MoreR+ dm4MoreR) * (dm2/(dm1+dm2));
                        dm1MoreR = - (dm3MoreR+ dm4MoreR) * (dm1/(dm1+dm2));

                        dm1Remove += dm1MoreR;  //in proportion
                        dm2Remove += dm2MoreR;
                        dm3Remove += dm3MoreR;
                        dm4Remove += dm4MoreR;
                    }

                    double pRest1 = 0;
                    double pRest2 = 0;
                    double pRest3 = 0;
                    double pRest4 = 0;
                    if (dm1 > 0) pRest1 = (dm1 - dm1Remove) / dm1; 
                    if (dm2 > 0) pRest2 = (dm2 - dm2Remove) / dm2;
                    if (dm3 > 0) pRest3 = (dm3 - dm3Remove) / dm3;
                    if (dm4 > 0) pRest4 = (dm4 - dm4Remove) / dm4;
        
                    dmleaf1 = pRest1 * dmleaf1;
                    dmleaf2 = pRest2 * dmleaf2;
                    dmleaf3 = pRest3 * dmleaf3;
                    dmleaf4 = pRest4 * dmleaf4;
                    dmstem1 = pRest1 * dmstem1;
                    dmstem2 = pRest2 * dmstem2;
                    dmstem3 = pRest3 * dmstem3;
                    dmstem4 = pRest4 * dmstem4;

                    double preNshoot = Nshoot; //before remove
                    //N remove 
                    Nleaf1 = pRest1 * Nleaf1;
                    Nleaf2 = pRest2 * Nleaf2;
                    Nleaf3 = pRest3 * Nleaf3;
                    Nleaf4 = pRest4 * Nleaf4;
                    Nstem1 = pRest1 * Nstem1;
                    Nstem2 = pRest2 * Nstem2;
                    Nstem3 = pRest3 * Nstem3;
                    Nstem4 = pRest4 * Nstem4;
            */

            // 1)Removing without preference   Mar2011: using different pRest for maintain a 'dmgreenmin'      
            dmleaf1 = pRest_green * dmleaf1;
            dmleaf2 = pRest_green * dmleaf2;
            dmleaf3 = pRest_green * dmleaf3;
            dmleaf4 = pRest_dead * dmleaf4;
            dmstem1 = pRest_green * dmstem1;
            dmstem2 = pRest_green * dmstem2;
            dmstem3 = pRest_green * dmstem3;
            dmstem4 = pRest_dead * dmstem4;
            //No stolon remove

            double preNshoot = Nshoot; //before remove
            //N remove 
            Nleaf1 = pRest_green * Nleaf1;
            Nleaf2 = pRest_green * Nleaf2;
            Nleaf3 = pRest_green * Nleaf3;
            Nleaf4 = pRest_dead * Nleaf4;
            Nstem1 = pRest_green * Nstem1;
            Nstem2 = pRest_green * Nstem2;
            Nstem3 = pRest_green * Nstem3;
            Nstem4 = pRest_dead * Nstem4;

            //Nremob also been emoved proportionally (not sensiive?)
            double preNremob = Nremob;
            Nremob = pRest * Nremob;
            double NremobRemove = preNremob - Nremob;

            updateAggregated();

            double removeN = preNshoot - Nshoot;
            Ndefoliated = Math.Max(0.0, removeN);

            return removeN;
        }

        //Species ------------------------------------------------------------
        public double calcDigestability()
        {
            if ((dmleaf + dmstem) <= 0)
            {
                digestHerbage = 0;
                return digestHerbage;
            }

            double fSugar = 0.5 * dGrowth / dmgreen;    //dmgreen: live shoots including leaves/stems/stolons
            double CNp = 3.5;                           //CN ratio of protein
            double CNw = 100;                           //CN ratio of cell wall    

            //Live 
            double digestabilityLive = 0;
            if (dmgreen > 0 & Ngreen > 0)
            {
                double CNlive = 0.4 * dmgreen / Ngreen;                                //CN ratio of live shoots            
                double fProteinLive = (CNw / CNlive - (1 - fSugar)) / (CNw / CNp - 1); //Fraction of protein in liveing shoots
                double fWallLive = 1 - fSugar - fProteinLive;                          //Fraction of cell wall in living shoots
                digestabilityLive = fSugar + fProteinLive + digestLive * fWallLive;
            }

            //Dead
            double digestabilityDead = 0;
            double standingDead = dmleaf4 + dmstem4;        //Not including stolons here for stolons are not grazed       
            if (standingDead > 0 && Ndead > 0)
            {
                double CNdead = 0.4 * dmdead / Ndead;                       //CN ratio of standing dead;
                double fProteinDead = (CNw / CNdead - 1) / (CNw / CNp - 1); //Fraction of protein in standing dead         
                double fWallDead = 1 - fProteinDead;                        //Fraction of cell wall in standing dead       
                digestabilityDead = fProteinDead + digestDead * fWallDead;
            }

            double deadFrac = standingDead / (dmleaf + dmstem);
            digestHerbage = (1 - deadFrac) * digestabilityLive + deadFrac * digestabilityDead;

            return digestHerbage;
        }
        //Species ------------------------------------------------------------
        public double updateAggregated()   //update DM, N & LAI
        {
            //DM
            dmleaf = dmleaf1 + dmleaf2 + dmleaf3 + dmleaf4;
            dmstem = dmstem1 + dmstem2 + dmstem3 + dmstem4;
            dmstol = dmstol1 + dmstol2 + dmstol3;
            dmshoot = dmleaf + dmstem + dmstol;

            dmleaf_green = dmleaf1 + dmleaf2 + dmleaf3;
            dmstem_green = dmstem1 + dmstem2 + dmstem3;
            dmstol_green = dmstol1 + dmstol2 + dmstol3;

            dmgreen = dmleaf1 + dmleaf2 + dmleaf3
                    + dmstem1 + dmstem2 + dmstem3
                    + dmstol1 + dmstol2 + dmstol3;

            dmdead = dmleaf4 + dmstem4;
            dmtotal = dmgreen + dmdead;

            //N
            Nleaf = Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4;
            Nstem = Nstem1 + Nstem2 + Nstem3 + Nstem4;// +Nremob;  //separately handled, not reported in stem 
            Nstolon = Nstol1 + Nstol2 + Nstol3;

            Nshoot = Nleaf + Nstem + Nstolon;   //shoot

            Ngreen = Nleaf1 + Nleaf2 + Nleaf3
                    + Nstem1 + Nstem2 + Nstem3
                    + Nstol1 + Nstol2 + Nstol3;
            Ndead = Nleaf4 + Nstem4;


            //LAI                                   //0.0001: kg/ha->kg/m2; SLA: m2/kg
            greenLAI = 0.0001 * dmleaf_green * SLA + 0.0001 * dmstol * 0.3 * SLA;   //insensitive? assuming Mass2GLA = 0.3*SLA 

            // Resilence after unfovoured conditions
            // Consider cover will be bigger for the same amount of DM when DM is low due to 
            // - light extinction coefficient will be bigger - plant leaves will be more plate than in dense high swards 
            // - more parts will turn green for photosysntheses? 
            // - quick response of plant shoots to fovoured conditions after release of stress
            if (!isLegume && dmgreen < 1000)
            {
                greenLAI += 0.0001 * dmstem_green * SLA * Math.Sqrt((1000 - dmgreen) / 1000);
            }

            deadLAI = 0.0001 * dmleaf4 * SLA;
            totalLAI = greenLAI + deadLAI;

            return totalLAI;

        }

        //Species --------------------------------------------
        public double rootGrowth()
        {
            if (isAnnual)
            {
                rootDepth = 50 + (maxRootDepth - 50) * daysfromEmergence / daysEmgToAnth;
                //considering root distribution change, here?
            }
            return rootDepth;  // no root depth change for pereniel pasture
        }

        //Species -------------------------------------------------
        public int CalcDaysEmgToAnth()
        {
            daysEmgToAnth = 0;
            int numbMonths = monAnth - monEmerg;  //emergence & anthesis in the same calendar year: monEmerg < monAnth 
            if (monEmerg >= monAnth)              //...across the calendar year        
                numbMonths += 12;

            daysEmgToAnth = (int)(30.5 * numbMonths + (dayAnth - dayEmerg));

            return daysEmgToAnth;
        }

        //Species -------------------------------------------------------------
        public int Phenology()
        {
            const double DDSEmergence = 150;   // to be an input parameter
            double meanT = 0.5 * (MetData.maxt + MetData.mint);

            if (bSown && phenoStage == 0)            //  before emergence
            {
                DDSfromSowing += meanT;
                if (DDSfromSowing > DDSEmergence)
                {
                    phenoStage = 1;
                    DDSfromSowing = 0;
                    SetEmergentState();      //Initial states at 50% emergence 

                }
            }

            /*TO DO later
             *      else if (phenoStage == 1)       //  Vege
                    {
                        DDSfromEmergence += meanT;
                        if (DDSfromEmergence > 1000)
                            phenoStage = 2;
                    }
                    else if (phenoStage == 2)       //  Reprod
                    {
                        DDSfromAnthesis += meanT;
                        if (DDSfromEmergence > 1000)
                            phenoStage = 3;
                    }
                    else if (phenoStage == 4)       //  Post_reprod
                    {
                        DDSfromAnthesis += meanT;
                        if (DDSfromEmergence > 1000)
                            phenoStage = 1;         // return to vege
                    }
            */
            return phenoStage;
        }

        //Species -------------------------------------------------------------
        private double SetEmergentState()
        {
            dmleaf1 = 10;   //(kg/ha)
            dmleaf2 = 20;
            dmleaf3 = 20;
            dmleaf4 = 0;
            if (!isLegume)
            {
                dmstem1 = 5;
                dmstem2 = 10;
                dmstem3 = 0;
                dmstem4 = 0;
                dmroot = 50;
            }
            else
            {
                dmstol1 = 5;
                dmstol2 = 10;
                dmstol3 = 0;
                dmroot = 25;
            }
            dmlitter = 0;

            //Init total N in each pool
            Nleaf1 = dmleaf1 * Ncleaf1;
            Nleaf2 = dmleaf2 * Ncleaf2;
            Nleaf3 = dmleaf3 * Ncleaf3;
            Nleaf4 = dmleaf4 * Ncleaf4;
            Nstem1 = dmstem1 * Ncstem1;
            Nstem2 = dmstem2 * Ncstem2;
            Nstem3 = dmstem3 * Ncstem3;
            Nstem4 = dmstem4 * Ncstem4;
            Nstol1 = dmstol1 * Ncstol1;
            Nstol2 = dmstol2 * Ncstol2;
            Nstol3 = dmstol3 * Ncstol3;
            Nroot = dmroot * Ncroot;
            Nlitter = dmlitter * Nclitter;

            //calculated, DM and LAI,  species-specific 
            updateAggregated();   // agregated properties, such as p_totalLAI            

            dGrowthPot = 0;       // daily growth potential
            dGrowthW = 0;         // daily growth considering only water deficit 
            dGrowth = 0;          // daily growth actual
            dGrowthRoot = 0;      // daily root growth
            fShoot = 1;	          // actual fraction of dGrowth allocated to shoot 

            return dmtotal;       // total shoot mass

        }

        //Species -------------------------------------------------------------
        public double DailyGrowthPot()   //GrassGro routine. Not used since Aug 09. FYLi    
        {
            //*** This process is finally not used, so not updated. Need reexmie it if it is used! FLi Dec 2010)  

            //phebology
            if (isAnnual)
            {
                if (month == monEmerg && day_of_month == dayEmerg)
                    phenoStage = 1;     //vegetative stage                            
                else if (month == monAnth && day_of_month == dayAnth)
                    phenoStage = 2;     //reproductive                

                if (phenoStage == 0)    //before emergence
                {
                    dGrowthPot = 0;
                    return dGrowthPot;  //no growth 
                }

                if (phenoStage == 1)        //vege  
                {
                    daysfromEmergence++;
                }
                else if (phenoStage == 2)   //repro
                {
                    daysfromAnthesis++;
                    if (daysfromAnthesis >= daysToMature)
                    {
                        phenoStage = 0;
                        daysfromEmergence = 0;
                        daysfromAnthesis = 0;
                        dGrowthPot = 0;
                        return dGrowthPot;      // no growth after mature
                    }
                }
            }

            //RUE //GrassGro routine                                          
            //     radiation use efficiency can be either entered as:
            //     a) defined at a reference solar radiation intensity of 1.67 MJ m-2 h-1
            //     b) defined in terms of maximum assimilation
            //
            //     Typically
            //     a) grxki3 units are 8.0 phalaris, 8.5 subterranean clover  (?refRUE)
            //     b) 280 kg/ha/day maximum assimilation at 20 MJ m-2 radiation and 12 hr daylength
            //
            //     Therefore redefine radiation use efficiency when case (b) entered
            //     as defined by equation 27 to convert kg/ha/day to g/MJ.      

            //If the reference maximum assimilation is entered, convert it into rue (g/MJ)         
            //  has the input, using GrassGro approach for potential growth
            //  that is, maxAssimiRate == 'maximum assimilation: kg/ha/day' is entered

            double refRadn = 20;
            double refDayL = 12;
            double refRI = refRadn / refDayL;   // 1.67 = 20MJ/12hours: reference solar radition flux intensity (IFstd)
            double riEffect = 0.6;              // Effects of refRI on RUE,    phxki4 = 0.6; 

            //maxAssimiRate;                    // kg/ha/day  phxki3 = 330.0; (SGS: 330 prereneial) or 240 annual) 
            double refRUE;
            if (maxAssimiRate > 25.0)
            {
                refRUE = 0.1 * maxAssimiRate / refRadn;   //0.1 -- for converting to g/m^2 from kg/ha 
            }
            else
            {
                refRUE = maxAssimiRate;
            }

            //To consider: should use MetData.radn or SP.intRadn - depending on the methods in seting up simulation
            double RI = ((refRI + riEffect) / ((MetData.radn / dayLength) + riEffect));
            rue = refRUE * RI; // ((refRI + riEffect) / ((MetData.radn / dayLength) + riEffect));  

            //consider a growth efficiency coefficient (as grgeff or sgseff in GRAZPLAN)
            // rue *= growthEfficiency; // Note: 'growthEfficiecy' was 1 in this routine
            //        It is now used as '(Pgross - Prespi)/Pgross' in DailyEMGrowthPot() 
            //        FYL - Sep 09  

            //This assume no difference in terms of layer distribution   
            dGrowthPot = rue * intRadn;                     //intRadn is the Radn intecepted by this species 

            double tempStress = HeatEffect() * ColdEffect();            //only one of teh effect is < 1
            dGrowthPot *= Math.Min(tempStress * GFTemperature(), Frgr);    //Temperature effects is considered in potential growth   
            //Frgr too, because it is considered for calculating PET     
            dGrowthPot *= 10;                                           //=> kg/ha


            dGrowthPot *= PCO2Effects();                    //multiply the CO2 effects


            /* //if RUE is not the net, but gross phs, then maizntenance (respiration) is calculated 
            bool bGrossPHS = false;
            if (bGrossPHS) 
            { 
                //Calculate maintenance respiration (kg C m2/day)   
                double Mcoeff = 0.03;   //grcowm = maintenenance coefficient at 20C (default=0.03/day            
                double Nlive = 0.1;     //fnlive = nitrogen content of the live shoot and root plant material
                double Nref  = 0.1;     //fnreff = reference palnt nitrogen content (0.1 Kg N/Kg C)
                double rootF = 1.0;     //xnetam = ????
                                        //fmtm20 = temperature response for for maintenence respiration
                                        //rm     = grcowm*fmtm20*(fnlive/fnreff)*(sumshoot+(xnetam*dmsrot(ipastr)))

                double Tresp = 0.5*(MetData.maxt+MetData.mint)/20;  //temperature response for maintenance with 20 C as reference 
                double Nresp = Nlive/Nref;                          //N response for maintenance with Nref as reference
                double maint = Mcoeff * (dmgreen + rootF*dmroot)* Tresp * Nresp;
            
                //Calculate scenescent carbonhydrates 
                double remob = 0.0;
                //This should be remobilised reserve from root? If so,

                double GLFcrit = 0.2;  // threthold growth-limiting factor for remobilisation for underground reserves
                double rebRate = 0.02; // relative rateDead2Litter of remobilisation (per day)            
                if (GLFcrit < Math.Min(gfwater, gftemp))
                {
                    remob = rebRate * dmroot;
                }
                //Calculate rate of synethesis of new structural material
                dGrowthPot = dGrowthPot + remob - maint;                         
            }
            */

            // phenologically related reduction of annual species (from IJ)
            if (isAnnual)
            {
                double rFactor = 1;  // reduction factor of annual species 
                if (phenoStage == 1 && daysfromEmergence < 60)  //decline at the begining due to seed bank effects ???
                {
                    rFactor = 0.5 + 0.5 * daysfromEmergence / 60;
                }
                else if (phenoStage == 2)                       //decline of photosynthesis when approaching maturity        
                {
                    rFactor = 1.0 - (double)daysfromAnthesis / daysToMature;
                }
                dGrowthPot *= rFactor;
            }

            return dGrowthPot;
        }

        //Species ----------------------------------------------------------    
        public double DailyEMGrowthPot()
        {
            //annual phebology
            if (isAnnual)
            {
                bool moreGrowth = annualPhenology();
                if (!moreGrowth)
                    return dGrowthPot = 0;
            }

            //
            if (phenoStage == 0 || greenLAI == 0) //Before gemination
                return dGrowthPot = 0;

            const double alfa = 0.01;                 //P_al, leaf gross photosynthesis rate: mg co2/J
            const double theta = 0.8;                 //P_th, curvature parameter: J /kg/s

            //following parometers are from input (.xml)
            double maint_coeff = 0.01 * maintRespiration;  //reference maintnance respiration as % of live weight
            double Yg = growthEfficiency;                  //default =0.75; //Efficiency of plant photosynthesis growth)
            //Pm is an input

            //Add temp effects to Pm
            double Tmean = (MetData.maxt + MetData.mint) / 2;
            double Tday = Tmean + 0.5 * (MetData.maxt - Tmean);

            double Pm_mean = Pm * GFTemperature(Tmean) * PCO2Effects() * PmxNeffect();  //Dec10: added CO2 & [N]effects
            double Pm_day = Pm * GFTemperature(Tday) * PCO2Effects() * PmxNeffect();    //Dec10: added CO2 & [N]effects

            double tau = 3600 * dayLength;                //conversion of hour to seconds //  tau := 0.0036 * hours ;            
            //IL_1 := k_light * 1.33333 * 0.5 * light/tau;  // flat bit - goes with Pm_day
            //FYL: k_light*light/tau = Irridance intercepted by 1 LAI on 1 m^2 ground: J/(m^2 ground)/s

            //IL:  irridance on the top of canopy, with unit: J/(m^2 LAI)/(m^2 ground)/second.  PAR = 0.5*Radn; 1 MJ = 10^6 J      

            //IL1 = 1.33333 * 0.5 * PIntRadn / (PCoverGreen*coverRF) * PLightExtCoeff * 1000000 / tau;
            IL1 = 1.33333 * 0.5 * PIntRadn * PLightExtCoeff * 1000000 / tau;                    //ignore putting 2 species seperately for now
            double IL2 = IL1 / 2;                      //IL for early & late period of a day 

            //Photosynthesis per LAI under full irridance at the top of the canopy
            double Pl1 = (0.5 / theta) * (alfa * IL1 + Pm_day
                         - Math.Sqrt((alfa * IL1 + Pm_day) * (alfa * IL1 + Pm_day) - 4 * theta * alfa * IL1 * Pm_day));
            double Pl2 = (0.5 / theta) * (alfa * IL2 + Pm_mean
                         - Math.Sqrt((alfa * IL2 + Pm_mean) * (alfa * IL2 + Pm_mean) - 4 * theta * alfa * IL2 * Pm_mean));

            //Upscaling from 'per LAI' to 'per ground area'                                                 
            double carbon_m2 = 0.000001 * CD2C * 0.5 * tau * (Pl1 + Pl2) * PCoverGreen * intRadnFrac / lightExtCoeff;
            //tau: per second => per day; 0.000001: mg/m^2=> kg/m^2_ground/day; 
            //only 'intRadnFrac' portion for this species; 
            //using lightExeCoeff (species, result in a lower yield with ample W & N)

            carbon_m2 *= 1;// coverRF;                       //coverRF == 1 when puting species together

            Pgross = 10000 * carbon_m2;                 //10000: 'kg/m^2' =>'kg/ha'        

            //Add extreme temperature effects; 
            Pgross *= HeatEffect() * ColdEffect();      // in practice only one temp stress factor is < 1

            //Maintenance respiration
            double Teffect = 0;                         //Add temperature effects on respi 
            if (Tmean > growthTmin)
            {
                if (Tmean < growthTopt)
                {
                    Teffect = GFTemperature(Tmean);
                    //Teffect = Math.Pow(Teffect, 1.5);                              
                }
                else
                {
                    //Teffect = 1;
                    Teffect = Tmean / growthTopt;        // Using growthTopt (e.g., 20 C) as reference, and set maximum
                    if (Teffect > 1.25) Teffect = 1.25;  // Resp_m 
                }   //The extreme high temperatue (heat) effect is added separately
            }


            double YgFactor = 1.0;
            //Ignore [N] effects in potential growth here 
            Resp_m = maint_coeff * Teffect * PmxNeffect() * (dmgreen + dmroot) * DM2C;       //converting DM to C    (kg/ha)
            //Dec10: added [N] effects here

            // ** C budget is not explicitly done here as in EM
            Cremob = 0;                     // Nremob* C2N_protein;    // No carbon budget here  
            // Nu_remob[elC] := C2N_protein * Nu_remob[elN];                                   
            // need to substract CRemob from dm rutnover?    
            dGrowthPot = Yg * YgFactor * (Pgross + Cremob - Resp_m);     //Net potential growth (C) of the day (excluding growth respiration)
            dGrowthPot = Math.Max(0.0, dGrowthPot);
            //double Resp_g = Pgross * (1 - Yg) / Yg;  
            //dGrowthPot *= PCO2Effects();                      //multiply the CO2 effects. Dec10: This ihas been now incoporated in Pm/leaf area above

            //convert C to DM  
            dGrowthPot *= C2DM;

            // phenologically related reduction of annual species (from IJ)
            if (isAnnual)
                dGrowthPot = annualSpeciesReduction();

            return dGrowthPot;

        }

        //Species --------------------------------------------------------------
        // phenology of anuual species
        public bool annualPhenology()
        {
            if (month == monEmerg && day_of_month == dayEmerg)
                phenoStage = 1;         //vegetative stage                            
            else if (month == monAnth && day_of_month == dayAnth)
                phenoStage = 2;         //reproductive                

            if (phenoStage == 0)        //before emergence
            {
                dGrowthPot = 0;
                return false;           //no growth 
            }

            if (phenoStage == 1)        //vege  
            {
                daysfromEmergence++;
                return true;
            }

            if (phenoStage == 2)
            {
                daysfromAnthesis++;
                if (daysfromAnthesis >= daysToMature)
                {
                    phenoStage = 0;
                    daysfromEmergence = 0;
                    daysfromAnthesis = 0;
                    dGrowthPot = 0;
                    return false;       // Flag no growth after mature
                }
                return true;
            }
            return true;
        }


        //Species --------------------------------------------------------------
        // phenologically related reduction of annual species
        public double annualSpeciesReduction()
        {
            double rFactor = 1;  // reduction factor of annual species 
            if (phenoStage == 1 && daysfromEmergence < 60)  //decline at the begining due to seed bank effects ???
            {
                rFactor = 0.5 + 0.5 * daysfromEmergence / 60;
            }
            else if (phenoStage == 2)                       //decline of photosynthesis when approaching maturity        
            {
                rFactor = 1.0 - (double)daysfromAnthesis / daysToMature;
            }
            dGrowthPot *= rFactor;
            return dGrowthPot;
        }



        //Species --------------------------------------------------------------
        //Plant photosynthesis increase to eleveated [CO2]
        public double PCO2Effects()
        {
            if (Math.Abs(CO2 - CO2ambient) < 0.5)
                return 1.0;

            double Kp = CO2PmaxScale; //700; for C3 plants & 150 for C4
            if (photoPath == 4)     //C4 plants
                Kp = 150;

            double Fp = (CO2 / (Kp + CO2)) * ((CO2ambient + Kp) / CO2ambient);
            return Fp;
        }

        //Species --------------------------------------------------------------
        // Plant nitrogen [N] decline to elevated [CO2]
        public double NCO2Effects()
        {
            if (Math.Abs(CO2 - CO2ambient) < 0.5)
                return 1.0;

            double L = CO2NMin;         // 0.7 - lamda: same for C3 & C4 plants
            double Kn = CO2NScale;      // 600 - ppm,   when CO2 = 600ppm, Fn = 0.5*(1+lamda);
            double Qn = CO2NCurvature;  //2 - curveture factor

            double interm = Math.Pow((Kn - CO2ambient), Qn);
            double Fn = (L + (1 - L) * interm / (interm + Math.Pow((CO2 - CO2ambient), Qn)));
            return Fn;
        }

        //Species --------------------------------------------------------------
        //Canopy conductiance decline to elevated [CO2]
        public double ConductanceCO2Effects()
        {
            if (Math.Abs(CO2 - CO2ambient) < 0.5)
                return 1.0;
            //Hard coded here, not used, should go to Micromet!
            double Gmin = 0.2;      //Fc = Gmin when CO2->unlimited
            double Gmax = 1.25;     //Fc = Gmax when CO2 = 0;
            double beta = 2.5;      //curvature factor,      

            double Fc = Gmin + (Gmax - Gmin) * (1 - Gmin) * Math.Pow(CO2ambient, beta) /
                               ((Gmax - 1) * Math.Pow(CO2, beta) + (1 - Gmin) * Math.Pow(CO2ambient, beta));
            return Fc;
        }

        //Species ---------------------------------------------------------------
        //Calculate species N demand for potential growth (soilNdemand); 
        public double CalcNdemand()
        {
            fShoot = NewGrowthToShoot();
            double fL = UpdatefLeaf(); //to consider more dm to leaf when DM is lower?

            double toRoot = dGrowthW * (1.0 - fShoot);
            double toStol = dGrowthW * fShoot * fStolon;
            double toLeaf = dGrowthW * fShoot * (1.0 - fStolon) * fLeaf;
            double toStem = dGrowthW * fShoot * (1.0 - fStolon) * (1.0 - fLeaf);

            //N demand for new growth (kg/ha)
            NdemandOpt = (toRoot * Ncroot + toStol * Ncstol1
                      + toLeaf * Ncleaf1 + toStem * Ncstem1);
            NdemandOpt *= NCO2Effects();    //reduce the demand under elevated [co2], 
            //this will reduce the N stress under N limitation for the same soilN

            //N demand for new growth assuming luxury uptake to max [N]
            Ndemand = (toRoot * NcrootMax + toStol * NcstolMax
               + toLeaf * NcleafMax + toStem * NcstemMax);
            //Ndemand *= NCO2Effects();       //luxary uptake not reduce

            //even with sufficient soil N available
            if (isLegume)
                Nfix = MinFix * Ndemand;

            return Nfix;
        }


        //------------------------------------------
        public double UpdatefLeaf()
        {
            //temporary, need to do as interpolatiopon set  
            double fL = 1.0;   //fraction of shoot goes to leaf
            if (isLegume)
            {
                if (dmgreen > 0 && (dmstol / dmgreen) > fStolon)
                    fL = 1.0;
                else if (Pdmshoot < 2000)
                    fL = fLeaf + (1 - fLeaf) * Pdmshoot / 2000;
                else
                    fL = fLeaf;
            }
            else //grasses
            {
                if (Pdmshoot < 2000)
                    fL = fLeaf + (1 - fLeaf) * Pdmshoot / 2000;
                else
                    fL = fLeaf;
            }
            return fL;
        }

        //Species -------------------------------------------------------------
        public double DailyGrowthW()
        {
            Ncfactor = PmxNeffect();

            // NcFactor were addeded in Pm and Resp_m, Dec 10
            //  dGrowthW = dGrowthPot * Math.Min(gfwater, Ncfactor);
            dGrowthW = dGrowthPot * Math.Pow(gfwater, waterStressFactor);

            /*if (dGrowthPot > 0)
            {
                Console.Out.WriteLine(" growthPot: " + dGrowthPot); 
                Console.Out.WriteLine(" gfwater: " + gfwater);
                Console.Out.WriteLine(" WstressW: " + waterStressFactor);
                Console.Out.WriteLine(" growthW: " + dGrowthW);
           
            }*/
            return dGrowthW;
        }

        //Species -------------------------------------------------------------
        public double DailyGrowthAct()
        {
            double gfnit = 0.0;
            if (isLegume)
                gfnit = gfn;                           //legume no dilution, but reducing more DM (therefore LAI) 
            else
                gfnit = Math.Pow(gfn, NdilutCoeff);    // more DM growth than N limited, due to dilution (typically NdilutCoeff = 0.5) 

            dGrowth = dGrowthW * Math.Min(gfnit, Frgr);
            return dGrowth;

            //RCichota, Jan/2014: updated the function, added account for Frgr
        }

        //Species -------------------------------------------------------------
        public double PmxNeffect()
        {
            double Fn = NCO2Effects();

            double Nleaf_green = 0;
            double effect = 1.0;
            if (!isAnnual)  //  &&and FVegPhase and ( VegDay < 10 ) ) then  // need this or it never gets going 
            {
                Nleaf_green = Nleaf1 + Nleaf2 + Nleaf3;
                if (dmleaf_green > 0)
                {
                    double Ncleaf_green = Nleaf_green / dmleaf_green;
                    if (Ncleaf_green < NcleafOpt * Fn)     //Fn
                    {
                        if (Ncleaf_green > NcleafMin)
                        {
                            //effect = Math.Min(1.0, Ncleaf_green / NcleafOpt*Fn);
                            effect = Math.Min(1.0, (Ncleaf_green - NcleafMin) / (NcleafOpt * Fn - NcleafMin));
                        }
                        else
                        {
                            effect = 0;
                        }
                    }
                }
            }
            return effect;
        }

        //Species -------------------------------------------------------------  
        public double NFixCost()
        {
            double costF = 1.0;    //  redcuiton fraction of net prodcution as cost of N-fixining     
            if (!isLegume || Nfix == 0 || Ndemand == 0)      //  happens when plant has no growth 
            { return costF; }

            double actFix = Nfix / Ndemand;
            costF = 1 - 0.24 * (actFix - MinFix) / (MaxFix - MinFix);
            if (costF < 0.76)
                costF = 0.76;
            return costF;
        }



        //Species -------------------------------------------------------------    
        public double PartitionTurnover()
        {
            double GFT = GFTemperature();       // Temperature response                                    

            //Leaf appearance rate is modified by temp & water stress
            double rateLeaf = leafRate * GFT * (Math.Pow(gfwater, 0.33333));  //why input is 3
            if (rateLeaf < 0.0) rateLeaf = 0.0;
            if (rateLeaf > 1.0) rateLeaf = 1.0;

            if (dGrowth > 0.0)                  // if no net growth, then skip "partition" part
            {
                //Not re-calculate fShoot for avoiding N-inbalance

                //New growth is allocated to the 1st pools
                //fLeaf & fStolon: fixed partition to leaf & stolon.
                //Fractions [eq.4.13]           
                double toRoot = 1.0 - fShoot;
                double toStol = fShoot * fStolon;
                double toLeaf = fShoot * (1.0 - fStolon) * fLeaf;
                double toStem = fShoot * (1.0 - fStolon) * (1.0 - fLeaf);

                //checking
                double ToAll = toLeaf + toStem + toStol + toRoot;
                if (Math.Abs(ToAll - 1.0) > 0.01)
                { /*Console.WriteLine("checking partitioning fractions");*/ }

                //Assign the partitioned growth to the 1st tissue pools
                double newLeaf1 = toLeaf * dGrowth;
                double newStem1 = toStem * dGrowth;
                double newStol1 = toStol * dGrowth;
                double newRoot = toRoot * dGrowth;

                double totalnewG = newLeaf1 + newStem1 + newStol1 + newRoot;
                //  accumtotalnewG  +=totalnewG;
                //DM
                dmleaf1 = pS.dmleaf1 + newLeaf1;
                dmstem1 = pS.dmstem1 + newStem1;
                dmstol1 = pS.dmstol1 + newStol1;
                dmroot = pS.dmroot + newRoot;

                //partitiing N based on not only the DM, but also [N] in plant parts
                double sum = toLeaf * NcleafMax + toStem * NcstemMax + toStol * NcstolMax + toRoot * NcrootMax;
                double toLeafN = toLeaf * NcleafMax / sum;
                double toStemN = toStem * NcstemMax / sum;
                double toStolN = toStol * NcstolMax / sum;
                double toRootN = toRoot * NcrootMax / sum;

                Nleaf1 += toLeafN * newGrowthN;
                Nstem1 += toStemN * newGrowthN;
                Nstol1 += toStolN * newGrowthN;
                Nroot += toRootN * newGrowthN;

                // accumtotalnewN += newGrowthN;

            }  //end of "partition" block

            //**Tussue turnover among the 12 standing biomass pools
            //The rates are affected by water and temperature factor 
            double gftt = GFTempTissue();
            double gfwt = GFWaterTissue();

            double gama = gftt * gfwt * rateLive2Dead;
            double gamas = gama;                                    //for stolon of legumes 
            //double gamad = gftt * gfwt * rateDead2Litter;
            double SR = 0;  //stocking rate affacting transfer of dead to little (default as 0 for now)
            double gamad = rateDead2Litter * Math.Pow(gfwater, 3) * digestDead / 0.4 + stockParameter * SR;

            double gamar = gftt * (2 - gfwater) * rateRootSen;  //gfwt * rateRootSen; 


            if (gama == 0.0) //if gama ==0 due to gftt or gfwt, then skip "turnover" part
            {
                //no new little or root senensing
                dLitter = 0;
                dNLitter = 0;
                dRootSen = 0;
                dNrootSen = 0;
                //Nremob = Nremob; //no change
                //Nroot = Nroot;   
            }
            else
            {
                if (isAnnual)
                {
                    if (phenoStage == 1)        //vege
                    {
                        double Kv = (double)daysfromEmergence / daysEmgToAnth;
                        gama *= Kv;
                        gamar *= Kv;
                    }
                    else if (phenoStage == 2)    //repro
                    {
                        double Kr = (double)daysfromAnthesis / daysToMature;
                        gama = 1 - (1 - gama) * (1 - Kr * Kr);
                    }
                }

                // get daily defoliation: Fd = fraction of defoliation
                double Fd = 0;                                  //TODO with animal module later
                if (pS.dmdefoliated != 0 && pS.dmshoot != 0)
                    Fd = pS.dmdefoliated / (pS.dmdefoliated + pS.dmshoot);

                //gamar = gamar + Fd * Fd * (1 - gamar);
                //**Nov 09: Decided not to reduce root mass mmediately in a high proportion according to defoliation,
                //**Gradual process is more reasonable, and this results in a very smmall difference in predicting prodution

                if (isLegume) gamas = gama + Fd * (1 - gama);   //increase stolon senescence              

                //if today's turnover will result in a dmgreen < dmgreen_minimum, then adjust the rate,                 
                //double dmgreenToBe = dmgreen + dGrowth - gamad * (pS.dmleaf4 + pS.dmstem4 + pS.dmstol3);
                //Possibly to skip this for annuals to allow them to die - phenololgy-related? 
                double dmgreenToBe = dmgreen + dGrowth - gama * (pS.dmleaf3 + pS.dmstem3 + pS.dmstol3);
                if (dmgreenToBe < dmgreenmin)
                {
                    double preDMgreen = pS.dmgreen;
                    if (gama > 0.0)
                    {
                        if (dmgreen + dGrowth < dmgreenmin)
                        {
                            gama = 0;
                            gamas = 0;
                            //  gamad = 0;
                            gamar = 0;
                        }
                        else
                        {
                            double gama_adj = (dmgreen + dGrowth - dmgreenmin) / (pS.dmleaf3 + pS.dmstem3 + pS.dmstol3);
                            gamar = gamar * gama_adj / gama;
                            gamad = gamad * gama_adj / gama;
                            gama = gama_adj;
                        }
                    }
                }
                if (dmroot < 0.5 * dmgreenmin)          //set a minimum root too
                    gamar = 0;

                //Do actual DM turnover
                dmleaf1 = dmleaf1 - 2 * gama * pS.dmleaf1;                //except dmleaf1, other pool dm* = pS.dm*
                dmleaf2 = dmleaf2 - gama * pS.dmleaf2 + 2 * gama * pS.dmleaf1;
                dmleaf3 = dmleaf3 - gama * pS.dmleaf3 + gama * pS.dmleaf2;
                dmleaf4 = dmleaf4 - gamad * pS.dmleaf4 + gama * pS.dmleaf3;

                dmstem1 = dmstem1 - 2 * gama * pS.dmstem1;
                dmstem2 = dmstem2 - gama * pS.dmstem2 + 2 * gama * pS.dmstem1;
                dmstem3 = dmstem3 - gama * pS.dmstem3 + gama * pS.dmstem2;
                dmstem4 = dmstem4 - gamad * pS.dmstem4 + gama * pS.dmstem3;

                dmstol1 = dmstol1 - 2 * gamas * pS.dmstol1;
                dmstol2 = dmstol2 - gamas * pS.dmstol2 + 2 * gamas * pS.dmstol1;
                dmstol3 = dmstol3 - gamas * pS.dmstol3 + gamas * pS.dmstol2;

                dRootSen = gamar * pS.dmroot;
                dmroot = dmroot - dRootSen;// -Resp_root; 

                //Previous: N (assuming that Ncdead = Ncleaf4, Ncstem4 or Nclitter):  Nc --[N]
                double Nleaf1to2 = Ncleaf1 * 2 * gama * pS.dmleaf1;
                double Nleaf2to3 = Ncleaf2 * gama * pS.dmleaf2;
                double Nleaf3to4 = Ncleaf4 * gama * pS.dmleaf3;         //Ncleaf = NcleafMin: [N] in naturally scenescend tissue
                double Nleaf3Remob = (Ncleaf3 - Ncleaf4) * gama * pS.dmleaf3;
                double Nleaf4toL = Ncleaf4 * gamad * pS.dmleaf4;        //to litter
                Nleaf1 = Nleaf1 - Nleaf1to2;
                Nleaf2 = Nleaf2 + Nleaf1to2 - Nleaf2to3;
                Nleaf3 = Nleaf3 + Nleaf2to3 - Nleaf3to4 - Nleaf3Remob;
                Nleaf4 = Nleaf4 + Nleaf3to4 - Nleaf4toL;

                if (dmleaf1 != 0) { Ncleaf1 = Nleaf1 / dmleaf1; }
                if (dmleaf2 != 0) { Ncleaf2 = Nleaf2 / dmleaf2; }
                if (dmleaf3 != 0) { Ncleaf3 = Nleaf3 / dmleaf3; }
                if (dmleaf4 != 0) { Ncleaf4 = Nleaf4 / dmleaf4; }

                double Nstem1to2 = Ncstem1 * 2 * gama * pS.dmstem1;
                double Nstem2to3 = Ncstem2 * gama * pS.dmstem2;
                double Nstem3to4 = Ncstem4 * gama * pS.dmstem3;
                double Nstem3Remob = (Ncstem3 - Ncstem4) * gama * pS.dmstem3;
                double Nstem4toL = Ncstem4 * gamad * pS.dmstem4;   //to litter

                Nstem1 = Nstem1 - Nstem1to2;
                Nstem2 = Nstem2 + Nstem1to2 - Nstem2to3;
                Nstem3 = Nstem3 + Nstem2to3 - Nstem3to4 - Nstem3Remob;
                Nstem4 = Nstem4 + Nstem3to4 - Nstem4toL;

                if (dmstem1 != 0) { Ncstem1 = Nstem1 / dmstem1; }
                if (dmstem2 != 0) { Ncstem2 = Nstem2 / dmstem2; }
                if (dmstem3 != 0) { Ncstem3 = Nstem3 / dmstem3; }
                if (dmstem4 != 0) { Ncstem4 = Nstem4 / dmstem4; }

                double Nstol1to2 = Ncstol1 * 2 * gamas * pS.dmstol1;
                double Nstol2to3 = Ncstol2 * gamas * pS.dmstol2;
                double Nstol3Remob = 0.5 * (Ncstol3 - NcstolMin) * gamas * pS.dmstol3;       //gamas is acelerated by defoliation
                double Nstol3toL = Ncstol3 * gamas * pS.dmstol3 - Nstol3Remob;

                Nstol1 = Nstol1 - Nstol1to2;
                Nstol2 = Nstol2 + Nstol1to2 - Nstol2to3;
                Nstol3 = Nstol3 + Nstol2to3 - Nstol3toL - Nstol3Remob;

                if (dmstol1 != 0) { Ncstol1 = Nstol1 / dmstol1; } //grass has no stolon      
                if (dmstol2 != 0) { Ncstol2 = Nstol2 / dmstol2; }
                if (dmstol3 != 0) { Ncstol3 = Nstol3 / dmstol3; }

                //rootN           
                NrootRemob = 0.5 * (Ncroot - NcrootMin) * dRootSen;    //acelerated by defoliation, the N remob smaller
                dNrootSen = Ncroot * dRootSen - NrootRemob;
                Nroot = Nroot - Ncroot * dRootSen;              // (Ncroot goes to both Remob & FOM in soil)      
                if (dmroot != 0) Ncroot = Nroot / dmroot;       // dmroot==0 this should not happen  

                dLitter = gamad * (pS.dmleaf4 + pS.dmstem4) + gamas * pS.dmstol3;

                double leftoverNremob = Nremob;
                dNLitter = Nleaf4toL + Nstem4toL + Nstol3toL + leftoverNremob;    //Nremob of previous day after newgrowth, go to litter

                //The leftover 'Nremob' of previous day (if>0) indicates more N should go to litter in previous day, so do it now 
                //this is especially importatn in automn
                Nremob = Nleaf3Remob + Nstem3Remob + Nstol3Remob + NrootRemob;  //today's N remob                   

                //Sugar remobilisation and C balance: 
                Cremob = 0;// not explicitely considered

                /*Cremob = (Nremob - leftoverNremob) * C2N_protein;    //Cremob is calculated one day later so as to know if it is really
                                                                     //remobilised with N            
                if (Cremob > 0)
                {
                    if (dLitter > Cremob * C2DM)
                    {
                        dLitter -= Cremob * C2DM;  //remove from litter (most likely in this case)                    
                    }
                    else
                    {
                        Cremob = dLitter / C2DM;
                        dLitter = 0;
                    }
                }
                else
                {
                    dLitter += Cremob * C2DM;
                    Cremob = 0;
                }*/


            }  //end of "turnover" block

            updateAggregated();

            calcDigestability();

            return dGrowth;
        }


        //Species ------------------------------------------------------------------
        private double NewGrowthToShoot()
        {
            //The input maxSRratio (maximum percentage allocated to roots = 20%) was converted into 
            //the real ratio (=4) at the beginning when setting specific values                        
            double GFmin = Math.Min(gfwater, gfn);      //To consider other nutrients later

            //Variable maxSR - maximum shoot/root ratio accoding to phenoloty
            double maxSR = maxSRratio;
            // fac: Assuming the new growth partition is towards a shoot:root ratio of 'maxSR' during reproductive stage,
            //      then the partition will be towards a lower shoot:root ratio of (frac*maxSRratio) during vegetative stage 

            double minF = allocationSeasonF;    //default = 0.8;
            double fac = 1.0;                   //day-to-day fraction of reduction        
            int doy = day_of_month + (int)((month - 1) * 30.5);

            // double pd = 4*Math.PI* doy/365;
            // double toRoot = 1/(1 + maxSRratio);
            // toRoot = toRoot + 0.25*maxSRratio * Math.Sin(pd);

            int doyC = 232;             // Default as in South-hemisphere                
            if (latitude > 0)           // If it is in North-hemisphere.
                doyC = doyC - 181;

            int doyF = doyC + 35;   //75
            int doyD = doyC + 95;   // 110;
            int doyE = doyC + 125;  // 140;
            if (doyE > 365) doyE = doyE - 365;

            if (doy > doyC)
            {
                if (doy <= doyF)
                    fac = minF + (1 - minF) * (doy - doyC) / (doyF - doyC);
                else if (doy <= doyD)
                    fac = 1.0;
                else if (doy <= doyE)
                    fac = 1 - (1 - minF) * (doy - doyD) / (doyE - doyD);
            }
            else
            {
                fac = minF;
                if (doyE < doyC && doy <= doyE)    //only happens in south hemisphere
                    fac = 1 - (1 - minF) * (365 + doy - doyD) / (doyE - doyD);

            }
            maxSR = 1.25 * fac * maxSRratio;    //maxR is bigger in reproductive stage (i.e., less PHT going to root)
            //fac = 0.8 ~ 1; i.e., maxSR = 1.0 ~ 1.25 of maxSRratio (i.e., SRratio may be 1.25 times of specified maxSRratio during reproductive stage)

            phenoFactor = fac;
            //calculate shoot:root partitioning: fShoot = fraction to shoot [eq.4.12c]
            if (pS.dmroot > 0.00001)                    //pS is the previous state (yesterday)
            {
                double SRratio = dmgreen / pS.dmroot;
                if (SRratio > maxSR) SRratio = maxSR;

                double param = GFmin * maxSR * maxSR / SRratio;
                fShoot = param / (1.0 + param);
            }
            else
            {
                fShoot = 1.0;
            }


            /* resistance after drought
             * 
             * if (gfwater > 0.5 && dayCounter >= 5 && sumGFW < 1)
            {
                fShoot = 1;
                sumGFW = 0;
            }
            else
            {
                dayCounter++;
                sumGFW +=gfwater;
            }*/

            if (fShoot / (1 - fShoot) < maxSR)  // Set daily minimum fraction to shoot (i.e., maximum to root) 
                fShoot = maxSR / (1 + maxSR);   // as the specified that the system maxSR towards to (useful under stress)

            if (dmgreen < pS.dmroot)  //this may happen under stress. There may be CHTs move up too
                fShoot = 1.0;

            return fShoot;
        }

        //Species -------------------------------------------------------------------
        public double coverGreen
        {
            get { return (1.0 - Math.Exp(-lightExtCoeff * greenLAI)); }
        }
        //Species -------------------------------------------------------------------
        public double coverDead
        {
            get { return (1.0 - Math.Exp(-lightExtCoeff * deadLAI)); }
        }
        //Species -------------------------------------------------------------------
        public double coverTot
        {
            get { return (1.0 - (Math.Exp(-lightExtCoeff * totalLAI))); }
        }

        //Species ---------------------------------------------------------------------
        public double GFTemperature()
        {
            if (photoPath == 4) gftemp = GFTempC4();
            else gftemp = GFTempC3();               //CAM path ?
            return gftemp;
        }
        public double GFTemperature(double T)       //passing T
        {
            if (photoPath == 4) gftemp = GFTempC4(T);
            else gftemp = GFTempC3(T);
            return gftemp;
        }
        //Species -------------------------------------------------
        // Photosynthesis temperature response curve for C3 plants
        public double GFTempC3()
        {
            double gft3 = 0.0;
            double T = (MetData.maxt + MetData.mint) / 2;
            if (T > growthTmin && T < growthTmax)
            {
                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((T - growthTmin), growthTq) * (Tmax - T);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft3 = val1 / val2;

                if (gft3 < 0.0) gft3 = 0.0;
                if (gft3 > 1.0) gft3 = 1.0;
            }
            return gft3;
        }
        //Species -------------------------------------------------
        // Photosynthesis temperature response curve for C3 plants, passing T
        public double GFTempC3(double T)
        {
            double gft3 = 0.0;
            if (T > growthTmin && T < growthTmax)
            {
                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((T - growthTmin), growthTq) * (Tmax - T);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft3 = val1 / val2;

                if (gft3 < 0.0) gft3 = 0.0;
                if (gft3 > 1.0) gft3 = 1.0;
            }
            return gft3;
        }

        //Species ---------------------------------------------
        // Photosynthesis temperature response curve for C4 plants
        public double GFTempC4()
        {
            double gft4 = 0.0;          // Assign value 0 for the case of T < Tmin
            double T = (MetData.maxt + MetData.mint) / 2;

            if (T > growthTmin)         // same as GFTempC3 for [Tmin,Topt], but T as Topt if T > Topt
            {
                if (T > growthTopt)
                    T = growthTopt;

                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((T - growthTmin), growthTq) * (Tmax - T);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft4 = val1 / val2;

                if (gft4 < 0.0) gft4 = 0.0;
                if (gft4 > 1.0) gft4 = 1.0;
            }
            return gft4;
        }

        //Species ---------------------------------------------
        // Photosynthesis temperature response curve for C4 plants, passing T
        public double GFTempC4(double T)
        {
            double gft4 = 0.0;          // Assign value 0 for the case of T < Tmin

            if (T > growthTmin)         // same as GFTempC3 for [Tmin,Topt], but T as Topt if T > Topt
            {
                if (T > growthTopt)
                    T = growthTopt;

                double Tmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                double val1 = Math.Pow((T - growthTmin), growthTq) * (Tmax - T);
                double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (Tmax - growthTopt);
                gft4 = val1 / val2;

                if (gft4 < 0.0) gft4 = 0.0;
                if (gft4 > 1.0) gft4 = 1.0;
            }
            return gft4;
        }

        //Species ---------------------------------------------
        // Heat effect: reduction = (MaxT-28)/35, recovery after accumulating 50C of (meanT-25)
        private double HeatEffect()
        {
            //constants are now set from interface       
            //recover from the previous high temp. effect
            double recoverF = 1.0;

            if (highTempEffect < 1.0)
            {
                double meanT = 0.5 * (MetData.maxt + MetData.mint);
                if (25 - meanT > 0)
                {
                    accumT += (25 - meanT);
                }

                if (accumT < heatSumT)
                {
                    recoverF = highTempEffect + (1 - highTempEffect) * accumT / heatSumT;
                }
            }

            //possible new high temp. effect
            double newHeatF = 1.0;
            if (MetData.maxt > heatFullT)
            {
                newHeatF = 0;
            }
            else if (MetData.maxt > heatOnsetT)
            {
                newHeatF = (MetData.maxt - heatOnsetT) / (heatFullT - heatOnsetT);
            }

            // If this new high temp. effect is compounded with the old one & 
            // re-start of the recovery from the new effect 
            if (newHeatF < 1.0)
            {
                highTempEffect = recoverF * newHeatF;
                accumT = 0;
                recoverF = highTempEffect;
            }

            return recoverF;
        }

        //Species ---------------------------------------------
        // Cold effect: reduction, recovery after accumulating 20C of meanT
        private double ColdEffect()
        {
            //recover from the previous high temp. effect
            double recoverF = 1.0;
            if (lowTempEffect < 1.0)
            {
                double meanT = 0.5 * (MetData.maxt + MetData.mint);
                if (meanT > 0)
                {
                    accumTLow += meanT;
                }

                if (accumTLow < coldSumT)
                {
                    recoverF = lowTempEffect + (1 - lowTempEffect) * accumTLow / coldSumT;
                }
            }

            //possible new low temp. effect
            double newColdF = 1.0;
            if (MetData.mint < coldFullT)
            {
                newColdF = 0;
            }
            else if (MetData.mint < coldOnsetT)
            {
                newColdF = (MetData.mint - coldFullT) / (coldOnsetT - coldFullT);
            }

            // If this new cold temp. effect happens when serious cold effect is still on, 
            // compound & then re-start of the recovery from the new effect 
            if (newColdF < 1.0)
            {
                lowTempEffect = newColdF * recoverF;
                accumTLow = 0;
                recoverF = lowTempEffect;
            }

            return recoverF;
        }

        //Species ----------------------------------------------------------
        // Tissue turnover rate's response to water stress (eq. 4.15h)
        public double GFWaterTissue()
        {
            double gfwt = 1.0;

            if (gfwater < massFluxWopt)
                gfwt = 1 + (massFluxW0 - 1.0) * ((massFluxWopt - gfwater) / massFluxWopt);

            if (gfwt < 1.0) gfwt = 1.0;
            if (gfwt > massFluxW0) gfwt = massFluxW0;
            return gfwt;
        }

        //Species ------------------------------------------------------
        // Tissue turnover rate's response to temperature (eq 4.15f)
        // Tissue turnover: Tmin=5, Topt=20 - same for C3 & C4 plants ?
        public double GFTempTissue()
        {
            double T = (MetData.maxt + MetData.mint) / 2;

            double gftt = 0.0;        //default as T < massFluxTmin     
            if (T > massFluxTmin && T <= massFluxTopt)
            {
                gftt = (T - massFluxTmin) / (massFluxTopt - massFluxTmin);
            }
            else if (T > massFluxTopt)
            {
                gftt = 1.0;
            }
            return gftt;
        }
        // Species ----------------------------------------------------------------------
        public void ResetZero()  //kill this crop
        {
            //Reset dm pools
            dmleaf1 = dmleaf2 = dmleaf3 = dmleaf4 = 0;	//(kg/ha)
            dmstem1 = dmstem2 = dmstem3 = dmstem4 = 0;	//sheath and stem
            dmstol1 = dmstol2 = dmstol3 = 0;
            dmroot = 0;
            dmlitter = 0;

            dmdefoliated = 0;

            //Reset N pools
            Nleaf1 = Nleaf2 = Nleaf3 = Nleaf4 = 0;
            Nstem1 = Nstem2 = Nstem3 = Nstem4 = 0;
            Nstol1 = Nstol2 = Nstol3 = Nroot = Nlitter = 0;

            phenoStage = 0;

            if (updateAggregated() > 0.0)  //return totalLAI = 0 
            {
                Console.WriteLine("Plant is not completely killed.");
            }
        }


        //Species ---------------------------------------------------------
        public void SetInGermination()
        {
            bSown = true;
            phenoStage = 0; //before germination
        }

        //Species ---------------------------------------------------------
        public bool SetPrevPools()
        {
            pS.dmleaf1 = dmleaf1;
            pS.dmleaf2 = dmleaf2;
            pS.dmleaf3 = dmleaf3;
            pS.dmleaf4 = dmleaf4;
            pS.dmstem1 = dmstem1;
            pS.dmstem2 = dmstem2;
            pS.dmstem3 = dmstem3;
            pS.dmstem4 = dmstem4;
            pS.dmstol1 = dmstol1;
            pS.dmstol2 = dmstol2;
            pS.dmstol3 = dmstol3;
            pS.dmlitter = dmlitter;
            pS.dmroot = dmroot;
            pS.dmleaf_green = dmleaf_green;
            pS.dmstem_green = dmstem_green;
            pS.dmstol_green = dmstol_green;
            pS.dmleaf = dmleaf;
            pS.dmstem = dmstem;
            pS.dmstol = dmstol;
            pS.dmshoot = dmshoot;
            pS.dmgreen = dmgreen;
            pS.dmdead = dmdead;
            pS.dmtotal = dmtotal;
            pS.dmdefoliated = dmdefoliated;

            pS.Nremob = Nremob;


            return true;
        }

    } //class Species


    //DMPools =================================================
    //for remember the pool status of previous day
    [Serializable]
    public class DMPools
    {
        public double dmleaf1;
        public double dmleaf2;
        public double dmleaf3;
        public double dmleaf4;
        public double dmstem1;
        public double dmstem2;
        public double dmstem3;
        public double dmstem4;
        public double dmstol1;
        public double dmstol2;
        public double dmstol3;
        public double dmlitter;
        public double dmroot;

        public double dmleaf;
        public double dmstem;
        public double dmleaf_green;
        public double dmstem_green;
        public double dmstol_green;
        public double dmstol;
        public double dmshoot;
        public double dmgreen;
        public double dmdead;
        public double dmtotal;
        public double dmdefoliated;
        public double Nremob;

        public DMPools() { }


    } //class DMPools
}