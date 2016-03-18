//-----------------------------------------------------------------------
// <copyright file="AgPasture1.Sward.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) ASPIM initiative. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
using Models.PMF;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.AgPasture1
{
	/// <summary>A multi-mySpecies pasture model</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class Sward : Model, ICrop, ICanopy, IUptake
	{
		#region Links, events and delegates  -------------------------------------------------------------------------------

		//- Links  ----------------------------------------------------------------------------------------------------

		/// <summary>Link to APSIM's WeatherFile (meteorological information)</summary>
		[Link]
		private IWeather myMetData = null;

		/// <summary>Link to the Soil (soil layers and other information)</summary>
		[Link]
		private Soils.Soil mySoil = null;

		//- Events  ---------------------------------------------------------------------------------------------------

		/// <summary>Reference to a NewCrop event</summary>
		/// <param name="Data">Data about crop type</param>
		public delegate void NewCropDelegate(PMF.NewCropType Data);

		/// <summary>Event to be invoked when sowing or at initialisation (tell models about existence of this plant).</summary>
		public event EventHandler Sowing;

		/// <summary>Reference to a FOM incorporation event</summary>
		/// <param name="Data">The data with soil FOM to be added.</param>
		public delegate void FOMLayerDelegate(Soils.FOMLayerType Data);

		/// <summary>Occurs when plant is depositing senesced roots.</summary>
		public event FOMLayerDelegate IncorpFOM;

		/// <summary>Reference to a BiomassRemoved event</summary>
		/// <param name="Data">The data about biomass deposited by this plant to the soil surface.</param>
		public delegate void BiomassRemovedDelegate(PMF.BiomassRemovedType Data);

		/// <summary>Occurs when plant is depositing litter.</summary>
		public event BiomassRemovedDelegate BiomassRemoved;

		/// <summary>Reference to a WaterChanged event</summary>
		/// <param name="Data">The changes in the amount of water for each soil layer.</param>
		public delegate void WaterChangedDelegate(PMF.WaterChangedType Data);

		/// <summary>Occurs when plant takes up water.</summary>
		public event WaterChangedDelegate WaterChanged;

		/// <summary>Reference to a NitrogenChanged event</summary>
		/// <param name="Data">The changes in the soil N for each soil layer.</param>
		public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);

		/// <summary>Occurs when the plant takes up soil N.</summary>
		public event NitrogenChangedDelegate NitrogenChanged;

		#endregion

        #region Canopy interface
        /// <summary>Canopy type</summary>
        [Description("Generic type of crop")]
        public string CanopyType
        {
            get { return Name; }
        }

        /// <summary>Albedo.</summary>
        public double Albedo { get { return 0.26; } }

        /// <summary>Gets or sets the gsmax.</summary>
        public double Gsmax { get { return 0.011; } }

        /// <summary>Gets or sets the R50.</summary>
        public double R50 { get { return 200; } }

        /// <summary>Gets the LAI (m^2/m^2)</summary>
        public double LAI { get { return LAIGreen; } }

//        /// <summary>Gets the maximum LAI (m^2/m^2)</summary>
//        public double LAITotal { get { return TotalLAI; } }

//        /// <summary>Gets the cover green (0-1)</summary>
//        public double CoverGreen { get { return GreenCover; } }

//        /// <summary>Gets the cover total (0-1)</summary>
//        public double CoverTotal { get { return TotalCover; } }

        /// <summary>Gets the canopy height (mm)</summary>
        [Description("Sward average height")]
        [Units("mm")]
        public double Height
        {
            get { return Math.Max(20.0, HeightFromMass.Value(AboveGroundWt)); } //speciesInSward.Max(mySpecies => mySpecies.Height); }
        }

        /// <summary>Gets the canopy depth (mm)</summary>
        public double Depth { get { return Height; } }

        //// TODO: have to verify how this works (what exactly is needed by MicroClimate
        /// <summary>Gets the plant growth limiting factor, supplied to another module calculating potential transpiration</summary>
        public double FRGR
        {
            get
            {
                double Tday = 0.75 * myMetData.MaxT + 0.25 * myMetData.MinT;
                double gft;
                if (Tday < 20)
                    gft = Math.Sqrt(GlfTemperature);
                else
                    gft = GlfTemperature;
                // Note: p_gftemp is for gross photosysthsis.
                // This is different from that for net production as used in other APSIM crop models, and is
                // assumesd in calculation of temperature effect on transpiration (in micromet).
                // Here we passed it as sqrt - (Doing so by a comparison of p_gftemp and that
                // used in wheat). Temperature effects on NET produciton of forage mySpecies in other models
                // (e.g., grassgro) are not so significant for T = 10-20 degrees(C)

                //Also, have tested the consequences of passing p_Ncfactor in (different concept for gfwater),
                //coulnd't see any differnece for results
                return Math.Min(FVPD, gft);
                //// RCichota, Jan/2014: removed AgPasture's Frgr from here, it is considered at the same level as nitrogen etc...
            }
        }

        /// <summary>Gets or sets the potential evapotranspiration, as calculated by MicroClimate</summary>
        [XmlIgnore]
        public double PotentialEP
        {
            get
            {
                return mySwardWaterDemand;
            }
            set
            {
                mySwardWaterDemand = value;

                // partition the demand among species
                if (myIsSwardControlled)
                {
                    double spDemand = 0.0;
                    foreach (PastureSpecies mySpecies in mySward)
                    {
                        spDemand = MathUtilities.Divide(mySwardWaterDemand * mySpecies.AboveGrounLivedWt, AboveGroundLiveWt, 0.0);
                        mySpecies.PotentialEP = spDemand;
                    }
                }
            }
        }

        /// <summary>Gets or sets the light profile for this plant, as calculated by MicroClimate</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile
        {
            get
            {
                return myLightProfile;
            }
            set
            {
                // get the total intercepted radiation (sum all canopy layers)
                myInterceptedRadn = 0.0;
                foreach (CanopyEnergyBalanceInterceptionlayerType canopyLayer in value)
                    myInterceptedRadn += canopyLayer.amount;

                // pass on the radiation to each species  -  shouldn't this be partitioned???
                if (myIsSwardControlled)
                    foreach (PastureSpecies mySpecies in mySward)
                        mySpecies.RadnIntercepted = myInterceptedRadn;
                //// TODO: remove this once energy balance by species is running
            }
        }

        #endregion

        #region ICrop implementation  --------------------------------------------------------------------------------------



		/// <summary>Gets a list of cultivar names (not used by AgPasture)</summary>
		public string[] CultivarNames
		{
			get { return null; }
		}

		/// <summary>The intercepted solar radiation</summary>
		private double myInterceptedRadn;

		/// <summary>Light profile (energy available for each canopy layer)</summary>
		private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile = null;

		//// TODO: Have to verify how this works, it seems Microclime needs a sow event, not new crop...
		/// <summary>Invokes the NewCrop event (info about this crop type)</summary>
		private void DoNewCropEvent()
		{
			if (Sowing != null)
				Sowing.Invoke(this, new EventArgs());
		}

		#endregion

		#region Model parameters  ------------------------------------------------------------------------------------------

		/// <summary>Gets the reference to the species present in the sward.</summary>
		/// <value>Pasture species.</value>
		[XmlIgnore]
		public PastureSpecies[] mySward { get; private set; }

		/// <summary>The number of species in the sward</summary>
		private int myNumSpecies = 1;

		/// <summary>Gets or sets the number species in the sward.</summary>
		/// <value>The number of species.</value>
		[XmlIgnore]
		public int NumSpecies
		{
			get { return myNumSpecies; }
			set { myNumSpecies = value; }
		}

		// - Parameters that are set via user interface  ---------------------------------------------------------------

		/// <summary>Flag whether the sward controls the species routines</summary>
		private bool myIsSwardControlled = true;

		/// <summary>Gets or sets whether the sward controls the processes in all species.</summary>
		/// <value>A yes/no value.</value>
		[Description("Is the sward controlling the processes in all pasture species?")]
		public string AgPastureControlled
		{
			get
			{
				if (myIsSwardControlled)
					return "yes";
				else
					return "no";
			}
			set
			{
				myIsSwardControlled = value.ToLower() == "yes";
			}
		}

		/// <summary>Flag for the model controlling the water uptake</summary>
		private string myWaterUptakeSource = "Sward";

		/// <summary>Gets or sets the model controlling the water uptake.</summary>
		/// <value>A flag indicating a valid model ('sward', 'species', or 'apsim').</value>
		[Description("Which model is responsible for water uptake ('sward', pasture 'species', or 'apsim')?")]
		public string WaterUptakeSource
		{
			get { return myWaterUptakeSource; }
			set { myWaterUptakeSource = value; }
		}

		/// <summary>Flag for the model controlling the N uptake</summary>
		private string myNUptakeSource = "Sward";

		/// <summary>Gets or sets the model controlling the N uptake.</summary>
		/// <value>A flag indicating a valid model ('sward', 'species', or 'apsim').</value>
		[Description("Which model is responsible for nitrogen uptake ('sward', pasture 'species', or 'apsim')?")]
		public string NUptakeSource
		{
			get { return myNUptakeSource; }
			set { myNUptakeSource = value; }
		}

		/// <summary>Flag whether the alternative water uptake process is to be used</summary>
		private string myUseAltWUptake = "no";

		/// <summary>Gets or sets whether the alternative water uptake is to be used.</summary>
		/// <value>A yes/no value.</value>
		[Description("Use alternative water uptake process?")]
		public string UseAlternativeWaterUptake
		{
			get { return myUseAltWUptake; }
			set { myUseAltWUptake = value; }
		}

		/// <summary>Flag whether the alternative N uptake process is to be used</summary>
		private string myUseAltNUptake = "no";

		/// <summary>Gets or sets whether the alternative N uptake is to be used.</summary>
		/// <value>A yes/no value.</value>
		[Description("Use alternative N uptake process?")]
		public string UseAlternativeNUptake
		{
			get { return myUseAltNUptake; }
			set { myUseAltNUptake = value; }
		}

		/// <summary>The reference hydraulic conductivity for water uptake</summary>
		private double myReferenceKSuptake = 1000.0;

		/// <summary>Gets or sets the reference soil hydraulic conductivity for water uptake.</summary>
		/// <value>The Ksat value.</value>
		[Description("Test - Reference soil Ksat for optimum water uptake (if using alternative method)")]
		public double ReferenceKSuptake
		{
			get { return myReferenceKSuptake; }
			set { myReferenceKSuptake = value; }
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

		/// <summary>Flag for the root distribution method</summary>
		private string myRootDistributionMethod = "ExpoLinear";

		/// <summary>Gets or sets the root distribution method.</summary>
		/// <value>The root distribution method.</value>
		/// <exception cref="System.Exception">No valid method for computing root distribution was selected</exception>
		[XmlIgnore]
		public string RootDistributionMethod
		{
			get
			{
				return myRootDistributionMethod;
			}
			set
			{
				switch (value.ToLower())
				{
					case "homogenous":
					case "userdefined":
					case "expolinear":
						myRootDistributionMethod = value;
						break;
					default:
						throw new Exception("No valid method for computing root distribution was selected");
				}
			}
		}

		/// <summary>The fraction of root zone where distribution is constant (expo-linear function)</summary>
		private double myExpoLinearDepthParam = 0.1;

		/// <summary>Gets or sets the depth parameter for the expo-linear function.</summary>
		/// <value>The fraction of root zone where distribution is constant.</value>
		[Description("Fraction of root depth where its DM proportion is constant")]
		public double ExpoLinearDepthParam
		{
			get
			{
				return myExpoLinearDepthParam;
			}
			set
			{
				myExpoLinearDepthParam = value;
				if (myExpoLinearDepthParam == 1.0)
					myRootDistributionMethod = "Homogeneous";
			}
		}

		/// <summary>The exponent to determine mass distribution in the soil profile (expo-linear function)</summary>
		private double myExpoLinearCurveParam = 3.0;

		/// <summary>Gets or sets the curve parameter for the expo-linear function.</summary>
		/// <value>The exponent to determine mass distribution in the soil profile.</value>
		[Description("Exponent determining distribution of root DM in the soil profile")]
		public double ExpoLinearCurveParam
		{
			get
			{
				return myExpoLinearCurveParam;
			}
			set
			{
				myExpoLinearCurveParam = value;
				if (myExpoLinearCurveParam == 0.0)
					myRootDistributionMethod = "Homogeneous";	// It is impossible to solve, but its limit is a homogeneous distribution
			}
		}

		/// <summary>Broken stick type function describing how plant height varies with DM</summary>
		[XmlIgnore]
		public BrokenStick HeightFromMass = new BrokenStick
		{
			X = new double[5] { 0, 1000, 2000, 3000, 4000 },
			Y = new double[5] { 0, 25, 75, 150, 250 }
		};

		#endregion

		#region Model outputs  ---------------------------------------------------------------------------------------------

		/// <summary>
		/// Gets a value indicating whether the plant is alive.
		/// </summary>
		public bool IsAlive
		{
			get { return PlantStatus == "alive"; }
		}

		/// <summary>Gets the plant status.</summary>
		/// <value>The plant status (dead, alive, etc).</value>
		[Description("Plant status (dead, alive, etc)")]
		[Units("")]
		public string PlantStatus
		{
			get
			{
				if (mySward.Any(mySpecies => mySpecies.PlantStatus == "alive"))
					return "alive";
				else
					return "out";
			}
		}

		/// <summary>Gets the index for the plant development stage.</summary>
		/// <value>The stage index.</value>
		[Description("Plant development stage number, approximate")]
		[Units("")]
		public int Stage
		{
			// An approximation of the stage number, corresponding to that of other arable crops; for management applications.
			// The highest (oldest) phenostage of any species in the sward is used for this approximation
			get
			{
				if (PlantStatus == "alive")
				{
					if (mySward.Any(mySpecies => mySpecies.Stage == 3))
						return 3;    // 'emergence'
					else
						return 1;    //'sowing or germination';
				}
				else
					return 0;
			}
		}

		/// <summary>Gets the name of the plant development stage.</summary>
		/// <value>The name of the stage.</value>
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

		/// <summary>Gets the total plant C content.</summary>
		/// <value>The plant C content.</value>
		[Description("Total amount of C in plants")]
		[Units("kgDM/ha")]
		public double TotalC
		{
			get { return mySward.Sum(mySpecies => mySpecies.TotalWt * CinDM); }
		}

		/// <summary>Gets the plant total dry matter weight.</summary>
		/// <value>The total DM weight.</value>
		[Description("Total dry matter weight of plants")]
		[Units("kgDM/ha")]
		public double TotalWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.TotalWt); }
		}

		/// <summary>Gets the plant DM weight above ground.</summary>
		/// <value>The above ground DM weight.</value>
		[Description("Total dry matter weight of plants above ground")]
		[Units("kgDM/ha")]
		public double AboveGroundWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.AboveGroundWt); }
		}

		/// <summary>Gets the DM weight of live plant parts above ground.</summary>
		/// <value>The above ground DM weight of live plants.</value>
		[Description("Total dry matter weight of plants alive above ground")]
		[Units("kgDM/ha")]
		public double AboveGroundLiveWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.AboveGrounLivedWt); }
		}

		/// <summary>Gets the DM weight of dead plant parts above ground.</summary>
		/// <value>The above ground dead DM weight.</value>
		[Description("Total dry matter weight of dead plants above ground")]
		[Units("kgDM/ha")]
		public double AboveGroundDeadWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.AboveGroundDeadWt); }
		}

		/// <summary>Gets the DM weight of the plant below ground.</summary>
		/// <value>The below ground DM weight of plant.</value>
		[Description("Total dry matter weight of plants below ground")]
		[Units("kgDM/ha")]
		public double BelowGroundWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.RootWt); }
		}

		/// <summary>Gets the total standing DM weight.</summary>
		/// <value>The DM weight of leaves and stems.</value>
		[Description("Total dry matter weight of standing plants parts")]
		[Units("kgDM/ha")]
		public double StandingWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StandingWt); }
		}

		/// <summary>Gets the DM weight of standing live plant material.</summary>
		/// <value>The DM weight of live leaves and stems.</value>
		[Description("Dry matter weight of live standing plants parts")]
		[Units("kgDM/ha")]
		public double StandingLiveWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StandingLiveWt); }
		}

		/// <summary>Gets the DM weight of standing dead plant material.</summary>
		/// <value>The DM weight of dead leaves and stems.</value>
		[Description("Dry matter weight of dead standing plants parts")]
		[Units("kgDM/ha")]
		public double StandingDeadWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StandingDeadWt); }
		}

		/// <summary>Gets the total DM weight of leaves.</summary>
		/// <value>The leaf DM weight.</value>
		[Description("Total dry matter weight of plant's leaves")]
		[Units("kgDM/ha")]
		public double LeafWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.LeafWt); }
		}

		/// <summary>Gets the DM weight of green leaves.</summary>
		/// <value>The green leaf DM weight.</value>
		[Description("Total dry matter weight of plant's live leaves")]
		[Units("kgDM/ha")]
		public double LeafLiveWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.LeafGreenWt); }
		}

		/// <summary>Gets the DM weight of dead leaves.</summary>
		/// <value>The dead leaf DM weight.</value>
		[Description("Total dry matter weight of plant's dead leaves")]
		[Units("kgDM/ha")]
		public double LeafDeadWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.LeafDeadWt); }
		}

		/// <summary>Gets the total DM weight of stems and sheath.</summary>
		/// <value>The stem DM weight.</value>
		[Description("Total dry matter weight of plant's stems")]
		[Units("kgDM/ha")]
		public double StemWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StemWt); }
		}

		/// <summary>Gets the DM weight of live stems and sheath.</summary>
		/// <value>The live stems DM weight.</value>
		[Description("Total dry matter weight of plant's stems alive")]
		[Units("kgDM/ha")]
		public double StemLiveWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StemGreenWt); }
		}

		/// <summary>Gets the DM weight of dead stems and sheath.</summary>
		/// <value>The dead stems DM weight.</value>
		[Description("Total dry matter weight of plant's stems dead")]
		[Units("kgDM/ha")]
		public double StemDeadWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StemDeadWt); }
		}

		/// <summary>Gets the total DM weight od stolons.</summary>
		/// <value>The stolon DM weight.</value>
		[Description("Total dry matter weight of plant's stolons")]
		[Units("kgDM/ha")]
		public double StolonWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.StolonWt); }
		}

		/// <summary>Gets the total DM weight of roots.</summary>
		/// <value>The root DM weight.</value>
		[Description("Total dry matter weight of plant's roots")]
		[Units("kgDM/ha")]
		public double RootWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.RootWt); }
		}

		#endregion

		#region - C and DM flows  ------------------------------------------------------------------------------------------

		/// <summary>Gets the gross potential growth rate.</summary>
		/// <value>The potential C assimilation, in DM equivalent.</value>
		[Description("Gross potential plant growth (potential C assimilation)")]
		[Units("kgDM/ha")]
		public double GrossPotentialGrowthWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.GrossPotentialGrowthWt); }
		}

		/// <summary>Gets the respiration rate.</summary>
		/// <value>The loss of C due to respiration, in DM equivalent.</value>
		[Description("Respiration rate (DM lost via respiration)")]
		[Units("kgDM/ha")]
		public double RespirationWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.RespirationWt); }
		}

		/// <summary>Gets the remobilisation rate.</summary>
		/// <value>The C remobilised, in DM equivalent.</value>
		[Description("C remobilisation (DM remobilised from old tissue to new growth)")]
		[Units("kgDM/ha")]
		public double RemobilisationWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.RemobilisationWt); }
		}

		/// <summary>Gets the net potential growth rate.</summary>
		/// <value>The potential growth rate after respiration and remobilisation.</value>
		[Description("Net potential plant growth")]
		[Units("kgDM/ha")]
		public double NetPotentialGrowthWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.NetPotentialGrowthWt); }
		}

		/// <summary>Gets the potential growth rate after water stress.</summary>
		/// <value>The potential growth after water stress.</value>
		[Description("Potential growth rate after water stress")]
		[Units("kgDM/ha")]
		public double PotGrowthWt_Wstress
		{
			get { return mySward.Sum(mySpecies => mySpecies.PotGrowthWt_Wstress); }
		}

		/// <summary>Gets the actual growth rate.</summary>
		/// <value>The actual growth rate, after nutrient limitations.</value>
		[Description("Actual plant growth (before littering)")]
		[Units("kgDM/ha")]
		public double ActualGrowthWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.ActualGrowthWt); }
		}

		/// <summary>Gets the effective growth rate.</summary>
		/// <value>The effective growth rate, after senescence.</value>
		[Description("Effective growth rate, after turnover")]
		[Units("kgDM/ha")]
		public double EffectiveGrowthWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.EffectiveGrowthWt); }
		}

		/// <summary>Gets the effective herbage growth rate.</summary>
		/// <value>The herbage growth rate.</value>
		[Description("Effective herbage (shoot) growth")]
		[Units("kgDM/ha")]
		public double HerbageGrowthWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.HerbageGrowthWt); }
		}

		/// <summary>Gets the effective root growth rate.</summary>
		/// <value>The root growth DM weight.</value>
		[Description("Effective root growth rate")]
		[Units("kgDM/ha")]
		public double RootGrowthWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.RootGrowthWt); }
		}

		/// <summary>Gets the litter DM weight deposited onto soil surface.</summary>
		/// <value>The litter DM weight deposited.</value>
		[Description("Dry matter amount of litter deposited onto soil surface")]
		[Units("kgDM/ha")]
		public double LitterDepositionWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.LitterWt); }
		}

		/// <summary>Gets the senesced root DM weight.</summary>
		/// <value>The senesced root DM weight.</value>
		[Description("Dry matter amount of senescent roots added to soil FOM")]
		[Units("kgDM/ha")]
		public double RootSenescenceWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.RootSenescedWt); }
		}

		/// <summary>Gets the gross primary productivity.</summary>
		/// <value>The gross primary productivity.</value>
		[Description("Gross primary productivity")]
		[Units("kgDM/ha")]
		public double GPP
		{
			get { return mySward.Sum(mySpecies => mySpecies.GPP); }
		}

		/// <summary>Gets the net primary productivity.</summary>
		/// <value>The net primary productivity.</value>
		[Description("Net primary productivity")]
		[Units("kgDM/ha")]
		public double NPP
		{
			get { return mySward.Sum(mySpecies => mySpecies.NPP); }
		}

		/// <summary>Gets the net above-ground primary productivity.</summary>
		/// <value>The net above-ground primary productivity.</value>
		[Description("Net above-ground primary productivity")]
		[Units("kgDM/ha")]
		public double NAPP
		{
			get { return mySward.Sum(mySpecies => mySpecies.NAPP); }
		}

		/// <summary>Gets the net below-ground primary productivity.</summary>
		/// <value>The net below-ground primary productivity.</value>
		[Description("Net below-ground primary productivity")]
		[Units("kgDM/ha")]
		public double NBPP
		{
			get { return mySward.Sum(mySpecies => mySpecies.NBPP); }
		}

		#endregion

		#region - N amounts  -----------------------------------------------------------------------------------------------

		/// <summary>Gets the plant total N content.</summary>
		/// <value>The total N content.</value>
		[Description("Total amount of N in plants")]
		[Units("kgN/ha")]
		public double TotalN
		{
			get { return mySward.Sum(mySpecies => mySpecies.TotalN); }
		}

		/// <summary>Gets the N content in the plant above ground.</summary>
		/// <value>The above ground N content.</value>
		[Description("Total amount of N in plants above ground")]
		[Units("kgN/ha")]
		public double AboveGroundN
		{
			get { return mySward.Sum(mySpecies => mySpecies.AboveGroundN); }
		}

		/// <summary>Gets the N content in live plant material above ground.</summary>
		/// <value>The N content above ground of live plants.</value>
		[Description("Total amount of N in plants alive above ground")]
		[Units("kgN/ha")]
		public double AboveGroundLiveN
		{
			get { return mySward.Sum(mySpecies => mySpecies.AboveGroundLiveN); }
		}

		/// <summary>Gets the N content of dead plant material above ground.</summary>
		/// <value>The N content above ground of dead plants.</value>
		[Description("Total amount of N in dead plants above ground")]
		[Units("kgN/ha")]
		public double AboveGroundDeadN
		{
			get { return mySward.Sum(mySpecies => mySpecies.AboveGroundDeadN); }
		}

		/// <summary>Gets the N content of plants below ground.</summary>
		/// <value>The below ground N content.</value>
		[Description("Total amount of N in plants below ground")]
		[Units("kgN/ha")]
		public double BelowGroundN
		{
			get { return mySward.Sum(mySpecies => mySpecies.BelowGroundN); }
		}

		/// <summary>Gets the N content of standing plants.</summary>
		/// <value>The N content of leaves and stems.</value>
		[Description("Total amount of N in standing plants")]
		[Units("kgN/ha")]
		public double StandingN
		{
			get { return mySward.Sum(mySpecies => mySpecies.StandingN); }
		}

		/// <summary>Gets the N content of standing live plant material.</summary>
		/// <value>The N content of live leaves and stems.</value>
		[Description("Total amount of N in standing alive plants")]
		[Units("kgN/ha")]
		public double StandingLiveN
		{
			get { return mySward.Sum(mySpecies => mySpecies.StandingLiveN); }
		}

		/// <summary>Gets the N content  of standing dead plant material.</summary>
		/// <value>The N content of dead leaves and stems.</value>
		[Description("Total amount of N in dead standing plants")]
		[Units("kgN/ha")]
		public double StandingDeadN
		{
			get { return mySward.Sum(mySpecies => mySpecies.StandingDeadN); }
		}

		/// <summary>Gets the total N content of leaves.</summary>
		/// <value>The leaf N content.</value>
		[Description("Total amount of N in the plant's leaves")]
		[Units("kgN/ha")]
		public double LeafN
		{
			get { return mySward.Sum(mySpecies => mySpecies.LeafN); }
		}

		/// <summary>Gets the total N content of stems and sheath.</summary>
		/// <value>The stem N content.</value>
		[Description("Total amount of N in the plant's stems")]
		[Units("kgN/ha")]
		public double StemN
		{
			get { return mySward.Sum(mySpecies => mySpecies.StemN); }
		}

		/// <summary>Gets the total N content of stolons.</summary>
		/// <value>The stolon N content.</value>
		[Description("Total amount of N in the plant's stolons")]
		[Units("kgN/ha")]
		public double StolonN
		{
			get { return mySward.Sum(mySpecies => mySpecies.StolonN); }
		}

		/// <summary>Gets the total N content of roots.</summary>
		/// <value>The roots N content.</value>
		[Description("Total amount of N in the plant's roots")]
		[Units("kgN/ha")]
		public double RootN
		{
			get { return mySward.Sum(mySpecies => mySpecies.RootN); }
		}

		#endregion

		#region - N concentrations  ----------------------------------------------------------------------------------------

		/// <summary>Gets the average N concentration of standing plant material.</summary>
		/// <value>The average N concentration of leaves and stems.</value>
		[Description("Average N concentration of standing plants")]
		[Units("kgN/kgDM")]
		public double StandingNConc
		{
			get { return MathUtilities.Divide(StandingN, StandingWt, 0.0); }
		}

		/// <summary>Gets the average N concentration of leaves.</summary>
		/// <value>The leaf N concentration.</value>
		[Description("Average N concentration of leaves")]
		[Units("kgN/kgDM")]
		public double LeafNConc
		{
			get { return MathUtilities.Divide(LeafN, LeafWt, 0.0); }
		}

		/// <summary>Gets the average N concentration of stems and sheath.</summary>
		/// <value>The stem N concentration.</value>
		[Description("Average N concentration in stems")]
		[Units("kgN/kgDM")]
		public double StemNConc
		{
			get { return MathUtilities.Divide(StemN, StemWt, 0.0); }
		}

		/// <summary>Gets the average N concentration of stolons.</summary>
		/// <value>The stolon N concentration.</value>
		[Description("Average N concentration in stolons")]
		[Units("kgN/kgDM")]
		public double StolonNConc
		{
			get { return MathUtilities.Divide(StolonN, StolonWt, 0.0); }
		}

		/// <summary>Gets the average N concentration of roots.</summary>
		/// <value>The root N concentration.</value>
		[Description("Average N concentration in roots")]
		[Units("kgN/kgDM")]
		public double RootNConc
		{
			get { return MathUtilities.Divide(RootN, RootWt, 0.0); }
		}

		/// <summary>Gets the average N concentration of new grown tissue.</summary>
		/// <value>The N concentration of new grown tissue.</value>
		[Description("Nitrogen concentration in new growth")]
		[Units("kgN/kgDM")]
		public double GrowthNConc
		{
			get { return MathUtilities.Divide(ActualGrowthN, ActualGrowthWt, 0.0); }
		}

		# endregion

		#region - N flows  -------------------------------------------------------------------------------------------------

		/// <summary>Gets the amount of N remobilised from senesced tissue.</summary>
		/// <value>The remobilised N amount.</value>
		[Description("Amount of N remobilised from senescing tissue")]
		[Units("kgN/ha")]
		public double RemobilisedN
		{
			get { return mySward.Sum(mySpecies => mySpecies.RemobilisedN); }
		}

		/// <summary>Gets the amount of luxury N potentially remobilisable.</summary>
		/// <value>The potentially remobilisable luxury N amount.</value>
		[Description("Amount of luxury N potentially remobilisable")]
		[Units("kgN/ha")]
		public double LuxuryNRemobilisable
		{
			get { return mySward.Sum(mySpecies => mySpecies.RemobilisableLuxuryN); }
		}

		/// <summary>Gets the amount of luxury N remobilised.</summary>
		/// <value>The remobilised luxury N amount.</value>
		[Description("Amount of luxury N remobilised")]
		[Units("kgN/ha")]
		public double LuxuryNRemobilised
		{
			get { return mySward.Sum(mySpecies => mySpecies.RemobilisedLuxuryN); }
		}

		/// <summary>Gets the amount of atmospheric N fixed.</summary>
		/// <value>The fixed N amount.</value>
		[Description("Amount of atmospheric N fixed")]
		[Units("kgN/ha")]
		public double FixedN
		{
			get { return mySward.Sum(mySpecies => mySpecies.FixedN); }
		}

		/// <summary>Gets the amount of N required with luxury uptake.</summary>
		/// <value>The required N with luxury.</value>
		[Description("Plant nitrogen requirement with luxury uptake")]
		[Units("kgN/ha")]
		public double NitrogenRequiredLuxury
		{
			get { return mySward.Sum(mySpecies => mySpecies.RequiredLuxuryN); }
		}

		/// <summary>Gets the amount of N required for optimum N content.</summary>
		/// <value>The required optimum N amount.</value>
		[Description("Plant nitrogen requirement for optimum growth")]
		[Units("kgN/ha")]
		public double NitrogenRequiredOptimum
		{
			get { return mySward.Sum(mySpecies => mySpecies.RequiredOptimumN); }
		}

		/// <summary>Gets the amount of N demanded from soil.</summary>
		/// <value>The N demand from soil.</value>
		[Description("Plant nitrogen demand from soil")]
		[Units("kgN/ha")]
		public double NitrogenDemand
		{
			get { return mySward.Sum(mySpecies => mySpecies.DemandSoilN); }
		}

		/// <summary>Gets the amount of plant available N in soil layer.</summary>
		/// <value>The soil available N.</value>
		[Description("Plant available nitrogen in each soil layer")]
		[Units("kgN/ha")]
		public double[] NitrogenAvailable
		{
			get
			{
				if (myIsSwardControlled)
				{
					return mySoilAvailableN;
				}
				else
				{
					double[] result = new double[myNLayers];
					for (int layer = 0; layer < myNLayers; layer++)
						result[layer] = mySward.Sum(mySpecies => mySpecies.SoilAvailableN[layer]);
					return result;
				}
			}
		}

		/// <summary>Gets the amount of N taken up from each soil layer.</summary>
		/// <value>The N taken up from soil.</value>
		[Description("Plant nitrogen uptake from each soil layer")]
		[Units("kgN/ha")]
		public double[] NitrogenUptake
		{
			get
			{
				double[] result = new double[myNLayers];
				for (int layer = 0; layer < myNLayers; layer++)
					result[layer] = mySward.Sum(mySpecies => mySpecies.UptakeN[layer]);
				return result;
			}
		}

		/// <summary>Gets the amount of N deposited as litter onto soil surface.</summary>
		/// <value>The litter N amount.</value>
		[Description("Amount of N deposited as litter onto soil surface")]
		[Units("kgN/ha")]
		public double LitterDepositionN
		{
			get { return mySward.Sum(mySpecies => mySpecies.LitterN); }
		}

		/// <summary>Gets the amount of N from senesced roots added to soil FOM.</summary>
		/// <value>The senesced root N amount.</value>
		[Description("Amount of N added to soil FOM by senescent roots")]
		[Units("kgN/ha")]
		public double RootSenescenceN
		{
			get { return mySward.Sum(mySpecies => mySpecies.SenescedRootN); }
		}

		/// <summary>Gets the amount of N in new grown tissue.</summary>
		/// <value>The actual growth N amount.</value>
		[Description("Nitrogen amount in new growth")]
		[Units("kgN/ha")]
		public double ActualGrowthN
		{
			get { return mySward.Sum(mySpecies => mySpecies.ActualGrowthN); }
		}

		/// <summary>Gets the N concentration in new grown tissue.</summary>
		/// <value>The actual growth N concentration.</value>
		[Description("Nitrogen concentration in new growth")]
		[Units("kgN/kgDM")]
		public double ActualGrowthNConc
		{
			get
			{
				return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.ActualGrowthN),
					mySward.Sum(mySpecies => mySpecies.ActualGrowthWt), 0.0);
			}
		}

		#endregion

		#region - Turnover and DM allocation  ------------------------------------------------------------------------------

		/// <summary>Gets the DM weight allocated to shoot.</summary>
		/// <value>The DM allocated to shoot.</value>
		[Description("Dry matter allocated to shoot")]
		[Units("kgDM/ha")]
		public double DMToShoot
		{
			get { return mySward.Sum(mySpecies => mySpecies.ActualGrowthWt * mySpecies.ShootDMAllocation); }
		}

		/// <summary>Gets the DM weight allocated to roots.</summary>
		/// <value>The DM allocated to roots.</value>
		[Description("Dry matter allocated to roots")]
		[Units("kgDM/ha")]
		public double DMToRoots
		{
			get { return mySward.Sum(mySpecies => mySpecies.ActualGrowthWt * mySpecies.RootDMAllocation); }
		}

		/// <summary>Gets the fraction of new growth allocated to root.</summary>
		/// <value>The fraction allocated to root.</value>
		[Description("Fraction of growth allocated to roots")]
		[Units("0-1")]
		public double FractionGrowthToRoot
		{
			get { return MathUtilities.Divide(DMToRoots, ActualGrowthWt, 0.0); }
		}

		/// <summary>Gets the fraction of new growth allocated to shoot.</summary>
		/// <value>The fraction allocated to shoot.</value>
		[Description("Fraction of growth allocated to shoot")]
		[Units("0-1")]
		public double FractionGrowthToShoot
		{
			get { return MathUtilities.Divide(DMToShoot, ActualGrowthWt, 0.0); }
		}

		#endregion

		#region - LAI and cover  -------------------------------------------------------------------------------------------

		/// <summary>Gets the total plant LAI (leaf area index).</summary>
		/// <value>The total LAI.</value>
		[Description("Total leaf area index")]
		[Units("m^2/m^2")]
		public double LAITotal
		{
			get { return LAIGreen + LAIDead; }
		}

		/// <summary>Gets the plant's green LAI (leaf area index).</summary>
		/// <value>The green LAI.</value>
		[Description("Leaf area index of green leaves")]
		[Units("m^2/m^2")]
		public double LAIGreen
		{
			get { return mySward.Sum(mySpecies => mySpecies.LAIGreen); }
		}

		/// <summary>Gets the plant's dead LAI (leaf area index).</summary>
		/// <value>The dead LAI.</value>
		[Description("Leaf area index of dead leaves")]
		[Units("m^2/m^2")]
		public double LAIDead
		{
			get { return mySward.Sum(mySpecies => mySpecies.LAIDead); }
		}

		/// <summary>Gets the average light extinction coefficient.</summary>
		/// <value>The light extinction coefficient.</value>
		[Description("Average light extinction coefficient")]
		[Units("0-1")]
		public double LightExtCoeff
		{
			get
			{
				// TODO: check whether this should be updated everyday - now it uses the value at the beginning of the simulation only
				//double result = mySward.Sum(mySpecies => mySpecies.LAITotal * mySpecies.LightExtentionCoeff)
				//              / mySward.Sum(mySpecies => mySpecies.LAITotal);

				return myInitialLightExtCoeff;
			}
		}

		/// <summary>Gets the plant's total cover.</summary>
		/// <value>The total cover.</value>
		[Description("Fraction of soil covered by plants")]
		[Units("%")]
		public double CoverTotal
		{
			get
			{
				if (LAITotal == 0) return 0;
				return 1.0 - Math.Exp(-LightExtCoeff * LAITotal);
			}
		}

		/// <summary>Gets the plant's green cover.</summary>
		/// <value>The green cover.</value>
		[Description("Fraction of soil covered by green leaves")]
		[Units("%")]
		public double CoverGreen
		{
			get
			{
				if (LAIGreen == 0)
					return 0.0;
				else
					return 1.0 - Math.Exp(-LightExtCoeff * LAIGreen);
			}
		}

		/// <summary>Gets the plant's dead cover.</summary>
		/// <value>The dead cover.</value>
		[Description("Fraction of soil covered by dead leaves")]
		[Units("%")]
		public double CoverDead
		{
			get
			{
				if (LAIDead == 0)
					return 0.0;
				else
					return 1.0 - Math.Exp(-LightExtCoeff * LAIDead);
			}
		}


		#endregion

		#region - Root depth and distribution  -----------------------------------------------------------------------------

		/// <summary>Gets the root zone depth.</summary>
		/// <value>The root depth.</value>
		[Description("Depth of root zone")]
		[Units("mm")]
		public double RootZoneDepth
		{
			get { return mySward.Max(mySpecies => mySpecies.RootDepth); }
		}

		/// <summary>Gets the root frontier.</summary>
		/// <value>The layer at bottom of root zone.</value>
		[Description("Layer at bottom of root zone")]
		[Units("mm")]
		public double RootFrontier
		{
			get { return mySward.Max(mySpecies => mySpecies.RootFrontier); }
		}

		/// <summary>Gets the fraction of root dry matter for each soil layer.</summary>
		/// <value>The root fraction.</value>
		[Description("Fraction of root dry matter for each soil layer")]
		[Units("0-1")]
		public double[] RootWtFraction
		{
			get
			{
				if (myIsSwardControlled)
				{
					return mySwardRootFraction;
				}
				else
				{
					double[] result = new double[myNLayers];
					if (RootWt > 0.0)
						for (int layer = 0; layer < myNLayers; layer++)
							result[layer] = mySward.Sum(mySpecies => mySpecies.RootWt * mySpecies.RootWtFraction[layer]) / RootWt;
					return result;
				}
			}
		}

		/// <summary>Gets the plant's root length density for each soil layer.</summary>
		/// <value>The root length density.</value>
		[Description("Root length density")]
		[Units("mm/mm^3")]
		public double[] RLD
		{
			get
			{
				double[] result = new double[myNLayers];
				if (myIsSwardControlled)
				{
					double Total_Rlength = RootWt * myAvgSRL;   // m root/ha
					Total_Rlength *= 0.0000001;  // convert into mm root/mm2 soil)
					for (int layer = 0; layer < result.Length; layer++)
					{
						result[layer] = mySwardRootFraction[layer] * Total_Rlength / mySoil.Thickness[layer];    // mm root/mm3 soil
					}
				}
				else
				{
					for (int layer = 0; layer < myNLayers; layer++)
						result[layer] = mySward.Sum(mySpecies => mySpecies.RLD[layer]);
				}
				return result;
			}
		}

		#endregion

		#region - Water amounts  -------------------------------------------------------------------------------------------

		/// <summary>Gets the amount of water demanded by plants.</summary>
		/// <value>The water demand.</value>
		[Description("Plant water demand")]
		[Units("mm")]
		public double WaterDemand
		{
			get { return mySward.Sum(mySpecies => mySpecies.WaterDemand); }
		}

		/// <summary>Gets the amount of soil water available for uptake.</summary>
		/// <value>The soil available water.</value>
		[Description("Plant available water in soil")]
		[Units("mm")]
		public double[] SoilAvailableWater
		{
			get
			{
				if (myIsSwardControlled)
				{
					return mySoilAvailableWater;
				}
				else
				{
					double[] result = new double[myNLayers];
					for (int layer = 0; layer < myNLayers; layer++)
						result[layer] = mySward.Sum(mySpecies => mySpecies.SoilAvailableWater[layer]);
					return result;
				}
			}
		}

		/// <summary>Gets the amount of water taken up by the plants.</summary>
		/// <value>The water uptake.</value>
		[Description("Plant water uptake from soil")]
		[Units("mm")]
		public double[] WaterUptake
		{
			get
			{
				double[] result = new double[myNLayers];
				for (int layer = 0; layer < myNLayers; layer++)
					result[layer] = mySward.Sum(mySpecies => mySpecies.WaterUptake[layer]);
				return result;
			}
		}

		#endregion

		#region - Growth limiting factors  ---------------------------------------------------------------------------------

		/// <summary>Gets the average growth limiting factor due to N availability.</summary>
		/// <value>The growth limiting factor due to N.</value>
		[Description("Average plant growth limiting factor due to nitrogen availability")]
		[Units("0-1")]
		public double GlfN
		{
			get
			{
				double result = 1.0;
				if (PotGrowthWt_Wstress > 0)
					result = MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.GlfN * mySpecies.PotGrowthWt_Wstress), PotGrowthWt_Wstress, 0.0);
				return result;
			}
		}

		/// <summary>Gets the average growth limiting factor due to N concentration in the plant.</summary>
		/// <value>The growth limiting factor due to N concentration.</value>
		[Description("Average plant growth limiting factor due to plant N concentration")]
		[Units("0-1")]
		public double GlfNConcentration
		{
			get { return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.GlfNConcentration * mySpecies.AboveGroundWt), AboveGroundWt, 0.0); }
		}

		/// <summary>Gets the average growth limiting factor due to temperature.</summary>
		/// <value>The growth limiting factor due to temperature.</value>
		[Description("Average plant growth limiting factor due to temperature")]
		[Units("0-1")]
		public double GlfTemperature
		{
			get { return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.GlfTemperature * mySpecies.AboveGrounLivedWt), AboveGroundLiveWt, 0.0); }
		}

		/// <summary>Gets the average growth limiting factor due to water availability.</summary>
		/// <value>The growth limiting factor due to water.</value>
		[Description("Average plant growth limiting factor due to water deficit")]
		[Units("0-1")]
		public double GlfWater
		{
			get
			{
				return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.GlfWater * mySpecies.LAIGreen), LAIGreen, 0.0);
			}
		}

		/// <summary>Gets the average generic growth limiting factor (arbitrary limitation).</summary>
		/// <value>The generic growth limiting factor.</value>
		[Description("Average generic plant growth limiting factor, used for other factors")]
		[Units("0-1")]
		public double GlfGeneric
		{
			get { return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.GlfGeneric * mySpecies.AboveGrounLivedWt), AboveGroundLiveWt, 0.0); }
		}

		//// TODO: verify that this is really needed
		/// <summary>Gets the vapour pressure deficit factor.</summary>
		/// <value>The vapour pressure deficit factor.</value>
		[Description("Effect of vapour pressure on growth (used by micromet)")]
		[Units("0-1")]
		public double FVPD
		{
			get { return mySward[0].FVPD; }
		}

		#endregion

		#region - Harvest variables  ---------------------------------------------------------------------------------------

		/// <summary>Gets the amount of dry matter harvestable (leaf + stem).</summary>
		/// <value>The harvestable DM weight.</value>
		[Description("Total dry matter amount available for removal (leaf+stem)")]
		[Units("kgDM/ha")]
		public double HarvestableWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.HarvestableWt); }
		}

		/// <summary>Gets the amount of dry matter harvested.</summary>
		/// <value>The harvested DM weight.</value>
		[Description("Amount of plant dry matter removed by harvest")]
		[Units("kgDM/ha")]
		public double HarvestedWt
		{
			get { return mySward.Sum(mySpecies => mySpecies.HarvestedWt); }
		}

		/// <summary>Gets the amount of plant N removed by harvest.</summary>
		/// <value>The harvested N amount.</value>
		[Description("Amount of N removed by harvest")]
		[Units("kgN/ha")]
		public double HarvestedN
		{
			get { return mySward.Sum(mySpecies => mySpecies.HarvestedN); }
		}

		/// <summary>Gets the N concentration in harvested DM.</summary>
		/// <value>The N concentration in harvested DM.</value>
		[Description("average N concentration of harvested material")]
		[Units("kgN/kgDM")]
		public double HarvestedNConc
		{
			get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
		}

		/// <summary>Gets the average herbage digestibility.</summary>
		/// <value>The herbage digestibility.</value>
		[Description("Average digestibility of herbage")]
		[Units("0-1")]
		public double HerbageDigestibility
		{
			get { return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.HerbageDigestibility * mySpecies.StandingWt), StandingWt, 0.0); }
		}

		//// TODO: Digestibility of harvested material should be better calculated (consider fraction actually removed)
		/// <summary>Gets the average digestibility of harvested DM.</summary>
		/// <value>The harvested digestibility.</value>
		[Description("Average digestibility of harvested material")]
		[Units("0-1")]
		public double HarvestedDigestibility
		{
			get { return MathUtilities.Divide(mySward.Sum(mySpecies => mySpecies.HarvestedDigestibility * mySpecies.HarvestedWt), HarvestedWt, 0.0); }
		}

		/// <summary>Gets the average herbage ME (metabolisable energy).</summary>
		/// <value>The herbage ME.</value>
		[Description("Average ME of herbage")]
		[Units("(MJ/ha)")]
		public double HerbageME
		{
			get { return 16 * HerbageDigestibility * StandingWt; }
		}

		/// <summary>Gets the average ME (metabolisable energy) of harvested DM.</summary>
		/// <value>The harvested ME.</value>
		[Description("Average ME of harvested material")]
		[Units("(MJ/ha)")]
		public double HarvestedME
		{
			get { return 16 * HarvestedDigestibility * HarvestedWt; }
		}

		#endregion

		#endregion

		#region Private variables  -----------------------------------------------------------------------------------------

		/// <summary>Flag whether the initialisation procedures have been performed</summary>
		private bool myHasInitialised = false;

		/// <summary>Flag whether crop is alive (not killed)</summary>
		private bool myIsAlive = true;

		// -- Root variables  -----------------------------------------------------------------------------------------

		/// <summary>sward root depth (maximum depth of all species)</summary>
		private double mySwardRootDepth;

		/// <summary>average root distribution over the soil profile</summary>
		private double[] mySwardRootFraction;

		/// <summary>average specific root length</summary>
		private double myAvgSRL;

		// -- Water variables  ----------------------------------------------------------------------------------------

		/// <summary>Amount of soil water available to the sward, from each soil layer (mm)</summary>
		private double[] mySoilAvailableWater;

		/// <summary>Daily soil water demand for the whole sward (mm)</summary>
		private double mySwardWaterDemand = 0.0;

		/// <summary>Soil water uptake for the whole sward, from each soil layer (mm)</summary>
		private double[] mySwardWaterUptake;

		// -- Nitrogen variables  -------------------------------------------------------------------------------------

		/// <summary>Amount of soil N available for uptake to the whole sward</summary>
		private double[] mySoilAvailableN;

		/// <summary>Amount of NH4 available for uptake to the whole sward</summary>
		private double[] mySoilNH4Available;

		/// <summary>Amount of NO3 available for uptake to the whole sward</summary>
		private double[] mySoilNO3Available;

		/// <summary>The N fixation amount for the whole sward</summary>
		private double mySwardNFixation = 0.0;

		/// <summary>The basic N demand for the sward (for optimum growth)</summary>
		private double mySwardNdemand = 0.0;

		/// <summary>The soil N demand for the whole sward</summary>
		private double mySwardSoilNdemand = 0.0;

		///// <summary>The N uptake for the whole sward</summary>
		//private double[] swardNUptake;

		/// <summary>The total N uptake for the whole sward</summary>
		private double mySwardSoilNuptake = 0.0;

		/// <summary>The remobilised N in the whole sward</summary>
		private double mySwardRemobilisedN = 0.0;

		/// <summary>The remobilised N to new growth</summary>
		private double mySwardNRemobNewGrowth = 0.0;

		/// <summary>The N amount in new growth, for the whole sward</summary>
		private double mySwardNewGrowthN = 0.0;

		/// <summary>The N remobilised from tissue 2, for the whole sward</summary>
		private double mySwardNFastRemob2 = 0.0;

		/// <summary>The N remobilised from tissue 3, for the whole sward</summary>
		private double mySwardNFastRemob3 = 0.0;

		// - General variables  ---------------------------------------------------------------------------------------

		/// <summary>Number of soil layers</summary>
		private int myNLayers = 0;

		/// <summary>The average light extinction coefficient for the whole sward at the beginning of the simulation</summary>
		private double myInitialLightExtCoeff;

		#endregion

		#region Constants  -------------------------------------------------------------------------------------------------

		/// <summary>Average carbon content in plant dry matter</summary>
		const double CinDM = 0.4;

		/// <summary>Nitrogen to protein conversion factor</summary>
		const double N2Protein = 6.25;

        /// <summary>Maximum difference between two values of double precision in this model</summary>
        const double myEpsilon = 0.000001;

        #endregion

        #region Initialisation methods  ------------------------------------------------------------------------------------

        /// <summary>Called when [loaded].</summary>
        [EventSubscribe("Loaded")]
		private void OnLoaded()
		{
			// get the number and reference to the mySpecies in the sward
			myNumSpecies = Apsim.Children(this, typeof(PastureSpecies)).Count;
			mySward = new PastureSpecies[myNumSpecies];
			int s = 0;
			foreach (PastureSpecies mySpecies in Apsim.Children(this, typeof(PastureSpecies)))
			{
				mySward[s] = mySpecies;
				s += 1;
			}
		}

		/// <summary>Called when [simulation commencing].</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			myHasInitialised = false;
			// This is needed to perform some initialisation set up at the first day of simulation,
			//  it cannot be done here because it needs the mySpecies to be initialised, which is not 
			//  only happen after this...

			foreach (PastureSpecies mySpecies in mySward)
			{
				mySpecies.myIsSwardControlled = myIsSwardControlled;
				mySpecies.myWaterUptakeSource = myWaterUptakeSource;
				mySpecies.myNitrogenUptakeSource = myNUptakeSource;

				if (myIsSwardControlled)
				{
					mySpecies.ExpoLinearDepthParam = myExpoLinearDepthParam;
					mySpecies.ExpoLinearCurveParam = myExpoLinearCurveParam;
				}
			}

			// get the number of layers in the soil profile
			myNLayers = mySoil.Thickness.Length;

			// initialise available N
			mySwardWaterUptake = new double[myNLayers];
			mySoilAvailableN = new double[myNLayers];
		}

		#endregion

		#region Daily processes  -------------------------------------------------------------------------------------------

		/// <summary>EventHandler - preparation before the main process</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/>instance containing the event data.</param>
		[EventSubscribe("DoDailyInitialisation")]
		private void OnDoDailyInitialisation(object sender, EventArgs e)
		{
			if (!myHasInitialised)
			{
				// perform the initialisation of some sward-species variables
				//  (this is suppose to be OnSimulationCommencing, but it should be done here because
				//   values from PastureSpecies are needed and there are initialise after the sward.
				//   once PastureSpecies is running properly the bits here might be unnecessary)
				if (myIsSwardControlled)
				{
					// root depth and distribution (assume a single and invariable root distribution for the whole sward)
					mySwardRootDepth = mySward.Max(mySpecies => mySpecies.RootDepth);
					mySwardRootFraction = RootProfileDistribution();

					// set the light extinction coefficient for each species
					double sumLightExtCoeff = mySward.Sum(mySpecies => mySpecies.LAITotal * mySpecies.LightExtentionCoeff);
					double sumLAI = mySward.Sum(mySpecies => mySpecies.LAITotal);
					myInitialLightExtCoeff = sumLightExtCoeff / sumLAI;
					foreach (PastureSpecies mySpecies in mySward)
						mySpecies.mySwardLightExtCoeff = myInitialLightExtCoeff;
					//// TODO: check whether this should be updated every day

					// tell other modules about the existence of this plant (whole sward)
					DoNewCropEvent();
				}

				// get the average specific root length (should go away with root distribution - done by species)
				myAvgSRL = mySward.Average(mySpecies => mySpecies.SpecificRootLength);

				myHasInitialised = true;
			}

			// Send new canopy event
			//  needed this for at least untill the radiation budget can be done for each species separately
			if (myIsSwardControlled)
			{

				// TODO: this event should be unnecessary once the energy balance for each species is done properly
			}
		}

		/// <summary>Performs the plant growth calculations</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("DoPlantGrowth")]
		private void OnDoPlantGrowth(object sender, EventArgs e)
		{
			if (myIsAlive)
			{
				foreach (PastureSpecies mySpecies in mySward)
				{
					// stores the current state for this mySpecies
					mySpecies.SaveCurrentState();

					// pass on the fraction of radiation and green vover to each species update 
					//  needed this for at least untill the radiation budget can be done for each species separately
					if (myIsSwardControlled)
					{
						// plant green cover
						mySpecies.mySwardGreenCover = CoverGreen;

						// fraction of light intercepted by each species
						mySpecies.myRadnIntFrac = mySpecies.CoverGreen / mySward.Sum(aSpecies => aSpecies.CoverGreen);
					}

					// step 01 - preparation and potential growth
					mySpecies.CalcPotentialGrowth();
				}

				// Water demand, supply, and uptake
				DoWaterCalculations();

				// step 02 - Potential growth after water limitations
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.CalcGrowthWithWaterLimitations();

				// Nitrogen demand, supply, and uptake
				DoNitrogenCalculations();

				foreach (PastureSpecies mySpecies in mySward)
				{
					// step 03 - Actual growth after nutrient limitations, but before senescence
					mySpecies.CalcActualGrowthAndPartition();

					// step 04 - Effective growth after all limitations and senescence
					mySpecies.CalcTurnoverAndEffectiveGrowth();
					mySwardRemobilisedN += mySpecies.RemobilisableN;
				}

				// step 05 - Send amounts of litter and senesced roots to other modules
				DoSurfaceOMReturn(LitterDepositionWt, LitterDepositionN);
				DoIncorpFomEvent(RootSenescenceWt, RootSenescenceN);

				// update/aggregate some variables
				//swardNRemobNewGrowth = mySward.Sum(mySpecies => mySpecies.RemobilisableN);
			}
		}

		#region - Water uptake process  ------------------------------------------------------------------------------------

		/// <summary>Gets the water uptake for each layer as calculated by an external module (SWIM)</summary>
		/// <param name="SoilWater">The soil water.</param>
		[EventSubscribe("WaterUptakesCalculated")]
		private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
		{
			for (int iCrop = 0; iCrop < SoilWater.Uptakes.Length; iCrop++)
			{
				if (SoilWater.Uptakes[iCrop].Name == Name)
				{
					for (int layer = 0; layer < SoilWater.Uptakes[iCrop].Amount.Length; layer++)
						mySwardWaterUptake[layer] = SoilWater.Uptakes[iCrop].Amount[layer];
				}
			}
		}

		/// <summary>Water uptake processes</summary>
		private void DoWaterCalculations()
		{
			// Find out soil available water
			mySoilAvailableWater = GetSoilAvailableWater();

			// Get the water demand for all mySpecies
			if (!myIsSwardControlled)
				mySwardWaterDemand = mySward.Sum(mySpecies => mySpecies.WaterDemand);

			// Do the water uptake (and partition between mySpecies)
			if (myWaterUptakeSource.ToLower() == "sward")
			{
				DoSoilWaterUptake();

				// calc and set the glf water for each species (in reality only one of the factors is different from 1.0)
				// TODO: can get rid of this calc here once the species computations are up an running...
				double waterDeficitFactor = Math.Max(0.0, Math.Min(1.0, MathUtilities.Divide(mySwardWaterUptake.Sum(), mySwardWaterDemand, 0.0)));
				double sWater = 0.0;
				double sSat = 0.0;
				double sDUL = 0.0;
				for (int layer = 0; layer < RootFrontier; layer++)  // TODO this should be <=
				{
					sWater += mySoil.Water[layer];
					sSat += mySoil.SoilWater.SATmm[layer];
					sDUL += mySoil.SoilWater.DULmm[layer];
				}
				double waterLoggingFactor = 1 - Math.Max(0.0, 0.1 * (sWater - sDUL) / (sSat - sDUL));
				double waterLimitingFactor = (waterDeficitFactor < 0.999) ? waterDeficitFactor : waterLoggingFactor;
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.myGLFWater = waterLimitingFactor;
				//mySpecies.glfWater = mySpecies.WaterLimitingFactor() * mySpecies.WaterLoggingFactor();
			}
			else
			{ //Water uptake is done by each species or by another apsim module
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.DoWaterCalculations();
			}
		}

		/// <summary>Finds out the amount soil water available (consider all mySpecies)</summary>
		/// <returns>The amount of water available to plants in each layer</returns>
		private double[] GetSoilAvailableWater()
		{
			double[] result = new double[myNLayers];
			SoilCrop soilCropData = (SoilCrop)mySoil.Crop(Name);
			double layerFraction = 0.0;   //fraction of soil layer explored by plants
			double layerLL = 0.0;         //LL value for a given layer (minimum of all plants)
			if (myUseAltWUptake == "no")
			{
				for (int layer = 0; layer <= RootFrontier; layer++)
				{
					layerFraction = LayerFractionWithRoots(layer);
					result[layer] = Math.Max(0.0, mySoil.Water[layer] - soilCropData.LL[layer] * mySoil.Thickness[layer])
								  * layerFraction;
					result[layer] *= soilCropData.KL[layer];
					//// Note: assumes KL and LL defined for whole sward, ignores the values for each mySpecies
				}
			}
			else
			{ // Method implemented by RCichota
				// Available Water is function of root density, soil water content, and soil hydraulic conductivity
				// See GetSoilAvailableWater method in the Species code for details on calculation of each mySpecies
				// Here it is assumed that the actual water available for each layer is the smaller value between the
				//  total theoretical available water (corrected for water status and conductivity) and the sum of 
				//  available water for all mySpecies

				double facCond = 0.0;
				double facWcontent = 0.0;

				// get sum water available for all mySpecies
				double[] sumWaterAvailable = new double[myNLayers];
				foreach (PastureSpecies plant in mySward)
					sumWaterAvailable.Zip(plant.GetSoilAvailableWater(), (x, y) => x + y);

				for (int layer = 0; layer <= RootFrontier; layer++)
				{
					facCond = 1 - Math.Pow(10, -mySoil.KS[layer] / myReferenceKSuptake);
					facWcontent = 1 - Math.Pow(10,
								-(Math.Max(0.0, mySoil.Water[layer] - mySoil.SoilWater.LL15mm[layer]))
								/ (mySoil.SoilWater.DULmm[layer] - mySoil.SoilWater.LL15mm[layer]));

					// theoretical total available water
					layerFraction = mySward.Max(mySpecies => mySpecies.LayerFractionWithRoots(layer));
					layerLL = mySward.Min(mySpecies => mySpecies.LL[layer]) * mySoil.Thickness[layer];
					result[layer] = Math.Max(0.0, mySoil.Water[layer] - layerLL) * layerFraction;

					// actual available water
					result[layer] = Math.Min(result[layer] * facCond * facWcontent, sumWaterAvailable[layer]);
				}
			}

			return result;
		}

		/// <summary>Does the actual water uptake and send the deltas to soil module</summary>
		/// <exception cref="System.Exception">Error on computing water uptake</exception>
		/// <remarks>The amount of water taken up from each soil layer is set per mySpecies</remarks>
		private void DoSoilWaterUptake()
		{
			PMF.WaterChangedType WaterTakenUp = new PMF.WaterChangedType();
			WaterTakenUp.DeltaWater = new double[myNLayers];

			double uptakeFraction = Math.Min(1.0, mySwardWaterDemand / mySoilAvailableWater.Sum());
			double speciesFraction = 0.0;

			mySwardWaterUptake = new double[myNLayers];

			if (myUseAltWUptake == "no")
			{
				// calc the amount of water to be taken up
				for (int layer = 0; layer <= RootFrontier; layer++)
				{
					mySwardWaterUptake[layer] += mySoilAvailableWater[layer] * uptakeFraction;
					WaterTakenUp.DeltaWater[layer] -= mySwardWaterUptake[layer];
				}

				// partition uptake between species, as function of their demand only
				foreach (PastureSpecies mySpecies in mySward)
				{
					mySpecies.mySoilWaterTakenUp = new double[myNLayers];
					speciesFraction = mySpecies.WaterDemand / mySwardWaterDemand;
					for (int layer = 0; layer <= RootFrontier; layer++)
						mySpecies.mySoilWaterTakenUp[layer] = mySwardWaterUptake[layer] * speciesFraction;
				}
			}
			else
			{ // Method implemented by RCichota
				// Uptake is distributed over the profile according to water availability,
				//  this means that water status and root distribution have been taken into account

				double[] adjustedWAvailable;

				double[] sumWaterAvailable = new double[myNLayers];
				for (int layer = 0; layer < RootFrontier; layer++)
					sumWaterAvailable[layer] = mySward.Sum(mySpecies => mySpecies.SoilAvailableWater[layer]);

				foreach (PastureSpecies mySpecies in mySward)
				{
					// get adjusted water available
					adjustedWAvailable = new double[myNLayers];
					for (int layer = 0; layer < mySpecies.RootFrontier; layer++)
						adjustedWAvailable[layer] = mySoilAvailableWater[layer] * mySpecies.SoilAvailableWater[layer] / sumWaterAvailable[layer];

					// get fraction of demand supplied by the soil
					uptakeFraction = Math.Min(1.0, mySpecies.WaterDemand / adjustedWAvailable.Sum());

					// get the actual amounts taken up from each layer
					mySpecies.mySoilWaterTakenUp = new double[myNLayers];
					for (int layer = 0; layer <= mySpecies.RootFrontier; layer++)
					{
						mySpecies.mySoilWaterTakenUp[layer] = adjustedWAvailable[layer] * uptakeFraction;
						WaterTakenUp.DeltaWater[layer] -= mySpecies.mySoilWaterTakenUp[layer];
					}
				}
				if (Math.Abs(WaterTakenUp.DeltaWater.Sum() + mySwardWaterDemand) > 0.0001)
					throw new Exception("Error on computing water uptake");
			}

			// aggregate all water taken up
			foreach (PastureSpecies mySpecies in mySward)
				mySwardWaterUptake.Zip(mySpecies.WaterUptake, (x, y) => x + y);

			// send the delta water taken up
			WaterChanged.Invoke(WaterTakenUp);
		}

		#endregion

		#region - Nitrogen uptake process  ---------------------------------------------------------------------------------

		/// <summary>Performs the computations for N uptake</summary>
		private void DoNitrogenCalculations()
		{
			if (myNUptakeSource.ToLower() == "sward")
			{
				// get N demand (optimum and luxury)
				GetNDemand();

				// get soil available N
				GetSoilAvailableN();

				// partition N available between species, based on their relative demand (luxury minus minimum fixation)
				double sumDemand = mySward.Sum(mySpecies => mySpecies.RequiredLuxuryN * (1 - mySpecies.MinimumNFixation));
				double speciesFraction = 0.0;
				foreach (PastureSpecies mySpecies in mySward)
				{
					speciesFraction = mySpecies.RequiredLuxuryN * (1 - mySpecies.MinimumNFixation) / sumDemand;
					for (int layer = 0; layer < myNLayers; layer++)
					{
						mySpecies.mySoilAvailableN[layer] = mySoilAvailableN[layer] * speciesFraction;
						mySpecies.mySoilNH4available[layer] = mySoilNH4Available[layer] * speciesFraction;
						mySpecies.mySoilNO3available[layer] = mySoilNO3Available[layer] * speciesFraction;
					}
				}

				// get N fixation
				mySwardNFixation = CalcNFixation();

				// evaluate the use of N remobilised and any soil demand
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.CalcSoilNDemand();
				mySwardRemobilisedN = mySward.Sum(mySpecies => mySpecies.RemobilisableN);
				mySwardNRemobNewGrowth = mySward.Sum(mySpecies => mySpecies.RemobilisedN);
				mySwardSoilNdemand = mySward.Sum(mySpecies => mySpecies.DemandSoilN);

				// get the amount of N taken up from soil
				mySwardSoilNuptake = CalcSoilNUptake();

				// preliminary N budget
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.myNewGrowthN = mySpecies.FixedN + mySpecies.RemobilisedN + mySpecies.mySoilNuptake;
				mySwardNewGrowthN = mySwardNFixation + mySwardNRemobNewGrowth + mySwardSoilNuptake;

				// evaluate whether further remobilisation (from luxury N) is needed
				foreach (PastureSpecies mySpecies in mySward)
				{
					mySpecies.CalcNLuxuryRemob();
					mySpecies.myNewGrowthN += mySpecies.RemobT2LuxuryN + mySpecies.RemobT3LuxuryN;
				}
				mySwardNFastRemob2 = mySward.Sum(mySpecies => mySpecies.RemobT2LuxuryN);
				mySwardNFastRemob3 = mySward.Sum(mySpecies => mySpecies.RemobT3LuxuryN);
				mySwardNewGrowthN += mySwardNFastRemob2 + mySwardNFastRemob3;

				// send delta N to the soil model
				DoSoilNitrogenUptake();

				// get the glfN for each species
				foreach (PastureSpecies mySpecies in mySward)
				{
					if (mySpecies.ActualGrowthN > 0.0)
						mySpecies.myGLFN = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(mySpecies.ActualGrowthN, mySpecies.RequiredOptimumN, 1.0)));
					else
						mySpecies.myGLFN = 1.0;
				}
			}
			else
			{ // N uptake is evaluated by the plant mySpecies or some other module
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.DoNitrogenCalculations();
			}
		}

		/// <summary>
		/// Find out the amount of Nitrogen in the soil available to plants for each soil layer
		/// </summary>
		private void GetSoilAvailableN()
		{
			mySoilNH4Available = new double[myNLayers];
			mySoilNO3Available = new double[myNLayers];
			mySoilAvailableN = new double[myNLayers];
			double layerFraction = 0.0;   //fraction of soil layer explored by plants
			double nK = 0.0;              //N availability factor
			double totWaterUptake = mySwardWaterUptake.Sum();
			double facWtaken = 0.0;

			for (int layer = 0; layer <= RootFrontier; layer++)
			{
				if (myUseAltNUptake == "no")
				{
					// simple way, all N in the root zone is available
					layerFraction = 1.0; //TODO: shold be this: LayerFractionWithRoots(layer);
					mySoilNH4Available[layer] = mySoil.NH4N[layer] * layerFraction;
					mySoilNO3Available[layer] = mySoil.NO3N[layer] * layerFraction;
				}
				else
				{
					// Method implemented by RCichota,
					// N is available following water uptake and a given 'availability' factor (for each N form)

					facWtaken = mySwardWaterUptake[layer] / Math.Max(0.0, mySoil.Water[layer] - mySoil.SoilWater.LL15mm[layer]);

					layerFraction = mySward.Max(mySpecies => mySpecies.LayerFractionWithRoots(layer));
					nK = mySward.Max(mySpecies => mySpecies.kuNH4);
					mySoilNH4Available[layer] = mySoil.NH4N[layer] * nK * layerFraction;
					mySoilNH4Available[layer] *= facWtaken;

					nK = mySward.Max(mySpecies => mySpecies.kuNO3);
					mySoilNO3Available[layer] = mySoil.NO3N[layer] * nK * layerFraction;
					mySoilNO3Available[layer] *= facWtaken;
				}
				mySoilAvailableN[layer] = mySoilNH4Available[layer] + mySoilNO3Available[layer];
			}
		}

		/// <summary>
		/// Get the N demanded for plant growth (with optimum and luxury uptake) for each mySpecies
		/// </summary>
		private void GetNDemand()
		{
			foreach (PastureSpecies mySpecies in mySward)
				mySpecies.CalcNDemand();
			// get N demand for optimum growth (discount minimum N fixation in legumes)
			mySwardNdemand = 0.0;
			foreach (PastureSpecies mySpecies in mySward)
				mySwardNdemand += mySpecies.RequiredOptimumN * (1 - mySpecies.MinimumNFixation);
		}

		/// <summary>Computes the amount of N fixed for each mySpecies</summary>
		/// <returns>The total amount of N fixed in the sward</returns>
		private double CalcNFixation()
		{
			foreach (PastureSpecies mySpecies in mySward)
				mySpecies.myNfixation = mySpecies.CalcNFixation();
			return mySward.Sum(mySpecies => mySpecies.FixedN);
		}

		/// <summary>Computes the amount of N to be taken up from the soil</summary>
		/// <returns>The total amount of N to be actually taken up from the soil</returns>
		private double CalcSoilNUptake()
		{
			double result;
			if (mySwardSoilNdemand == 0.0)
			{ // No demand, no uptake
				result = 0.0;
				foreach (PastureSpecies mySpecies in mySward)
					mySpecies.mySoilNuptake = 0.0;
			}
			else
			{
				if (mySoilAvailableN.Sum() >= mySwardSoilNdemand)
				{ // soil can supply all remaining N needed
					result = mySwardSoilNdemand;
					foreach (PastureSpecies mySpecies in mySward)
						mySpecies.mySoilNuptake = mySpecies.DemandSoilN;
				}
				else
				{ // soil cannot supply all N needed. Get the available N
					result = mySoilAvailableN.Sum();
					// for species, uptake is equal demand adjusted to total uptake
					double uptakeFraction = result / mySwardSoilNdemand;
					foreach (PastureSpecies mySpecies in mySward)
						mySpecies.mySoilNuptake = mySpecies.DemandSoilN * uptakeFraction;
				}
			}

			return result;
		}

		/// <summary>
		/// Computes the distribution of N uptake over the soil profile and send the delta to soil module
		/// </summary>
		/// <exception cref="System.Exception">
		///  Error on computing N uptake
		///  or
		///  N uptake source was not recognised. Please specify it as either \"sward\" or \"species\".
		/// </exception>
		private void DoSoilNitrogenUptake()
		{
			if (myNUptakeSource.ToLower() == "sward")
			{
				if (mySoilAvailableN.Sum() > 0.0 && mySwardSoilNuptake > 0.0)
				{
					// there is N in the soil and there is plant uptake
					Soils.NitrogenChangedType NTakenUp = new Soils.NitrogenChangedType();
					NTakenUp.Sender = Name;
					NTakenUp.SenderType = "Plant";
					NTakenUp.DeltaNO3 = new double[myNLayers];
					NTakenUp.DeltaNH4 = new double[myNLayers];

					double uptakeFraction = Math.Min(1.0, mySwardSoilNuptake / mySoilAvailableN.Sum());
					double speciesFraction = 0.0;

					if (myUseAltNUptake == "no")
					{
						// calc the amount of each N form taken up
						for (int layer = 0; layer <= RootFrontier; layer++)
						{
							NTakenUp.DeltaNH4[layer] = -mySoil.NH4N[layer] * uptakeFraction;
							NTakenUp.DeltaNO3[layer] = -mySoil.NO3N[layer] * uptakeFraction;
						}

						// partition the amount taken up between species, considering amount actually taken up
						foreach (PastureSpecies mySpecies in mySward)
						{
							mySpecies.mySoilNitrogenTakenUp = new double[myNLayers];
							if (mySwardSoilNuptake > 0.0)
							{
								speciesFraction = mySpecies.mySoilNuptake / mySwardSoilNuptake;
								for (int layer = 0; layer <= RootFrontier; layer++)
									mySpecies.mySoilNitrogenTakenUp[layer] = -(NTakenUp.DeltaNH4[layer] + NTakenUp.DeltaNO3[layer]) * speciesFraction;
							}
						}
					}
					else
					{ // Method implemented by RCichota,
						// Uptake is distributed over the profile according to N availability,
						//  this means that N and water status as well as root distribution have been taken into account

						double[] adjustedNH4Available;
						double[] adjustedNO3Available;

						double[] sumNH4Available = new double[myNLayers];
						double[] sumNO3Available = new double[myNLayers];
						for (int layer = 0; layer < RootFrontier; layer++)
						{
							sumNH4Available[layer] = mySward.Sum(mySpecies => mySpecies.SoilAvailableWater[layer]);
							sumNO3Available[layer] = mySward.Sum(mySpecies => mySpecies.SoilAvailableWater[layer]);
						}
						foreach (PastureSpecies mySpecies in mySward)
						{
							// get adjusted N available
							adjustedNH4Available = new double[myNLayers];
							adjustedNO3Available = new double[myNLayers];
							for (int layer = 0; layer <= mySpecies.RootFrontier; layer++)
							{
								adjustedNH4Available[layer] = mySoilNH4Available[layer] * mySpecies.mySoilNH4available[layer] / sumNH4Available[layer];
								adjustedNO3Available[layer] = mySoilNO3Available[layer] * mySpecies.mySoilNO3available[layer] / sumNO3Available[layer];
							}

							// get fraction of demand supplied by the soil
							uptakeFraction = Math.Min(1.0, mySpecies.mySoilNuptake / (adjustedNH4Available.Sum() + adjustedNO3Available.Sum()));

							// get the actual amounts taken up from each layer
							mySpecies.mySoilNitrogenTakenUp = new double[myNLayers];
							for (int layer = 0; layer <= mySpecies.RootFrontier; layer++)
							{
								mySpecies.mySoilNitrogenTakenUp[layer] = (adjustedNH4Available[layer] + adjustedNO3Available[layer]) * uptakeFraction;
								NTakenUp.DeltaNH4[layer] -= mySoil.NH4N[layer] * uptakeFraction;
								NTakenUp.DeltaNO3[layer] -= mySoil.NO3N[layer] * uptakeFraction;
							}
						}
					}
					double totalUptake = mySward.Sum(mySpecies => mySpecies.UptakeN.Sum());
					if ((Math.Abs(mySwardSoilNuptake - totalUptake) > 0.0001) || (Math.Abs(NTakenUp.DeltaNH4.Sum() + NTakenUp.DeltaNO3.Sum() + totalUptake) > 0.0001))
						throw new Exception("Error on computing N uptake");

					// do the actual N changes
					NitrogenChanged.Invoke(NTakenUp);
				}
				else
				{
					// No uptake, just zero out the arrays
					foreach (PastureSpecies mySpecies in mySward)
						mySpecies.mySoilNitrogenTakenUp = new double[myNLayers];
				}
			}
			else
			{
				// N uptake calculated by other modules (e.g., SWIM) - not actually implemented
				string msg = "N uptake source was not recognised. Please specify it as either \"sward\" or \"species\".";
				throw new Exception(msg);
			}
		}

		#endregion

		#endregion

		#region Other processes  -------------------------------------------------------------------------------------------

		//--- Not supported yet  -----------------------------------------        

		/// <summary>Sows the plant</summary>
		/// <param name="cultivar">Cultivar type</param>
		/// <param name="population">Plants per area</param>
		/// <param name="depth">Sowing depth</param>
		/// <param name="rowSpacing">space between rows</param>
		/// <param name="maxCover">maximum ground cover</param>
		/// <param name="budNumber">Number of buds</param>
		public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
		{
			//isAlive = true;
			//ResetZero();
			//for (int s = 0; s < numSpecies; s++)
			//    mySpecies[s].SetInGermination();
		}

        /// <summary>Kills a fraction of each plant in the sward</summary>
        /// <remarks>
        /// This will move DM and N from live to dead pools, 
        /// if killFraction is 1.0 then the crops will be ended
        /// </remarks>
        /// <param name="killFraction">Fraction of crop to kill (0-1)</param>
        [EventSubscribe("KillCrop")]
        public void OnKillCrop(double killFraction)
        {
            foreach (PastureSpecies mySpecies in mySward)
				mySpecies.OnKillCrop(killFraction);

            if (1.0 - killFraction <= myEpsilon)
		        myIsAlive = false;
		}

		/// <summary>Harvest (remove DM) the sward</summary>
		/// <param name="amount">DM amount</param>
		/// <param name="type">How the amount is interpreted (remove or residual)</param>
		public void Harvest(double amount, string type)
		{
			GrazeType GrazeData = new GrazeType();
			GrazeData.amount = amount;
			GrazeData.type = type;
			OnGraze(GrazeData);
		}

		/// <summary>Graze event, remove DM from sward</summary>
		/// <param name="GrazeData">How amount of DM to remove is defined</param>
		[EventSubscribe("Graze")]
		private void OnGraze(GrazeType GrazeData)
		{
			if ((!myIsAlive) || StandingWt == 0)
				return;

			// Get the amount that can potentially be removed
			double amountRemovable = mySward.Sum(mySpecies => mySpecies.HarvestableWt);

			// get the amount required to remove
			double amountRequired = 0.0;
			if (GrazeData.type.ToLower() == "SetResidueAmount".ToLower())
			{ // Remove all DM above given residual amount
				amountRequired = Math.Max(0.0, StandingWt - GrazeData.amount);
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

			// get the amounts to remove by mySpecies:
			if (amountRequired > 0.0)
			{
				// get the weights for each mySpecies, consider preference and available DM
				double[] tempWeights = new double[myNumSpecies];
				double[] tempAmounts = new double[myNumSpecies];
				double tempTotal = 0.0;
				double totalPreference = mySward.Sum(mySpecies => mySpecies.PreferenceForGreenDM + mySpecies.PreferenceForDeadDM);
				for (int s = 0; s < myNumSpecies; s++)
				{
					tempWeights[s] = mySward[s].PreferenceForGreenDM + mySward[s].PreferenceForDeadDM;
					tempWeights[s] += (totalPreference - tempWeights[s]) * (amountToRemove / amountRemovable);
					tempAmounts[s] = Math.Max(0.0, mySward[s].StandingLiveWt - mySward[s].MinimumGreenWt)
								   + Math.Max(0.0, mySward[s].StandingDeadWt - mySward[s].MinimumDeadWt);
					tempTotal += tempAmounts[s] * tempWeights[s];
				}

				// do the actual removal for each mySpecies
				for (int s = 0; s < myNumSpecies; s++)
				{
					// get the actual fractions to remove for each mySpecies
					if (tempTotal > 0.0)
						mySward[s].myFractionHarvested = Math.Max(0.0, Math.Min(1.0, tempWeights[s] * tempAmounts[s] / tempTotal));
					else
						mySward[s].myFractionHarvested = 0.0;

					// remove DM and N for each mySpecies (digestibility is also evaluated)
					mySward[s].RemoveDM(amountToRemove * mySward[s].HarvestedFraction);
				}
			}
		}

		/// <summary>Remove biomass from sward</summary>
		/// <param name="RemovalData">Info about what and how much to remove</param>
		/// <remarks>Greater details on how much and which parts are removed is given</remarks>
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
						for (int s = 0; s < myNumSpecies; s++)		   //for each mySpecies
						{
							if (LeafLiveWt - amountToRemove > 0.0)
							{
								fractionToRemove = amountToRemove / LeafLiveWt;
								mySward[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
							}
						}
					}
					else if (plantPool.ToLower() == "green" && plantPart.ToLower() == "stem")
					{
						for (int s = 0; s < myNumSpecies; s++)		   //for each mySpecies
						{
							if (StemLiveWt - amountToRemove > 0.0)
							{
								fractionToRemove = amountToRemove / StemLiveWt;
								mySward[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
							}
						}
					}
					else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "leaf")
					{
						for (int s = 0; s < myNumSpecies; s++)		   //for each mySpecies
						{
							if (LeafDeadWt - amountToRemove > 0.0)
							{
								fractionToRemove = amountToRemove / LeafDeadWt;
								mySward[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
							}
						}
					}
					else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "stem")
					{
						for (int s = 0; s < myNumSpecies; s++)		   //for each mySpecies
						{
							if (StemDeadWt - amountToRemove > 0.0)
							{
								fractionToRemove = amountToRemove / StemDeadWt;
								mySward[s].RemoveFractionDM(fractionToRemove, plantPool, plantPart);
							}
						}
					}
				}
			}

			// update digestibility and fractionToHarvest
			for (int s = 0; s < myNumSpecies; s++)
				mySward[s].RefreshAfterRemove();
		}

		/// <summary>Return a given amount of DM (and N) to surface organic matter</summary>
		/// <param name="amountDM">DM amount to return</param>
		/// <param name="amountN">N amount to return</param>
		private void DoSurfaceOMReturn(double amountDM, double amountN)
		{
			if (BiomassRemoved != null)
			{
				Single dDM = (Single)amountDM;

				PMF.BiomassRemovedType BR = new PMF.BiomassRemovedType();
				String[] type = new String[] { "grass" };  // TODO: this should be "pasture" ??
				Single[] dltdm = new Single[] { (Single)amountDM };
				Single[] dltn = new Single[] { (Single)amountN };
				Single[] dltp = new Single[] { 0 };         // P not considered here
				Single[] fraction = new Single[] { 1 };     // fraction is always 1.0 here

				BR.crop_type = "grass";   //TODO: this could be the Name, what is the diff between name and type??
				BR.dm_type = type;
				BR.dlt_crop_dm = dltdm;
				BR.dlt_dm_n = dltn;
				BR.dlt_dm_p = dltp;
				BR.fraction_to_residue = fraction;
				BiomassRemoved.Invoke(BR);
			}
		}

		/// <summary>Return senescent roots to fresh organic matter pool in the soil</summary>
		/// <param name="amountDM">DM amount to return</param>
		/// <param name="amountN">N amount to return</param>
		private void DoIncorpFomEvent(double amountDM, double amountN)
		{
			Soils.FOMLayerLayerType[] FOMdataLayer = new Soils.FOMLayerLayerType[myNLayers];

			// ****  RCichota, Jun/2014
			// root senesced are returned to soil (as FOM) considering return is proportional to root mass

			for (int layer = 0; layer < myNLayers; layer++)
			{
				Soils.FOMType fomData = new Soils.FOMType();
				fomData.amount = amountDM * mySwardRootFraction[layer];
				fomData.N = amountN * mySwardRootFraction[layer];
				fomData.C = amountDM * mySwardRootFraction[layer] * CinDM;
				fomData.P = 0.0;			  // P not considered here
				fomData.AshAlk = 0.0;		  // Ash not considered here

				Soils.FOMLayerLayerType layerData = new Soils.FOMLayerLayerType();
				layerData.FOM = fomData;
				layerData.CNR = 0.0;	    // not used here
				layerData.LabileP = 0;      // not used here

				FOMdataLayer[layer] = layerData;
			}

			if (IncorpFOM != null)
			{
				Soils.FOMLayerType FOMData = new Soils.FOMLayerType();
				FOMData.Type = "Pasture";
				FOMData.Layer = FOMdataLayer;
				IncorpFOM.Invoke(FOMData);
			}
		}

		#endregion

		#region Functions  -------------------------------------------------------------------------------------------------

		/// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">some info</param>
		/// <returns>soil info</returns>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
		{
            throw new NotImplementedException();
		}
        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">soilstate</param>
        /// <returns></returns>
        public List<ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            throw new NotImplementedException();
        }

		/// <summary>Set the soil water uptake for today</summary>
		/// <param name="info">Some info</param>
		public void SetSWUptake(List<ZoneWaterAndN> info)
		{
		}
        /// <summary>
        /// Set the n uptake for today
        /// </summary>
        /// <param name="info">Some info</param>
        public void SetNUptake(List<ZoneWaterAndN> info)
        { }    

		/// <summary>Compute the distribution of roots in the soil profile (sum is equal to one)</summary>
		/// <returns>The proportion of root mass in each soil layer</returns>
		/// <exception cref="System.Exception">
		/// No valid method for computing root distribution was selected
		/// or
		/// Could not calculate root distribution
		/// </exception>
		private double[] RootProfileDistribution()
		{
			double[] result = new double[myNLayers];
			double sumProportion = 0;

			switch (myRootDistributionMethod.ToLower())
			{
				case "homogeneous":
					{
						// homogenous distribution over soil profile (same root density throughout the profile)
						double DepthTop = 0;
						for (int layer = 0; layer < myNLayers; layer++)
						{
							if (DepthTop >= mySwardRootDepth)
								result[layer] = 0.0;
							else if (DepthTop + mySoil.Thickness[layer] <= mySwardRootDepth)
								result[layer] = 1.0;
							else
								result[layer] = (mySwardRootDepth - DepthTop) / mySoil.Thickness[layer];
							sumProportion += result[layer] * mySoil.Thickness[layer];
							DepthTop += mySoil.Thickness[layer];
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
						double DepthFirstStage = mySwardRootDepth * myExpoLinearDepthParam;
						double DepthSecondStage = mySwardRootDepth - DepthFirstStage;
						for (int layer = 0; layer < myNLayers; layer++)
						{
							if (DepthTop >= mySwardRootDepth)
								result[layer] = 0.0;
							else if (DepthTop + mySoil.Thickness[layer] <= DepthFirstStage)
								result[layer] = 1.0;
							else
							{
								if (DepthTop < DepthFirstStage)
									result[layer] = (DepthFirstStage - DepthTop) / mySoil.Thickness[layer];
								if ((myExpoLinearDepthParam < 1.0) && (myExpoLinearCurveParam > 0.0))
								{
									double thisDepth = Math.Max(0.0, DepthTop - DepthFirstStage);
									double Ftop = (thisDepth - DepthSecondStage) * Math.Pow(1 - thisDepth / DepthSecondStage, myExpoLinearCurveParam) / (myExpoLinearCurveParam + 1);
									thisDepth = Math.Min(DepthTop + mySoil.Thickness[layer] - DepthFirstStage, DepthSecondStage);
									double Fbottom = (thisDepth - DepthSecondStage) * Math.Pow(1 - thisDepth / DepthSecondStage, myExpoLinearCurveParam) / (myExpoLinearCurveParam + 1);
									result[layer] += Math.Max(0.0, Fbottom - Ftop) / mySoil.Thickness[layer];
								}
								else if (DepthTop + mySoil.Thickness[layer] <= mySwardRootDepth)
									result[layer] += Math.Min(DepthTop + mySoil.Thickness[layer], mySwardRootDepth) - Math.Max(DepthTop, DepthFirstStage) / mySoil.Thickness[layer];
							}
							sumProportion += result[layer];
							DepthTop += mySoil.Thickness[layer];
						}
						break;
					}
				default:
					{
						throw new Exception("No valid method for computing root distribution was selected");
					}
			}
			if (sumProportion > 0)
				for (int layer = 0; layer < myNLayers; layer++)
					result[layer] = result[layer] / sumProportion;
			else
				throw new Exception("Could not calculate root distribution");
			return result;
		}

		/// <summary>
		/// Compute how much of the layer is actually explored by roots (considering depth only)
		/// </summary>
		/// <param name="layer">The index for the layer being considered</param>
		/// <returns>Fraction of the layer in consideration that is explored by roots</returns>
		public double LayerFractionWithRoots(int layer)
		{
			if (layer > RootFrontier)
				return 0.0;
			else
			{
				double depthAtTopThisLayer = 0;   // depth till the top of the layer being considered
				for (int z = 0; z < layer; z++)
					depthAtTopThisLayer += mySoil.Thickness[z];
				double result = (mySwardRootDepth - depthAtTopThisLayer) / mySoil.Thickness[layer];
				return Math.Min(1.0, Math.Max(0.0, result));
			}
		}

		#endregion
	}
}
