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
using Models.Soils;

namespace Models.AgPasture1
{

    /// <summary>
    /// A multi-species pasture model 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Sward : Model, ICrop
    {
        #region Links, events and delegates  -------------------------------------------------------------------------------

        [Link]
        private Clock clock = null;

        [Link]
        private Soils.Soil Soil = null;

        [Link]
        private WeatherFile MetData = null;

        [Link]
        private ISummary Summary = null;

        //Events
        public delegate void NewCropDelegate(PMF.NewCropType Data);
        public event NewCropDelegate NewCrop;

        public delegate void NewCanopyDelegate(NewCanopyType Data);
        public event NewCanopyDelegate NewCanopy;

        public delegate void FOMLayerDelegate(Soils.FOMLayerType Data);
        public event FOMLayerDelegate IncorpFOM;

        public delegate void BiomassRemovedDelegate(PMF.BiomassRemovedType Data);
        public event BiomassRemovedDelegate BiomassRemoved;

        public delegate void WaterChangedDelegate(PMF.WaterChangedType Data);
        public event WaterChangedDelegate WaterChanged;

        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        public event NitrogenChangedDelegate NitrogenChanged;

        #endregion

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>
        /// Generic decriptor used by MicroClimate to look up for canopy properties for this plant
        /// </summary>
        [Description("Generic type of crop")]
        [Units("")]
        public string CropType
        {
            get { return swardName; }
        }

        /// <summary>
        /// Gets a list of cultivar names (not used by AgPasture)
        /// </summary>
        public string[] CultivarNames
        {
            get { return null; }
        }

        /// <summary>
        /// Potential evapotranspiration, as calculated by MicroClimate
        /// </summary>
        [XmlIgnore]
        public double PotentialEP
        {
            get { return SwardWaterDemand; }
            set { SwardWaterDemand = value; }
        }

        private double interceptedRadn;
        private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;
        /// <summary>
        /// Energy available for each canopy layer, as calcualted by MicroClimate
        /// </summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile
        {
            get { return myLightProfile; }
            set
            {
                interceptedRadn = 0.0;
                for (int s = 0; s < value.Length; s++)
                {
                    myLightProfile = value;
                    interceptedRadn += myLightProfile[s].amount;
                }
            }
        }

        /// <summary>
        /// Data about this plant's canopy (LAI, height, etc), used by MicroClimate
        /// </summary>
        private NewCanopyType myCanopyData = new NewCanopyType();

        /// <summary>
        /// Data about this plant's canopy (LAI, height, etc), used by MicroClimate
        /// </summary>
        public NewCanopyType CanopyData
        {
            get { return myCanopyData; }
        }

        private Soils.RootSystem rootSystem;
        /// <summary>
        /// Root system information for this crop
        /// </summary>
        [XmlIgnore]
        public Soils.RootSystem RootSystem
        {
            get { return rootSystem; }
            set { rootSystem = value; }
        }

        /// <summary>
        /// Plant growth limiting factor for other module calculating potential transpiration
        /// </summary>
        public double FRGR
        {
            get
            {
                double Tday = 0.75 * MetData.MaxT + 0.25 * MetData.MinT; //Tday
                double gft;
                if (Tday < 20) gft = Math.Sqrt(GLFtemp);
                else gft = GLFtemp;
                // Note: p_gftemp is for gross photosysthsis.
                // This is different from that for net production as used in other APSIM crop models, and is
                // assumesd in calculation of temperature effect on transpiration (in micromet).
                // Here we passed it as sqrt - (Doing so by a comparison of p_gftemp and that
                // used in wheat). Temperature effects on NET produciton of forage species in other models
                // (e.g., grassgro) are not so significant for T = 10-20 degrees(C)

                //Also, have tested the consequences of passing p_Ncfactor in (different concept for gfwater),
                //coulnd't see any differnece for results
                return Math.Min(FVPD, gft);
                // RCichota, Jan/2014: removed AgPasture's Frgr from here, it is considered at the same level as nitrogen etc...
            }
        }

        // ** Have to verify if this is still needed, it is not part of ICrop
        /// <summary>
        /// Event publication - new crop
        /// </summary>
        private void DoNewCropEvent()
        {
            if (NewCrop != null)
            {
                // Send out New Crop Event to tell other modules who I am and what I am
                PMF.NewCropType EventData = new PMF.NewCropType();
                EventData.crop_type = swardName;  // need to separate crop type for micromet & canopy name !!
                EventData.sender = Name;		//
                NewCrop.Invoke(EventData);
            }
        }

        #endregion

        #region Model parameters  ------------------------------------------------------------------------------------------

        // = General parameters  ==================================================================

        // [Link]
        //mySpecies[] mySpecies;
        [XmlIgnore]
        public PastureSpecies[] mySpecies { get; private set; }

        private int numSpecies = 1;
        //[Description("Number of species")] 
        [XmlIgnore]
        public int NumSpecies
        {
            get { return numSpecies; }
            set { numSpecies = value; }
        }

       // * Parameters that are set via user interface -------------------------------------------

        private string swardName = "AgPasture";
        [Description("Sward name (as shown on the simulation tree)")]
        public string SwardName
        {
            get { return swardName; }
            set { swardName = value; }
        }

        private bool isAgPastureControlled = true;
        [Description("Test - Is AgPasture controlling species?")]
        public string AgPastureControlled
        {
            get {
                if (isAgPastureControlled)
                    return "yes";
                else
                    return "no";
            }
            set { isAgPastureControlled = (value.ToLower()=="yes"); }
        }

        private string waterUptakeSource = "AgPasture";
        [Description("Water uptake done by AgPasture, by species (calc), or by apsim?")]
        public string WaterUptakeSource
        {
            get { return waterUptakeSource; }
            set { waterUptakeSource = value; }
        }

        private string nUptakeSource = "AgPasture";
        [Description("Nitrogen uptake done by AgPasture, by species (calc), or by apsim?")]
        public string NUptakeSource
        {
            get { return nUptakeSource; }
            set { nUptakeSource = value; }
        }

        private string useAltWUptake = "no";
        [Description("Test - Use alternative water uptake process")]
        public string UseAlternativeWaterUptake
        {
            get { return useAltWUptake; }
            set { useAltWUptake = value; }
        }

        private string useAltNUptake = "no";
        [Description("Test - Use alternative N uptake process")]
        public string UseAlternativeNUptake
        {
            get { return useAltNUptake; }
            set { useAltNUptake = value; }
        }

        private double referenceKSuptake = 1000.0;
        [Description("Test - Reference soil Ksat for optimum water uptake (if using alternative method)")]
        public double ReferenceKSuptake
        {
            get { return referenceKSuptake; }
            set { referenceKSuptake = value; }
        }

        //private double[] preferenceForGreenDM = new double[] { 1.0, 1.0, 1.0 };
        //[XmlIgnore]
        //public double[] PreferenceForGreenDM
        //{
        //    get { return preferenceForGreenDM; }
        //    set
        //    {
        //        int NSp = value.Length;
        //        preferenceForGreenDM = new double[NSp];
        //        for (int sp = 0; sp < NSp; sp++)
        //            preferenceForGreenDM[sp] = value[sp];
        //    }
        //}

        //private double[] preferenceForDeadDM = new double[] { 1.0, 1.0, 1.0 };
        //[XmlIgnore]
        //public double[] PreferenceForDeadDM
        //{
        //    get { return preferenceForDeadDM; }
        //    set
        //    {
        //        int NSp = value.Length;
        //        preferenceForDeadDM = new double[NSp];
        //        for (int sp = 0; sp < NSp; sp++)
        //            preferenceForDeadDM[sp] = value[sp];
        //    }
        //}

        // * Other parameters (changed via manager) -----------------------------------------------

        private string rootDistributionMethod = "ExpoLinear";
        //[Description("Root distribution method")]
        [XmlIgnore]
        public string RootDistributionMethod
        {
            get
            { return rootDistributionMethod; }
            set
            {
                switch (value.ToLower())
                {
                    case "homogenous":
                    case "userdefined":
                    case "expolinear":
                        rootDistributionMethod = value;
                        break;
                    default:
                        throw new Exception("No valid method for computing root distribution was selected");
                }
            }
        }

        private double expoLinearDepthParam = 0.1;
        [Description("Fraction of root depth where its proportion starts to decrease")]
        public double ExpoLinearDepthParam
        {
            get { return expoLinearDepthParam; }
            set
            {
                expoLinearDepthParam = value;
                if (expoLinearDepthParam == 1.0)
                    rootDistributionMethod = "Homogeneous";
            }
        }

        private double expoLinearCurveParam = 0.1;
        [Description("Exponent to determine mass distribution in the soil profile")]
        public double ExpoLinearCurveParam
        {
            get { return expoLinearCurveParam; }
            set
            {
                expoLinearCurveParam = value;
                if (expoLinearCurveParam == 0.0)
                    rootDistributionMethod = "Homogeneous";	// It is impossible to solve, but its limit is a homogeneous distribution
            }
        }

        #endregion

        #region Model outputs  ---------------------------------------------------------------------------------------------

        [Description("Plant status (dead, alive, etc)")]
        [Units("")]
        public string PlantStatus
        {
            get
            {
                if (mySpecies.Any(x => x.PlantStatus == "alive"))
                    return "alive";
                else
                    return "out";
            }
        }

        [Description("Plant development stage number, approximate")]
        [Units("")]
        public int Stage
        {
            // An approximate of the stage number, corresponding to that of other arable crops. For management applications.
            // Phenostage of the first species (ryegrass) is used for this approximation
            get
            {
                if (PlantStatus == "alive")
                {
                    if (mySpecies.Any(x => x.Stage == 3))
                        return 3;    // 'emergence'
                    else
                        return 1;    //'sowing or germination';
                }
                else
                    return 0;
            }
        }

        [Description("Plant development stage name")]
        [Units("")]
        public string StageName
        {
            get
            {
                if (PlantStatus == "alive")
                {
                    if (Stage == 1)
                        return "sowing";
                    else
                        return "emergence";
                }
                return "out";
            }
        }

        #region - DM and C amounts  ----------------------------------------------------------------------------------------

        [Description("Total amount of C in plants")]
        [Units("kgDM/ha")]
        public double TotalPlantC
        {
            get { return mySpecies.Sum(x => x.TotalWt * CinDM); }
        }

        [Description("Total dry matter weight of plants")]
        [Units("kgDM/ha")]
        public double TotalPlantWt
        {
            get { return mySpecies.Sum(x => x.TotalWt); }
        }

        [Description("Total dry matter weight of plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundPlantWt
        {
            get { return mySpecies.Sum(x => x.AboveGroundWt); }
        }

        [Description("Total dry matter weight of plants alive above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundLiveWt
        {
            get { return mySpecies.Sum(x => x.AboveGrounLivedWt); }
        }

        [Description("Total dry matter weight of dead plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundDeadWt
        {
            get { return mySpecies.Sum(x => x.AboveGroundDeadWt); }
        }

        [Description("Total dry matter weight of plants below ground")]
        [Units("kgDM/ha")]
        public double BelowGroundWt
        {
            get { return mySpecies.Sum(x => x.RootWt); }
        }

        [Description("Total dry matter weight of standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingPlantWt
        {
            get { return mySpecies.Sum(x => x.StandingWt); }
        }

        [Description("Dry matter weight of live standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingLiveWt
        {
            get { return mySpecies.Sum(x => x.StandingLiveWt); }
        }

        [Description("Dry matter weight of dead standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingDeadWt
        {
            get { return mySpecies.Sum(x => x.StandingDeadWt); }
        }

        [Description("Total dry matter weight of plant's leaves")]
        [Units("kgDM/ha")]
        public double LeafWt
        {
            get { return mySpecies.Sum(x => x.LeafWt); }
        }

        [Description("Total dry matter weight of plant's leaves alive")]
        [Units("kgDM/ha")]
        public double LeafLiveWt
        {
            get { return mySpecies.Sum(x => x.LeafGreenWt); }
        }

        [Description("Total dry matter weight of plant's leaves dead")]
        [Units("kgDM/ha")]
        public double LeafDeadWt
        {
            get { return mySpecies.Sum(x => x.LeafDeadWt); }
        }

        [Description("Total dry matter weight of plant's stems")]
        [Units("kgDM/ha")]
        public double StemWt
        {
            get { return mySpecies.Sum(x => x.StemWt); }
        }

        [Description("Total dry matter weight of plant's stems alive")]
        [Units("kgDM/ha")]
        public double StemLiveWt
        {
            get { return mySpecies.Sum(x => x.StemGreenWt); }
        }

        [Description("Total dry matter weight of plant's stems dead")]
        [Units("kgDM/ha")]
        public double StemDeadWt
        {
            get { return mySpecies.Sum(x => x.StemDeadWt); }
        }

        [Description("Total dry matter weight of plant's stolons")]
        [Units("kgDM/ha")]
        public double StolonWt
        {
            get { return mySpecies.Sum(x => x.StolonWt); }
        }

        [Description("Total dry matter weight of plant's roots")]
        [Units("kgDM/ha")]
        public double RootWt
        {
            get { return mySpecies.Sum(x => x.RootWt); }
        }

        #endregion

        #region - C and DM flows  ------------------------------------------------------------------------------------------

        [Description("Gross potential plant growth (potential C assimilation)")]
        [Units("kgDM/ha")]
        public double PlantGrossPotentialGrowthWt
        {
            get { return mySpecies.Sum(x => x.GrossPotentialGrowthWt); }
        }

        [Description("Respiration rate (DM lost via respiration)")]
        [Units("kgDM/ha")]
        public double PlantRespirationWt
        {
            get { return mySpecies.Sum(x => x.RespirationWt); }
        }

        [Description("C remobilisation (DM remobilised from old tissue to new growth)")]
        [Units("kgDM/ha")]
        public double PlantRemobilisationWt
        {
            get { return mySpecies.Sum(x => x.RemobilisationWt); }
        }

        [Description("Net potential plant growth")]
        [Units("kgDM/ha")]
        public double PlantNetPotentialGrowthWt
        {
            get { return mySpecies.Sum(x => x.NetPotentialGrowthWt); }
        }

        [Description("Potential growth rate after water stress")]
        [Units("kgDM/ha")]
        public double PlantPotGrowthWt_Wstress
        {
            get { return mySpecies.Sum(x => x.PotGrowthWt_Wstress); }
        }

        [Description("Actual plant growth (before littering)")]
        [Units("kgDM/ha")]
        public double PlantActualGrowthWt
        {
            get { return mySpecies.Sum(x => x.ActualGrowthWt); }
        }

        [Description("Effective growth rate, after turnover")]
        [Units("kgDM/ha")]
        public double PlantEffectiveGrowthWt
        {
            get { return mySpecies.Sum(x => x.EffectiveGrowthWt); }
        }

        [Description("Effective herbage (shoot) growth")]
        [Units("kgDM/ha")]
        public double HerbageGrowthWt
        {
            get { return mySpecies.Sum(x => x.HerbageGrowthWt); }
        }

        [Description("Dry matter amount of litter deposited onto soil surface")]
        [Units("kgDM/ha")]
        public double LitterDepositionWt
        {
            get { return mySpecies.Sum(x => x.LitterWt); }
        }

        [Description("Dry matter amount of senescent roots added to soil FOM")]
        [Units("kgDM/ha")]
        public double RootSenescenceWt
        {
            get { return mySpecies.Sum(x => x.RootSenescedWt); }
        }

        [Description("Gross primary productivity")]
        [Units("kgDM/ha")]
        public double GPP
        {
            get { return mySpecies.Sum(x => x.GPP); }
        }

        [Description("Net primary productivity")]
        [Units("kgDM/ha")]
        public double NPP
        {
            get { return mySpecies.Sum(x => x.NPP); }
        }

        [Description("Net above-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NAPP
        {
            get { return mySpecies.Sum(x => x.NAPP); }
        }

        #endregion

        #region - N amounts  -----------------------------------------------------------------------------------------------

        [Description("Total amount of N in plants")]
        [Units("kgN/ha")]
        public double TotalPlantN
        {
            get { return mySpecies.Sum(x => x.TotalN); }
        }

        [Description("Total amount of N in plants above ground")]
        [Units("kgN/ha")]
        public double AboveGroundN
        {
            get { return mySpecies.Sum(x => x.AboveGroundN); }
        }

        [Description("Total amount of N in plants alive above ground")]
        [Units("kgN/ha")]
        public double AboveGroundLiveN
        {
            get { return mySpecies.Sum(x => x.AboveGroundLiveN); }
        }

        [Description("Total amount of N in dead plants above ground")]
        [Units("kgN/ha")]
        public double AboveGroundDeadN
        {
            get { return mySpecies.Sum(x => x.AboveGroundDeadN); }
        }

        [Description("Total amount of N in standing plants")]
        [Units("kgN/ha")]
        public double StandingPlantN
        {
            get { return mySpecies.Sum(x => x.StandingN); }
        }

        [Description("Total amount of N in standing alive plants")]
        [Units("kgN/ha")]
        public double StandingLivePlantN
        {
            get { return mySpecies.Sum(x => x.StandingLiveN); }
        }

        [Description("Total amount of N in dead standing plants")]
        [Units("kgN/ha")]
        public double StandingDeadPlantN
        {
            get { return mySpecies.Sum(x => x.StandingDeadN); }
        }

        [Description("Total amount of N in plants below ground")]
        [Units("kgN/ha")]
        public double BelowGroundN
        {
            get { return mySpecies.Sum(x => x.BelowGroundN); }
        }

        [Description("Total amount of N in the plant's leaves")]
        [Units("kgN/ha")]
        public double LeafN
        {
            get { return mySpecies.Sum(x => x.LeafN); }
        }

        [Description("Total amount of N in the plant's stems")]
        [Units("kgN/ha")]
        public double StemN
        {
            get { return mySpecies.Sum(x => x.StemN); }
        }

        [Description("Total amount of N in the plant's stolons")]
        [Units("kgN/ha")]
        public double StolonN
        {
            get { return mySpecies.Sum(x => x.StolonN); }
        }

        [Description("Total amount of N in the plant's roots")]
        [Units("kgN/ha")]
        public double RootN
        {
            get { return mySpecies.Sum(x => x.RootN); }
        }

        #endregion

        #region - N concentrations  ----------------------------------------------------------------------------------------

        [Description("Proportion of N above ground in relation to below ground")]
        [Units("%")]
        public double ShootRootNPct
        {
            get { return 100 * (AboveGroundN / BelowGroundN); }
        }

        [Description("Average N concentration of standing plants")]
        [Units("kgN/kgDM")]
        public double StandingPlantNConc
        {
            get { return StandingPlantN / StandingPlantWt; }
        }

        [Description("Average N concentration of leaves")]
        [Units("kgN/kgDM")]
        public double LeafNConc
        {
            get { return LeafN / LeafWt; }
        }

        [Description("Average N concentration in stems")]
        [Units("kgN/kgDM")]
        public double StemNConc
        {
            get { return StemN / StemWt; }
        }

        [Description("Average N concentration in stolons")]
        [Units("kgN/kgDM")]
        public double StolonNConc
        {
            get { return StolonN / StolonWt; }
        }

        [Description("Average N concentration in roots")]
        [Units("kgN/kgDM")]
        public double RootNConc
        {
            get { return RootN / RootWt; }
        }

        [Description("Nitrogen concentration in new growth")]
        [Units("kgN/kgDM")]
        public double PlantGrowthNconc
        {
            get { return PlantActualGrowthN / PlantActualGrowthWt; }
        }
        # endregion

        #region - N flows  -------------------------------------------------------------------------------------------------

        [Description("Amount of N remobilised from senescing tissue")]
        [Units("kgN/ha")]
        public double PlantRemobilisedN
        {
            get { return mySpecies.Sum(x => x.RemobilisedN); }
        }

        [Description("Amount of luxury N remobilised")]
        [Units("kgN/ha")]
        public double PlantLuxuryNRemobilised
        {
            get { return mySpecies.Sum(x => x.RemobilisedLuxuryN); }
        }

        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kgN/ha")]
        public double PlantRemobilisableLuxuryN
        {
            get { return mySpecies.Sum(x => x.RemobilisableLuxuryN); }
        }

        [Description("Amount of atmospheric N fixed")]
        [Units("kgN/ha")]
        public double PlantFixedN
        {
            get { return mySpecies.Sum(x => x.FixedN); }
        }

        [Description("Plant nitrogen requirement with luxury uptake")]
        [Units("kgN/ha")]
        public double NitrogenRequiredLuxury
        {
            get { return mySpecies.Sum(x => x.RequiredNLuxury); }
        }

        [Description("Plant nitrogen requirement for optimum growth")]
        [Units("kgN/ha")]
        public double NitrogenRequiredOptimum
        {
            get { return mySpecies.Sum(x => x.RequiredNOptimum); }
        }

        [Description("Plant nitrogen demand from soil")]
        [Units("kgN/ha")]
        public double NitrogenDemand
        {
            get { return mySpecies.Sum(x => x.DemandN); }
        }

        [Description("Plant available nitrogen in each soil layer")]
        [Units("kgN/ha")]
        public double[] NitrogenAvailable
        {
            get
            {
                double[] result = new double[Soil.Thickness.Length];
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    result[layer] = mySpecies.Sum(x => x.SoilAvailableN[layer]);
                return result;
            }
        }

        [Description("Plant nitrogen uptake from each soil layer")]
        [Units("kgN/ha")]
        public double[] NitrogenUptake
        {
            get
            {
                double[] result = new double[Soil.Thickness.Length];
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    result[layer] = mySpecies.Sum(x => x.UptakeN[layer]);
                return result;
            }
        }

        [Description("Amount of N deposited as litter onto soil surface")]
        [Units("kgN/ha")]
        public double LitterDepositionN
        {
            get { return mySpecies.Sum(x => x.LitterN); }
        }

        [Description("Amount of N added to soil FOM by senescent roots")]
        [Units("kgN/ha")]
        public double RootSenescenceN
        {
            get { return mySpecies.Sum(x => x.SenescedRootN); }
        }

        [Description("Nitrogen amount in new growth")]
        [Units("kgN/ha")]
        public double PlantActualGrowthN
        {
            get { return mySpecies.Sum(x => x.ActualGrowthN); }
        }

        #endregion

        #region - Turnover and DM allocation  ------------------------------------------------------------------------------

        [Description("Dry matter allocated to shoot")]
        [Units("kgDM/ha")]
        public double DMToShoot
        {
            get { return mySpecies.Sum(x => x.ActualGrowthWt * x.ShootDMAllocation); }
        }

        [Description("Dry matter allocated to roots")]
        [Units("kgDM/ha")]
        public double DMToRoots
        {
            get { return mySpecies.Sum(x => x.ActualGrowthWt * x.RootDMAllocation); }
        }

        [Description("Fraction of growth allocated to roots")]
        [Units("0-1")]
        public double FractionGrowthToRoot
        {
            get { return DMToRoots / PlantActualGrowthWt; }
        }

        [Description("Fraction of growth allocated to shoot")]
        [Units("0-1")]
        public double FractionGrowthToShoot
        {
            get { return DMToShoot / PlantActualGrowthWt; }
        }

        #endregion

        #region - LAI and cover  -------------------------------------------------------------------------------------------

        [Description("Total leaf area index")]
        [Units("m^2/m^2")]
        public double TotalLAI
        {
            get { return GreenLAI + DeadLAI; }
        }

        [Description("Leaf area index of green leaves")]
        [Units("m^2/m^2")]
        public double GreenLAI
        {
            get { return mySpecies.Sum(x => x.GreenLAI); }
        }

        [Description("Leaf area index of dead leaves")]
        [Units("m^2/m^2")]
        public double DeadLAI
        {
            get { return mySpecies.Sum(x => x.DeadLAI); }
        }

        [Description("Average light extintion coefficient")]
        [Units("0-1")]
        public double LightExtCoeff
        {
            get
            {
                double result = mySpecies.Sum(x => x.TotalLAI * x.LightExtentionCoeff)
                              / mySpecies.Sum(x => x.TotalLAI);

                return result;
            }
        }

        [Description("Fraction of soil covered by green leaves")]
        [Units("%")]
        public double Cover_green
        {
            get
            {
                if (GreenLAI == 0)
                    return 0.0;
                else
                    return (1.0 - Math.Exp(-LightExtCoeff * GreenLAI));
            }
        }

        [Description("Fraction of soil covered by dead leaves")]
        [Units("%")]
        public double Cover_dead
        {
            get
            {
                if (DeadLAI == 0)
                    return 0.0;
                else
                    return (1.0 - Math.Exp(-LightExtCoeff * DeadLAI));
            }
        }

        [Description("Fraction of soil covered by plants")]
        [Units("%")]
        public double Cover_tot
        {
            get
            {
                if (TotalLAI == 0) return 0;
                return (1.0 - (Math.Exp(-LightExtCoeff * TotalLAI)));
            }
        }

        [Description("Sward average height")]                 //needed by micromet
        [Units("mm")]
        public double Height
        {
            get { return mySpecies.Max(x => x.Height); }
        }

        #endregion

        #region - Root depth and distribution  -----------------------------------------------------------------------------

        [Description("Depth of root zone")]
        [Units("mm")]
        public double RootZoneDepth
        {
            get { return mySpecies.Max(x => x.RootDepth); }
        }

        [Description("Layer at bottom of root zone")]
        [Units("mm")]
        public double RootFrontier
        {
            get { return mySpecies.Max(x => x.RootFrontier); }
        }

        private double[] RootFraction;
        [Description("Fraction of root dry matter for each soil layer")]
        [Units("0-1")]
        public double[] RootWtFraction
        {
            get
            {
                double[] result = new double[Soil.Thickness.Length];
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    result[layer] = mySpecies.Sum(x => x.RootWt * x.RootWtFraction[layer]) / RootWt;
                return result;
            }
        }

        [Description("Root length density")]
        [Units("mm/mm^3")]
        public double[] RLV
        {
            get
            {
                double[] result = new double[Soil.Thickness.Length];
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    result[layer] = mySpecies.Sum(x => x.RLV[layer]);
                return result;
            }
        }

        #endregion

        #region - Water amounts  -------------------------------------------------------------------------------------------

        [Description("Plant water demand")]
        [Units("mm")]
        public double PlantWaterDemand
        {
            get { return mySpecies.Sum(x => x.WaterDemand); }
        }

        [Description("Plant available water in soil")]
        [Units("mm")]
        public double[] PlantSoilAvailableWater
        {
            get
            {
                double[] result = new double[Soil.Thickness.Length];
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    result[layer] = mySpecies.Sum(x => x.SoilAvailableWater[layer]);
                return result;
            }
        }

        [Description("Plant water uptake from soil")]
        [Units("mm")]
        public double[] PlantWaterUptake
        {
            get
            {
                double[] result = new double[Soil.Thickness.Length];
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    result[layer] = mySpecies.Sum(x => x.WaterUptake[layer]);
                return result;
            }
        }

        #endregion

        #region - Growth limiting factors  ---------------------------------------------------------------------------------

        [Description("Average plant growth limiting factor due to nitrogen availability")]
        [Units("0-1")]
        public double GLFn
        {
            get { return mySpecies.Sum(x => x.GLFN * x.ActualGrowthWt) / PlantActualGrowthWt; }
        }

        [Description("Average plant growth limiting factor due to plant N concentration")]
        [Units("0-1")]
        public double GLFnConcentration
        {
            get { return mySpecies.Sum(x => x.GLFnConcentration * x.ActualGrowthWt) / PlantActualGrowthWt; }
        }

        [Description("Average plant growth limiting factor due to temperature")]
        [Units("0-1")]
        public double GLFtemp
        {
            get { return mySpecies.Sum(x => x.GLFTemp * x.ActualGrowthWt) / PlantActualGrowthWt; }
        }

        [Description("Average plant growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GLFwater
        {
            get { return Math.Max(0.0, Math.Min(1.0, PlantWaterUptake.Sum() / PlantWaterDemand)); }
        }

        [Description("Average generic plant growth limiting factor, used for other factors")]
        [Units("0-1")]
        public double GLFrgr
        {
            get { return mySpecies.Sum(x => x.GlfGeneric * x.ActualGrowthWt) / PlantActualGrowthWt; }
        }

        [Description("Effect of vapour pressure on growth (used by micromet)")]
        [Units("0-1")]
        public double FVPD
        {
            get { return mySpecies[0].FVPD; }
        }

        #endregion

        #region - Harvest variables  ---------------------------------------------------------------------------------------

        [Description("Total dry matter amount available for removal (leaf+stem)")]
        [Units("kgDM/ha")]
        public double HarvestableWt
        {
            get { return mySpecies.Sum(x => x.HarvestableWt); }
        }

        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kgDM/ha")]
        public double HarvestedWt
        {
            get { return mySpecies.Sum(x => x.HarvestedWt); }
        }

        [Description("Amount of N removed by harvest")]
        [Units("kgN/ha")]
        public double HarvestedN
        {
            get { return mySpecies.Sum(x => x.HarvestedN); }
        }

        [Description("average N concentration of harvested material")]
        [Units("kgN/kgDM")]
        public double HarvestedNconc
        {
            get { return HarvestedN / HarvestedWt; }
        }

        [Description("Average digestibility of herbage")]
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get { return mySpecies.Sum(x => x.HerbageDigestibility * x.StandingWt) / StandingPlantWt; }
        }

        [Description("Average digestibility of harvested material")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get { return mySpecies.Sum(x => x.HarvestedDigestibility * x.HarvestedWt) / HarvestedWt; }
        }

        [Description("Average ME of herbage")]
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get { return 16 * HerbageDigestibility * StandingPlantWt; }
        }

        [Description("Average ME of harvested material")]
        [Units("(MJ/ha)")]
        public double HarvestedME
        {
            get { return 16 * HarvestedDigestibility * HarvestedWt; }
        }

        #endregion

        #endregion

        #region Private variables  -----------------------------------------------------------------------------------------

        /// <summary>
        /// flag signialling whether the initialisation procedures have been performed
        /// </summary>
        private bool HaveInitialised = false;
       
        /// <summary>
        /// flag signialling whether crop is alive (not killed)
        /// </summary>
        private bool isAlive = true;

        private double swardRootDepth;
        private double[] swardRootDistribution;

        // soil related
        private double[] soilAvailableN;
        private double[] soilNH4Available;
        private double[] soilNO3Available;
        private double swardNFixation;
        private double swardNdemand;
        private double swardSoilNdemand;
        private double[] swardNUptake;
        private double swardSoilNUptake;
        private double swardRemobilisedN;
        private double swardNRemobNewGrowth;
        private double swardNewGrowthN;
        private double swardNFastRemob2;
        private double swardNFastRemob3;

        /// <summary>
        /// Amount of soil water available to the sward, from each soil layer (mm)
        /// </summary>
        private double[] soilAvailableWater;
        /// <summary>
        /// Daily soil water demand for the whole sward (mm)
        /// </summary>
        private double SwardWaterDemand;
        /// <summary>
        /// Soil water uptake for the whole sward, from each soil layer (mm)
        /// </summary>
        private double[] swardWaterUptake;


        #endregion

        #region Constants  -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Average carbon content in plant dry matter
        /// </summary>
        const double CinDM = 0.4;
        /// <summary>
        /// Nitrogen to protein convertion factor
        /// </summary>
        const double N2Protein = 6.25;

        #endregion

        #region Initialisation methods  ------------------------------------------------------------------------------------

        [EventSubscribe("Loaded")]
        private void OnLoaded()
        {
            //mySpecies = Children.Matching(typeof(mySpecies)) as mySpecies;
            //FindChildren();
        }

        /// <summary>
        /// Performs initialisation tasks (overrides Init2)
        /// COuld this be in OnSimulationCommencing ???
        /// </summary>
        [EventSubscribe("Initialised")]
        private void Initialise()
        {
            InitParameters();			// Init parameters after reading the data

            //foreach(PastureSpecies myPlant in mySpecies)
            //    myPlant.IncorpFOM()

            DoNewCropEvent();			// Tell other modules that I exist
            DoNewCanopyEvent();		  // Tell other modules about my canopy

        }

        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            HaveInitialised = false;
            //mySpecies = Children.Matching(typeof(Species1)) as Species1;

        }

        /// <summary>
        /// Initialise parameters
        /// </summary>
        private void InitParameters()
        {
            // zero out the global variables
            SwardWaterDemand = 0;
            soilAvailableWater = new double[Soil.SoilWater.dlayer.Length];
            swardWaterUptake = new double[Soil.SoilWater.dlayer.Length];

            soilAvailableN = new double[Soil.SoilWater.dlayer.Length];
            swardNUptake = new double[Soil.SoilWater.dlayer.Length];

            if (isAgPastureControlled)
            {
                swardRootDepth = mySpecies.Max(x => x.RootDepth);
                swardRootDistribution = RootProfileDistribution();
            }
        }

        #endregion

        #region Daily processes  -------------------------------------------------------------------------------------------

        /// <summary>
        /// EventHandeler - preparation befor the main process
        /// </summary>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (!HaveInitialised)
            {
                Initialise();
                HaveInitialised = true;
            }

            // Moved to OnDoPlantGrowth
            //DoNewCanopyEvent();
        }

        /// <summary>
        /// Performs the plant growth calculations
        /// </summary>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (isAlive)
            {
                foreach (PastureSpecies myPlant in mySpecies)
                {
                    // stores the current state for this species
                    myPlant.SaveState();

                    // step 01 - preparation and potential growth
                    myPlant.CalcPotentialGrowth();
                }

                // Send information about this species canopy, MicroClimate will compute intercepted radiation and water demand
                DoNewCanopyEvent();

                // Water demand, supply, and uptake
                DoWaterCalculations();

                // step 02 - Potential growth after water limitations
                foreach (PastureSpecies myPlant in mySpecies)
                    myPlant.CalcGrowthWithWaterLimitations();

                // Nitrogen demand, supply, and uptake
                DoNitrogenCalculations();

                foreach (PastureSpecies myPlant in mySpecies)
                {
                    // step 03 - Actual growth after nutrient limitations, but before senescence
                    myPlant.CalcActualGrowthAndPartition();

                    // step 04 - Effective growth after all limitations and senescence
                    myPlant.CalcTurnoverAndEffectiveGrowth();
                }

                // update/aggregate some variables
                swardNRemobNewGrowth = mySpecies.Sum(x => x.RemobilisableN);
            }
        }

        #region - Water uptake process  ------------------------------------------------------------------------------------

        /// <summary>
        /// Provides canopy data for MicroClimate, who will do the energy balance and calc water demand
        /// </summary>
        private void DoNewCanopyEvent()
        {
            if (NewCanopy != null)
            {
                myCanopyData.sender = swardName;
                myCanopyData.lai = GreenLAI;
                myCanopyData.lai_tot = TotalLAI;
                myCanopyData.height = Height;
                myCanopyData.depth = Height;			  // canopy depth = canopy height
                myCanopyData.cover = Cover_green;
                myCanopyData.cover_tot = Cover_tot;
                NewCanopy.Invoke(myCanopyData);
            }
        }

        /// <summary>
        /// Gets the water uptake for each layer as calculated by an external module (SWIM)
        /// </summary>
        /// <param name="SoilWater"></param>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
        {
            for (int iCrop = 0; iCrop < SoilWater.Uptakes.Length; iCrop++)
            {
                if (SoilWater.Uptakes[iCrop].Name == SwardName)
                {
                    for (int layer = 0; layer < SoilWater.Uptakes[iCrop].Amount.Length; layer++)
                        swardWaterUptake[layer] = SoilWater.Uptakes[iCrop].Amount[layer];
                }
            }
        }

        /// <summary>
        /// Water uptake processes
        /// </summary>
        private void DoWaterCalculations()
        {
            // Find out soil available water
            soilAvailableWater = GetSoilAvailableWater();

            // Get the water demand for all species
            SwardWaterDemand = mySpecies.Sum(x => x.WaterDemand);

            // Do the water uptake (and partition between species)
            swardWaterUptake = new double[Soil.Thickness.Length];
            DoSoilWaterUptake();
        }

        /// <summary>
        /// Finds out the amount soil water available (consider all species)
        /// </summary>
        /// <returns>The amount of water available to plants in each layer</returns>
        private double[] GetSoilAvailableWater()
        {
            double[] result = new double[Soil.Thickness.Length];
            SoilCrop soilInfo = (SoilCrop)Soil.Crop(Name);
            double layerFraction = 0.0;   //fraction of soil layer explored by plants
            double layerLL = 0.0;         //LL value for a given layer (minimum of all plants)
            if (useAltWUptake == "no")
            {
                for (int layer = 0; layer <= RootFrontier; layer++)
                {
                    layerFraction = mySpecies.Max(x => x.LayerFractionWithRoots(layer));
                    result[layer] = Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - soilInfo.LL[layer] * Soil.Thickness[layer])
                                  * layerFraction;
                    result[layer] *= soilInfo.KL[layer];
                    // Note: assumes KL and LL defined for AgPasture, ignores the values for each species
                }
            }
            else
            { // Method implemented by RCichota
                // Available Water is function of root density, soil water content, and soil hydraulic conductivity
                // See GetSoilAvailableWater method in the Species code for details on calculation of each species
                // Here it is assumed that the actual water available for each layer is the smaller value between the
                //  total theoretical available water (corrected for water status and conductivity) and the sum of 
                //  available water for all species

                double facCond = 0.0;
                double facWcontent = 0.0;

                // get sum water available for all species
                double[] sumWaterAvailable = new double[Soil.Thickness.Length];
                foreach (PastureSpecies plant in mySpecies)
                    sumWaterAvailable.Zip(plant.GetSoilAvailableWater(), (x, y) => x + y);

                for (int layer = 0; layer <= RootFrontier; layer++)
                {
                    facCond = 1 - Math.Pow(10, -Soil.Water.KS[layer] / referenceKSuptake);
                    facWcontent = 1 - Math.Pow(10,
                                -(Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - Soil.SoilWater.ll15_dep[layer]))
                                / (Soil.SoilWater.dul_dep[layer] - Soil.SoilWater.ll15_dep[layer]));

                    // theoretical total available water
                    layerFraction = mySpecies.Max(x => x.LayerFractionWithRoots(layer));
                    layerLL = mySpecies.Min(x => x.SpeciesLL[layer]) * Soil.Thickness[layer];
                    result[layer] = Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - layerLL) * layerFraction;

                    // actual available water
                    result[layer] = Math.Min(result[layer] * facCond * facWcontent, sumWaterAvailable[layer]);
                }
            }

            return result;
        }

        /// <summary>
        /// Does the actual water uptake and send the deltas to soil module
        /// </summary>
        /// <remarks>
        /// The amount of water taken up from each soil layer is set per species
        /// </remarks>
        private void DoSoilWaterUptake()
        {
            PMF.WaterChangedType WaterTakenUp = new PMF.WaterChangedType();
            WaterTakenUp.DeltaWater = new double[Soil.Thickness.Length];

            double uptakeFraction = Math.Min(1.0, SwardWaterDemand / soilAvailableWater.Sum());
            double speciesFraction = 0.0;

            if (useAltWUptake == "no")
            {
                foreach (PastureSpecies myPlant in mySpecies)
                {
                    myPlant.mySoilWaterUptake = new double[Soil.Thickness.Length];

                    // partition between species as function of their demand only
                    speciesFraction = myPlant.WaterDemand / SwardWaterDemand;
                    for (int layer = 0; layer < myPlant.RootFrontier; layer++)
                    {
                        myPlant.mySoilWaterUptake[layer] = soilAvailableWater[layer] * uptakeFraction * speciesFraction;
                        WaterTakenUp.DeltaWater[layer] -= myPlant.mySoilWaterUptake[layer];
                    }
                }
            }
            else
            { // Method implemented by RCichota
                // Uptake is distributed over the profile according to water availability,
                //  this means that water status and root distribution have been taken into account

                double[] adjustedWAvailable;

                double totalWaterAvailableLayer = 0.0;

                foreach (PastureSpecies myPlant in mySpecies)
                {
                    // get adjusted water available
                    adjustedWAvailable = new double[Soil.Thickness.Length];
                    for (int layer = 0; layer < myPlant.RootFrontier; layer++)
                    {
                        totalWaterAvailableLayer = mySpecies.Sum(x => x.SoilAvailableWater[layer]);
                        adjustedWAvailable[layer] = soilAvailableWater[layer] * myPlant.SoilAvailableWater[layer] / totalWaterAvailableLayer;
                    }

                    // get fraction of demand supplied by the soil
                    uptakeFraction = Math.Min(1.0, myPlant.WaterDemand / adjustedWAvailable.Sum());

                    // get the actual amounts taken up from each layer
                    myPlant.mySoilWaterUptake = new double[Soil.Thickness.Length];
                    for (int layer = 0; layer < myPlant.RootFrontier; layer++)
                    {
                        myPlant.mySoilWaterUptake[layer] = adjustedWAvailable[layer] * uptakeFraction;
                        WaterTakenUp.DeltaWater[layer] -= myPlant.mySoilWaterUptake[layer];
                    }
                }
                if (Math.Abs(WaterTakenUp.DeltaWater.Sum() + SwardWaterDemand) > 0.0001)
                    throw new Exception("Error on computing water uptake");
            }

            // aggregate all water taken up
            foreach (PastureSpecies myPlant in mySpecies)
                swardWaterUptake.Zip(myPlant.WaterUptake, (x, y) => x + y);

            // send the delta water taken up
            WaterChanged.Invoke(WaterTakenUp);
        }

        #endregion

        #region - Nitrogen uptake process  ---------------------------------------------------------------------------------

        /// <summary>
        /// Performs the computations for N uptake
        /// </summary>
        private void DoNitrogenCalculations()
        {
            // get N demand for optimum growth (discount minimum N fixation)
            swardNdemand = 0.0;
            foreach (PastureSpecies myPlant in mySpecies)
                swardNdemand += myPlant.RequiredNOptimum * (1 - myPlant.MinimumNFixation);

            // get soil available N
            if (nUptakeSource.ToLower() == "agpasture")
            {
                GetSoilAvailableN();
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    soilAvailableN[layer] = soilNH4Available[layer] + soilNO3Available[layer];

                // get N demand (optimum and luxury)
                GetNDemand();

                // get N fixation
                swardNFixation = CalcNFixation();

                // evaluate the use of N remobilised and any soil demand
                if (NitrogenRequiredLuxury - PlantFixedN > -0.0001)
                { // N demand is fulfilled by fixation alone
                    swardNRemobNewGrowth = 0.0;
                    swardSoilNdemand = 0.0;
                }
                else if (NitrogenRequiredLuxury - (PlantFixedN + swardRemobilisedN) > -0.0001)
                { // N demand is fulfilled by fixation plus remobilisation of senescent
                    swardNRemobNewGrowth = Math.Max(0.0, swardRemobilisedN - (NitrogenRequiredLuxury - PlantFixedN));
                    swardRemobilisedN -= swardNRemobNewGrowth;
                    swardSoilNdemand = 0.0;
                }
                else
                { // N demand is greater than fixation and remobilisation of senescent, N uptake is needed
                    swardNRemobNewGrowth = swardRemobilisedN;
                    swardRemobilisedN = 0.0;
                    swardSoilNdemand = NitrogenRequiredLuxury - (PlantFixedN + swardNRemobNewGrowth);
                }


                // get the amount of N taken up from soil
                swardSoilNUptake = CalcSoilNUptake();
                swardNewGrowthN = PlantFixedN + swardNRemobNewGrowth + swardSoilNUptake;


                // evaluate whether further remobilisation (from luxury N) is needed
                if (swardNewGrowthN - NitrogenRequiredOptimum > -0.0001)
                { // total N available is not enough for optimum growth, check remobilisation of luxury N
                    CalcNLuxuryRemob();
                    swardNewGrowthN += swardNFastRemob3 + swardNFastRemob2;
                }
                //else
                //    there is enough N for at least optimum N content, there is no need for further considerations

            }
            //else
            //    N available is evaluated by the plant species or some other module

            // send delta N to the soil model
            DoSoilNitrogenUptake();
        }

        /// <summary>
        /// Find out the amount of Nitrogen in the soil available to plants for each soil layer
        /// </summary>
        /// <returns>The amount of N in the soil available to plants (kgN/ha)</returns>
        private void GetSoilAvailableN()
        {
            soilNH4Available = new double[Soil.Thickness.Length];
            soilNO3Available = new double[Soil.Thickness.Length];
            double layerFraction = 0.0;   //fraction of soil layer explored by plants
            double nK = 0.0;              //N availability factor
            double totWaterUptake = swardWaterUptake.Sum();
            double facWtaken = 0.0;

            for (int layer = 0; layer <= RootFrontier; layer++)
            {
                if (useAltNUptake == "no")
                {
                    // simple way, all N in the root zone is available
                    layerFraction = mySpecies.Max(x => x.LayerFractionWithRoots(layer));
                    soilNH4Available[layer] = Soil.SoilNitrogen.nh4[layer] * layerFraction;
                    soilNO3Available[layer] = Soil.SoilNitrogen.no3[layer] * layerFraction;
                }
                else
                {
                    // Method implemented by RCichota,
                    // N is available following water uptake and a given 'availability' factor (for each N form)

                    facWtaken = swardWaterUptake[layer] / Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - Soil.SoilWater.ll15_dep[layer]);

                    layerFraction = mySpecies.Max(x => x.LayerFractionWithRoots(layer));
                    nK = mySpecies.Max(x => x.kuNH4);
                    soilNH4Available[layer] = Soil.SoilNitrogen.nh4[layer] * nK * layerFraction;
                    soilNH4Available[layer] *= facWtaken;

                    nK = mySpecies.Max(x => x.kuNO3);
                    soilNO3Available[layer] = Soil.SoilNitrogen.no3[layer] * nK * layerFraction;
                    soilNO3Available[layer] *= facWtaken;
                }
            }
        }

        /// <summary>
        /// Get the N demanded for plant growth (with optimum and luxury uptake) for each species
        /// </summary>
        private void GetNDemand()
        {
            foreach (PastureSpecies myPlant in mySpecies)
                myPlant.CalcNDemand();
            // get N demand for optimum growth (discount minimum N fixation in legumes)
            swardNdemand = 0.0;
            foreach (PastureSpecies myPlant in mySpecies)
                swardNdemand += myPlant.RequiredNOptimum * (1 - myPlant.MinimumNFixation);
        }

        /// <summary>
        /// Computes the amout of N fixed for each species
        /// </summary>
        /// <returns>The total amount of N fixed in the sward</returns>
        private double CalcNFixation()
        {
            foreach (PastureSpecies myPlant in mySpecies)
                myPlant.FixedN = myPlant.CalcNFixation();
            return mySpecies.Sum(x => x.FixedN);
        }

        /// <summary>
        /// Computes the amount of N to be taken up from the soil
        /// </summary>
        /// <returns>The amount of N to be taken up from each soil layer</returns>
        private double CalcSoilNUptake()
        {
            double result;
            if (swardSoilNdemand == 0.0)
            { // No demand, no uptake
                result = 0.0;
            }
            else
            {
                if (soilAvailableN.Sum() >= swardSoilNdemand)
                { // soil can supply all remaining N needed
                    result = swardSoilNdemand;
                }
                else
                { // soil cannot supply all N needed. Get the available N and partition between species
                    result = soilAvailableN.Sum() * swardSoilNdemand;
                }
            }

            return result;
        }

        /// <summary>
        /// Computes the remobilisation of luxury N (from tissues 2 and 3)
        /// </summary>
        private void CalcNLuxuryRemob()
        {
            // plant still needs more N for optimum growth (luxury uptake is ignored), check whether luxury N in plants can be used
            double Nmissing = NitrogenRequiredOptimum - swardNewGrowthN;
            double NLuxury2 = mySpecies.Sum(x => x.RemobLuxuryN2);
            double NLuxury3 = mySpecies.Sum(x => x.RemobLuxuryN3);
            if (Nmissing <= NLuxury2 + NLuxury3)
            {
                // There is luxury N that can be used for optimum growth, first from tissue 3
                if (Nmissing <= NLuxury3)
                {
                    swardNFastRemob3 = Nmissing;
                    swardNFastRemob2 = 0.0;
                    Nmissing = 0.0;
                }
                else
                {
                    // first from tissue 3
                    swardNFastRemob3 = NLuxury3;
                    Nmissing -= NLuxury3;

                    // remaining from tissue 2
                    swardNFastRemob2 = Nmissing;
                    Nmissing = 0.0;
                }
            }
            else
            {
                // N luxury is not enough for optimum growth, use up all there is
                if (NLuxury2 + NLuxury3 > 0)
                {
                    swardNFastRemob3 = NLuxury3;
                    swardNFastRemob2 = NLuxury2;
                    Nmissing -= (NLuxury3 + NLuxury2);
                }
            }
        }

        /// <summary>
        /// Computes the distribution of N uptake over the soil profile and send the delta to soil module
        /// </summary>
        private void DoSoilNitrogenUptake()
        {
            if (NUptakeSource.ToLower() == "agpasture")
            {
                Soils.NitrogenChangedType NTakenUp = new Soils.NitrogenChangedType();
                NTakenUp.Sender = swardName;
                NTakenUp.SenderType = "Plant";
                NTakenUp.DeltaNO3 = new double[Soil.Thickness.Length];
                NTakenUp.DeltaNH4 = new double[Soil.Thickness.Length];

                double uptakeFraction = 0.0;
                if (soilAvailableN.Sum() > 0.0)
                    uptakeFraction = Math.Min(1.0, swardSoilNUptake / soilAvailableN.Sum());
                double speciesFraction = 0.0;

                if (useAltNUptake == "no")
                {
                    foreach (PastureSpecies myPlant in mySpecies)
                    {
                        myPlant.mySoilNUptake = new double[Soil.Thickness.Length];

                        // partition between species as function of their demand only
                        speciesFraction = myPlant.RequiredNOptimum * (1 - myPlant.MinimumNFixation) / swardNdemand;

                        for (int layer = 0; layer < RootFrontier; layer++)
                        {
                            myPlant.mySoilNUptake[layer] = (Soil.SoilNitrogen.nh4[layer] + Soil.SoilNitrogen.no3[layer]) * uptakeFraction * speciesFraction;
                            NTakenUp.DeltaNH4[layer] -= Soil.SoilNitrogen.nh4[layer] * uptakeFraction * speciesFraction;
                            NTakenUp.DeltaNO3[layer] -= Soil.SoilNitrogen.no3[layer] * uptakeFraction * speciesFraction;
                        }
                    }
                }
                else
                { // Method implemented by RCichota,
                    // Uptake is distributed over the profile according to N availability,
                    //  this means that N and water status as well as root distribution have been taken into account

                    double[] adjustedNH4Available;
                    double[] adjustedNO3Available;

                    double totalNH4AvailableLayer = 0.0;
                    double totalNO3AvailableLayer = 0.0;

                    foreach (PastureSpecies myPlant in mySpecies)
                    {
                        // get adjusted N available
                        adjustedNH4Available = new double[Soil.Thickness.Length];
                        adjustedNO3Available = new double[Soil.Thickness.Length];
                        for (int layer = 0; layer < myPlant.RootFrontier; layer++)
                        {
                            totalNH4AvailableLayer = mySpecies.Sum(x => x.SoilAvailableWater[layer]);
                            adjustedNH4Available[layer] = soilNH4Available[layer] * myPlant.mySoilNH4available[layer] / totalNH4AvailableLayer;

                            totalNO3AvailableLayer = mySpecies.Sum(x => x.SoilAvailableWater[layer]);
                            adjustedNO3Available[layer] = soilNO3Available[layer] * myPlant.mySoilNO3available[layer] / totalNO3AvailableLayer;
                        }

                        // get fraction of demand supplied by the soil
                        uptakeFraction = Math.Min(1.0, myPlant.soilNuptake / (adjustedNH4Available.Sum() + adjustedNO3Available.Sum()));

                        // get the actual amounts taken up from each layer
                        myPlant.mySoilNUptake = new double[Soil.Thickness.Length];
                        for (int layer = 0; layer < myPlant.RootFrontier; layer++)
                        {
                            myPlant.mySoilNUptake[layer] = (adjustedNH4Available[layer] + adjustedNO3Available[layer]) * uptakeFraction;
                            NTakenUp.DeltaNH4[layer] -= Soil.SoilNitrogen.nh4[layer] * uptakeFraction;
                            NTakenUp.DeltaNO3[layer] -= Soil.SoilNitrogen.no3[layer] * uptakeFraction;
                        }
                    }
                    //else
                    //{ // N uptake is distributed considering water uptake and N availability
                    //    double[] fNH4Avail = new double[Soil.Thickness.Length];
                    //    double[] fNO3Avail = new double[Soil.Thickness.Length];
                    //    double[] fWUptake = new double[Soil.Thickness.Length];
                    //    double totNH4Available = mySoilAvailableN.Sum();
                    //    double totNO3Available = mySoilAvailableN.Sum();
                    //    double totWuptake = mySoilWaterUptake.Sum();
                    //    for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    //    {
                    //        fNH4Avail[layer] = mySoilAvailableN[layer] / totNH4Available;
                    //        fNO3Avail[layer] = mySoilAvailableN[layer] / totNO3Available;
                    //        fWUptake[layer] = mySoilWaterUptake[layer] / totWuptake;
                    //    }
                    //    double totFacNH4 = fNH4Avail.Sum() + fWUptake.Sum();
                    //    double totFacNO3 = fNO3Avail.Sum() + fWUptake.Sum();
                    //    for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    //    {
                    //        uptakeFraction = (fNH4Avail[layer] + fWUptake[layer]) / totFacNH4;
                    //        NTakeup.DeltaNH4[layer] = -Soil.SoilNitrogen.nh4[layer] * uptakeFraction;

                    //        uptakeFraction = (fNO3Avail[layer] + fWUptake[layer]) / totFacNO3;
                    //        NTakeup.DeltaNO3[layer] = -Soil.SoilNitrogen.no3[layer] * uptakeFraction;
                    //    }
                    //}

                }
                if (Math.Abs(NTakenUp.DeltaNH4.Sum() + NTakenUp.DeltaNO3.Sum() + swardSoilNdemand) > 0.0001)
                    throw new Exception("Error on computing N uptake");

                // do the actual N changes
                NitrogenChanged.Invoke(NTakenUp);
            }
            else
            {
                // N uptake calculated by other modules (e.g., SWIM)
                string msg = "Only one option for N uptake is implemented in AgPasture. Please specify N uptake source as either \"AgPasture\" or \"calc\".";
                throw new Exception(msg);
            }
        }

        #endregion

        #endregion

        #region Other processes  -------------------------------------------------------------------------------------------

        //--- Not supported yet  -----------------------------------------
        [EventSubscribe("Sow")]
        private void OnSow(SowType PSow)
        {
            //isAlive = true;
            //ResetZero();
            //for (int s = 0; s < numSpecies; s++)
            //    mySpecies[s].SetInGermination();
        }

        /// <summary>
        /// Kills all the plant species in the sward (zero variables and set to not alive)
        /// </summary>
        /// <param name="KillData">Fraction of crop to kill (here always 100%)</param>
        [EventSubscribe("KillCrop")]
        private void OnKillCrop(KillCropType KillData)
        {
            for (int s = 0; s < numSpecies; s++)
                mySpecies[s].OnKillCrop(KillData);

            isAlive = false;
        }

        /// <summary>
        /// Harvest (remove DM) the sward
        /// </summary>
        /// <param name="amount">DM amount</param>
        /// <param name="type">How the amount is interpreted (remove or residual)</param>
        public void Harvest(double amount, string type)
        {
            GrazeType GrazeData = new GrazeType();
            GrazeData.amount = amount;
            GrazeData.type = type;
            OnGraze(GrazeData);
        }

        /// <summary>
        /// Graze event, remove DM from sward
        /// </summary>
        /// <param name="GrazeData">How amount of DM to remove is defined</param>
        [EventSubscribe("Graze")]
        private void OnGraze(GrazeType GrazeData)
        {
            if ((!isAlive) || StandingPlantWt == 0)
                return;

            // Get the amount that can potentially be removed
            double amountRemovable = mySpecies.Sum(x => x.HarvestableWt);

            // get the amount required to remove
            double amountRequired = 0.0;
            if (GrazeData.type.ToLower() == "SetResidueAmount".ToLower())
            { // Remove all DM above given residual amount
                amountRequired = Math.Max(0.0, StandingPlantWt - GrazeData.amount);
            }
            else if (GrazeData.type.ToLower() == "SetRemoveAmount".ToLower())
            { // Attempt to remove a given amount
                amountRequired = Math.Max(0.0, GrazeData.amount);
            }
            else
            {
                Console.WriteLine("  AgPasture - Method to set amount to remove not recognized, command will be ignored");
            }
            // get the actual amount to remove
            double amountToRemove = Math.Min(amountRequired, amountRemovable);

            // get the amounts to remove by species:
            if (amountRequired > 0.0)
            {
                // get the weights for each species, consider preference and available DM
                double[] tempWeights = new double[numSpecies];
                double[] tempAmounts = new double[numSpecies];
                double tempTotal = 0.0;
                double totalPreference = mySpecies.Sum(x => x.PreferenceForGreenDM + x.PreferenceForDeadDM);
                for (int s = 0; s < numSpecies; s++)
                {
                    tempWeights[s] = mySpecies[s].PreferenceForGreenDM + mySpecies[s].PreferenceForDeadDM;
                    tempWeights[s] += (totalPreference - tempWeights[s]) * (amountToRemove / amountRemovable);
                    tempAmounts[s] = Math.Max(0.0, mySpecies[s].StandingLiveWt - mySpecies[s].MinimumGreenWt)
                                   + Math.Max(0.0, mySpecies[s].StandingDeadWt - mySpecies[s].MinimumDeadWt);
                    tempTotal += tempAmounts[s] * tempWeights[s];
                }

                // do the actual removal for each species
                for (int s = 0; s < numSpecies; s++)
                {
                    // get the actual fractions to remove for each species
                    if (tempTotal > 0.0)
                        mySpecies[s].HarvestedFraction = Math.Max(0.0, Math.Min(1.0, tempWeights[s] * tempAmounts[s] / tempTotal));
                    else
                        mySpecies[s].HarvestedFraction = 0.0;

                    // remove DM and N for each species (digestibility is also evaluated)
                    mySpecies[s].RemoveDM(amountToRemove * mySpecies[s].HarvestedFraction);
                }
            }
        }

        /// <summary>
        /// Remove biomass from sward
        /// </summary>
        /// <remarks>
        /// Greater details on how much and which parts are removed is given
        /// </remarks>
        /// <param name="RemovalData">Info about what and how much to remove</param>
        [EventSubscribe("RemoveCropBiomass")]
        private void Onremove_crop_biomass(RemoveCropBiomassType RemovalData)
        {
            // NOTE: It is responsability of the calling module to check that the amount of 
            //  herbage in each plant part is correct
            // No checking if the removing amount passed in are too much here

            // ATTENTION: The amounts passed should be in g/m^2

            double fractionToRemove = 0.0;

            for (int i = 0; i < RemovalData.dm.Length; i++)			  // for each pool (green or dead)
            {
                string plantPool = RemovalData.dm[i].pool;
                for (int j = 0; j < RemovalData.dm[i].dlt.Length; j++)   // for each part (leaf or stem)
                {
                    string plantPart = RemovalData.dm[i].part[j];
                    double amountToRemove = RemovalData.dm[i].dlt[j] * 10.0;    // convert to kgDM/ha
                    if (plantPool.ToLower() == "green" && plantPart.ToLower() == "leaf")
                    {
                        for (int s = 0; s < numSpecies; s++)		   //for each species
                        {
                            if (LeafLiveWt - amountToRemove > 0.0)
                            {
                                fractionToRemove = amountToRemove / LeafLiveWt;
                                mySpecies[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                            }
                        }
                    }
                    else if (plantPool.ToLower() == "green" && plantPart.ToLower() == "stem")
                    {
                        for (int s = 0; s < numSpecies; s++)		   //for each species
                        {
                            if (StemLiveWt - amountToRemove > 0.0)
                            {
                                fractionToRemove = amountToRemove / StemLiveWt;
                                mySpecies[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                            }
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "leaf")
                    {
                        for (int s = 0; s < numSpecies; s++)		   //for each species
                        {
                            if (LeafDeadWt - amountToRemove > 0.0)
                            {
                                fractionToRemove = amountToRemove / LeafDeadWt;
                                mySpecies[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                            }
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "stem")
                    {
                        for (int s = 0; s < numSpecies; s++)		   //for each species
                        {
                            if (StemDeadWt - amountToRemove > 0.0)
                            {
                                fractionToRemove = amountToRemove / StemDeadWt;
                                mySpecies[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                            }
                        }
                    }
                }
            }

            // update digestibility and fractionToHarvest
            for (int s = 0; s < numSpecies; s++)
                mySpecies[s].RefreshAfterRemove();
        }

        #endregion

        #region Functions  -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Placeholder for SoilArbitrator
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public List<Soils.UptakeInfo> GetSWUptake(List<Soils.UptakeInfo> info)
        {
            return info;
        }
        
        /// <summary>
        /// Compute the distribution of roots in the soil profile (sum is equal to one)
        /// </summary>
        /// <returns>The proportion of root mass in each soil layer</returns>
        private double[] RootProfileDistribution()
        {
            int nLayers = Soil.Thickness.Length;
            double[] result = new double[nLayers];
            double sumProportion = 0;

            switch (rootDistributionMethod.ToLower())
            {
                case "homogeneous":
                    {
                        // homogenous distribution over soil profile (same root density throughout the profile)
                        double DepthTop = 0;
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            if (DepthTop >= swardRootDepth)
                                result[layer] = 0.0;
                            else if (DepthTop + Soil.Thickness[layer] <= swardRootDepth)
                                result[layer] = 1.0;
                            else
                                result[layer] = (swardRootDepth - DepthTop) / Soil.Thickness[layer];
                            sumProportion += result[layer] * Soil.Thickness[layer];
                            DepthTop += Soil.Thickness[layer];
                        }
                        break;
                    }
                case "userdefined":
                    {
                        // distribution given by the user
                        // Option no longer available
                        break;
                    }
                case "expolinear":
                    {
                        // distribution calculated using ExpoLinear method
                        //  Considers homogeneous distribution from surface down to a fraction of root depth (p_ExpoLinearDepthParam)
                        //   below this depth, the proportion of root decrease following a power function (exponent = p_ExpoLinearCurveParam)
                        //   if exponent is one than the proportion decreases linearly.
                        double DepthTop = 0;
                        double DepthFirstStage = swardRootDepth * expoLinearDepthParam;
                        double DepthSecondStage = swardRootDepth - DepthFirstStage;
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            if (DepthTop >= swardRootDepth)
                                result[layer] = 0.0;
                            else if (DepthTop + Soil.Thickness[layer] <= DepthFirstStage)
                                result[layer] = 1.0;
                            else
                            {
                                if (DepthTop < DepthFirstStage)
                                    result[layer] = (DepthFirstStage - DepthTop) / Soil.Thickness[layer];
                                if ((expoLinearDepthParam < 1.0) && (expoLinearCurveParam > 0.0))
                                {
                                    double thisDepth = Math.Max(0.0, DepthTop - DepthFirstStage);
                                    double Ftop = (thisDepth - DepthSecondStage) * Math.Pow(1 - thisDepth / DepthSecondStage, expoLinearCurveParam) / (expoLinearCurveParam + 1);
                                    thisDepth = Math.Min(DepthTop + Soil.Thickness[layer] - DepthFirstStage, DepthSecondStage);
                                    double Fbottom = (thisDepth - DepthSecondStage) * Math.Pow(1 - thisDepth / DepthSecondStage, expoLinearCurveParam) / (expoLinearCurveParam + 1);
                                    result[layer] += Math.Max(0.0, Fbottom - Ftop) / Soil.Thickness[layer];
                                }
                                else if (DepthTop + Soil.Thickness[layer] <= swardRootDepth)
                                    result[layer] += Math.Min(DepthTop + Soil.Thickness[layer], swardRootDepth) - Math.Max(DepthTop, DepthFirstStage) / Soil.Thickness[layer];
                            }
                            sumProportion += result[layer];
                            DepthTop += Soil.Thickness[layer];
                        }
                        break;
                    }
                default:
                    {
                        throw new Exception("No valid method for computing root distribution was selected");
                    }
            }
            if (sumProportion > 0)
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = result[layer] * Soil.Thickness[layer] / sumProportion;
            else
                throw new Exception("Could not calculate root distribution");
            return result;
        }

        #endregion
    }
}
