//-----------------------------------------------------------------------
// <copyright file="AgPasture1.PastureSpecies.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
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
using Models.Arbitrator;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.AgPasture1
{
	/// <summary>Describes a pasture species</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class PastureSpecies : Model, ICrop, ICrop2, IUptake
	{
		#region Links, events and delegates  -------------------------------------------------------------------------------

		//- Links  ----------------------------------------------------------------------------------------------------

		/// <summary>Link to APSIM's Clock (time information)</summary>
		[Link]
		private Clock Clock = null;

		/// <summary>Link to APSIM's WeatherFile (meteorological information)</summary>
		[Link]
		private IWeather MetData = null;

		/// <summary>Link to the Soil (soil layers and other information)</summary>
		[Link]
		private Soils.Soil Soil = null;

		/// <summary>Link to apsim's Resource Arbitrator module</summary>
		[Link(IsOptional=true)]
		private Arbitrator.Arbitrator apsimArbitrator = null;

		//- Events  ---------------------------------------------------------------------------------------------------

		/// <summary>Reference to a NewCrop event</summary>
		/// <param name="Data">Data about crop type</param>
		public delegate void NewCropDelegate(PMF.NewCropType Data);
		/// <summary>Event to be invoked to tell other models about the existence of this species</summary>
		public event NewCropDelegate NewCrop;

		/// <summary>Event to be invoked when sowing or at initialisation (tell models about existence of this species).</summary>
		public event EventHandler Sowing;

		/// <summary>Reference to a NewCanopy event</summary>
		/// <param name="Data">The data about this plnat's canopy.</param>
		public delegate void NewCanopyDelegate(NewCanopyType Data);
		/// <summary>Event to be invoked at the beginning of everyday.</summary>
		public event NewCanopyDelegate NewCanopy;

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
        public string CanopyType { get { return CropType; } }

        /// <summary>Gets the LAI (m^2/m^2)</summary>
        public double LAI { get { return GreenLAI; } }

        /// <summary>Gets the maximum LAI (m^2/m^2)</summary>
        public double LAITotal { get { return TotalLAI; } }

        /// <summary>Gets the cover green (0-1)</summary>
        public double CoverGreen { get { return GreenCover; } }

        /// <summary>Gets the cover total (0-1)</summary>
        public double CoverTotal { get { return TotalCover; } }

        /// <summary>Gets the canopy height (mm)</summary>
        [Description("Plants average height")]
        [Units("mm")]
        public double Height
        {
            get { return myHeight; }  // minimum = 20mm  - TODO: update this function
        }

        /// <summary>Gets the canopy depth (mm)</summary>
        public double Depth { get { return Height; } }

        // TODO: have to verify how this works (what exactly is needed by MicroClimate
        /// <summary>Plant growth limiting factor, supplied to another module calculating potential transpiration</summary>
        public double FRGR
        { get { return 1.0; } }

        /// <summary>Potential evapotranspiration, as calculated by MicroClimate</summary>
        [XmlIgnore]
        public double PotentialEP
        {
            get
            {
                return myWaterDemand;
            }
            set
            {
                myWaterDemand = value;
                demandWater = myWaterDemand;
            }
        }

		/// <summary>Gets or sets the light profile for this plant, as calculated by MicroClimate</summary>
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
        #endregion

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>
		/// Generic descriptor used by MicroClimate to look up for canopy properties for this plant
		/// </summary>
		[Description("Generic type of crop")]
		[Units("")]
		public string CropType
		{
			get { return Name; }
		}

		/// <summary>Gets a list of cultivar names (not used by AgPasture)</summary>
		public string[] CultivarNames
		{
			get { return null; }
		}



		/// <summary>The intercepted solar radiation</summary>
		public double interceptedRadn;
		/// <summary>Light profile (energy available for each canopy layer)</summary>
		private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;


		/// <summary>Data about this plant's canopy (LAI, height, etc)</summary>
		private NewCanopyType myCanopyData = new NewCanopyType();
		/// <summary>Gets or sets the data about this plant's canopy, used by MicroClimate</summary>
		public NewCanopyType CanopyData
		{
			get
			{
				if (!isSwardControlled)
					return myCanopyData;
				else
					return null;
			}
		}


		// TODO: Have to verify how this works, it seems Microclime needs a sow event, not new crop...
		/// <summary>Invokes the NewCrop event (info about this crop type)</summary>
		private void DoNewCropEvent()
		{
			if (NewCrop != null)
			{
				// Send out New Crop Event to tell other modules who I am and what I am
				PMF.NewCropType EventData = new PMF.NewCropType();
				EventData.crop_type = speciesFamily;
				EventData.sender = Name;
				NewCrop.Invoke(EventData);
			}

			if (Sowing != null)
				Sowing.Invoke(this, new EventArgs());
		}

		/// <summary>Sows the plant</summary>
		/// <param name="cultivar"></param>
		/// <param name="population"></param>
		/// <param name="depth"></param>
		/// <param name="rowSpacing"></param>
		/// <param name="maxCover"></param>
		/// <param name="budNumber"></param>
		public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
		{

		}

		#endregion

		#region ICrop2 implementation  -------------------------------------------------------------------------------------

		///// <summary>
		///// Generic descriptor used by MicroClimate to look up for canopy properties for this plant
		///// </summary>
		//[Description("Generic type of crop")]
		//[Units("")]
		//public string CropType
		//{
		//    get { return speciesFamily; }
		//}

		///// <summary>Gets a list of cultivar names (not used by AgPasture)</summary>
		//public string[] CultivarNames
		//{
		//    get { return null; }
		//}

		/// <summary>Flag whether the plant in the ground</summary>
		[XmlIgnore]
		public bool PlantInGround { get { return true; } }

		/// <summary>Flag whether the plant has emerged</summary>
		[XmlIgnore]
		public bool PlantEmerged { get { return true; } }

		/// <summary>
		/// The set of crop canopy properties used by Arbitrator for light and energy calculations
		/// </summary>
		public CanopyProperties CanopyProperties { get { return myCanopyProperties; } }
		/// <summary>The canopy data for this plant</summary>
		CanopyProperties myCanopyProperties = new CanopyProperties();

		/// <summary>
		/// The set of crop root properties used by Arbitrator for water and nutrient calculations
		/// </summary>
		public RootProperties RootProperties { get { return myRootProperties; } }
		/// <summary>The root data for this plant</summary>
		RootProperties myRootProperties = new RootProperties();

		///// <summary>The intercepted solar radiation</summary>
		//internal double interceptedRadn;
		///// <summary>Light profile (energy available for each canopy layer)</summary>
		//private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;
		///// <summary>Gets or sets the light profile for this plant, as calculated by MicroClimate</summary>
		//[XmlIgnore]
		//public CanopyEnergyBalanceInterceptionlayerType[] LightProfile
		//{
		//    get { return myLightProfile; }
		//    set
		//    {
		//        interceptedRadn = 0.0;
		//        for (int s = 0; s < value.Length; s++)
		//        {
		//            myLightProfile = value;
		//            interceptedRadn += myLightProfile[s].amount;
		//        }
		//    }
		//}

		/// <summary> Water demand for this plant (mm/day)</summary>
		[XmlIgnore]
		public double demandWater { get; set; }

		/// <summary> The actual supply of water to the plant (mm), values given for each soil layer</summary>
		[XmlIgnore]
		public double[] uptakeWater { get; set; }

		/// <summary>Nitrogen demand for this plant (kgN/ha/day)</summary>
		[XmlIgnore]
		public double demandNitrogen { get; set; }

		/// <summary>
		/// The actual supply of nitrogen (nitrate plus ammonium) to the plant (kgN/ha), values given for each soil layer
		/// </summary>
		[XmlIgnore]
		public double[] uptakeNitrogen { get; set; }

		/// <summary>The proportion of nitrogen uptake from each layer in the form of nitrate (0-1)</summary>
		[XmlIgnore]
		public double[] uptakeNitrogenPropNO3 { get; set; }

		# endregion

		#region Model parameters  ------------------------------------------------------------------------------------------

		// NOTE: default parameters describe a generic perennial ryegrass species

		/// <summary>Family type for this plant species (grass/legume/brassica)</summary>
		private string speciesFamily = "Grass";
		/// <summary>Gets or sets the species family type.</summary>
		/// <value>The species family descriptor.</value>
		[Description("Family type for this plant species [grass/legume/brassica]:")]
		public string SpeciesFamily
		{
			get { return speciesFamily; }
			set
			{
				speciesFamily = value;
				isLegume = value.ToLower().Contains("legume");
			}
		}

		/// <summary>Metabolic pathway for C fixation during photosynthesis (C3/C4/CAM)</summary>
		private string photosynthesisPathway = "C3";
		/// <summary>Gets or sets the species photosynthetic pathway.</summary>
		/// <value>The species photo pathway.</value>
		[Description("Metabolic pathway for C fixation during photosynthesis [C3/C4/CAM]:")]
		public string SpeciesPhotoPathway
		{
			get { return photosynthesisPathway; }
			set { photosynthesisPathway = value; }
		}

		/// <summary>The initial DM amount above ground (shoot)</summary>
		private double iniDMShoot = 2000.0;
		/// <summary>Gets or sets the initial shoot DM.</summary>
		/// <value>The initial DM amount.</value>
		[Description("Initial above ground DM (leaf, stem, stolon, etc) [kg DM/ha]:")]
		[Units("kg/ha")]
		public double InitialDMShoot
		{
			get { return iniDMShoot; }
			set { iniDMShoot = value; }
		}

		/// <summary>The ini dm root</summary>
		private double iniDMRoot = 500.0;
		/// <summary>Gets or sets the initial dm root.</summary>
		/// <value>The initial dm root.</value>
		[Description("Initial below ground DM (roots) [kg DM/ha]:")]
		[Units("kg/ha")]
		public double InitialDMRoot
		{
			get { return iniDMRoot; }
			set { iniDMRoot = value; }
		}

		/// <summary>The ini root depth</summary>
		private double iniRootDepth = 750.0;
		/// <summary>Gets or sets the initial root depth.</summary>
		/// <value>The initial root depth.</value>
		[Description("Initial depth for roots [mm]:")]
		[Units("mm")]
		public double InitialRootDepth
		{
			get { return iniRootDepth; }
			set { iniRootDepth = value; }
		}

		// temporary?? initial DM fractions for grass or legume species
		/// <summary>The initial fractions of DM for grass</summary>
		private double[] initialDMFractions_grass = new double[] { 0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00 };
		/// <summary>The initial fractions of DM for legume</summary>
		private double[] initialDMFractions_legume = new double[] { 0.20, 0.25, 0.25, 0.00, 0.02, 0.04, 0.04, 0.00, 0.06, 0.12, 0.12 };

		/// <summary>
		/// Initial DM fractions for each plant tissue (in order: leaf1, leaf2, leaf3, leaf4, stem1, stem2, stem3, stem4, stolon1, stolon2, stolon3)
		/// </summary>
		private double[] iniDMFraction;
		/// <summary>Gets or sets the initial dm fractions.</summary>
		/// <value>The initial dm fractions.</value>
		[XmlIgnore]
		[Units("0-1")]
		public double[] initialDMFractions
		{
			get { return iniDMFraction; }
			set
			{
				//make sure we have te right number of values
				Array.Resize(ref value, 12);
				iniDMFraction = new double[12];
				for (int i = 0; i < 12; i++)
					iniDMFraction[i] = value[i];
			}
		}

		// - Growth and photosysnthesis  ------------------------------------------------------------------------------

		/// <summary>Reference CO2 assimilation rate during photosynthesis [mg CO2/m2 leaf/s]</summary>
		private double referencePhotosynthesisRate = 1.0;
		/// <summary>Reference CO2 assimilation rate during photosynthesis [mg CO2/m2 leaf/s]</summary>
		/// <value>The reference photosynthesis rate.</value>
		[Description("Reference CO2 assimilation rate during photosynthesis [mg CO2/m2/s]:")]
		[Units("mg/m^2/s")]
		public double ReferencePhotosynthesisRate
		{
			get { return referencePhotosynthesisRate; }
			set { referencePhotosynthesisRate = value; }
		}

		/// <summary>
		/// Maintenance respiration coefficient - Fraction of DM consumed by respiration [0-1]
		/// </summary>
		private double maintenanceRespirationCoef = 0.03;
		/// <summary>
		/// Maintenance respiration coefficient - Fraction of DM consumed by respiration [0-1]
		/// </summary>
		/// <value>The maintenance respiration coefficient.</value>
		[Description("Maintenance respiration coefficient [0-1]:")]
		[Units("0-1")]
		public double MaintenanceRespirationCoefficient
		{
			get { return maintenanceRespirationCoef; }
			set { maintenanceRespirationCoef = value; }
		}

		/// <summary>
		/// Growth respiration coefficient - fraction of photosynthesis CO2 not assimilated [0-1]
		/// </summary>
		private double growthRespirationCoef = 0.25;
		/// <summary>
		/// Growth respiration coefficient - fraction of photosynthesis CO2 not assimilated (0-1)
		/// </summary>
		/// <value>The growth respiration coefficient.</value>
		[Description("Growth respiration coefficient [0-1]:")]
		[Units("0-1")]
		public double GrowthRespirationCoefficient
		{
			get { return growthRespirationCoef; }
			set { growthRespirationCoef = value; }
		}

		/// <summary>Light extinction coefficient [0-1]</summary>
		private double lightExtentionCoeff = 0.5;
		/// <summary>Light extinction coefficient (0-1)</summary>
		/// <value>The light extention coeff.</value>
		[Description("Light extinction coefficient [0-1]:")]
		[Units("0-1")]
		public double LightExtentionCoeff
		{
			get { return lightExtentionCoeff; }
			set { lightExtentionCoeff = value; }
		}

		/// <summary>Minimum temperature for growth [oC]</summary>
		private double growthTmin = 2.0;
		/// <summary>Minimum temperature for growth [oC]</summary>
		/// <value>The growth tmin.</value>
		[Description("Minimum temperature for growth [oC]:")]
		[Units("oC")]
		public double GrowthTmin
		{
			get { return growthTmin; }
			set { growthTmin = value; }
		}

		/// <summary>Maximum temperature for growth [oC]</summary>
		private double growthTmax = 32.0;
		/// <summary>Maximum temperature for growth [oC]</summary>
		/// <value>The growth tmax.</value>
		[Description("Maximum temperature for growth [oC]:")]
		[Units("oC")]
		public double GrowthTmax
		{
			get { return growthTmax; }
			set { growthTmax = value; }
		}

		/// <summary>Optimum temperature for growth [oC]</summary>
		private double growthTopt = 20.0;
		/// <summary>Optimum temperature for growth [oC]</summary>
		/// <value>The growth topt.</value>
		[Description("Optimum temperature for growth [oC]:")]
		[Units("oC")]
		public double GrowthTopt
		{
			get { return growthTopt; }
			set { growthTopt = value; }
		}

		/// <summary>Curve parameter for growth response to temperature</summary>
		private double growthTq = 1.75;
		/// <summary>Curve parameter for growth response to temperature</summary>
		/// <value>The growth tq.</value>
		[Description("Curve parameter for growth response to temperature:")]
		[Units("-")]
		public double GrowthTq
		{
			get { return growthTq; }
			set { growthTq = value; }
		}

		/// <summary>Onset temperature for heat effects on growth [oC]</summary>
		private double heatOnsetT = 28.0;
		/// <summary>Onset temperature for heat effects on growth [oC]</summary>
		/// <value>The heat onset t.</value>
		[Description("Onset temperature for heat effects on growth [oC]:")]
		[Units("oC")]
		public double HeatOnsetT
		{
			get { return heatOnsetT; }
			set { heatOnsetT = value; }
		}

		/// <summary>Temperature for full heat effect on growth (no growth) [oC]</summary>
		private double heatFullT = 35.0;
		/// <summary>Temperature for full heat effect on growth (no growth) [oC]</summary>
		/// <value>The heat full t.</value>
		[Description("Temperature for full heat effect on growth [oC]:")]
		[Units("oC")]
		public double HeatFullT
		{
			get { return heatFullT; }
			set { heatFullT = value; }
		}

		/// <summary>Cumulative degrees for recovery from heat stress [oC]</summary>
		private double heatSumT = 30.0;
		/// <summary>Cumulative degrees for recovery from heat stress [oC]</summary>
		/// <value>The heat sum t.</value>
		[Description("Cumulative degrees for recovery from heat stress [oC]:")]
		[Units("oC")]
		public double HeatSumT
		{
			get { return heatSumT; }
			set { heatSumT = value; }
		}

		/// <summary>Reference temperature for recovery from heat stress [oC]</summary>
		private double referenceT4Heat = 25.0;
		/// <summary>Reference temperature for recovery from heat stress [oC]</summary>
		/// <value>The reference t4 heat.</value>
		[Description("Reference temperature for recovery from heat stress [oC]:")]
		[Units("oC")]
		public double ReferenceT4Heat
		{
			get { return referenceT4Heat; }
			set { referenceT4Heat = value; }
		}

		/// <summary>Onset temperature for cold effects on growth [oC]</summary>
		private double coldOnsetT = 0.0;
		/// <summary>Onset temperature for cold effects on growth [oC]</summary>
		/// <value>The cold onset t.</value>
		[Description("Onset temperature for cold effects on growth [oC]:")]
		[Units("oC")]
		public double ColdOnsetT
		{
			get { return coldOnsetT; }
			set { coldOnsetT = value; }
		}

		/// <summary>Temperature for full cold effect on growth (no growth) [oC]</summary>
		private double coldFullT = -3.0;
		/// <summary>Temperature for full cold effect on growth (no growth) [oC]</summary>
		/// <value>The cold full t.</value>
		[Description("Temperature for full cold effect on growth [oC]:")]
		[Units("oC")]
		public double ColdFullT
		{
			get { return coldFullT; }
			set { coldFullT = value; }
		}

		/// <summary>Cumulative degrees for recovery from cold stress [oC]</summary>
		private double coldSumT = 20.0;
		/// <summary>Cumulative degrees for recovery from cold stress [oC]</summary>
		/// <value>The cold sum t.</value>
		[Description("Cumulative degrees for recovery from cold stress [oC]:")]
		[Units("oC")]
		public double ColdSumT
		{
			get { return coldSumT; }
			set { coldSumT = value; }
		}

		/// <summary>Reference temperature for recovery from cold stress [oC]</summary>
		private double referenceT4Cold = 0.0;
		/// <summary>Reference temperature for recovery from cold stress [oC]</summary>
		/// <value>The reference t4 cold.</value>
		[Description("Reference temperature for recovery from cold stress [oC]:")]
		[Units("oC")]
		public double ReferenceT4Cold
		{
			get { return referenceT4Cold; }
			set { referenceT4Cold = value; }
		}

		/// <summary>Specific leaf area [m^2/kg DM]</summary>
		private double specificLeafArea = 20.0;
		/// <summary>Specific leaf area [m^2/kg DM]</summary>
		/// <value>The specific leaf area.</value>
		[Description("Specific leaf area [m^2/kg DM]:")]
		[Units("m^2/kg")]
		public double SpecificLeafArea
		{
			get { return specificLeafArea; }
			set { specificLeafArea = value; }
		}

		/// <summary>Specific root length [m/g DM]</summary>
		private double specificRootLength = 75.0;
		/// <summary>Specific root length [m/g DM]</summary>
		/// <value>The length of the specific root.</value>
		[Description("Specific root length [m/g DM]:")]
		[Units("m/g")]
		public double SpecificRootLength
		{
			get { return specificRootLength; }
			set { specificRootLength = value; }
		}

		/// <summary>Maximum fraction of DM allocated to roots (from daily growth) [0-1]</summary>
		private double maxRootFraction = 0.25;
		/// <summary>Maximum fraction of DM allocated to roots (from daily growth) [0-1]</summary>
		/// <value>The maximum root fraction.</value>
		[Description("Maximum fraction of DM allocated to roots (from daily growth) [0-1]:")]
		[Units("0-1")]
		public double MaxRootFraction
		{
			get { return maxRootFraction; }
			set { maxRootFraction = value; }
		}

		/// <summary>Factor by which DM allocation to shoot is increased during 'spring'[0-1]</summary>
		private double shootSeasonalAllocationIncrease = 0.8;
		/// <summary>Factor by which DM allocation to shoot is increased during 'spring' [0-1]</summary>
		/// <value>The shoot seasonal allocation increase.</value>
		/// <remarks>
		/// Allocation to shoot is typically given by 1-maxRootFraction, but for a certain 'spring' period it can be increased to simulate reproductive growth
		/// at this period shoot allocation is corrected by multiplying it by 1 + SeasonShootAllocationIncrease
		/// </remarks>
		[Description("Factor by which DM allocation to shoot is increased during 'spring' [0-1]:")]
		[Units("0-1")]
		public double ShootSeasonalAllocationIncrease
		{
			get { return shootSeasonalAllocationIncrease; }
			set { shootSeasonalAllocationIncrease = value; }
		}

		/// <summary>Day for the beginning of the period with higher shoot allocation ('spring')</summary>
		private int doyIniHighShoot = 232;
		/// <summary>Day for the beginning of the period with higher shoot allocation ('spring')</summary>
		/// <value>The day initialize higher shoot allocation.</value>
		/// <remarks>Care must be taken as this varies with north or south hemisphere</remarks>
		[Description("Day for the beginning of the period with higher shoot allocation ('spring'):")]
		[Units("-")]
		public int DayInitHigherShootAllocation
		{
			get { return doyIniHighShoot; }
			set { doyIniHighShoot = value; }
		}

		/// <summary>
		/// Number of days defining the duration of the three phases with higher DM allocation to shoot (onset, sill, return)
		/// </summary>
		private int[] higherShootAllocationPeriods = new int[] { 35, 60, 30 };
		/// <summary>
		/// Number of days defining the duration of the three phases with higher DM allocation to shoot (onset, sill, return)
		/// </summary>
		/// <value>The higher shoot allocation periods.</value>
		/// <remarks>
		/// Three numbers are needed, they define the duration of the phases for increase, plateau, and the deacrease in allocation
		/// The allocation to shoot is maximum at the plateau phase, it is 1 + SeasonShootAllocationIncrease times the value of maxSRratio
		/// </remarks>
		[Description("Duration of the three phases of higher DM allocation to shoot [days]:")]
		[Units("days")]
		public int[] HigherShootAllocationPeriods
		{
			get { return higherShootAllocationPeriods; }
			set
			{
				for (int i = 0; i < 3; i++)
					higherShootAllocationPeriods[i] = value[i];
				// so, if 1 or 2 values are supplied the remainder are not changed, if more values are given, they are ignored
			}
		}

		/// <summary>Fraction of new shoot growth allocated to leaves [0-1]</summary>
		private double fracToLeaf = 0.7;
		/// <summary>Fraction of new shoot growth allocated to leaves [0-1]</summary>
		/// <value>The frac to leaf.</value>
		[Description("Fraction of new shoot growth allocated to leaves [0-1]:")]
		[Units("0-1")]
		public double FracToLeaf
		{
			get { return fracToLeaf; }
			set { fracToLeaf = value; }
		}

		/// <summary>Fraction of new shoot growth allocated to stolons [0-1]</summary>
		private double fracToStolon = 0.0;
		/// <summary>Fraction of new shoot growth allocated to stolons [0-1]</summary>
		/// <value>The frac to stolon.</value>
		[Description("Fraction of new shoot growth allocated to stolons [0-1]:")]
		[Units("0-1")]
		public double FracToStolon
		{
			get { return fracToStolon; }
			set { fracToStolon = value; }
		}

		// Turnover rate  ---------------------------------------------------------------------------------------------

		/// <summary>Daily turnover rate for DM live to dead [0-1]</summary>
		private double turnoverRateLive2Dead = 0.025;
		/// <summary>Daily turnover rate for DM live to dead [0-1]</summary>
		/// <value>The turnover rate live2 dead.</value>
		[Description("Daily turnover rate for DM live to dead [0-1]:")]
		[Units("0-1")]
		public double TurnoverRateLive2Dead
		{
			get { return turnoverRateLive2Dead; }
			set { turnoverRateLive2Dead = value; }
		}

		/// <summary>Daily turnover rate for DM dead to litter [0-1]</summary>
		private double turnoverRateDead2Litter = 0.11;
		/// <summary>Daily turnover rate for DM dead to litter [0-1]</summary>
		/// <value>The turnover rate dead2 litter.</value>
		[Description("Daily turnover rate for DM dead to litter [0-1]:")]
		[Units("0-1")]
		public double TurnoverRateDead2Litter
		{
			get { return turnoverRateDead2Litter; }
			set { turnoverRateDead2Litter = value; }
		}

		/// <summary>Daily turnover rate for root senescence [0-1]</summary>
		private double turnoverRateRootSenescence = 0.02;
		/// <summary>Daily turnover rate for root senescence [0-1]</summary>
		/// <value>The turnover rate root senescence.</value>
		[Description("Daily turnover rate for root senescence [0-1]")]
		[Units("0-1")]
		public double TurnoverRateRootSenescence
		{
			get { return turnoverRateRootSenescence; }
			set { turnoverRateRootSenescence = value; }
		}

		/// <summary>Minimum temperature for tissue turnover [oC]</summary>
		private double tissueTurnoverTmin = 2.0;
		/// <summary>Minimum temperature for tissue turnover [oC]</summary>
		/// <value>The tissue turnover tmin.</value>
		[Description("Minimum temperature for tissue turnover [oC]:")]
		[Units("oC")]
		public double TissueTurnoverTmin
		{
			get { return tissueTurnoverTmin; }
			set { tissueTurnoverTmin = value; }
		}

		/// <summary>Optimum temperature for tissue turnover [oC]</summary>
		private double tissueTurnoverTopt = 20.0;
		/// <summary>Optimum temperature for tissue turnover [oC]</summary>
		/// <value>The tissue turnover topt.</value>
		[Description("Optimum temperature for tissue turnover [oC]:")]
		[Units("oC")]
		public double TissueTurnoverTopt
		{
			get { return tissueTurnoverTopt; }
			set { tissueTurnoverTopt = value; }
		}

		/// <summary>Maximum increase in tissue turnover due to water stress</summary>
		private double tissueTurnoverWFactorMax = 2.0;
		/// <summary>Maximum increase in tissue turnover due to water stress</summary>
		/// <value>The tissue turnover w factor maximum.</value>
		[Description("Maximum increase in tissue turnover due to water stress:")]
		[Units("-")]
		public double TissueTurnoverWFactorMax
		{
			get { return tissueTurnoverWFactorMax; }
			set { tissueTurnoverWFactorMax = value; }
		}

		/// <summary>
		/// Optimum value GLFwater for tissue turnover [0-1] - below this value tissue turnover increases
		/// </summary>
		private double tissueTurnoverGLFWopt = 0.5;
		/// <summary>
		/// Optimum value GLFwater for tissue turnover [0-1] - below this value tissue turnover increases
		/// </summary>
		/// <value>The tissue turnover GLF wopt.</value>
		[Description("Optimum value GLFwater for tissue turnover [0-1]")]
		[Units("0-1")]
		public double TissueTurnoverGLFWopt
		{
			get { return tissueTurnoverGLFWopt; }
			set { tissueTurnoverGLFWopt = value; }
		}

		/// <summary>Stock factor for increasing tissue turnover rate</summary>
		private double stockParameter = 0.05;
		/// <summary>Stock factor for increasing tissue turnover rate</summary>
		/// <value>The stock parameter.</value>
		[XmlIgnore]
		[Units("-")]
		public double StockParameter
		{
			get { return stockParameter; }
			set { stockParameter = value; }
		}

		// - Digestibility values  ------------------------------------------------------------------------------------

		/// <summary>Digestibility of live plant material [0-1]</summary>
		private double digestibilityLive = 0.6;
		/// <summary>Digestibility of live plant material [0-1]</summary>
		/// <value>The digestibility live.</value>
		[Description("Digestibility of live plant material [0-1]:")]
		[Units("0-1")]
		public double DigestibilityLive
		{
			get { return digestibilityLive; }
			set { digestibilityLive = value; }
		}

		/// <summary>Digestibility of dead plant material [0-1]</summary>
		private double digestibilityDead = 0.2;
		/// <summary>Digestibility of dead plant material [0-1]</summary>
		/// <value>The digestibility dead.</value>
		[Description("Digestibility of dead plant material [0-1]:")]
		[Units("0-1")]
		public double DigestibilityDead
		{
			get { return digestibilityDead; }
			set { digestibilityDead = value; }
		}

		// - Minimum DM and preferences when harvesting  --------------------------------------------------------------

		/// <summary>Minimum above ground green DM [kg DM/ha]</summary>
		private double minimumGreenWt = 300.0;
		/// <summary>Minimum above ground green DM [kg DM/ha]</summary>
		/// <value>The minimum green DM weight.</value>
		[Description("Minimum above ground green DM [kg DM/ha]:")]
		[Units("kg/ha")]
		public double MinimumGreenWt
		{
			get { return minimumGreenWt; }
			set { minimumGreenWt = value; }
		}

		/// <summary>Minimum above ground dead DM [kg DM/ha]</summary>
		private double minimumDeadWt = 0.0;
		/// <summary>Minimum above ground dead DM [kg DM/ha]</summary>
		/// <value>The minimum dead DM weight.</value>
		[Description("Minimum above ground dead DM [kg DM/ha]")]
		[Units("kg/ha")]
		public double MinimumDeadWt
		{
			get { return minimumDeadWt; }
			set { minimumDeadWt = value; }
		}

		/// <summary>Preference for green DM during graze (weight factor)</summary>
		private double preferenceForGreenDM = 1.0;
		/// <summary>Preference for green DM during graze (weight factor)</summary>
		/// <value>The preference for green dm.</value>
		[Description("Preference for green DM during graze (weight factor):")]
		[Units("-")]
		public double PreferenceForGreenDM
		{
			get { return preferenceForGreenDM; }
			set { preferenceForGreenDM = value; }
		}

		/// <summary>Preference for dead DM during graze (weight factor)</summary>
		private double preferenceForDeadDM = 1.0;
		/// <summary>Preference for dead DM during graze (weight factor)</summary>
		/// <value>The preference for dead dm.</value>
		[Description("Preference for dead DM during graze (weight factor):")]
		[Units("-")]
		public double PreferenceForDeadDM
		{
			get { return preferenceForDeadDM; }
			set { preferenceForDeadDM = value; }
		}

		// - N concentration  -----------------------------------------------------------------------------------------

		/// <summary>Optimum N concentration in leaves [0-1]</summary>
		private double leafNopt = 0.04;
		/// <summary>Optimum N concentration in leaves [%]</summary>
		/// <value>The leaf nopt.</value>
		[Description("Optimum N concentration in young leaves [%]:")]
		[Units("%")]
		public double LeafNopt
		{
			get { return leafNopt * 100; }
			set { leafNopt = value / 100; }
		}

		/// <summary>Maximum N concentration in leaves (luxury N) [0-1]</summary>
		private double leafNmax = 0.05;
		/// <summary>Maximum N concentration in leaves (luxury N) [%]</summary>
		/// <value>The leaf nmax.</value>
		[Description("Maximum N concentration in leaves (luxury N) [%]:")]
		[Units("%")]
		public double LeafNmax
		{
			get { return leafNmax * 100; }
			set { leafNmax = value / 100; }
		}

		/// <summary>Minimum N concentration in leaves (dead material) [0-1]</summary>
		private double leafNmin = 0.012;
		/// <summary>Minimum N concentration in leaves (dead material) [%]</summary>
		/// <value>The leaf nmin.</value>
		[Description("Minimum N concentration in leaves (dead material) [%]:")]
		[Units("%")]
		public double LeafNmin
		{
			get { return leafNmin * 100; }
			set { leafNmin = value / 100; }
		}

		/// <summary>Concentration of N in stems relative to leaves [0-1]</summary>
		private double relativeNStems = 0.5;
		/// <summary>Concentration of N in stems relative to leaves [0-1]</summary>
		/// <value>The relative n stems.</value>
		[Description("Concentration of N in stems relative to leaves [0-1]:")]
		[Units("0-1")]
		public double RelativeNStems
		{
			get { return relativeNStems; }
			set { relativeNStems = value; }
		}

		/// <summary>Concentration of N in stolons relative to leaves [0-1]</summary>
		private double relativeNStolons = 0.0;
		/// <summary>Concentration of N in stolons relative to leaves [0-1]</summary>
		/// <value>The relative n stolons.</value>
		[Description("Concentration of N in stolons relative to leaves [0-1]:")]
		[Units("0-1")]
		public double RelativeNStolons
		{
			get { return relativeNStolons; }
			set { relativeNStolons = value; }
		}

		/// <summary>Concentration of N in roots relative to leaves [0-1]</summary>
		private double relativeNRoots = 0.5;
		/// <summary>Concentration of N in roots relative to leaves [0-1]</summary>
		/// <value>The relative n roots.</value>
		[Description("Concentration of N in roots relative to leaves [0-1]:")]
		[Units("0-1")]
		public double RelativeNRoots
		{
			get { return relativeNRoots; }
			set { relativeNRoots = value; }
		}

		/// <summary>Concentration of N in tissues at stage 2 relative to stage 1 [0-1]</summary>
		private double relativeNStage2 = 1.0;
		/// <summary>Concentration of N in tissues at stage 2 relative to stage 1 [0-1]</summary>
		/// <value>The relative n stage2.</value>
		[Description("Concentration of N in tissues at stage 2 relative to stage 1 [0-1]:")]
		[Units("0-1")]
		public double RelativeNStage2
		{
			get { return relativeNStage2; }
			set { relativeNStage2 = value; }
		}

		/// <summary>Concentration of N in tissues at stage 3 relative to stage 1 [0-1]</summary>
		private double relativeNStage3 = 1.0;
		/// <summary>Concentration of N in tissues at stage 3 relative to stage 1 [0-1]</summary>
		/// <value>The relative n stage3.</value>
		[Description("Concentration of N in tissues at stage 3 relative to stage 1 [0-1]:")]
		[Units("0-1")]
		public double RelativeNStage3
		{
			get { return relativeNStage3; }
			set { relativeNStage3 = value; }
		}

		// - N fixation  ----------------------------------------------------------------------------------------------

		/// <summary>Minimum fraction of N demand supplied by biologic N fixation [0-1]</summary>
		private double minimumNFixation = 0.0;
		/// <summary>Minimum fraction of N demand supplied by biologic N fixation [0-1]</summary>
		/// <value>The minimum n fixation.</value>
		[Description("Minimum fraction of N demand supplied by biologic N fixation [0-1]:")]
		[Units("0-1")]
		public double MinimumNFixation
		{
			get { return minimumNFixation; }
			set { minimumNFixation = value; }
		}

		/// <summary>Maximum fraction of N demand supplied by biologic N fixation [0-1]</summary>
		private double maximumNFixation = 0.0;
		/// <summary>Maximum fraction of N demand supplied by biologic N fixation [0-1]</summary>
		/// <value>The maximum n fixation.</value>
		[Description("Maximum fraction of N demand supplied by biologic N fixation [0-1]:")]
		[Units("0-1")]
		public double MaximumNFixation
		{
			get { return maximumNFixation; }
			set { maximumNFixation = value; }
		}

		// - Remobilisation and luxury N  -----------------------------------------------------------------------------

		/// <summary>Fraction of luxury N in tissue 2 available for remobilisation [0-1]</summary>
		private double kappaNRemob2 = 0.0;
		/// <summary>Fraction of luxury N in tissue 2 available for remobilisation [0-1]</summary>
		/// <value>The kappa n remob2.</value>
		[Description("Fraction of luxury N in tissue 2 available for remobilisation [0-1]:")]
		[Units("0-1")]
		public double KappaNRemob2
		{
			get { return kappaNRemob2; }
			set { kappaNRemob2 = value; }
		}

		/// <summary>Fraction of luxury N in tissue 3 available for remobilisation [0-1]</summary>
		private double kappaNRemob3 = 0.0;
		/// <summary>Fraction of luxury N in tissue 3 available for remobilisation [0-1]</summary>
		/// <value>The kappa n remob3.</value>
		[Description("Fraction of luxury N in tissue 3 available for remobilisation [0-1]:")]
		[Units("0-1")]
		public double KappaNRemob3
		{
			get { return kappaNRemob3; }
			set { kappaNRemob3 = value; }
		}

		/// <summary>Fraction of non-utilised remobilised N that is returned to dead material [0-1]</summary>
		private double kappaNRemob4 = 0.0;
		/// <summary>Fraction of non-utilised remobilised N that is returned to dead material [0-1]</summary>
		/// <value>The kappa n remob4.</value>
		[Description("Fraction of non-utilised remobilised N that is returned to dead material [0-1]:")]
		[Units("0-1")]
		public double KappaNRemob4
		{
			get { return kappaNRemob4; }
			set { kappaNRemob4 = value; }
		}

		/// <summary>Fraction of senescent DM that is remobilised (as carbohydrate) [0-1]</summary>
		private double kappaCRemob = 0.0;
		/// <summary>Fraction of senescent DM that is remobilised (as carbohydrate) [0-1]</summary>
		/// <value>The kappa c remob.</value>
		[XmlIgnore]
		[Units("0-1")]
		public double KappaCRemob
		{
			get { return kappaCRemob; }
			set { kappaCRemob = value; }
		}

		/// <summary>Fraction of senescent DM (protein) that is remobilised to new growth [0-1]</summary>
		private double facCNRemob = 0.0;
		/// <summary>Fraction of senescent DM (protein) that is remobilised to new growth [0-1]</summary>
		/// <value>The fac cn remob.</value>
		[XmlIgnore]
		[Units("0-1")]
		public double FacCNRemob
		{
			get { return facCNRemob; }
			set { facCNRemob = value; }
		}

		// - Effect of stress on growth  ------------------------------------------------------------------------------

		/// <summary>Curve parameter for the effect of N deficiency on plant growth</summary>
		private double dillutionCoefN = 0.5;
		/// <summary>Curve parameter for the effect of N deficiency on plant growth</summary>
		/// <value>The dillution coef n.</value>
		[Description("Curve parameter for the effect of N deficiency on plant growth:")]
		[Units("-")]
		public double DillutionCoefN
		{
			get { return dillutionCoefN; }
			set { dillutionCoefN = value; }
		}

		/// <summary>Generic growth limiting factor [0-1]</summary>
		private double glfGeneric = 1.0;
		/// <summary>Gets or sets a generic growth limiting factor (arbitrary limitation).</summary>
		/// <value>The generic growth limiting factor.</value>
		/// <remarks> This factor is applied at same level as N, so it can be considered a nutrient limitation effect </remarks>
		[Description("Generic growth limiting factor [0-1]:")]
		[Units("0-1")]
		public double GlfGeneric
		{
			get { return glfGeneric; }
			set { glfGeneric = value; }
		}

		/// <summary>Exponent factor for the water stress function</summary>
		private double waterStressExponent = 1.0;
		/// <summary>Exponent factor for the water stress function</summary>
		/// <value>The water stress exponent.</value>
		[Description("Exponent factor for the water stress function:")]
		[Units("-")]
		public double WaterStressExponent
		{
			get { return waterStressExponent; }
			set { waterStressExponent = value; }
		}

		/// <summary>Maximum reduction in plant growth due to water logging (saturated soil) [0-1]</summary>
		private double waterLoggingCoefficient = 0.1;
		/// <summary>Maximum reduction in plant growth due to water logging (saturated soil) [0-1]</summary>
		/// <value>The water logging coefficient.</value>
		[Description("Maximum reduction in plant growth due to water logging (saturated soil) [0-1]:")]
		[Units("0-1")]
		public double WaterLoggingCoefficient
		{
			get { return waterLoggingCoefficient; }
			set { waterLoggingCoefficient = value; }
		}

		// - CO2 related  ---------------------------------------------------------------------------------------------

		/// <summary>Reference CO2 concentration for photosynthesis [ppm]</summary>
		private double referenceCO2 = 380.0;
		/// <summary>Reference CO2 concentration for photosynthesis [ppm]</summary>
		/// <value>The reference c o2.</value>
		[Description("Reference CO2 concentration for photosynthesis [ppm]:")]
		[Units("ppm")]
		public double ReferenceCO2
		{
			get { return referenceCO2; }
			set { referenceCO2 = value; }
		}

		/// <summary>
		/// Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]
		/// </summary>
		private double coefficientCO2EffectOnPhotosynthesis = 700.0;
		/// <summary>
		/// Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]
		/// </summary>
		/// <value>The coefficient c o2 effect on photosynthesis.</value>
		[Description("Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]:")]
		[Units("ppm")]
		public double CoefficientCO2EffectOnPhotosynthesis
		{
			get { return coefficientCO2EffectOnPhotosynthesis; }
			set { coefficientCO2EffectOnPhotosynthesis = value; }
		}

		/// <summary>Scalling paramenter for the CO2 effects on N uptake [ppm Co2]</summary>
		private double offsetCO2EffectOnNuptake = 600.0;
		/// <summary>Scalling paramenter for the CO2 effects on N uptake [ppm Co2]</summary>
		/// <value>The offset c o2 effect on nuptake.</value>
		[Description("Scalling paramenter for the CO2 effects on N requirement [ppm Co2]:")]
		[Units("ppm")]
		public double OffsetCO2EffectOnNuptake
		{
			get { return offsetCO2EffectOnNuptake; }
			set { offsetCO2EffectOnNuptake = value; }
		}

		/// <summary>Minimum value for the effect of CO2 on N requirement [0-1]</summary>
		private double minimumCO2EffectOnNuptake = 0.7;
		/// <summary>Minimum value for the effect of CO2 on N requirement [0-1]</summary>
		/// <value>The minimum c o2 effect on nuptake.</value>
		[Description("Minimum value for the effect of CO2 on N requirement [0-1]:")]
		[Units("0-1")]
		public double MinimumCO2EffectOnNuptake
		{
			get { return minimumCO2EffectOnNuptake; }
			set { minimumCO2EffectOnNuptake = value; }
		}

		/// <summary>Exponent of the function describing the effect of CO2 on N requirement</summary>
		private double exponentCO2EffectOnNuptake = 2.0;
		/// <summary>Exponent of the function describing the effect of CO2 on N requirement</summary>
		/// <value>The exponent c o2 effect on nuptake.</value>
		[Description("Exponent of the function describing the effect of CO2 on N requirement:")]
		[Units("-")]
		public double ExponentCO2EffectOnNuptake
		{
			get { return exponentCO2EffectOnNuptake; }
			set { exponentCO2EffectOnNuptake = value; }
		}

		// - Root distribution and height  ----------------------------------------------------------------------------

		/// <summary>Root distribution method (Homogeneous, ExpoLinear, UserDefined)</summary>
		private string rootDistributionMethod = "ExpoLinear";
		/// <summary>Root distribution method (Homogeneous, ExpoLinear, UserDefined)</summary>
		/// <value>The root distribution method.</value>
		/// <exception cref="System.Exception">Root distribution method given ( + value +  is no valid</exception>
		[XmlIgnore]
		public string RootDistributionMethod
		{
			get { return rootDistributionMethod; }
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
						throw new Exception("Root distribution method given (" + value + " is no valid");
				}
			}
		}

		/// <summary>Fraction of root depth where its proportion starts to decrease</summary>
		private double expoLinearDepthParam = 0.12;
		/// <summary>Fraction of root depth where its proportion starts to decrease</summary>
		/// <value>The expo linear depth parameter.</value>
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

		/// <summary>Exponent to determine mass distribution in the soil profile</summary>
		private double expoLinearCurveParam = 3.2;
		/// <summary>Exponent to determine mass distribution in the soil profile</summary>
		/// <value>The expo linear curve parameter.</value>
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

		// - Other parameters  ----------------------------------------------------------------------------------------
		/// <summary>Broken stick type function describing how plant height varies with DM</summary>
		[XmlIgnore]
		public BrokenStick HeightFromMass = new BrokenStick
		{
			X = new double[5] { 0, 1000, 2000, 3000, 4000 },
			Y = new double[5] { 0, 25, 75, 150, 250 }
		};

		/// <summary>The FVPD function</summary>
		[XmlIgnore]
		public BrokenStick FVPDFunction = new BrokenStick
		{
			X = new double[3] { 0.0, 10.0, 50.0 },
			Y = new double[3] { 1.0, 1.0, 1.0 }
		};

		/// <summary>Flag which module will perform the water uptake process</summary>
		internal string myWaterUptakeSource = "species";
		/// <summary>Flag whether the alternative water uptake process will be used</summary>
		internal string useAltWUptake = "no";
		/// <summary>Reference value of Ksat for water availability function</summary>
		internal double ReferenceKSuptake = 1000.0;
		/// <summary>Flag which module will perform the nitrogen uptake process</summary>
		internal string myNitrogenUptakeSource = "species";
		/// <summary>Flag whether the alternative nitrogen uptake process will be used</summary>
		internal string useAltNUptake = "no";
		/// <summary>Availability factor for NH4</summary>
		internal double kuNH4 = 0.50;
		/// <summary>Availability factor for NO3</summary>
		internal double kuNO3 = 0.95;
		/// <summary>Reference value for root length density fot the Water and N availability</summary>
		internal double ReferenceRLD = 2.0;

		/// <summary>the local value for stomatal conductance</summary>
		private double myStomatalConductanceMax = 1.0;
		/// <summary>The value for the maximum stomatal conductance (m/s)</summary>
		/// <value>Maximum stomatal conductance.</value>
		[Description("Maximum stomatal conductance (m/s)")]
		public double MaximumStomatalConductance
		{
			get { return myStomatalConductanceMax; }
			set { myStomatalConductanceMax = value; }
		}

		/// <summary>the local value for KNO3</summary>
		private double myKNO3 = 1.0;
		/// <summary>The value for the nitrate uptake coefficient</summary>
		/// <value>The kNO3 for this plant.</value>
		[Description("Nitrate uptake coefficient")]
		public double KNO3
		{
			get { return myKNO3; }
			set { myKNO3 = value; }
		}

		/// <summary>the local value for KNH4</summary>
		private double myKNH4 = 1.0;
		/// <summary>The value for the ammonium uptake coefficient</summary>
		/// <value>The local kNH4 for this plant.</value>
		[Description("Ammonium uptake coefficient")]
		public double KNH4
		{
			get { return myKNH4; }
			set { myKNH4 = value; }
		}

		#endregion

		#region Model outputs  ---------------------------------------------------------------------------------------------

		/// <summary>
		/// Is the plant alive?
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
				if (isAlive)
					return "alive";
				else
					return "out";
			}
		}

		/// <summary>Gets the index for the plant development stage.</summary>
		/// <value>The stage index.</value>
		[Description("Plant development stage number")]
		[Units("")]
		public int Stage
		{
			get
			{
				if (isAlive)
				{
					if (phenoStage == 0)
						return 1;    //"sowing & germination";
					else
						return 3;    //"emergence" & "reproductive";
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
				if (isAlive)
				{
					if (phenoStage == 0)
						return "sowing";
					else
						return "emergence";
				}
				else
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
			get { return TotalWt * CarbonFractionInDM; }
		}

		/// <summary>Gets the plant total dry matter weight.</summary>
		/// <value>The total DM weight.</value>
		[Description("Total plant dry matter weight")]
		[Units("kgDM/ha")]
		public double TotalWt
		{
			get { return dmShoot + dmRoot; }
		}

		/// <summary>Gets the plant DM weight above ground.</summary>
		/// <value>The above ground DM weight.</value>
		[Description("Dry matter weight above ground")]
		[Units("kgDM/ha")]
		public double AboveGroundWt
		{
			get { return dmShoot; }
		}

		/// <summary>Gets the DM weight of live plant parts above ground.</summary>
		/// <value>The above ground DM weight of live plant parts.</value>
		[Description("Dry matter weight of alive plants above ground")]
		[Units("kgDM/ha")]
		public double AboveGrounLivedWt
		{
			get { return dmGreen; }
		}

		/// <summary>Gets the DM weight of dead plant parts above ground.</summary>
		/// <value>The above ground dead DM weight.</value>
		[Description("Dry matter weight of dead plants above ground")]
		[Units("kgDM/ha")]
		public double AboveGroundDeadWt
		{
			get { return dmDead; }
		}

		/// <summary>Gets the DM weight of the plant below ground.</summary>
		/// <value>The below ground DM weight of plant.</value>
		[Description("Dry matter weight below ground")]
		[Units("kgDM/ha")]
		public double BelowGroundWt
		{
			get { return dmRoot; }
		}

		/// <summary>Gets the total standing DM weight.</summary>
		/// <value>The DM weight of leaves and stems.</value>
		[Description("Dry matter weight of standing herbage")]
		[Units("kgDM/ha")]
		public double StandingWt
		{
			get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmLeaf4 + dmStem1 + dmStem2 + dmStem3 + dmStem4; }
		}

		/// <summary>Gets the DM weight of standing live plant material.</summary>
		/// <value>The DM weight of live leaves and stems.</value>
		[Description("Dry matter weight of live standing plants parts")]
		[Units("kgDM/ha")]
		public double StandingLiveWt
		{
			get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmStem1 + dmStem2 + dmStem3; }
		}

		/// <summary>Gets the DM weight of standing dead plant material.</summary>
		/// <value>The DM weight of dead leaves and stems.</value>
		[Description("Dry matter weight of dead standing plants parts")]
		[Units("kgDM/ha")]
		public double StandingDeadWt
		{
			get { return dmLeaf4 + dmStem4; }
		}

		/// <summary>Gets the total DM weight of leaves.</summary>
		/// <value>The leaf DM weight.</value>
		[Description("Dry matter weight of leaves")]
		[Units("kgDM/ha")]
		public double LeafWt
		{
			get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmLeaf4; }
		}

		/// <summary>Gets the DM weight of green leaves.</summary>
		/// <value>The green leaf DM weight.</value>
		[Description("Dry matter weight of live leaves")]
		[Units("kgDM/ha")]
		public double LeafGreenWt
		{
			get { return dmLeaf1 + dmLeaf2 + dmLeaf3; }
		}

		/// <summary>Gets the DM weight of dead leaves.</summary>
		/// <value>The dead leaf DM weight.</value>
		[Description("Dry matter weight of dead leaves")]
		[Units("kgDM/ha")]
		public double LeafDeadWt
		{
			get { return dmLeaf4; }
		}

		/// <summary>Gets the toal DM weight of stems and sheath.</summary>
		/// <value>The stem DM weight.</value>
		[Description("Dry matter weight of stems and sheath")]
		[Units("kgDM/ha")]
		public double StemWt
		{
			get { return dmStem1 + dmStem2 + dmStem3 + dmStem4; }
		}

		/// <summary>Gets the DM weight of live stems and sheath.</summary>
		/// <value>The live stems DM weight.</value>
		[Description("Dry matter weight of alive stems and sheath")]
		[Units("kgDM/ha")]
		public double StemGreenWt
		{
			get { return dmStem1 + dmStem2 + dmStem3; }
		}

		/// <summary>Gets the DM weight of dead stems and sheath.</summary>
		/// <value>The dead stems DM weight.</value>
		[Description("Dry matter weight of dead stems and sheath")]
		[Units("kgDM/ha")]
		public double StemDeadWt
		{
			get { return dmStem4; }
		}

		/// <summary>Gets the total DM weight od stolons.</summary>
		/// <value>The stolon DM weight.</value>
		[Description("Dry matter weight of stolons")]
		[Units("kgDM/ha")]
		public double StolonWt
		{
			get { return dmStolon1 + dmStolon2 + dmStolon3; }
		}

		/// <summary>Gets the total DM weight of roots.</summary>
		/// <value>The root DM weight.</value>
		[Description("Dry matter weight of roots")]
		[Units("kgDM/ha")]
		public double RootWt
		{
			get { return dmRoot; }
		}

		/// <summary>Gets the DM weight of leaves at stage1 (developing).</summary>
		/// <value>The stage1 leaf DM weight.</value>
		[Description("Dry matter weight of leaves at stage 1 (developing)")]
		[Units("kgDM/ha")]
		public double LeafStage1Wt
		{
			get { return dmLeaf1; }
		}

		/// <summary>Gets the DM weight of leaves stage2 (mature).</summary>
		/// <value>The stage2 leaf DM weight.</value>
		[Description("Dry matter weight of leaves at stage 2 (mature)")]
		[Units("kgDM/ha")]
		public double LeafStage2Wt
		{
			get { return dmLeaf2; }
		}

		/// <summary>Gets the DM weight of leaves at stage3 (senescing).</summary>
		/// <value>The stage3 leaf DM weight.</value>
		[Description("Dry matter weight of leaves at stage 3 (senescing)")]
		[Units("kgDM/ha")]
		public double LeafStage3Wt
		{
			get { return dmLeaf3; }
		}

		/// <summary>Gets the DM weight of leaves at stage4 (dead).</summary>
		/// <value>The stage4 leaf DM weight.</value>
		[Description("Dry matter weight of leaves at stage 4 (dead)")]
		[Units("kgDM/ha")]
		public double LeafStage4Wt
		{
			get { return dmLeaf4; }
		}

		/// <summary>Gets the DM weight stems and sheath at stage1 (developing).</summary>
		/// <value>The stage1 stems DM weight.</value>
		[Description("Dry matter weight of stems at stage 1 (developing)")]
		[Units("kgDM/ha")]
		public double StemStage1Wt
		{
			get { return dmStem1; }
		}

		/// <summary>Gets the DM weight of stems and sheath at stage2 (mature).</summary>
		/// <value>The stage2 stems DM weight.</value>
		[Description("Dry matter weight of stems at stage 2 (mature)")]
		[Units("kgDM/ha")]
		public double StemStage2Wt
		{
			get { return dmStem2; }
		}

		/// <summary>Gets the DM weight of stems and sheath at stage3 (senescing)).</summary>
		/// <value>The stage3 stems DM weight.</value>
		[Description("Dry matter weight of stems at stage 3 (senescing)")]
		[Units("kgDM/ha")]
		public double StemStage3Wt
		{
			get { return dmStem3; }
		}

		/// <summary>Gets the DM weight of stems and sheath at stage4 (dead).</summary>
		/// <value>The stage4 stems DM weight.</value>
		[Description("Dry matter weight of stems at stage 4 (dead)")]
		[Units("kgDM/ha")]
		public double StemStage4Wt
		{
			get { return dmStem4; }
		}

		/// <summary>Gets the DM weight of stolons at stage1 (developing).</summary>
		/// <value>The stage1 stolon DM weight.</value>
		[Description("Dry matter weight of stolons at stage 1 (developing)")]
		[Units("kgDM/ha")]
		public double StolonStage1Wt
		{
			get { return dmStolon1; }
		}

		/// <summary>Gets the DM weight of stolons at stage2 (mature).</summary>
		/// <value>The stage2 stolon DM weight.</value>
		[Description("Dry matter weight of stolons at stage 2 (mature)")]
		[Units("kgDM/ha")]
		public double StolonStage2Wt
		{
			get { return dmStolon2; }
		}

		/// <summary>Gets the DM weight of stolons at stage3 (senescing).</summary>
		/// <value>The stage3 stolon DM weight.</value>
		[Description("Dry matter weight of stolons at stage 3 (senescing)")]
		[Units("kgDM/ha")]
		public double StolonStage3Wt
		{
			get { return dmStolon3; }
		}

		#endregion

		#region - C and DM flows  ------------------------------------------------------------------------------------------

		/// <summary>Gets the potential carbon assimilation.</summary>
		/// <value>The potential carbon assimilation.</value>
		[Description("Potential C assimilation, corrected for extreme temperatures")]
		[Units("kgC/ha")]
		public double PotCarbonAssimilation
		{
			get { return Pgross; }
		}

		/// <summary>Gets the carbon loss via respiration.</summary>
		/// <value>The carbon loss via respiration.</value>
		[Description("Loss of C via respiration")]
		[Units("kgC/ha")]
		public double CarbonLossRespiration
		{
			get { return Resp_m; }
		}

		/// <summary>Gets the carbon remobilised from senescent tissue.</summary>
		/// <value>The carbon remobilised.</value>
		[Description("C remobilised from senescent tissue")]
		[Units("kgC/ha")]
		public double CarbonRemobilised
		{
			get { return CRemobilised; }
		}

		/// <summary>Gets the gross potential growth rate.</summary>
		/// <value>The potential C assimilation, in DM equivalent.</value>
		[Description("Gross potential growth rate (potential C assimilation)")]
		[Units("kgDM/ha")]
		public double GrossPotentialGrowthWt
		{
			get { return Pgross / CarbonFractionInDM; }
		}

		/// <summary>Gets the respiration rate.</summary>
		/// <value>The loss of C due to respiration, in DM equivalent.</value>
		[Description("Respiration rate (DM lost via respiration)")]
		[Units("kgDM/ha")]
		public double RespirationWt
		{
			get { return Resp_m / CarbonFractionInDM; }
		}

		/// <summary>Gets the remobilisation rate.</summary>
		/// <value>The C remobilised, in DM equivalent.</value>
		[Description("C remobilisation (DM remobilised from old tissue to new growth)")]
		[Units("kgDM/ha")]
		public double RemobilisationWt
		{
			get { return CRemobilised / CarbonFractionInDM; }
		}

		/// <summary>Gets the net potential growth rate.</summary>
		/// <value>The net potential growth rate.</value>
		[Description("Net potential growth rate")]
		[Units("kgDM/ha")]
		public double NetPotentialGrowthWt
		{
			get { return dGrowthPot; }
		}

		/// <summary>Gets the potential growth rate after water stress.</summary>
		/// <value>The potential growth after water stress.</value>
		[Description("Potential growth rate after water stress")]
		[Units("kgDM/ha")]
		public double PotGrowthWt_Wstress
		{
			get { return dGrowthWstress; }
		}

		/// <summary>Gets the actual growth rate.</summary>
		/// <value>The actual growth rate.</value>
		[Description("Actual growth rate, after nutrient stress")]
		[Units("kgDM/ha")]
		public double ActualGrowthWt
		{
			get { return dGrowthActual; }
		}

		/// <summary>Gets the effective growth rate.</summary>
		/// <value>The effective growth rate.</value>
		[Description("Effective growth rate, after turnover")]
		[Units("kgDM/ha")]
		public double EffectiveGrowthWt
		{
			get { return dGrowthEff; }
		}

		/// <summary>Gets the effective herbage growth rate.</summary>
		/// <value>The herbage growth rate.</value>
		[Description("Effective herbage growth rate, above ground")]
		[Units("kgDM/ha")]
		public double HerbageGrowthWt
		{
			get { return dGrowthShoot; }
		}

		/// <summary>Gets the effective root growth rate.</summary>
		/// <value>The root growth DM weight.</value>
		[Description("Effective root growth rate")]
		[Units("kgDM/ha")]
		public double RootGrowthWt
		{
			get { return dGrowthRoot; }
		}

		/// <summary>Gets the litter DM weight deposited onto soil surface.</summary>
		/// <value>The litter DM weight deposited.</value>
		[Description("Litter amount deposited onto soil surface")]
		[Units("kgDM/ha")]
		public double LitterWt
		{
			get { return dLitter; }
		}

		/// <summary>Gets the senesced root DM weight.</summary>
		/// <value>The senesced root DM weight.</value>
		[Description("Amount of senesced roots added to soil FOM")]
		[Units("kgDM/ha")]
		public double RootSenescedWt
		{
			get { return dRootSen; }
		}

		/// <summary>Gets the gross primary productivity.</summary>
		/// <value>The gross primary productivity.</value>
		[Description("Gross primary productivity")]
		[Units("kgDM/ha")]
		public double GPP
		{
			get { return Pgross / CarbonFractionInDM; }
		}

		/// <summary>Gets the net primary productivity.</summary>
		/// <value>The net primary productivity.</value>
		[Description("Net primary productivity")]
		[Units("kgDM/ha")]
		public double NPP
		{
			get { return (Pgross * (1 - growthRespirationCoef) - Resp_m) / CarbonFractionInDM; }
		}

		/// <summary>Gets the net above-ground primary productivity.</summary>
		/// <value>The net above-ground primary productivity.</value>
		[Description("Net above-ground primary productivity")]
		[Units("kgDM/ha")]
		public double NAPP
		{
			get { return (Pgross * (1 - growthRespirationCoef) - Resp_m) * fShoot / CarbonFractionInDM; }
		}

		/// <summary>Gets the net below-ground primary productivity.</summary>
		/// <value>The net below-ground primary productivity.</value>
		[Description("Net below-ground primary productivity")]
		[Units("kgDM/ha")]
		public double NBPP
		{
			get { return (Pgross * (1 - growthRespirationCoef) - Resp_m) * (1 - fShoot) / CarbonFractionInDM; }
		}
		#endregion

		#region - N amounts  -----------------------------------------------------------------------------------------------

		/// <summary>Gets the plant total N content.</summary>
		/// <value>The total N content.</value>
		[Description("Total plant N amount")]
		[Units("kgN/ha")]
		public double TotalN
		{
			get { return Nshoot + Nroot; }
		}

		/// <summary>Gets the N content in the plant above ground.</summary>
		/// <value>The above ground N content.</value>
		[Description("N amount of plant parts above ground")]
		[Units("kgN/ha")]
		public double AboveGroundN
		{
			get { return Nshoot; }
		}

		/// <summary>Gets the N content in live plant material above ground.</summary>
		/// <value>The N content above ground of live plants.</value>
		[Description("N amount of alive plant parts above ground")]
		[Units("kgN/ha")]
		public double AboveGroundLiveN
		{
			get { return Nleaf1 + Nleaf2 + Nleaf3 + Nstem1 + Nstem2 + Nstem3 + Nstolon1 + Nstolon2 + Nstolon3; }
		}

		/// <summary>Gets the N content of dead plant material above ground.</summary>
		/// <value>The N content above ground of dead plants.</value>
		[Description("N amount of dead plant parts above ground")]
		[Units("kgN/ha")]
		public double AboveGroundDeadN
		{
			get { return Nleaf4 + Nstem4; }
		}

		/// <summary>Gets the N content of plants below ground.</summary>
		/// <value>The below ground N content.</value>
		[Description("N amount of plant parts below ground")]
		[Units("kgN/ha")]
		public double BelowGroundN
		{
			get { return Nroot; }
		}

		/// <summary>Gets the N content of standing plants.</summary>
		/// <value>The N content of leaves and stems.</value>
		[Description("N amount of standing herbage")]
		[Units("kgN/ha")]
		public double StandingN
		{
			get { return Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4 + Nstem1 + Nstem2 + Nstem3 + Nstem4; }
		}

		/// <summary>Gets the N content of standing live plant material.</summary>
		/// <value>The N content of live leaves and stems.</value>
		[Description("N amount of alive standing herbage")]
		[Units("kgN/ha")]
		public double StandingLiveN
		{
			get { return Nleaf1 + Nleaf2 + Nleaf3 + Nstem1 + Nstem2 + Nstem3; }
		}

		/// <summary>Gets the N content  of standing dead plant material.</summary>
		/// <value>The N content of dead leaves and stems.</value>
		[Description("N amount of dead standing herbage")]
		[Units("kgN/ha")]
		public double StandingDeadN
		{
			get { return Nleaf4 + Nstem4; }
		}

		/// <summary>Gets the total N content of leaves.</summary>
		/// <value>The leaf N content.</value>
		[Description("N amount in the plant's leaves")]
		[Units("kgN/ha")]
		public double LeafN
		{
			get { return Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4; }
		}

		/// <summary>Gets the total N content of stems and sheath.</summary>
		/// <value>The stem N content.</value>
		[Description("N amount in the plant's stems")]
		[Units("kgN/ha")]
		public double StemN
		{
			get { return Nstem1 + Nstem2 + Nstem3 + Nstem4; }
		}

		/// <summary>Gets the total N content of stolons.</summary>
		/// <value>The stolon N content.</value>
		[Description("N amount in the plant's stolons")]
		[Units("kgN/ha")]
		public double StolonN
		{
			get { return Nstolon1 + Nstolon2 + Nstolon3; }
		}

		/// <summary>Gets the total N content of roots.</summary>
		/// <value>The root N content.</value>
		[Description("N amount in the plant's roots")]
		[Units("kgN/ha")]
		public double RootN
		{
			get { return Nroot; }
		}

		/// <summary>Gets the N content of green leaves.</summary>
		/// <value>The green leaf N content.</value>
		[Description("N amount in alive leaves")]
		[Units("kgN/ha")]
		public double LeafGreenN
		{
			get { return Nleaf1 + Nleaf2 + Nleaf3; }
		}

		/// <summary>Gets the N content of dead leaves.</summary>
		/// <value>The dead leaf N content.</value>
		[Description("N amount in dead leaves")]
		[Units("kgN/ha")]
		public double LeafDeadN
		{
			get { return Nleaf4; }
		}

		/// <summary>Gets the N content of green stems and sheath.</summary>
		/// <value>The green stem N content.</value>
		[Description("N amount in alive stems")]
		[Units("kgN/ha")]
		public double StemGreenN
		{
			get { return Nstem1 + Nstem2 + Nstem3; }
		}

		/// <summary>Gets the N content  of dead stems and sheath.</summary>
		/// <value>The dead stem N content.</value>
		[Description("N amount in dead sytems")]
		[Units("kgN/ha")]
		public double StemDeadN
		{
			get { return Nstem4; }
		}

		/// <summary>Gets the N content of leaves at stage1 (developing).</summary>
		/// <value>The stage1 leaf N.</value>
		[Description("N amount in leaves at stage 1 (developing)")]
		[Units("kgN/ha")]
		public double LeafStage1N
		{
			get { return Nleaf1; }
		}

		/// <summary>Gets the N content of leaves at stage2 (mature).</summary>
		/// <value>The stage2 leaf N.</value>
		[Description("N amount in leaves at stage 2 (mature)")]
		[Units("kgN/ha")]
		public double LeafStage2N
		{
			get { return Nleaf2; }
		}

		/// <summary>Gets the N content of leaves at stage3 (senescing).</summary>
		/// <value>The stage3 leaf N.</value>
		[Description("N amount in leaves at stage 3 (senescing)")]
		[Units("kgN/ha")]
		public double LeafStage3N
		{
			get { return Nleaf3; }
		}

		/// <summary>Gets the N content of leaves at stage4 (dead).</summary>
		/// <value>The stage4 leaf N.</value>
		[Description("N amount in leaves at stage 4 (dead)")]
		[Units("kgN/ha")]
		public double LeafStage4N
		{
			get { return Nleaf4; }
		}

		/// <summary>Gets the N content of stems and sheath at stage1 (developing).</summary>
		/// <value>The stage1 stem N.</value>
		[Description("N amount in stems at stage 1 (developing)")]
		[Units("kgN/ha")]
		public double StemStage1N
		{
			get { return Nstem1; }
		}

		/// <summary>Gets the N content of stems and sheath at stage2 (mature).</summary>
		/// <value>The stage2 stem N.</value>
		[Description("N amount in stems at stage 2 (mature)")]
		[Units("kgN/ha")]
		public double StemStage2N
		{
			get { return Nstem2; }
		}

		/// <summary>Gets the N content of stems and sheath at stage3 (senescing).</summary>
		/// <value>The stage3 stem N.</value>
		[Description("N amount in stems at stage 3 (senescing)")]
		[Units("kgN/ha")]
		public double StemStage3N
		{
			get { return Nstem3; }
		}

		/// <summary>Gets the N content of stems and sheath at stage4 (dead).</summary>
		/// <value>The stage4 stem N.</value>
		[Description("N amount in stems at stage 4 (dead)")]
		[Units("kgN/ha")]
		public double StemStage4N
		{
			get { return Nstem4; }
		}

		/// <summary>Gets the N content of stolons at stage1 (developing).</summary>
		/// <value>The stage1 stolon N.</value>
		[Description("N amount in stolons at stage 1 (developing)")]
		[Units("kgN/ha")]
		public double StolonStage1N
		{
			get { return Nstolon1; }
		}

		/// <summary>Gets the N content of stolons at stage2 (mature).</summary>
		/// <value>The stage2 stolon N.</value>
		[Description("N amount in stolons at stage 2 (mature)")]
		[Units("kgN/ha")]
		public double StolonStage2N
		{
			get { return Nstolon2; }
		}

		/// <summary>Gets the N content of stolons as stage3 (senescing).</summary>
		/// <value>The stolon stage3 n.</value>
		[Description("N amount in stolons at stage 3 (senescing)")]
		[Units("kgN/ha")]
		public double StolonStage3N
		{
			get { return Nstolon3; }
		}

		#endregion

		#region - N concentrations  ----------------------------------------------------------------------------------------

		/// <summary>Gets the average N concentration of standing plant material.</summary>
		/// <value>The average N concentration of leaves and stems.</value>
		[Description("Average N concentration in standing plant parts")]
		[Units("kgN/kgDM")]
		public double StandingNConc
		{
			get { return MathUtilities.Divide(StandingN, StandingWt, 0.0); }
		}

		/// <summary>Gets the average N concentration of leaves.</summary>
		/// <value>The leaf N concentration.</value>
		[Description("Average N concentration in leaves")]
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

		/// <summary>Gets the N concentration of leaves at stage1 (developing).</summary>
		/// <value>The stage1 leaf N concentration.</value>
		[Description("N concentration of leaves at stage 1 (developing)")]
		[Units("kgN/kgDM")]
		public double LeafStage1NConc
		{
			get { return MathUtilities.Divide(Nleaf1, dmLeaf1, 0.0); }
		}

		/// <summary>Gets the N concentration of leaves at stage2 (mature).</summary>
		/// <value>The stage2 leaf N concentration.</value>
		[Description("N concentration of leaves at stage 2 (mature)")]
		[Units("kgN/kgDM")]
		public double LeafStage2NConc
		{
			get { return MathUtilities.Divide(Nleaf2, dmLeaf2, 0.0); }
		}

		/// <summary>Gets the N concentration of leaves at stage3 (senescing).</summary>
		/// <value>The stage3 leaf N concentration.</value>
		[Description("N concentration of leaves at stage 3 (senescing)")]
		[Units("kgN/kgDM")]
		public double LeafStage3NConc
		{
			get { return MathUtilities.Divide(Nleaf3, dmLeaf3, 0.0); }
		}

		/// <summary>Gets the N concentration of leaves at stage4 (dead).</summary>
		/// <value>The stage4 leaf N concentration.</value>
		[Description("N concentration of leaves at stage 4 (dead)")]
		[Units("kgN/kgDM")]
		public double LeafStage4NConc
		{
			get { return MathUtilities.Divide(Nleaf4, dmLeaf4, 0.0); }
		}

		/// <summary>Gets the N concentration of stems at stage1 (developing).</summary>
		/// <value>The stage1 stem N concentration.</value>
		[Description("N concentration of stems at stage 1 (developing)")]
		[Units("kgN/kgDM")]
		public double StemStage1NConc
		{
			get { return MathUtilities.Divide(Nstem1, dmStem1, 0.0); }
		}

		/// <summary>Gets the N concentration of stems at stage2 (mature).</summary>
		/// <value>The stage2 stem N concentration.</value>
		[Description("N concentration of stems at stage 2 (mature)")]
		[Units("kgN/kgDM")]
		public double StemStage2NConc
		{
			get { return MathUtilities.Divide(Nstem2, dmStem2, 0.0); }
		}

		/// <summary>Gets the N concentration of stems at stage3 (senescing).</summary>
		/// <value>The stage3 stem N concentration.</value>
		[Description("N concentration of stems at stage 3 (senescing)")]
		[Units("kgN/kgDM")]
		public double StemStage3NConc
		{
			get { return MathUtilities.Divide(Nstem3, dmStem3, 0.0); }
		}

		/// <summary>Gets the N concentration of stems at stage4 (dead).</summary>
		/// <value>The stage4 stem N concentration.</value>
		[Description("N concentration of stems at stage 4 (dead)")]
		[Units("kgN/kgDM")]
		public double StemStage4NConc
		{
			get { return MathUtilities.Divide(Nstem4, dmStem4, 0.0); }
		}

		/// <summary>Gets the N concentration of stolons at stage1 (developing).</summary>
		/// <value>The stage1 stolon N concentration.</value>
		[Description("N concentration of stolons at stage 1 (developing)")]
		[Units("kgN/kgDM")]
		public double StolonStage1NConc
		{
			get { return MathUtilities.Divide(Nstolon1, dmStolon1, 0.0); }
		}

		/// <summary>Gets the N concentration of stolons at stage2 (mature).</summary>
		/// <value>The stage2 stolon N concentration.</value>
		[Description("N concentration of stolons at stage 2 (mature)")]
		[Units("kgN/kgDM")]
		public double StolonStage2NConc
		{
			get { return MathUtilities.Divide(Nstolon2, dmStolon2, 0.0); }
		}

		/// <summary>Gets the N concentration of stolons at stage3 (senescing).</summary>
		/// <value>The stage3 stolon N concentration.</value>
		[Description("N concentration of stolons at stage 3 (senescing)")]
		[Units("kgN/kgDM")]
		public double StolonStage3NConc
		{
			get { return MathUtilities.Divide(Nstolon3, dmStolon3, 0.0); }
		}

		/// <summary>Gets the N concentration in new grown tissue.</summary>
		/// <value>The actual growth N concentration.</value>
		[Description("Concentration of N in new growth")]
		[Units("kgN/kgDM")]
		public double ActualGrowthNConc
		{
			get { return MathUtilities.Divide(newGrowthN, dGrowthActual, 0.0); }
		}


		#endregion

		#region - N flows  -------------------------------------------------------------------------------------------------

		/// <summary>Gets amount of N remobilisable from senesced tissue.</summary>
		/// <value>The remobilisable N amount.</value>
		[Description("Amount of N remobilisable from senesced material")]
		[Units("kgN/ha")]
		public double RemobilisableN
		{
			get { return NRemobilised; }
		}

		/// <summary>Gets the amount of N remobilised from senesced tissue.</summary>
		/// <value>The remobilised N amount.</value>
		[Description("Amount of N remobilised from senesced material")]
		[Units("kgN/ha")]
		public double RemobilisedN
		{
			get { return Nremob2NewGrowth; }
		}

		/// <summary>Gets the amount of luxury N potentially remobilisable.</summary>
		/// <value>The remobilisable luxury N amount.</value>
		[Description("Amount of luxury N potentially remobilisable")]
		[Units("kgN/ha")]
		public double RemobilisableLuxuryN
		{
			get { return NLuxury2 + NLuxury3; }
		}

		/// <summary>Gets the amount of luxury N remobilised.</summary>
		/// <value>The remobilised luxury N amount.</value>
		[Description("Amount of luxury N remobilised")]
		[Units("kgN/ha")]
		public double RemobilisedLuxuryN
		{
			get { return NFastRemob2 + NFastRemob3; }
		}

		/// <summary>Gets the amount of luxury N potentially remobilisable from tissue 2.</summary>
		/// <value>The remobilisable luxury N amoount.</value>
		[Description("Amount of luxury N potentially remobilisable from tissue 2")]
		[Units("kgN/ha")]
		public double RemobT2LuxuryN
		{
			get { return NLuxury2; }
		}

		/// <summary>Gets the amount of luxury N potentially remobilisable from tissue 3.</summary>
		/// <value>The remobilisable luxury N amount.</value>
		[Description("Amount of luxury N potentially remobilisable from tissue 3")]
		[Units("kgN/ha")]
		public double RemobT3LuxuryN
		{
			get { return NLuxury3; }
		}

		/// <summary>Gets the amount of atmospheric N fixed.</summary>
		/// <value>The fixed N amount.</value>
		[Description("Amount of atmospheric N fixed")]
		[Units("kgN/ha")]
		public double FixedN
		{
			get { return Nfixation; }
		}

		/// <summary>Gets the amount of N required with luxury uptake.</summary>
		/// <value>The required N with luxury.</value>
		[Description("Amount of N required with luxury uptake")]
		[Units("kgN/ha")]
		public double RequiredLuxuryN
		{
			get { return NdemandLux; }
		}

		/// <summary>Gets the amount of N required for optimum N content.</summary>
		/// <value>The required optimum N amount.</value>
		[Description("Amount of N required for optimum growth")]
		[Units("kgN/ha")]
		public double RequiredOptimumN
		{
			get { return NdemandOpt; }
		}

		/// <summary>Gets the amount of N demanded from soil.</summary>
		/// <value>The N demand from soil.</value>
		[Description("Amount of N demanded from soil")]
		[Units("kgN/ha")]
		public double DemandSoilN
		{
			get { return mySoilNDemand; }
		}

		/// <summary>Gets the amount of plant available N in the soil.</summary>
		/// <value>The soil available N.</value>
		[Description("Amount of N available in the soil")]
		[Units("kgN/ha")]
		public double[] SoilAvailableN
		{
			get { return mySoilAvailableN; }
		}

		/// <summary>Gets the amount of N taken up from soil.</summary>
		/// <value>The N uptake.</value>
		[Description("Amount of N uptake")]
		[Units("kgN/ha")]
		public double[] UptakeN
		{
			get { return mySoilNitrogenTakenUp; }
		}

		/// <summary>Gets the amount of N deposited as litter onto soil surface.</summary>
		/// <value>The litter N amount.</value>
		[Description("Amount of N deposited as litter onto soil surface")]
		[Units("kgN/ha")]
		public double LitterN
		{
			get { return dNlitter; }
		}

		/// <summary>Gets the amount of N from senesced roots added to soil FOM.</summary>
		/// <value>The senesced root N amount.</value>
		[Description("Amount of N from senesced roots added to soil FOM")]
		[Units("kgN/ha")]
		public double SenescedRootN
		{
			get { return dNrootSen; }
		}

		/// <summary>Gets the amount of N in new grown tissue.</summary>
		/// <value>The actual growth N amount.</value>
		[Description("Amount of N in new growth")]
		[Units("kgN/ha")]
		public double ActualGrowthN
		{
			get { return newGrowthN; }
		}

		#endregion

		#region - Turnover rates and DM allocation  ------------------------------------------------------------------------

		/// <summary>Gets the turnover rate for live DM (leaves, stems and sheath).</summary>
		/// <value>The turnover rate for live DM.</value>
		[Description("Turnover rate for live DM (leaves and stem)")]
		[Units("0-1")]
		public double LiveDMTurnoverRate
		{
			get { return gama; }
		}

		/// <summary>Gets the turnover rate for dead DM (leaves, stems and sheath).</summary>
		/// <value>The turnover rate for dead DM.</value>
		[Description("Turnover rate for dead DM (leaves and stem)")]
		[Units("0-1")]
		public double DeadDMTurnoverRate
		{
			get { return gamaD; }
		}

		/// <summary>Gets the turnover rate for live DM in stolons.</summary>
		/// <value>The turnover rate for stolon DM.</value>
		[Description("DM turnover rate for stolons")]
		[Units("0-1")]
		public double StolonDMTurnoverRate
		{
			get { return gamaS; }
		}

		/// <summary>Gets the turnover rate for live DM in roots.</summary>
		/// <value>The turnover rate for root DM.</value>
		[Description("DM turnover rate for roots")]
		[Units("0-1")]
		public double RootDMTurnoverRate
		{
			get { return gamaR; }
		}

		/// <summary>Gets the DM allocation to shoot.</summary>
		/// <value>The shoot DM allocation.</value>
		[Description("Fraction of DM allocated to Shoot")]
		[Units("0-1")]
		public double ShootDMAllocation
		{
			get { return fShoot; }
		}

		/// <summary>Gets the DM allocation to roots.</summary>
		/// <value>The root dm allocation.</value>
		[Description("Fraction of DM allocated to roots")]
		[Units("0-1")]
		public double RootDMAllocation
		{
			get { return 1 - fShoot; }
		}

		#endregion

		#region - LAI and cover  -------------------------------------------------------------------------------------------

		/// <summary>Gets the total plant LAI (leaf area index).</summary>
		/// <value>The total LAI.</value>
		[Description("Total leaf area index")]
		[Units("m^2/m^2")]
		public double TotalLAI
		{
			get { return greenLAI + deadLAI; }
		}

		/// <summary>Gets the plant's green LAI (leaf area index).</summary>
		/// <value>The green LAI.</value>
		[Description("Leaf area index of green leaves")]
		[Units("m^2/m^2")]
		public double GreenLAI
		{
			get { return greenLAI; }
		}

		/// <summary>Gets the plant's dead LAI (leaf area index).</summary>
		/// <value>The dead LAI.</value>
		[Description("Leaf area index of dead leaves")]
		[Units("m^2/m^2")]
		public double DeadLAI
		{
			get { return deadLAI; }
		}

		/// <summary>Gets the irradiance on top of canopy.</summary>
		/// <value>The irradiance on top of canopy.</value>
		[Description("Irridance on the top of canopy")]
		[Units("W.m^2/m^2")]
		public double IrradianceTopCanopy
		{
			get { return IL; }
		}

		/// <summary>Gets the plant's total cover.</summary>
		/// <value>The total cover.</value>
		[Description("Fraction of soil covered by plants")]
		[Units("%")]
		public double TotalCover
		{
			get
			{
				if (greenLAI + deadLAI == 0) return 0;
				return (1.0 - (Math.Exp(-lightExtentionCoeff * (greenLAI + deadLAI))));
			}
		}

		/// <summary>Gets the plant's green cover.</summary>
		/// <value>The green cover.</value>
		[Description("Fraction of soil covered by green leaves")]
		[Units("%")]
		public double GreenCover
		{
			get
			{
				if (greenLAI == 0)
					return 0.0;
				else
					return (1.0 - Math.Exp(-lightExtentionCoeff * greenLAI));
			}
		}

		/// <summary>Gets the plant's dead cover.</summary>
		/// <value>The dead cover.</value>
		[Description("Fraction of soil covered by dead leaves")]
		[Units("%")]
		public double DeadCover
		{
			get
			{
				if (deadLAI == 0)
					return 0.0;
				else
					return (1.0 - Math.Exp(-lightExtentionCoeff * deadLAI));
			}
		}

		#endregion

		#region - Root depth and distribution  -----------------------------------------------------------------------------

		/// <summary>Gets the root depth.</summary>
		/// <value>The root depth.</value>
		[Description("Depth of roots")]
		[Units("mm")]
		public double RootDepth
		{
			get { return myRootDepth; }
		}

		/// <summary>Gets the root frontier.</summary>
		/// <value>The layer at bottom of root zone.</value>
		[Description("Layer at bottom of root zone")]
		[Units("mm")]
		public double RootFrontier
		{
			get { return myRootFrontier; }
		}

		/// <summary>Gets the fraction of root dry matter for each soil layer.</summary>
		/// <value>The root fraction.</value>
		[Description("Fraction of root dry matter for each soil layer")]
		[Units("0-1")]
		public double[] RootWtFraction
		{
			get { return rootFraction; }
		}

		/// <summary>Gets the plant's root length density for each soil layer.</summary>
		/// <value>The root length density.</value>
		[Description("Root length density")]
		[Units("mm/mm^3")]
		public double[] RLD
		{
			get
			{
				double[] result = new double[nLayers];
				double Total_Rlength = dmRoot * specificRootLength;   // m root/ha
				Total_Rlength *= 0.0000001;  // convert into mm root/mm2 soil)
				for (int layer = 0; layer < result.Length; layer++)
				{
					result[layer] = rootFraction[layer] * Total_Rlength / Soil.Thickness[layer];    // mm root/mm3 soil
				}
				return result;
			}
		}

		#endregion

		#region - Water amounts  -------------------------------------------------------------------------------------------

		/// <summary>Gets the lower limit of soil water content for plant uptake.</summary>
		/// <value>The water uptake lower limit.</value>
		[Description("Lower limit of soil water content for plant uptake")]
		[Units("mm^3/mm^3")]
		public double[] LL
		{
			get
			{
				SoilCrop soilInfo = (SoilCrop)Soil.Crop(Name);
				return soilInfo.LL;
			}
		}

		/// <summary>Gets the amount of water demanded by the plant.</summary>
		/// <value>The water demand.</value>
		[Description("Plant water demand")]
		[Units("mm")]
		public double WaterDemand
		{
			get { return myWaterDemand; }
		}

		/// <summary>Gets the amount of soil water available for uptake.</summary>
		/// <value>The soil available water.</value>
		[Description("Plant availabe water")]
		[Units("mm")]
		public double[] SoilAvailableWater
		{
			get { return mySoilAvailableWater; }
		}

		/// <summary>Gets the amount of water taken up by the plant.</summary>
		/// <value>The water uptake.</value>
		[Description("Plant water uptake")]
		[Units("mm")]
		public double[] WaterUptake
		{
			get { return mySoilWaterTakenUp; }
		}

		#endregion

		#region - Growth limiting factors  ---------------------------------------------------------------------------------

		/// <summary>Gets the growth limiting factor due to N availability.</summary>
		/// <value>The growth limiting factor due to N.</value>
		[Description("Growth limiting factor due to nitrogen")]
		[Units("0-1")]
		public double GlfN
		{
			get { return glfN; }
		}

		/// <summary>Gets the growth limiting factor due to N concentration in the plant.</summary>
		/// <value>The growth limiting factor due to N concentration.</value>
		[Description("Plant growth limiting factor due to plant N concentration")]
		[Units("0-1")]
		public double GlfNConcentration
		{
			get { return NcFactor; }
		}

		/// <summary>Gets the growth limiting factor due to temperature.</summary>
		/// <value>The growth limiting factor due to temperature.</value>
		[Description("Growth limiting factor due to temperature")]
		[Units("0-1")]
		public double GlfTemperature
		{
			get { return TemperatureLimitingFactor(TmeanW); }
		}

		/// <summary>Gets the growth limiting factor due to water availability.</summary>
		/// <value>The growth limiting factor due to water.</value>
		[Description("Growth limiting factor due to water deficit")]
		[Units("0-1")]
		public double GlfWater
		{
			get { return glfWater; }
		}

		// TODO: verify that this is really needed
		/// <summary>Gets the vapour pressure deficit factor.</summary>
		/// <value>The vapour pressure deficit factor.</value>
		[Description("Effect of vapour pressure on growth (used by micromet)")]
		[Units("0-1")]
		public double FVPD
		{
			get { return FVPDFunction.Value(VPD()); }
		}

		#endregion

		#region - Harvest variables  ---------------------------------------------------------------------------------------

		/// <summary>Gets the amount of dry matter harvestable (leaf + stem).</summary>
		/// <value>The harvestable DM weight.</value>
		[Description("Amount of dry matter harvestable (leaf+stem)")]
		[Units("kgDM/ha")]
		public double HarvestableWt
		{
			get { return Math.Max(0.0, StandingLiveWt - minimumGreenWt) + Math.Max(0.0, StandingDeadWt - minimumDeadWt); }
		}

		/// <summary>Gets the amount of dry matter harvested.</summary>
		/// <value>The harvested DM weight.</value>
		[Description("Amount of plant dry matter removed by harvest")]
		[Units("kgDM/ha")]
		public double HarvestedWt
		{
			get { return dmDefoliated; }
		}

		/// <summary>Gets the fraction of the plant that was harvested.</summary>
		/// <value>The fraction harvested.</value>
		[Description("Fraction harvested")]
		[Units("0-1")]
		public double HarvestedFraction
		{
			get { return fractionHarvested; }
		}

		/// <summary>Gets the amount of plant N removed by harvest.</summary>
		/// <value>The harvested N amount.</value>
		[Description("Amount of plant nitrogen removed by harvest")]
		[Units("kgN/ha")]
		public double HarvestedN
		{
			get { return Ndefoliated; }
		}

		/// <summary>Gets the N concentration in harvested DM.</summary>
		/// <value>The N concentration in harvested DM.</value>
		[Description("average N concentration of harvested material")]
		[Units("kgN/kgDM")]
		public double HarvestedNconc
		{
			get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
		}

		/// <summary>Gets the average herbage digestibility.</summary>
		/// <value>The herbage digestibility.</value>
		[Description("Average digestibility of herbage")]
		[Units("0-1")]
		public double HerbageDigestibility
		{
			get { return digestHerbage; }
		}

		// TODO: Digestibility of harvested material should be better calculated (consider fraction actually removed)
		/// <summary>Gets the average digestibility of harvested DM.</summary>
		/// <value>The harvested digestibility.</value>
		[Description("Average digestibility of harvested meterial")]
		[Units("0-1")]
		public double HarvestedDigestibility
		{
			get { return digestDefoliated; }
		}

		/// <summary>Gets the average herbage ME (metabolisable energy).</summary>
		/// <value>The herbage ME.</value>
		[Description("Average ME of herbage")]
		[Units("(MJ/ha)")]
		public double HerbageME
		{
			get { return 16 * digestHerbage * StandingWt; }
		}

		/// <summary>Gets the average ME (metabolisable energy) of harvested DM.</summary>
		/// <value>The harvested ME.</value>
		[Description("Average ME of harvested material")]
		[Units("(MJ/ha)")]
		public double HarvestedME
		{
			get { return 16 * digestDefoliated * HarvestedWt; }
		}

		#endregion

		#endregion

		#region Private variables  -----------------------------------------------------------------------------------------

		/// <summary>flag whether several routines are ran by species or are controlled by the Swar</summary>
		internal bool isSwardControlled = false;

		/// <summary>flag whether this species is alive (activelly growing)</summary>
		private bool isAlive = true;

		// defining the plant type  -----------------------------------------------------------------------------------

		/// <summary>flag this species type, annual or perennial</summary>
		private bool isAnnual = false;
		/// <summary>flag whether this species is a legume</summary>
		private bool isLegume = false;

		// TODO: do we want to keep this??
		// Parameters for annual species  -----------------------------------------------------------------------------

		/// <summary>The day of year for emergence</summary>
		private int dayEmerg = 0;
		/// <summary>The monthe of emergence</summary>
		private int monEmerg = 0;
		/// <summary>The day of anthesis</summary>
		private int dayAnth = 0;
		/// <summary>The month of anthesis</summary>
		private int monAnth = 0;
		/// <summary>The number of days to mature</summary>
		private int daysToMature = 0;
		/// <summary>The number of days between emergence and anthesis</summary>
		private int daysEmgToAnth = 0;
		/// <summary>The phenologic stage (0= pre_emergence, 1= vegetative, 2= reproductive)</summary>
		private int phenoStage = 1;
		///// <summary>The phenologic factor</summary>
		//private double phenoFactor = 1;
		/// <summary>The number of days from emergence</summary>
		private int daysfromEmergence = 0;
		/// <summary>The number of days from anthesis</summary>
		private int daysfromAnthesis = 0;

		/// <summary>The daily variation in root depth</summary>
		private double dRootDepth = 50;
		/// <summary>The maximum root depth</summary>
		private double maxRootDepth = 900;

		// DM in various plant parts and tissue pools (kg DM/ha)  -----------------------------------------------------

		/// <summary>The total plant DM</summary>
		private double dmTotal;
		/// <summary>The DM in shoot</summary>
		private double dmShoot;
		/// <summary>The DM in green parts 9live above ground)</summary>
		private double dmGreen;
		/// <summary>The DM of dead parts</summary>
		private double dmDead;
		/// <summary>The DM of all leaves</summary>
		private double dmLeaf;
		/// <summary>The DM of all stems and sheath</summary>
		private double dmStem;
		/// <summary>The DM of all stolons</summary>
		private double dmStolon;
		/// <summary>The DM of roots</summary>
		private double dmRoot;

		/// <summary>The DM of leaf stage1</summary>
		private double dmLeaf1;
		/// <summary>The DM of leaf stage2</summary>
		private double dmLeaf2;
		/// <summary>The DM of leaf stage3</summary>
		private double dmLeaf3;
		/// <summary>The DM of leaf stage4</summary>
		private double dmLeaf4;
		/// <summary>The DM of stem and sheath stage1</summary>
		private double dmStem1;
		/// <summary>The DM of stem and sheath  stage2</summary>
		private double dmStem2;
		/// <summary>The DM of stem and sheath stage3</summary>
		private double dmStem3;
		/// <summary>The DM of stem and sheath  stage4</summary>
		private double dmStem4;
		/// <summary>The DM stolon stage1</summary>
		private double dmStolon1;
		/// <summary>The DM of stolon stage2</summary>
		private double dmStolon2;
		/// <summary>The DM of stolon stage3</summary>
		private double dmStolon3;

		// N concentration thresholds for various tissues (set relative to leaf N)  -----------------------------------

		/// <summary>The optimum N concentration of stems and sheath</summary>
		private double NcStemOpt;
		/// <summary>The optimum N concentration of stolons</summary>
		private double NcStolonOpt;
		/// <summary>The optimum N concentration of roots</summary>
		private double NcRootOpt;
		/// <summary>The maximum N concentration of stems andd sheath</summary>
		private double NcStemMax;
		/// <summary>The maximum N concentration of stolons</summary>
		private double NcStolonMax;
		/// <summary>The maximum N concentration of roots</summary>
		private double NcRootMax;
		/// <summary>The minimum N concentration of stems and sheath</summary>
		private double NcStemMin;
		/// <summary>The minimum N concentration of stolons</summary>
		private double NcStolonMin;
		/// <summary>The minimum N concentration of roots</summary>
		private double NcRootMin;

		// N amount in various plant parts and tissue pools (kg N/ha)  ------------------------------------------------

		/// <summary>The amount of N above ground (shoot)</summary>
		private double Nshoot;

		/// <summary>The amount of N in leaf1</summary>
		private double Nleaf1;
		/// <summary>The amount of N in leaf2</summary>
		private double Nleaf2;
		/// <summary>The amount of N in leaf3</summary>
		private double Nleaf3;
		/// <summary>The amount of N in leaf4</summary>
		private double Nleaf4;
		/// <summary>The amount of N in stem1</summary>
		private double Nstem1;
		/// <summary>The amount of N in stem2</summary>
		private double Nstem2;
		/// <summary>The amount of N in stem3</summary>
		private double Nstem3;
		/// <summary>The amount of N in stem4</summary>
		private double Nstem4;
		/// <summary>The amount of N in stolon1</summary>
		private double Nstolon1;
		/// <summary>The amount of N in stolon2</summary>
		private double Nstolon2;
		/// <summary>The amount of N in stolon3</summary>
		private double Nstolon3;
		/// <summary>The amount of N in roots</summary>
		private double Nroot;

		// Amounts and fluxes of N in the plant  ----------------------------------------------------------------------

		/// <summary>The N demand for new growth, with luxury uptake</summary>
		private double NdemandLux;
		/// <summary>The N demand for new growth, at optimum N content</summary>
		private double NdemandOpt;
		/// <summary>The amount of N fixation from atmosphere (for legumes)</summary>
		internal double Nfixation = 0.0;
		/// <summary>The amount of N remobilised from senesced tissue</summary>
		private double NRemobilised = 0.0;
		/// <summary>The amount of N actually remobilised to new growth</summary>
		private double Nremob2NewGrowth = 0.0;
		/// <summary>The amount of N used in new growth</summary>
		internal double newGrowthN = 0.0;
		/// <summary>The aount of luxury N (above Nopt) in tissue 2 potentially remobilisable</summary>
		private double NLuxury2;
		/// <summary>The amount of luxury N (above Nopt) in tissue 3 potentially remobilisable</summary>
		private double NLuxury3;
		/// <summary>The amount of luxury N actually remobilised from tissue 2</summary>
		private double NFastRemob2 = 0.0;
		/// <summary>The amount of luxury N actually remobilised from tissue 3</summary>
		private double NFastRemob3 = 0.0;

		// N uptake process  ------------------------------------------------------------------------------------------

		///// <summary>The amount of N demanded for new growth</summary>
		//private double myNitrogenDemand = 0.0;
		/// <summary>The amount of N in the soil available to the plant</summary>
		internal double[] mySoilAvailableN;
		/// <summary>The amount of NH4 in the soil available to the plant</summary>
		internal double[] mySoilNH4available;
		/// <summary>The amount of NO3 in the soil available to the plant</summary>
		internal double[] mySoilNO3available;
		/// <summary>The amount of N demanded from the soil</summary>
		private double mySoilNDemand;
		/// <summary>The amount of N actually taken up</summary>
		internal double mySoilNuptake;
		/// <summary>The amount of N uptake from each soil layer</summary>
		internal double[] mySoilNitrogenTakenUp;

		// water uptake process  --------------------------------------------------------------------------------------

		/// <summary>The amount of water demanded for new growth</summary>
		internal double myWaterDemand = 0.0;
		/// <summary>The amount of soil available water</summary>
		private double[] mySoilAvailableWater;
		/// <summary>The amount of soil water taken up</summary>
		internal double[] mySoilWaterTakenUp;

		// harvest and digestibility  ---------------------------------------------------------------------------------

		/// <summary>The DM amount harvested (defoliated)</summary>
		private double dmDefoliated = 0.0;
		/// <summary>The N amount harvested (defoliated)</summary>
		private double Ndefoliated = 0.0;
		/// <summary>The digestibility of herbage</summary>
		private double digestHerbage = 0.0;
		/// <summary>The digestibility of defoliated material</summary>
		private double digestDefoliated = 0.0;
		/// <summary>The fraction of standing DM harvested</summary>
		internal double fractionHarvested = 0.0;

		// Plant height, LAI and cover  -------------------------------------------------------------------------------

		/// <summary>The plant's average height</summary>
		private double myHeight;
		/// <summary>The plant's green LAI</summary>
		private double greenLAI;
		/// <summary>The plant's dead LAI</summary>
		private double deadLAI;

		// root variables  --------------------------------------------------------------------------------------------

		/// <summary>The plant's root depth</summary>
		private double myRootDepth = 0.0;
		/// <summary>The layer at the bottom of the root zone</summary>
		private int myRootFrontier = 1;
		/// <summary>The fraction of roots DM in each layer</summary>
		private double[] rootFraction;
		/// <summary>The maximum shoot-root ratio</summary>
		private double maxSRratio;

		/// <summary>The fraction of each layer that is actually explored by roots (0-1)</summary>
		private double[] myRootExplorationFactor;

		// photosynthesis, growth and turnover  -----------------------------------------------------------------------

		/// <summary>The irradiance on top of canopy</summary>
		private double IL;
		/// <summary>The gross photosynthesis rate (C assimilation)</summary>
		private double Pgross = 0.0;
		/// <summary>The growth respiration rate (C loss)</summary>
		private double Resp_g = 0.0;
		/// <summary>The maintenance respiration rate (C loss)</summary>
		private double Resp_m = 0.0;
		/// <summary>The amount of C remobilised from senesced tissue</summary>
		private double CRemobilised = 0.0;

		/// <summary>Daily net growth potential (kgDM/ha)</summary>
		private double dGrowthPot;
		/// <summary>Daily potential growth after water stress</summary>
		private double dGrowthWstress;
		/// <summary>Daily growth after nutrient stress (actual growth)</summary>
		private double dGrowthActual;

		/// <summary>Effective growth of roots</summary>
		private double dGrowthRoot;
		/// <summary>Effective growth of shoot (herbage growth)</summary>
		private double dGrowthShoot;
		/// <summary>Effective plant growth (actual growth minus senescence)</summary>
		private double dGrowthEff;

		/// <summary>Daily litter production (dead to surface OM)</summary>
		private double dLitter;
		/// <summary>N amount in litter procuded</summary>
		private double dNlitter;
		/// <summary>Daily root sennesce (added to soil FOM)</summary>
		private double dRootSen;
		/// <summary>N amount in senesced roots</summary>
		private double dNrootSen;

		/// <summary>Fraction of growth allocated to shoot (0-1)</summary>
		private double fShoot;

		/// <summary>The daily DM turnover rate (from tissue 1 to 2, then to 3, then to 4)</summary>
		private double gama = 0.0;
		/// <summary>The daily DM turnover rate for stolons</summary>
		private double gamaS = 0.0;	  // for stolons
		/// <summary>The daily DM turnover rate for dead tissue (from tissue 4 to litter)</summary>
		private double gamaD = 0.0;
		/// <summary>The daily DM turnover rate for roots</summary>
		private double gamaR = 0.0;

		// growth limiting factors ------------------------------------------------------------------------------------
		/// <summary>The GLF due to water stress</summary>
		internal double glfWater = 1.0;
		// private double glfTemp;   //The GLF due to temperature stress
		/// <summary>The GLF due to N stress</summary>
		internal double glfN = 0.0;
		/// <summary>The growth factor for N concentration</summary>
		private double NcFactor = 1.0;

		// auxiliary variables for radiation and temperature stress  --------------------------------------------------

		/// <summary>Growth rate reduction factor due to high temperatures</summary>
		private double highTempEffect = 1.0;
		/// <summary>Growth rate reduction factor due to low temperatures</summary>
		private double lowTempEffect = 1.0;
		/// <summary>Cumulative degress of temperature for recovery from heat damage</summary>
		private double accumT4Heat = 0.0;
		/// <summary>Cumulative degress of temperature for recovry from cold damage</summary>
		private double accumT4Cold = 0.0;

		// general auxiliary variables  -------------------------------------------------------------------------------

		/// <summary>Number of layers in the soil</summary>
		private int nLayers = 0;
		/// <summary>Today's average temperature</summary>
		private double Tmean;
		/// <summary>Today's weighted mean temperature</summary>
		private double TmeanW;
		/// <summary>State for this plant on the previous day</summary>
		private SpeciesState prevState;

		// TODO: Hope to get rid of these soon  --------------------------------------------------------
		/// <summary>fraction of Radn intercepted by this species</summary>
		internal double intRadnFrac = 1.0;
		/// <summary>Light extintion coefficient for all species</summary>
		internal double swardLightExtCoeff;
		/// <summary>average green cover for all species</summary>
		internal double swardGreenCover;

		#endregion

		#region Constants  -------------------------------------------------------------------------------------------------

		/// <summary>Average carbon content in plant dry matter</summary>
		const double CarbonFractionInDM = 0.4;

		/// <summary>Factor for converting nitrogen to protein</summary>
		const double NitrogenToProteinFactor = 6.25;

		/// <summary>The C:N ratio of protein</summary>
		const double CNratioProtein = 3.5;

		/// <summary>The C:N ratio of cell wall</summary>
		const double CNratioCellWall = 100.0;

		#endregion

		#region Initialisation methods  ------------------------------------------------------------------------------------

		/// <summary>Performs the initialisation procedures for this species (set DM, N, LAI, etc)</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// get the number of layers in the soil profile
			nLayers = Soil.Thickness.Length;

			// initialise soil water and N variables
			InitiliaseSoilArrays();

			// set initial plant state
			SetInitialState();

			// initialise the class which will hold yesterday's plant state
			prevState = new SpeciesState();

			// check whether uptake is done here or by another module
			if (apsimArbitrator != null)
			{
				myWaterUptakeSource = "arbitrator";
				myNitrogenUptakeSource = "arbitrator";
			}

			// tell other modules about the existence of this species
			if (!isSwardControlled)
				DoNewCropEvent();
		}

		/// <summary>
		/// Initialise arrays to same length as soil layers
		/// </summary>
		private void InitiliaseSoilArrays()
		{
			mySoilAvailableWater = new double[nLayers];
			mySoilWaterTakenUp = new double[nLayers];
			mySoilNH4available = new double[nLayers];
			mySoilNO3available = new double[nLayers];
			mySoilAvailableN = new double[nLayers];
			mySoilNitrogenTakenUp = new double[nLayers];
		}

		/// <summary>
		/// Set the initial parameters for this plant, including DM and N content of various pools plus plant height and root depth
		/// </summary>
		private void SetInitialState()
		{
			// 1. Initialise DM of various tissue pools, user should supply initial values for shoot and root
			dmTotal = iniDMShoot + iniDMRoot;

			// set initial DM fractions - Temporary?? TODO
			if (initialDMFractions == null)
			{
				if (isLegume)
					initialDMFractions = initialDMFractions_legume;
				else
					initialDMFractions = initialDMFractions_grass;
			}

			dmLeaf1 = iniDMFraction[0] * iniDMShoot;
			dmLeaf2 = iniDMFraction[1] * iniDMShoot;
			dmLeaf3 = iniDMFraction[2] * iniDMShoot;
			dmLeaf4 = iniDMFraction[3] * iniDMShoot;
			dmStem1 = iniDMFraction[4] * iniDMShoot;
			dmStem2 = iniDMFraction[5] * iniDMShoot;
			dmStem3 = iniDMFraction[6] * iniDMShoot;
			dmStem4 = iniDMFraction[7] * iniDMShoot;
			dmStolon1 = iniDMFraction[8] * iniDMShoot;
			dmStolon2 = iniDMFraction[9] * iniDMShoot;
			dmStolon3 = iniDMFraction[10] * iniDMShoot;

			dmRoot = iniDMRoot;

			// 2. Initialise N content thresholds (optimum, maximum, and minimum)
			NcStemOpt = leafNopt * relativeNStems;
			NcStolonOpt = leafNopt * relativeNStolons;
			NcRootOpt = leafNopt * relativeNRoots;

			NcStemMax = leafNmax * relativeNStems;
			NcStolonMax = leafNmax * relativeNStolons;
			NcRootMax = leafNmax * relativeNRoots;

			NcStemMin = leafNmin * relativeNStems;
			NcStolonMin = leafNmin * relativeNStolons;
			NcRootMin = leafNmin * relativeNRoots;

			// 3. Initialise the N amounts in each pool (assume to be at optimum)
			Nleaf1 = dmLeaf1 * leafNopt;
			Nleaf2 = dmLeaf2 * leafNopt;
			Nleaf3 = dmLeaf3 * leafNopt;
			Nleaf4 = dmLeaf4 * leafNmin;
			Nstem1 = dmStem1 * NcStemOpt;
			Nstem2 = dmStem2 * NcStemOpt;
			Nstem3 = dmStem3 * NcStemOpt;
			Nstem4 = dmStem4 * NcStemMin;
			Nstolon1 = dmStolon1 * NcStolonOpt;
			Nstolon2 = dmStolon2 * NcStolonOpt;
			Nstolon3 = dmStolon3 * NcStolonOpt;
			Nroot = dmRoot * NcRootOpt;

			// 4. Root depth and distribution
			myRootDepth = iniRootDepth;
			myRootExplorationFactor = new double[nLayers];
			double cumDepth = 0.0;
			for (int layer = 0; layer < nLayers; layer++)
			{
				cumDepth += Soil.Thickness[layer];
				if (cumDepth <= myRootDepth)
				{
					myRootFrontier = layer;
					myRootExplorationFactor[layer] = LayerFractionWithRoots(layer);
				}
				else
				{
					layer = Soil.Thickness.Length;
				}
			}
			rootFraction = RootProfileDistribution();
			InitialiseRootsProperties();

			// 5. Canopy height and related variables
			myHeight = Math.Max(20.0, HeightFromMass.Value(StandingWt));  // TODO:update this approach
			InitialiseCanopy();
			// maximum shoot:root ratio
			maxSRratio = (1 - MaxRootFraction) / MaxRootFraction;

			// 6. Set initial phenological stage
			if (dmTotal == 0.0)
				phenoStage = 0;
			else
				phenoStage = 1;

			// 7. aggregated auxiliary DM and N variables
			updateAggregated();

			// 8. Calculate the values for LAI
			EvaluateLAI();
		}

		/// <summary>Initialise the variables in canopy properties</summary>
		private void InitialiseCanopy()
		{
			myCanopyProperties.Name = Name;
			myCanopyProperties.CoverGreen = GreenCover;
			myCanopyProperties.CoverTot = TotalCover;
			myCanopyProperties.CanopyDepth = myHeight;
			myCanopyProperties.CanopyHeight = myHeight;
			myCanopyProperties.LAIGreen = greenLAI;
			myCanopyProperties.LAItot = TotalLAI;
			myCanopyProperties.MaximumStomatalConductance = myStomatalConductanceMax;
			myCanopyProperties.HalfSatStomatalConductance = 200.0;  // TODO: this should be on the UI
			myCanopyProperties.CanopyEmissivity = 0.96;  // TODO: this should be on the UI
			myCanopyProperties.Frgr = FRGR;
		}

		/// <summary>Initialise the variables in root properties</summary>
		private void InitialiseRootsProperties()
		{
			SoilCrop soilCrop = this.Soil.Crop(Name) as SoilCrop;

			myRootProperties.RootDepth = myRootDepth;
			myRootProperties.KL = soilCrop.KL;
			myRootProperties.MinNO3ConcForUptake = new double[Soil.Thickness.Length];
			myRootProperties.MinNH4ConcForUptake = new double[Soil.Thickness.Length];
			myRootProperties.KNO3 = myKNO3;
			myRootProperties.KNH4 = myKNH4;

			myRootProperties.LowerLimitDep = new double[Soil.Thickness.Length];
			myRootProperties.UptakePreferenceByLayer = new double[Soil.Thickness.Length];
			myRootProperties.RootExplorationByLayer = new double[Soil.Thickness.Length];
			for (int layer = 0; layer < Soil.Thickness.Length; layer++)
			{
				myRootProperties.LowerLimitDep[layer] = soilCrop.LL[layer] * Soil.Thickness[layer];
				myRootProperties.MinNO3ConcForUptake[layer] = 0.0;
				myRootProperties.MinNH4ConcForUptake[layer] = 0.0;
				myRootProperties.UptakePreferenceByLayer[layer] = 1.0;
				myRootProperties.RootExplorationByLayer[layer] = myRootExplorationFactor[layer];
			}
			myRootProperties.RootLengthDensityByVolume = RLD;
		}

		/// <summary>Calculates the days emg to anth.</summary>
		/// <returns></returns>
		private int CalcDaysEmgToAnth()
		{
			int numbMonths = monAnth - monEmerg;  //emergence & anthesis in the same calendar year: monEmerg < monAnth
			if (monEmerg >= monAnth)			  //...across the calendar year
				numbMonths += 12;

			daysEmgToAnth = (int)(30.5 * numbMonths + (dayAnth - dayEmerg));

			return daysEmgToAnth;
		}

		#endregion

		#region Daily processes  -------------------------------------------------------------------------------------------

		/// <summary>EventHandler - preparation befor the main process</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("DoDailyInitialisation")]
		private void OnDoDailyInitialisation(object sender, EventArgs e)
		{
			// 1. Zero out several variables
			RefreshVariables();

			// mean air temperature for today
			Tmean = (MetData.MaxT + MetData.MinT) * 0.5;
			TmeanW = (MetData.MaxT * 0.75) + (MetData.MinT * 0.25);

			// Send information about this species canopy, MicroClimate will compute intercepted radiation and water demand
			if (!isSwardControlled)
				DoNewCanopyEvent();
		}

		/// <summary>Performs the plant growth calculations</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("DoPlantGrowth")]
		private void OnDoPlantGrowth(object sender, EventArgs e)
		{
			if (!isSwardControlled)
			{
				if (isAlive)
				{
					// stores the current state for this species
					SaveState();

					// step 01 - preparation and potential growth
					CalcPotentialGrowth();

					// Water demand, supply, and uptake
					DoWaterCalculations();

					// step 02 - Potential growth after water limitations
					CalcGrowthWithWaterLimitations();

					// Nitrogen demand, supply, and uptake
					DoNitrogenCalculations();

					// step 03 - Actual growth after nutrient limitations, but before senescence
					CalcActualGrowthAndPartition();

					// step 04 - Effective growth after all limitations and senescence
					CalcTurnoverAndEffectiveGrowth();

					// Send amounts of litter and senesced roots to other modules
					DoSurfaceOMReturn(dLitter, dNlitter);
					DoIncorpFomEvent(dRootSen, dNrootSen);
				}
			}
			//else
			//    Growth is controlled by Sward (all species)
		}

		/// <summary>Calculates the potential growth.</summary>
		internal void CalcPotentialGrowth()
		{
			// update root depth (for annuals only)
			EvaluateRootGrowth();

			// Evaluate the phenologic stage, for annuals
			if (isAnnual)
				phenoStage = annualsPhenology();

			// Compute the potential growth
			if (phenoStage == 0 || greenLAI == 0.0)
			{
				// Growth before germination is null
				Pgross = 0.0;
				Resp_m = 0.0;
				Resp_g = 0.0;
				CRemobilised = 0.0;
				dGrowthPot = 0.0;
			}
			else
			{
				// Gross potential growth (kgC/ha/day)
				Pgross = DailyGrossPotentialGrowth();

				// Respiration (kgC/ha/day)
				Resp_m = DailyMaintenanceRespiration();
				Resp_g = DailyGrowthRespiration();

				// Remobilisation (kgC/ha/day) (got from previous day turnover)

				// Net potential growth (kgDM/ha/day)
				dGrowthPot = DailyNetPotentialGrowth();
			}
		}

		/// <summary>Calculates the growth with water limitations.</summary>
		internal void CalcGrowthWithWaterLimitations()
		{
			// Potential growth after water limitations
			dGrowthWstress = dGrowthPot * Math.Pow(glfWater, waterStressExponent);

			// allocation of todays growth
			fShoot = ToShootFraction();
			//   FL = UpdatefLeaf();
		}

		/// <summary>Calculates the actual growth and partition.</summary>
		internal void CalcActualGrowthAndPartition()
		{
			// Actual daily growth
			dGrowthActual = DailyActualGrowth();

			// Partition growth into various tissues
			PartitionNewGrowth();
		}

		/// <summary>Calculates the turnover and effective growth.</summary>
		internal void CalcTurnoverAndEffectiveGrowth()
		{
			// Compute tissue turnover and remobilisation (C and N)
			TissueTurnoverAndRemobilisation();

			// Effective, or net, growth
			dGrowthEff = dGrowthShoot + dGrowthRoot;

			// Update aggregate variables and digetibility
			updateAggregated();

			// Update LAI
			EvaluateLAI();

			digestHerbage = calcDigestibility();
		}

		#region - Handling and auxilary processes  -------------------------------------------------------------------------

		/// <summary>Refresh the value of several variables</summary>
		internal void RefreshVariables()
		{
			// reset some variables
			dmDefoliated = 0.0;
			Ndefoliated = 0.0;
			digestDefoliated = 0.0;

			// TODO:
			// these are needed when AgPasture controls the growth (not necessarily the partition of soil stuff)
			// this is not sound and we should get rid of it very soon
			intRadnFrac = 1.0;
			if (!isSwardControlled)
			{
				swardLightExtCoeff = lightExtentionCoeff;
				swardGreenCover = CalcPlantCover(greenLAI);
			}
			//else
			//    Sward will set these values

		}

		/// <summary>Stores the current state for this species</summary>
		internal void SaveState()
		{
			prevState.dmLeaf1 = dmLeaf1;
			prevState.dmLeaf2 = dmLeaf2;
			prevState.dmLeaf3 = dmLeaf3;
			prevState.dmLeaf4 = dmLeaf4;

			prevState.dmStem1 = dmStem1;
			prevState.dmStem2 = dmStem2;
			prevState.dmStem3 = dmStem3;
			prevState.dmStem4 = dmStem4;

			prevState.dmStolon1 = dmStolon1;
			prevState.dmStolon2 = dmStolon2;
			prevState.dmStolon3 = dmStolon3;

			prevState.dmRoot = dmRoot;

			prevState.Nleaf1 = Nleaf1;
			prevState.Nleaf2 = Nleaf2;
			prevState.Nleaf3 = Nleaf3;
			prevState.Nleaf4 = Nleaf4;

			prevState.Nstem1 = Nstem1;
			prevState.Nstem2 = Nstem2;
			prevState.Nstem3 = Nstem3;
			prevState.Nstem4 = Nstem4;

			prevState.Nstolon1 = Nstolon1;
			prevState.Nstolon2 = Nstolon2;
			prevState.Nstolon3 = Nstolon3;

			prevState.Nroot = Nroot;
		}

		/// <summary>Computes the value of auxiliary variables (aggregates for DM and N content)</summary>
		/// <exception cref="System.Exception">
		/// Loss of mass balance when aggregating plant dry matter after growth
		/// </exception>
		private void updateAggregated()
		{
			// auxiliary DM variables
			dmLeaf = dmLeaf1 + dmLeaf2 + dmLeaf3 + dmLeaf4;
			dmStem = dmStem1 + dmStem2 + dmStem3 + dmStem4;
			dmStolon = dmStolon1 + dmStolon2 + dmStolon3;
			dmShoot = dmLeaf + dmStem + dmStolon;

			dmGreen = dmLeaf1 + dmLeaf2 + dmLeaf3
					+ dmStem1 + dmStem2 + dmStem3
					+ dmStolon1 + dmStolon2 + dmStolon3;
			dmDead = dmLeaf4 + dmStem4;
			dmTotal = dmShoot + dmRoot;

			if (Math.Abs((dmGreen + dmDead) - dmShoot) > 0.0001)
				throw new Exception("Loss of mass balance when aggregating plant dry matter after growth");

			// auxiliary N variables
			Nshoot = Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4
				   + Nstem1 + Nstem2 + Nstem3 + Nstem4
				   + Nstolon1 + Nstolon2 + Nstolon3;
		}

		/// <summary>
		/// Evaluates the phenologic stage of annual plants, plus days from emergence or from anthesis
		/// </summary>
		/// <returns>An integer representing the plant's phenologic stage</returns>
		private int annualsPhenology()
		{
			int result = 0;
			if (Clock.Today.Month == monEmerg && Clock.Today.Day == dayEmerg)
			{
				result = 1;		 //vegetative stage
				daysfromEmergence++;
			}
			else if (Clock.Today.Month == monAnth && Clock.Today.Day == dayAnth)
			{
				result = 2;		 //reproductive stage
				daysfromAnthesis++;
				if (daysfromAnthesis >= daysToMature)
				{
					phenoStage = 0;
					daysfromEmergence = 0;
					daysfromAnthesis = 0;
				}
			}
			return result;
		}

		/// <summary>Reduction factor for potential growth due to phenology of annual species</summary>
		/// <returns>A factor to reduce plant growth (0-1)</returns>
		private double annualSpeciesReduction()
		{
			double rFactor = 1.0;
			if (phenoStage == 1 && daysfromEmergence < 60)  //decline at the begining due to seed bank effects ???
				rFactor = 0.5 + 0.5 * daysfromEmergence / 60;
			else if (phenoStage == 2)                       //decline of photosynthesis when approaching maturity
				rFactor = 1.0 - (double)daysfromAnthesis / daysToMature;
			return rFactor;
		}

		/// <summary>
		/// Computes the values of LAI (leaf area index) for green, dead, and total plant material
		/// </summary>
		private void EvaluateLAI()
		{
			double greenTissue = dmLeaf1 + dmLeaf2 + dmLeaf3 + (dmStolon * 0.3);  // assuming stolons have 0.3*SLA
			greenTissue /= 10000;   // converted from kg/ha to kg/m2
			greenLAI = greenTissue * specificLeafArea;

			// Adjust accounting for resilience after unfavoured conditions
			if (!isLegume && dmGreen < 1000)
			{
				greenTissue = (dmStem1 + dmStem2 + dmStem3) / 10000;
				greenLAI += greenTissue * specificLeafArea * Math.Sqrt((1000 - dmGreen) / 10000);
			}
			/* 
			 This adjust assumes cover will be bigger for the same amount of DM when DM is low, due to:
			 - light extinction coefficient will be bigger - plant leaves will be more horizontal than in dense high swards
			 - more parts (stems) will turn green for photosysnthesis (?)
			 - quick response of plant shoots to favoured conditions after release of stress
			 » Specific leaf area should be reduced (RCichota2014) - TODO
			 */

			deadLAI = (dmLeaf4 / 10000) * specificLeafArea;
		}

		/// <summary>Compute the average digestibility of aboveground plant material</summary>
		/// <returns>The digestibility of plant material (0-1)</returns>
		private double calcDigestibility()
		{
			if ((dmLeaf + dmStem) <= 0.0)
			{
				return 0.0;
			}

			// fraction of sugar (soluble carbohydrates)  - RCichota: this ignores any stored reserves (TODO: revise this approach)
			double fSugar = 0.5 * MathUtilities.Divide(dGrowthActual, dmGreen, 0.0);

			//Live
			double digestLive = 0.0;
			double Ngreen = Nleaf1 + Nleaf2 + Nleaf3
				          + Nstem1 + Nstem2 + Nstem3
						  + Nstolon1 + Nstolon2 + Nstolon3;
			if (dmGreen > 0.0 & Ngreen > 0.0)
			{
				double CNlive = MathUtilities.Divide(dmGreen * CarbonFractionInDM, Ngreen, 0.0);   //CN ratio of live shoot tissue
				double ratio1 = CNratioCellWall / CNlive;
				double ratio2 = CNratioCellWall / CNratioProtein;
				double fProteinLive = (ratio1 - (1 - fSugar)) / (ratio2 - 1);          //Fraction of protein in living shoot
				double fWallLive = 1 - fSugar - fProteinLive;                          //Fraction of cell wall in living shoot
				digestLive = fSugar + fProteinLive + (digestibilityLive * fWallLive);
			}

			//Dead
			double digestDead = 0;
			if (dmDead > 0.0 && (Nleaf4 + Nstem4) > 0.0)
			{
				double CNdead = MathUtilities.Divide(dmDead * CarbonFractionInDM, Nleaf4 + Nstem4, 0.0);   //CN ratio of standing dead;
				double ratio1 = CNratioCellWall / CNdead;
				double ratio2 = CNratioCellWall / CNratioProtein;
				double fProteinDead = (ratio1 - 1) / (ratio2 - 1);          //Fraction of protein in standing dead
				double fWallDead = 1 - fProteinDead;                        //Fraction of cell wall in standing dead
				digestDead = fProteinDead + digestibilityDead * fWallDead;
			}

			double deadFrac = MathUtilities.Divide(dmDead, dmLeaf + dmStem, 1.0);
			double result = (1 - deadFrac) * digestLive + deadFrac * digestDead;

			return result;
		}

		#endregion

		#region - Plant growth processes  ----------------------------------------------------------------------------------

		/// <summary>
		/// Computes the variations in root depth, including the layer containing the root frontier (for annuals only)
		/// </summary>
		/// <remarks>
		/// For perennials, the root depth and distribution are set at initialisation and do not change throughtout the simulation
		/// </remarks>
		private void EvaluateRootGrowth()
		{
			if (isAnnual)
			{
				//considering root distribution change, here?
				myRootDepth = dRootDepth + (maxRootDepth - dRootDepth) * daysfromEmergence / daysEmgToAnth;

				// get new layer for root frontier
				double cumDepth = 0.0;
				for (int layer = 0; layer < Soil.Thickness.Length; layer++)
				{
					cumDepth += Soil.Thickness[layer];
					if (cumDepth <= myRootDepth)
					{
						myRootFrontier = layer;
						myRootExplorationFactor[layer] = LayerFractionWithRoots(layer);
					}
					else
					{
						layer = Soil.Thickness.Length;
					}
				}
			}
			// else:  both myRootDepth and myRootFrontier have been set at initialisation and do not change
		}

		/// <summary>Computes the plant's gross potential growth rate</summary>
		/// <returns>The potential amount of C assimilated via photosynthesis (kgC/ha)</returns>
		private double DailyGrossPotentialGrowth()
		{
			// 1. compute photosynthesis rate per leaf area

			// to be moved to parameter section
			// Photochemical, or photosynthetic, efficiency (mg CO2/J) - typically with small variance and little effect
			const double alpha = 0.01;
			// Photosynthesis curvature parameter (J/kg^2/s) - typically with small variance and little effect
			const double theta = 0.8;

			// Temp effects to Pmax
			double effTemp1 = TemperatureLimitingFactor(Tmean);
			double effTemp2 = TemperatureLimitingFactor(TmeanW);

			// CO2 effects on Pmax
			double efCO2 = PCO2Effects();

			// N effects on Pmax
			NcFactor = PmxNeffect();

			// Maximum photosynthetic rate (mg CO2/m^2 leaf/s)
			double Pmax_EarlyLateDay = referencePhotosynthesisRate * effTemp1 * efCO2 * NcFactor;
			double Pmax_MiddleDay = referencePhotosynthesisRate * effTemp2 * efCO2 * NcFactor;

			double myDayLength = 3600 * MetData.DayLength;  //conversion of hour to seconds

			// Photosynthetically active radiation, PAR = 0.5*Radn, converted from MJ/m2 to J/2 (10^6)
			double myPAR = 0.5 * interceptedRadn * 1000000;

			// Irradiance, or radiation, on the canopy at the middle of the day (W/m^2)
			//IL = (4.0 / 3.0) * myPAR * swardLightExtCoeff / myDayLength;  TODO: enable this
			IL = 1.33333 * myPAR * swardLightExtCoeff / myDayLength;
			double IL2 = IL / 2;                      //IL for early & late period of a day

			// Photosynthesis per LAI under full irradiance at the top of the canopy (mg CO2/m^2 leaf/s)
			double photoAux1 = alpha * IL + Pmax_MiddleDay;
			double photoAux2 = 4 * theta * alpha * IL * Pmax_MiddleDay;
			double Pl_MiddleDay = (0.5 / theta) * (photoAux1 - Math.Sqrt(Math.Pow(photoAux1, 2.0) - photoAux2));

			photoAux1 = alpha * IL2 + Pmax_EarlyLateDay;
			photoAux2 = 4 * theta * alpha * IL2 * Pmax_EarlyLateDay;
			double Pl_EarlyLateDay = (0.5 / theta) * (photoAux1 - Math.Sqrt(Math.Pow(photoAux1, 2.0) - photoAux2));

			// Photosynthesis per leaf area for the day (mg CO2/m^2 leaf/day)
			double Pl_Daily = myDayLength * (Pl_MiddleDay + Pl_EarlyLateDay) * 0.5;

			// Photosynthesis for whole canopy, per ground area (mg CO2/m^2/day)
			double Pc_Daily = Pl_Daily * swardGreenCover * intRadnFrac / lightExtentionCoeff;

			//  Carbon assimilation per leaf area (g C/m^2/day)
			double CarbonAssim = Pc_Daily * 0.001 * (12.0 / 44.0);         // Convert to from mgCO2 to kgC           

			// Base gross photosynthesis, converted to kg C/ha/day)
			double BaseGrossPhotosynthesis = CarbonAssim * 10;             // convertion = 10000 / 1000

			// Consider the extreme temperature effects (in practice only one temp stress factor is < 1)
			double ExtremeTemperatureFactor = HeatStress() * ColdStress();

			// Actual gross photosynthesis (gross potential growth - kg C/ha/day)
			return BaseGrossPhotosynthesis * ExtremeTemperatureFactor;

			// TODO: implement GLFGeneric...
		}

		/// <summary>Computes the plant's loss of C due to respiration</summary>
		/// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
		private double DailyMaintenanceRespiration()
		{
			// Temperature effects on respiration
			double Teffect = 0;
			if (Tmean > growthTmin)
			{
				if (Tmean < growthTopt)
				{
					Teffect = TemperatureLimitingFactor(Tmean);
				}
				else
				{
					Teffect = Math.Min(1.25, Tmean / growthTopt);		// Using growthTopt as reference temperature, and maximum of 1.25
					Teffect *= TemperatureLimitingFactor(growthTopt);
				}
			}

			// Total DM converted to C (kg/ha)
			double dmLive = (dmGreen + dmRoot) * CarbonFractionInDM;
			double result = dmLive * maintenanceRespirationCoef * Teffect * NcFactor;
			return Math.Max(0.0, result);
		}

		/// <summary>Computes the plant's loss of C due to growth respiration</summary>
		/// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
		private double DailyGrowthRespiration()
		{
			return Pgross * growthRespirationCoef;
		}
		/// <summary>Compute the plant's net potential growth</summary>
		/// <returns>The net potential growth (kg DM/ha)</returns>
		private double DailyNetPotentialGrowth()
		{
			// Net potential growth (C assimilation) for the day (excluding respiration)
			double NetPotGrowth = 0.0;
			NetPotGrowth = (1 - growthRespirationCoef) * (Pgross + CRemobilised - Resp_m);  // TODO: the respCoeff should only multiply Pgross
			//NetPotGrowth = Pgross + CRemobilised - Resp_g - Resp_m;
			NetPotGrowth = Math.Max(0.0, NetPotGrowth);

			// Net daily potential growth (kg DM/ha)
			NetPotGrowth /= CarbonFractionInDM;

			// phenologically related reduction in growth of annual species (from IJ)
			if (isAnnual)
				NetPotGrowth *= annualSpeciesReduction();

			return NetPotGrowth;
		}

		/// <summary>Computes the plant's potential growth rate</summary>
		/// <returns></returns>
		private double DailyActualGrowth()
		{
			// Adjust GLF due to N deficiency. Many plants (grasses) can grow more by reducing the N concentration
			//  in its tissues. This is represented here by reducing the effect of N deficiency using a power function,
			//  when exponent is 1.0, the reduction in growth is proportional to N deficiency; for many plants the value
			//  should be smaller than that. For grasses, the exponent is typically around 0.5.
			double glfNit = Math.Pow(glfN, dillutionCoefN);

			// The generic limitation factor is assumed to be equivalent to a nutrient deficiency, so it is considered here
			dGrowthActual = dGrowthWstress * Math.Min(glfNit, glfGeneric);   // TODO: uptade the use of GLFGeneric

			return dGrowthActual;
		}

		/// <summary>Update DM and N amounts of all tissues accounting for the new growth (plus leftover remobilisation)</summary>
		/// <exception cref="System.Exception">
		/// Mass balance lost on partition of new growth DM
		/// or
		/// Mass balance lost on partition of new growth N
		/// </exception>
		private void PartitionNewGrowth()
		{
			// TODO: implement fLeaf
			// Leaf appearance rate, as modified by temp & water stress  -  Not really used, should it??
			//double effTemp = TemperatureLimitingFactor(Tmean);
			//double effWater = Math.Pow(glfWater, 0.33333);
			//double rateLeafGrowth = leafRate * effTemp * effWater;
			//rateLeafGrowth = Math.Max(0.0, Math.Min(1.0, rateLeafGrowth));

			if (dGrowthActual > 0.0)
			{
				// Fractions of new growth for each plant part (fShoot was calculated in DoPlantGrowth)
				double toLeaf = fShoot * fracToLeaf;
				double toStem = fShoot * (1.0 - fracToStolon - fracToLeaf);
				double toStolon = fShoot * fracToStolon;
				double toRoot = 1.0 - fShoot;

				// Checking mass balance
				double ToAll = toLeaf + toStolon + toStem + toRoot;
				if (Math.Abs(ToAll - 1.0) > 0.0001)
					throw new Exception("Mass balance lost on partition of new growth DM");

				// New growth is allocated to the first tissue pools
				dmLeaf1 += toLeaf * dGrowthActual;
				dmStem1 += toStem * dGrowthActual;
				dmStolon1 += toStolon * dGrowthActual;
				dmRoot += toRoot * dGrowthActual;
				dGrowthShoot = (toLeaf + toStem + toStolon) * dGrowthActual;
				dGrowthRoot = toRoot * dGrowthActual;

				// Partitioning N based on DM fractions and on max [N] in plant parts
				double Nsum = toLeaf * leafNmax 
					        + toStem * NcStemMax
							+ toStolon * NcStolonMax
							+ toRoot * NcRootMax;
				double toLeafN = toLeaf * MathUtilities.Divide(leafNmax, Nsum, 0.0);
				double toStemN = toStem * MathUtilities.Divide(NcStemMax, Nsum, 0.0);
				double toStolonN = toStolon * MathUtilities.Divide(NcStolonMax, Nsum, 0.0);
				double toRootN = toRoot * MathUtilities.Divide(NcRootMax, Nsum, 0.0);

				// Checking mass balance
				ToAll = toRootN + toLeafN + toStolonN + toStemN;
				if (Math.Abs(ToAll - 1.0) > 0.0001)
					throw new Exception("Mass balance lost on partition of new growth N");

				// Allocate N from new growth to the first tissue pools
				Nleaf1 += toLeafN * newGrowthN;
				Nstem1 += toStemN * newGrowthN;
				Nstolon1 += toStolonN * newGrowthN;
				Nroot += toRootN * newGrowthN;

				// Fraction of Nremob not used in new growth that is returned to dead tissue
				double leftoverNremob = NRemobilised * kappaNRemob4;
				if ((leftoverNremob > 0.0) && (prevState.Nleaf4 + prevState.Nstem4 > 0.0))
				{
					Nsum = prevState.Nleaf4 + prevState.Nstem4;
					Nleaf4 += leftoverNremob * MathUtilities.Divide(prevState.Nleaf4, Nsum, 0.0);
					Nstem4 += leftoverNremob * MathUtilities.Divide(prevState.Nstem4, Nsum, 0.0);
					// Note: this is only valid for leaf and stems, the remaining (1-kappaNRemob4) and the amounts in roots
					//  and stolon is disposed (added to soil FOM or Surface OM via litter)
				}

				// Check whether luxury N was remobilised during N balance
				if (NFastRemob2 + NFastRemob3 > 0.0)
				{
					// If N was remobilised, update the N content in tissues accordingly
					//  partition between parts is assumed proportional to N content
					if (NFastRemob2 > 0.0)
					{
						Nsum = prevState.Nleaf2 + prevState.Nstem2 + prevState.Nstolon2;
						Nleaf2 += NFastRemob2 * MathUtilities.Divide(prevState.Nleaf2, Nsum, 0.0);
						Nstem2 += NFastRemob2 * MathUtilities.Divide(prevState.Nstem2, Nsum, 0.0);
						Nstolon2 += NFastRemob2 * MathUtilities.Divide(prevState.Nstolon2, Nsum, 0.0);
					}
					if (NFastRemob3 > 0.0)
					{
						Nsum = prevState.Nleaf3 + prevState.Nstem3 + prevState.Nstolon3;
						Nleaf3 += NFastRemob3 * MathUtilities.Divide(prevState.Nleaf3, Nsum, 0.0);
						Nstem3 += NFastRemob3 * MathUtilities.Divide(prevState.Nstem3, Nsum, 0.0);
						Nstolon3 += NFastRemob3 * MathUtilities.Divide(prevState.Nstolon3, Nsum, 0.0);
					}
				}
			}
			else
			{
				// no actuall growth, just zero out some variables
				dGrowthShoot = 0.0;
				dGrowthRoot = 0.0;
			}
		}

		/// <summary>Computes the fraction of today's growth allocated to shoot</summary>
		/// <returns>The fraction of DM growth allocated to shoot (0-1)</returns>
		/// <remarks>
		/// Takes into consideration any seasonal variations and defoliation, this is done by
		/// targeting a given shoot:root ratio (that is the maxSRratio)
		/// </remarks>
		private double ToShootFraction()
		{
			double result = 1.0;

			if (prevState.dmRoot > 0.00001 || dmGreen < prevState.dmRoot)
			{
				double fac = 1.0;
				int doyIncrease = doyIniHighShoot + higherShootAllocationPeriods[0];  //35;   //75
				int doyPlateau = doyIncrease + higherShootAllocationPeriods[1];   // 95;   // 110;
				int doyDecrease = doyPlateau + higherShootAllocationPeriods[2];  // 125;  // 140;
				int doy = Clock.Today.DayOfYear;

				if (doy > doyIniHighShoot)
				{
					if (doy < doyIncrease)
						fac = 1 + shootSeasonalAllocationIncrease * MathUtilities.Divide(doy - doyIniHighShoot, higherShootAllocationPeriods[0], 0.0);
					else if (doy <= doyPlateau)
						fac = 1.0 + shootSeasonalAllocationIncrease;
					else if (doy <= doyDecrease)
						fac = 1 + shootSeasonalAllocationIncrease * (1 - MathUtilities.Divide(doy - doyPlateau, higherShootAllocationPeriods[2], 0.0));
					else
						fac = 1;
				}
				else
				{
					if (doyDecrease > 365 && doy <= doyDecrease - 365)
						fac = 1 + shootSeasonalAllocationIncrease * (1 - MathUtilities.Divide(365 + doy - doyPlateau, higherShootAllocationPeriods[2], 0.0));
				}

				double presentSRratio = dmGreen / prevState.dmRoot;
				double targetedSRratio = fac * maxSRratio;
				double newSRratio;

				if (presentSRratio > targetedSRratio)
					newSRratio = targetedSRratio;
				else
					newSRratio = targetedSRratio * (targetedSRratio / presentSRratio);

				newSRratio *= Math.Min(glfWater, glfN);

				result = newSRratio / (1.0 + newSRratio);

				if (result / (1 - result) < targetedSRratio)
					result = targetedSRratio / (1 + targetedSRratio);
			}

			return result;
		}

		/// <summary>Tentative - correction for the fraction of DM allocated to leaves</summary>
		/// <returns></returns>
		private double UpdatefLeaf()
		{
			double result;
			if (isLegume)
			{
				if (dmGreen > 0.0 && (dmStolon / dmGreen) > fracToStolon)
					result = 1.0;
				else if (dmGreen + dmStolon < 2000)
					result = fracToLeaf + (1 - fracToLeaf) * (dmGreen + dmStolon) / 2000;
				else
					result = fracToLeaf;
			}
			else
			{
				if (dmGreen < 2000)
					result = fracToLeaf + (1 - fracToLeaf) * dmGreen / 2000;
				else
					result = fracToLeaf;
			}
			return result;
		}

		/// <summary>Computes the turnover rate and update each tissue pool of all plant parts</summary>
		/// <exception cref="System.Exception">
		/// Loss of mass balance on C remobilisation - leaf
		/// or
		/// Loss of mass balance on C remobilisation - stem
		/// or
		/// Loss of mass balance on C remobilisation - stolon
		/// or
		/// Loss of mass balance on C remobilisation - root
		/// </exception>
		/// <remarks>The C and N amounts for remobilisation are also computed in here</remarks>
		private void TissueTurnoverAndRemobilisation()
		{
			// The turnover rates are affected by temperature and soil moisture
			double TempFac = TempFactorForTissueTurnover(Tmean);
			double WaterFac = WaterFactorForTissueTurnover();
			double WaterFac2Litter = Math.Pow(glfWater, 3);
			double WaterFac2Root = 2 - glfWater;
			double SR = 0;  //stocking rate affecting transfer of dead to litter (default as 0 for now - should be read in)
			double StockFac2Litter = stockParameter * SR;

			// Turnover rate for leaf and stem
			gama = turnoverRateLive2Dead * TempFac * WaterFac;

			// Turnover rate for stolon
			gamaS = gama;

			//double gamad = gftt * gfwt * rateDead2Litter;

			// Turnover rate for dead to litter (TODO: check the use of digestibility here)
			gamaD = turnoverRateDead2Litter * WaterFac2Litter * digestibilityDead / 0.4;
			gamaD += StockFac2Litter;

			// Turnover rate for roots
			gamaR = turnoverRateRootSenescence * TempFac * WaterFac2Root;


			if (gama > 0.0)
			{ // there is some tissue turnover
				if (isAnnual)
				{
					if (phenoStage == 1)
					{ //vegetative
						gama *= MathUtilities.Divide(daysfromEmergence, daysEmgToAnth, 1.0);
						gamaR *= MathUtilities.Divide(daysfromEmergence, daysEmgToAnth, 1.0);
					}
					else if (phenoStage == 2)
					{ //reproductive
						gama = 1 - (1 - gama) * (1 - Math.Pow(MathUtilities.Divide(daysfromAnthesis, daysToMature, 1.0), 2));
					}
				}

				// Fraction of DM defoliated today
				double FracDefoliated = MathUtilities.Divide(dmDefoliated, dmDefoliated + prevState.dmLeaf + prevState.dmStem + prevState.dmStolon, 0.0);

				// Adjust stolon turnover due to defoliation (increase stolon senescence)
				gamaS += FracDefoliated * (1 - gama);

				// Check whether todays senescence will result in dmGreen < dmGreenmin
				//   if that is the case then adjust (reduce) the turnover rates
				// TODO: possibly should skip this for annuals to allow them to die - phenololgy-related?

				// TODO: here it should be dGrowthShoot, not total (will fix after tests)
				//double dmGreenToBe = dmGreen + dGrowthShoot - gama * (prevState.dmLeaf3 + prevState.dmStem3 + prevState.dmStolon3);
				double dmGreenToBe = dmGreen + dGrowthActual - gama * (prevState.dmLeaf3 + prevState.dmStem3 + prevState.dmStolon3);
				if (dmGreenToBe < minimumGreenWt)
				{
					if (dmGreen + dGrowthShoot < minimumGreenWt)
					{ // this should not happen anyway
						gama = 0.0;
						gamaS = 0.0;
						gamaR = 0.0;
					}
					else
					{
						double gama_adj = MathUtilities.Divide(dmGreen + dGrowthShoot - minimumGreenWt,
										  prevState.dmLeaf3 + prevState.dmStem3 + prevState.dmStolon3, gama);
						gamaR *= gama_adj / gama;
						gamaD *= gama_adj / gama;
						gama = gama_adj;
					}
				}
				if (dmRoot < 0.5 * minimumGreenWt)          // set a minimum root too, probably not really needed
					gamaR = 0;    // TODO: check this

				// Do the actual DM turnover for all tissues
				double dDM_in = 0.0;    // growth has been accounted for in PartitionNewGrowth
				double dDM_out = 2 * gama * prevState.dmLeaf1;
				dmLeaf1 += dDM_in - dDM_out;
				Nleaf1 += -dDM_out * prevState.NconcLeaf1;
				dDM_in = dDM_out;
				dDM_out = gama * prevState.dmLeaf2;
				dmLeaf2 += dDM_in - dDM_out;
				Nleaf2 += dDM_in * prevState.NconcLeaf1 - dDM_out * prevState.NconcLeaf2;
				dDM_in = dDM_out;
				dDM_out = gama * prevState.dmLeaf3;
				dmLeaf3 += dDM_in - dDM_out;
				Nleaf3 += dDM_in * prevState.NconcLeaf2 - dDM_out * prevState.NconcLeaf3;
				dDM_in = dDM_out;
				dDM_out = gamaD * prevState.dmLeaf4;
				double ChRemobSugar = dDM_in * kappaCRemob;
				double ChRemobProtein = dDM_in * (prevState.NconcLeaf3 - leafNmin) * CNratioProtein * facCNRemob;
				dDM_in -= ChRemobSugar + ChRemobProtein;
				if (dDM_in < 0.0)
					throw new Exception("Loss of mass balance on C remobilisation - leaf");
				dmLeaf4 += dDM_in - dDM_out;
				Nleaf4 += dDM_in * leafNmin - dDM_out * prevState.NconcLeaf4;
				dLitter = dDM_out;
				dNlitter = dDM_out * prevState.NconcLeaf4;
				dGrowthShoot -= dDM_out;
				double NRemobl = dDM_in * (prevState.NconcLeaf3 - leafNmin);
				double ChRemobl = ChRemobSugar + ChRemobProtein;

				dDM_in = 0.0;    // growth has been accounted for in PartitionNewGrowth
				dDM_out = 2 * gama * prevState.dmStem1;
				dmStem1 += dDM_in - dDM_out;
				Nstem1 += -dDM_out * prevState.NconcStem1;
				dDM_in = dDM_out;
				dDM_out = gama * prevState.dmStem2;
				dmStem2 += dDM_in - dDM_out;
				Nstem2 += dDM_in * prevState.NconcStem1 - dDM_out * prevState.NconcStem2;
				dDM_in = dDM_out;
				dDM_out = gama * prevState.dmStem3;
				dmStem3 += dDM_in - dDM_out;
				Nstem3 += dDM_in * prevState.NconcStem2 - dDM_out * prevState.NconcStem3;
				dDM_in = dDM_out;
				dDM_out = gamaD * prevState.dmStem4;
				ChRemobSugar = dDM_in * kappaCRemob;
				ChRemobProtein = dDM_in * (prevState.NconcStem3 - NcStemMin) * CNratioProtein * facCNRemob;
				dDM_in -= ChRemobSugar + ChRemobProtein;
				if (dDM_in < 0.0)
					throw new Exception("Loss of mass balance on C remobilisation - stem");
				dmStem4 += dDM_in - dDM_out;
				Nstem4 += dDM_in * NcStemMin - dDM_out * prevState.NconcStem4;
				dLitter += dDM_out;
				dNlitter += dDM_out * prevState.NconcStem4;
				dGrowthShoot -= dDM_out;
				NRemobl += dDM_in * (prevState.NconcStem3 - NcStemMin);
				ChRemobl += ChRemobSugar + ChRemobProtein;

				dDM_in = 0.0;    // growth has been accounted for in PartitionNewGrowth
				dDM_out = 2 * gamaS * prevState.dmStolon1;
				dmStolon1 += dDM_in - dDM_out;
				Nstolon1 += -dDM_out * prevState.NconcStolon1;
				dDM_in = dDM_out;
				dDM_out = gamaS * prevState.dmStolon2;
				dmStolon2 += dDM_in - dDM_out;
				Nstolon2 += dDM_in * prevState.NconcStolon1 - dDM_out * prevState.NconcStolon2;
				dDM_in = dDM_out;
				dDM_out = gamaS * prevState.dmStolon3;
				dmStolon3 += dDM_in - dDM_out;
				Nstolon3 += dDM_in * prevState.NconcStolon2 - dDM_out * prevState.NconcStolon3;
				dDM_in = dDM_out;
				ChRemobSugar = dDM_in * kappaCRemob;
				ChRemobProtein = dDM_in * (prevState.NconcStolon3 - NcStolonMin) * CNratioProtein * facCNRemob;
				dDM_in -= ChRemobSugar + ChRemobProtein;
				if (dDM_in < 0.0)
					throw new Exception("Loss of mass balance on C remobilisation - stolon");
				dLitter += dDM_in;
				dNlitter += dDM_in * NcStolonMin + 0.5 * dDM_in * (prevState.NconcStolon3 - NcStolonMin);
				dGrowthShoot -= dDM_in;
				NRemobl += 0.5 * dDM_in * (prevState.NconcStolon3 - NcStolonMin);
				ChRemobl += ChRemobSugar + ChRemobProtein;

				dRootSen = gamaR * prevState.dmRoot;
				dmRoot -= dRootSen;
				ChRemobSugar = dRootSen * kappaCRemob;
				ChRemobProtein = dRootSen * (prevState.NconcRoot - NcRootMin) * CNratioProtein * facCNRemob;
				dRootSen -= ChRemobSugar + ChRemobProtein;
				if (dRootSen < 0.0)
					throw new Exception("Loss of mass balance on C remobilisation - root");
				dNrootSen = gamaR * prevState.Nroot - 0.5 * dRootSen * (prevState.NconcRoot - NcRootMin);
				Nroot -= gamaR * prevState.Nroot;
				dGrowthRoot -= dRootSen;
				NRemobl += 0.5 * dRootSen * (prevState.NconcRoot - NcRootMin);
				ChRemobl += ChRemobSugar + ChRemobProtein;

				// Remobilised C to be used in tomorrow's growth (converted from carbohydrate to C)
				CRemobilised = ChRemobl * CarbonFractionInDM;

				// Fraction of N remobilised yesterday that not used in new growth
				//  it is added to today's litter
				double leftoverNremob = NRemobilised * (1 - kappaNRemob4);
				dNlitter += leftoverNremob;

				// N remobilised to be potentially used for growth tomorrow
				NRemobilised = NRemobl;
			}
			else
			{
				// No turnover, just zero out some variables
				dLitter = 0.0;
				dNlitter = 0.0;
				dRootSen = 0.0;
				dNrootSen = 0.0;
				CRemobilised = 0.0;
				NRemobilised = 0.0;
			}
			// N remobilisable from luxury N to be potentially used for growth tomorrow
			NLuxury2 = Math.Max(0.0, Nleaf2 - (dmLeaf2 * leafNopt * relativeNStage3))
					 + Math.Max(0.0, Nstem2 - (dmStem2 * NcStemOpt * relativeNStage3))
					 + Math.Max(0.0, Nstolon2 - (dmStolon2 * NcStolonOpt * relativeNStage3));
			NLuxury3 = Math.Max(0.0, Nleaf3 - (dmLeaf3 * leafNopt * relativeNStage3))
					 + Math.Max(0.0, Nstem3 - (dmStem3 * NcStemOpt * relativeNStage3))
					 + Math.Max(0.0, Nstolon3 - (dmStolon3 * NcStolonOpt * relativeNStage3));
			// only a fraction of luxury N is actually available for remobilisation:
			NLuxury2 *= kappaNRemob2;
			NLuxury3 *= kappaNRemob3;
		}

		#endregion

		#region - Water uptake processes  ----------------------------------------------------------------------------------

		/// <summary>
		/// Provides canopy data for MicroClimate, who will do the energy balance and calc water demand
		/// </summary>
		private void DoNewCanopyEvent()
		{
			if (NewCanopy != null)
			{
				myCanopyData.sender = Name;
				myCanopyData.lai = greenLAI;
				myCanopyData.lai_tot = TotalLAI;
				myCanopyData.height = myHeight;
				myCanopyData.depth = myHeight;
				myCanopyData.cover = GreenCover;
				myCanopyData.cover_tot = TotalCover;
				NewCanopy.Invoke(myCanopyData);
			}
		}

		/// <summary>Gets the water uptake for each layer as calculated by an external module (SWIM)</summary>
		/// <param name="SoilWater">The soil water.</param>
		/// <remarks>
		/// This method is only used when an external method is used to compute water uptake (this includes AgPasture)
		/// </remarks>
		[EventSubscribe("WaterUptakesCalculated")]
		private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
		{
			for (int iCrop = 0; iCrop < SoilWater.Uptakes.Length; iCrop++)
			{
				if (SoilWater.Uptakes[iCrop].Name == Name)
				{
					for (int layer = 0; layer < SoilWater.Uptakes[iCrop].Amount.Length; layer++)
						mySoilWaterTakenUp[layer] = SoilWater.Uptakes[iCrop].Amount[layer];
				}
			}
		}
		
		/// <summary>
		/// Gets the amount of water uptake for this species as computed by the resource Arbitrator
		/// </summary>
		private void GetWaterUptake()
		{
			Array.Clear(mySoilWaterTakenUp, 0, mySoilWaterTakenUp.Length);
			for (int layer = 0; layer <= myRootFrontier; layer++)
				mySoilWaterTakenUp[layer] = uptakeWater[layer];
		}

		/// <summary>
		/// Consider water uptake calculations (plus GLFWater)
		/// </summary>
		internal void DoWaterCalculations()
		{
			if (myWaterUptakeSource == "species")
			{ // this module will compute water uptake
				MyWaterCalculations();

				// get the drought effects
				glfWater = WaterDeficitFactor();
				// get the water logging effects (only if there is no drought effect)
				if (glfWater > 0.999)
					glfWater = WaterLoggingFactor();
			}
			//else if myWaterUptakeSource == "AgPasture"
			//      myWaterDemand should have been supplied by MicroClimate (supplied as PotentialEP)
			//      water supply is hold by AgPasture only
			//      myWaterUptake should have been computed by AgPasture (set directly)
			//      glfWater is computed and set by AgPasture
			else if (myWaterUptakeSource == "arbitrator")
			{ // water uptake has been calcualted by the resource arbitrator

				// get the array with the amount of water taken up
				GetWaterUptake();

				// get the drought effects
				glfWater = WaterDeficitFactor();
				// get the water logging effects (only if there is no drought effect)
				if (glfWater > 0.999)
					glfWater = WaterLoggingFactor();
			}
			//else
			//      water uptake be calculated by other modules (e.g. SWIM) and supplied as
			//  Note: when AgPasture is doing the water uptake, it can do it using its own calculations or other module's...
		}

		/// <summary>
		/// Gather the amount of available eater and computes the water uptake for this species
		/// </summary>
		/// <remarks>
		/// Using this routine is discourage as it ignores the presence of other species and thus
		/// might result in loss of mass balance or unbalanced supply, i.e. over-supply for one
		/// while under-supply for other species (depending on the order that species are considered)
		/// </remarks>
		private void MyWaterCalculations()
		{
			mySoilAvailableWater = GetSoilAvailableWater();
			// myWaterDemand given by MicroClimate
			if (myWaterUptakeSource.ToLower() == "species")
				mySoilWaterTakenUp = DoSoilWaterUptake();
			//else
			//    uptake is controlled by the sward or by another apsim module
		}

		/// <summary>
		/// Finds out the amount soil water available for this plant (ignoring any other species)
		/// </summary>
		/// <returns>The amount of water available to plants in each layer</returns>
		internal double[] GetSoilAvailableWater()
		{
			double[] result = new double[nLayers];
			SoilCrop soilCropData = (SoilCrop)Soil.Crop(Name);
			if (useAltWUptake == "no")
			{
				for (int layer = 0; layer <= myRootFrontier; layer++)
				{
					result[layer] = Math.Max(0.0, Soil.Water[layer] - soilCropData.LL[layer] * Soil.Thickness[layer])
								  * LayerFractionWithRoots(layer);
					result[layer] *= soilCropData.KL[layer];
				}
			}
			else
			{ // Method implemented by RCichota
				// Available Water is function of root density, soil water content, and soil hydraulic conductivity
				// Assumptions: all factors are exponential functions and vary between 0 and 1;
				//   - If root density is equal to ReferenceRLD then plant can explore 90% of the water;
				//   - If soil Ksat is equal to ReferenceKSuptake then soil can supply 90% of its available water;
				//   - If soil water content is at DUL then 90% of its water is available;
				double[] myRLD = RLD;
				double facRLD = 0.0;
				double facCond = 0.0;
				double facWcontent = 0.0;
				for (int layer = 0; layer <= myRootFrontier; layer++)
				{
					facRLD = 1 - Math.Pow(10, -myRLD[layer] / ReferenceRLD);
					facCond = 1 - Math.Pow(10, -Soil.KS[layer] / ReferenceKSuptake);
					facWcontent = 1 - Math.Pow(10,
								-(Math.Max(0.0, Soil.Water[layer] - Soil.SoilWater.LL15mm[layer]))
								/ (Soil.SoilWater.DULmm[layer] - Soil.SoilWater.LL15mm[layer]));

					// Theoretical total available water
					result[layer] = Math.Max(0.0, Soil.Water[layer] - soilCropData.LL[layer] * Soil.Thickness[layer])
								  * LayerFractionWithRoots(layer);
					// Actual available water
					result[layer] *= facRLD * facCond * facWcontent;
				}
			}

			return result;
		}

		/// <summary>Computes the actual water uptake and send the deltas to soil module</summary>
		/// <returns>The amount of water taken up for each soil layer</returns>
		/// <exception cref="System.Exception">Error on computing water uptake</exception>
		private double[] DoSoilWaterUptake()
		{
			PMF.WaterChangedType WaterTakenUp = new PMF.WaterChangedType();
			WaterTakenUp.DeltaWater = new double[nLayers];

			double uptakeFraction = Math.Min(1.0, MathUtilities.Divide(myWaterDemand, mySoilAvailableWater.Sum(), 0.0));
			double[] result = new double[nLayers];

			if (useAltWUptake == "no")
			{
				for (int layer = 0; layer <= myRootFrontier; layer++)
				{
					result[layer] = mySoilAvailableWater[layer] * uptakeFraction;
					WaterTakenUp.DeltaWater[layer] = -result[layer];
				}
			}
			else
			{ // Method implemented by RCichota
				// Uptake is distributed over the profile according to water availability,
				//  this means that water status and root distribution have been taken into account

				for (int layer = 0; layer <= myRootFrontier; layer++)
				{
					result[layer] = mySoilAvailableWater[layer] * uptakeFraction;
					WaterTakenUp.DeltaWater[layer] = -result[layer];
				}
				if (Math.Abs(WaterTakenUp.DeltaWater.Sum() + myWaterDemand) > 0.0001)
					throw new Exception("Error on computing water uptake");
			}

			// send the delta water taken up
			WaterChanged.Invoke(WaterTakenUp);

			return result;
		}

		#endregion

		#region - Nitrogen uptake processes  -------------------------------------------------------------------------------

		/// <summary>
		/// Gets the amount of nitrogen uptake for this species as computed by the resource Arbitrator
		/// </summary>
		private void GetNitrogenUptake()
		{
			// get N demand (optimum and luxury)
			CalcNDemand();

			// get N fixation
			Nfixation = CalcNFixation();

			// evaluate the use of N remobilised and get soil N demand
			CalcSoilNDemand();

			// get the amount of N taken up from soil
			Array.Clear(mySoilNitrogenTakenUp, 0, mySoilNitrogenTakenUp.Length);
			mySoilNuptake = 0.0;
			for (int layer = 0; layer <= myRootFrontier; layer++)
			{
				mySoilNitrogenTakenUp[layer] = uptakeNitrogen[layer];
				mySoilNuptake += mySoilNitrogenTakenUp[layer];
			}
			newGrowthN = Nfixation + Nremob2NewGrowth + mySoilNuptake;

			// evaluate whether further remobilisation (from luxury N) is needed
			CalcNLuxuryRemob();
			newGrowthN += NFastRemob3 + NFastRemob2;
		}

		/// <summary>
		/// Consider nitrogen uptake calculations (plus GLFN)
		/// </summary>
		internal void DoNitrogenCalculations()
		{
			if (myNitrogenUptakeSource == "species")
			{ // this module will compute the N uptake
				MyNitrogenCalculations();
				if (newGrowthN > 0.0)
					glfN = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(newGrowthN, NdemandOpt, 1.0)));
				else
					glfN = 1.0;
			}
			//else if (myNitrogenUptakeSource == "AgPasture")
			//{
			//    NdemandOpt is called by AgPasture
			//    NdemandLux is called by AgPasture
			//    Nfix is called by AgPasture
			//    myNitrogenSupply is hold by AgPasture
			//    soilNdemand is computed by AgPasture
			//    soilNuptake is computed by AgPasture
			//    remob2NewGrowth is computed by AgPasture
			//}
			else if (myNitrogenUptakeSource == "arbitrator")
			{ // Nitrogen uptake was computed by the resource arbitrator

				// get the amount of N taken up
				GetNitrogenUptake();
				if (newGrowthN > 0.0)
					glfN = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(newGrowthN, NdemandOpt, 1.0)));
				else
					glfN = 1.0;
			}
			//else
			//   N uptake is computed by another module (not implemented yet)
		}

		/// <summary>Performs the computations for N balance and uptake</summary>
		private void MyNitrogenCalculations()
		{
			// get soil available N
			if (myNitrogenUptakeSource.ToLower() == "species")
				GetSoilAvailableN();
			//else
			//    N available is computed in another module

			// get N demand (optimum and luxury)
			CalcNDemand();

			// get N fixation
			Nfixation = CalcNFixation();

			// evaluate the use of N remobilised and get soil N demand
			CalcSoilNDemand();

			// get the amount of N taken up from soil
			mySoilNuptake = CalcSoilNUptake();
			newGrowthN = Nfixation + Nremob2NewGrowth + mySoilNuptake;

			// evaluate whether further remobilisation (from luxury N) is needed
			CalcNLuxuryRemob();
			newGrowthN += NFastRemob3 + NFastRemob2;

			// send delta N to the soil model
			DoSoilNitrogenUptake();
		}

		/// <summary>Computes the N demanded for optimum N content as well as luxury uptake</summary>
		internal void CalcNDemand()
		{
			double toRoot = dGrowthWstress * (1.0 - fShoot);
			double toStol = dGrowthWstress * fShoot * fracToStolon;
			double toLeaf = dGrowthWstress * fShoot * fracToLeaf;
			double toStem = dGrowthWstress * fShoot * (1.0 - fracToStolon - fracToLeaf);

			// N demand for new growth, with optimum N (kg/ha)
			NdemandOpt = toRoot * NcRootOpt + toStol * NcStolonOpt + toLeaf * leafNopt + toStem * NcStemOpt;

			// get the factor to reduce the demand under elevated CO2
			double fN = NCO2Effects();
			NdemandOpt *= fN;

			// N demand for new growth, with luxury uptake (maximum [N])
			NdemandLux = toRoot * NcRootMax + toStol * NcStolonMax + toLeaf * leafNmax + toStem * NcStemMax;
			// It is assumed that luxury uptake is not affected by CO2 variations
		}

		/// <summary>Computes the amount of N fixed from atmosphere</summary>
		/// <returns>The amount of N fixed (kgN/ha)</returns>
		internal double CalcNFixation()
		{
			double result = 0.0;

			if (Clock.Today.Date.Day == 31)
				result = 0.0;

			if (isLegume)
			{
				// Start with minimum fixation
				double iniFix = minimumNFixation * NdemandLux;

				// evaluate N stress
				double Nstress = 1.0;
				if (NdemandLux > 0.0 && (NdemandLux > mySoilAvailableN.Sum() + iniFix))
					Nstress = MathUtilities.Divide(mySoilAvailableN.Sum(), NdemandLux - iniFix, 1.0);

				// Update N fixation if under N stress
				if (Nstress < 0.99)
					result = maximumNFixation - (maximumNFixation - minimumNFixation) * Nstress;
				else
					result = minimumNFixation;
			}

			return Math.Max(0.0, result) * NdemandLux;
		}

		/// <summary>Perform preliminary N budget and get soil N demand</summary>
		internal void CalcSoilNDemand()
		{
			if (Nfixation - NdemandLux > -0.0001)
			{ // N demand is fulfilled by fixation alone
				Nfixation = NdemandLux;  // should not be needed, but just in case...
				Nremob2NewGrowth = 0.0;
				mySoilNDemand = 0.0;
			}
			else if ((Nfixation + NRemobilised) - NdemandLux > -0.0001)
			{ // N demand is fulfilled by fixation plus N remobilised from senescent material
				Nremob2NewGrowth = Math.Max(0.0, NdemandLux - Nfixation);
				NRemobilised -= Nremob2NewGrowth;
				mySoilNDemand = 0.0;
			}
			else
			{ // N demand is greater than fixation and remobilisation of senescent, N uptake is needed
				Nremob2NewGrowth = NRemobilised;
				NRemobilised = 0.0;
				mySoilNDemand = NdemandLux - (Nfixation + Nremob2NewGrowth);
			}

			// variable used by arbitrator
			demandNitrogen = mySoilNDemand;
		}

		/// <summary>
		/// Find out the amount of Nitrogen (NH4 and NO3) in the soil available to plants for each soil layer
		/// </summary>
		internal void GetSoilAvailableN()
		{
			mySoilNH4available = new double[nLayers];
			mySoilNO3available = new double[nLayers];
			mySoilAvailableN = new double[nLayers];

			double facWtaken = 0.0;
			for (int layer = 0; layer <= myRootFrontier; layer++)   // TODO: this should be <=
			{
				if (useAltNUptake == "no")
				{
					// simple way, all N in the root zone is available
					mySoilNH4available[layer] = Soil.NH4N[layer] * LayerFractionWithRoots(layer);
					mySoilNO3available[layer] = Soil.NO3N[layer] * LayerFractionWithRoots(layer);
				}
				else
				{
					// Method implemented by RCichota,
					// N is available following water and a given 'availability' factor (for each N form) and the fraction of water taken up

					// fraction of available water taken up
					facWtaken = MathUtilities.Divide(mySoilWaterTakenUp[layer],
								Math.Max(0.0, Soil.Water[layer] - Soil.SoilWater.LL15mm[layer]), 0.0);

					// Theoretical amount available
					mySoilNH4available[layer] = Soil.NH4N[layer] * kuNH4 * LayerFractionWithRoots(layer);
					mySoilNO3available[layer] = Soil.NO3N[layer] * kuNO3 * LayerFractionWithRoots(layer);

					// actual amount available
					mySoilNH4available[layer] *= facWtaken;
					mySoilNO3available[layer] *= facWtaken;
				}
				mySoilAvailableN[layer] = mySoilNH4available[layer] + mySoilNO3available[layer];
			}
		}

		/// <summary>Computes the amount of N to be taken up from the soil</summary>
		/// <returns>The amount of N to be taken up from each soil layer</returns>
		private double CalcSoilNUptake()
		{
			double result;
			if (mySoilNDemand == 0.0)
			{ // No demand, no uptake
				result = 0.0;
			}
			else
			{
				if (mySoilAvailableN.Sum() >= mySoilNDemand)
				{ // soil can supply all remaining N needed
					result = mySoilNDemand;
				}
				else
				{ // soil cannot supply all N needed. Get the available N
					result = mySoilAvailableN.Sum();
				}
			}
			return result;
		}

		/// <summary>Computes the remobilisation of luxury N (from tissues 2 and 3)</summary>
		internal void CalcNLuxuryRemob()
		{
			// check whether N demand for optimum growth has been matched
			if (newGrowthN - NdemandOpt > -0.0001)
			{
				// N demand has been matched, no further remobilisation is needed
				NFastRemob3 = 0.0;
				NFastRemob2 = 0.0;
			}
			else
			{
				// all N already considered is not enough for optimum growth, check remobilisation of luxury N
				//  check whether luxury N in plants can be used (luxury uptake is ignored)
				double Nmissing = NdemandOpt - newGrowthN;
				if (Nmissing > NLuxury2 + NLuxury3)
				{
					// N luxury is still not enough for optimum growth, use up all there is
					if (NLuxury2 + NLuxury3 > 0)
					{
						NFastRemob3 = NLuxury3;
						NFastRemob2 = NLuxury2;
						Nmissing -= (NLuxury3 + NLuxury2);
					}
				}
				else
				{
					// There is luxury N that can be used for optimum growth, get first from tissue 3
					if (Nmissing <= NLuxury3)
					{
						// tissue 3 is enough
						NFastRemob3 = Nmissing;
						NFastRemob2 = 0.0;
						Nmissing = 0.0;
					}
					else
					{
						// get first from tissue 3
						NFastRemob3 = NLuxury3;
						Nmissing -= NLuxury3;

						// remaining from tissue 2
						NFastRemob2 = Nmissing;
						Nmissing = 0.0;
					}
				}
			}
		}

		/// <summary>
		/// Computes the distribution of N uptake over the soil profile and send the delta to soil module
		/// </summary>
		/// <exception cref="System.Exception">
		/// Error on computing N uptake
		/// or
		/// N uptake source was not recognised. Please specify it as either \"sward\" or \"species\".
		/// </exception>
		private void DoSoilNitrogenUptake()
		{
			if (myNitrogenUptakeSource.ToLower() == "species")
			{
				// check whether there is any uptake
				if (mySoilAvailableN.Sum() > 0.0 && mySoilNuptake > 0.0)
				{

					Soils.NitrogenChangedType NUptake = new Soils.NitrogenChangedType();
					NUptake.Sender = Name;
					NUptake.SenderType = "Plant";
					NUptake.DeltaNO3 = new double[nLayers];
					NUptake.DeltaNH4 = new double[nLayers];

					mySoilNitrogenTakenUp = new double[nLayers];
					double uptakeFraction = 0;

					if (useAltNUptake == "no")
					{
						if (mySoilAvailableN.Sum() > 0.0)
							uptakeFraction = Math.Min(1.0, MathUtilities.Divide(mySoilNuptake, mySoilAvailableN.Sum(), 0.0));

						for (int layer = 0; layer <= myRootFrontier; layer++)
						{
							NUptake.DeltaNH4[layer] = -Soil.NH4N[layer] * uptakeFraction;
							NUptake.DeltaNO3[layer] = -Soil.NO3N[layer] * uptakeFraction;

							mySoilNitrogenTakenUp[layer] = -(NUptake.DeltaNH4[layer] + NUptake.DeltaNO3[layer]);
						}
					}
					else
					{ // Method implemented by RCichota,
						// N uptake is distributed considering water uptake and N availability
						double[] fNH4Avail = new double[nLayers];
						double[] fNO3Avail = new double[nLayers];
						double[] fWUptake = new double[nLayers];
						double totNH4Available = mySoilAvailableN.Sum();
						double totNO3Available = mySoilAvailableN.Sum();
						double totWuptake = mySoilWaterTakenUp.Sum();
						for (int layer = 0; layer < nLayers; layer++)
						{
							fNH4Avail[layer] = Math.Min(1.0, MathUtilities.Divide(mySoilAvailableN[layer], totNH4Available, 0.0));
							fNO3Avail[layer] = Math.Min(1.0, MathUtilities.Divide(mySoilAvailableN[layer], totNO3Available, 0.0));
							fWUptake[layer] = Math.Min(1.0, MathUtilities.Divide(mySoilWaterTakenUp[layer], totWuptake, 0.0));
						}
						double totFacNH4 = fNH4Avail.Sum() + fWUptake.Sum();
						double totFacNO3 = fNO3Avail.Sum() + fWUptake.Sum();
						for (int layer = 0; layer < nLayers; layer++)
						{
							uptakeFraction = Math.Min(1.0, MathUtilities.Divide(fNH4Avail[layer] + fWUptake[layer], totFacNH4, 0.0));
							NUptake.DeltaNH4[layer] = -Soil.NH4N[layer] * uptakeFraction;

							uptakeFraction = Math.Min(1.0, MathUtilities.Divide(fNO3Avail[layer] + fWUptake[layer], totFacNO3, 0.0));
							NUptake.DeltaNO3[layer] = -Soil.NO3N[layer] * uptakeFraction;

							mySoilNitrogenTakenUp[layer] = NUptake.DeltaNH4[layer] + NUptake.DeltaNO3[layer];
						}
					}

					//mySoilUptakeN.Sum()	2.2427998752781684	double

					if (Math.Abs(mySoilNuptake - mySoilNitrogenTakenUp.Sum()) > 0.0001)
						throw new Exception("Error on computing N uptake");

					// do the actual N changes
					NitrogenChanged.Invoke(NUptake);
				}
				else
				{
					// no uptake, just zero out the array
					mySoilNitrogenTakenUp = new double[nLayers];
				}
			}
			else
			{
				// N uptake calculated by other modules (e.g., SWIM)
				string msg = "N uptake source was not recognised. Please specify it as either \"sward\" or \"species\".";
				throw new Exception(msg);
			}
		}

		#endregion

		#region - Organic matter processes  --------------------------------------------------------------------------------

		/// <summary>Return a given amount of DM (and N) to surface organic matter</summary>
		/// <param name="amountDM">DM amount to return</param>
		/// <param name="amountN">N amount to return</param>
		private void DoSurfaceOMReturn(double amountDM, double amountN)
		{
			if (BiomassRemoved != null)
			{
				Single dDM = (Single)amountDM;

				PMF.BiomassRemovedType BR = new PMF.BiomassRemovedType();
				String[] type = new String[] { "grass" };  // TODO:, shoud this be speciesFamily??
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

		/// <summary>Return scenescent roots to fresh organic matter pool in the soil</summary>
		/// <param name="amountDM">DM amount to return</param>
		/// <param name="amountN">N amount to return</param>
		private void DoIncorpFomEvent(double amountDM, double amountN)
		{
			Soils.FOMLayerLayerType[] FOMdataLayer = new Soils.FOMLayerLayerType[nLayers];

			// ****  RCichota, Jun/2014
			// root senesced are returned to soil (as FOM) considering return is proportional to root mass

			double dAmtLayer = 0.0; //amount of root litter in a layer
			double dNLayer = 0.0;
			for (int layer = 0; layer < nLayers; layer++)
			{
				dAmtLayer = amountDM * rootFraction[layer];
				dNLayer = amountN * rootFraction[layer];

				float amt = (float)dAmtLayer;

				Soils.FOMType fomData = new Soils.FOMType();
				fomData.amount = amountDM * rootFraction[layer];
				fomData.N = amountN * rootFraction[layer];
				fomData.C = amountDM * rootFraction[layer] * CarbonFractionInDM;
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
				FOMData.Type = speciesFamily;
				FOMData.Layer = FOMdataLayer;
				IncorpFOM.Invoke(FOMData);
			}
		}

		#endregion

		#endregion

		#region Other processes  -------------------------------------------------------------------------------------------

		/// <summary>Harvests the specified type.</summary>
		/// <param name="type">The type.</param>
		/// <param name="amount">The amount.</param>
		public void Harvest(string type, double amount)
		{
			GrazeType GrazeData = new GrazeType();
			GrazeData.amount = amount;
			GrazeData.type = type;
			OnGraze(GrazeData);
		}

		/// <summary>Called when [graze].</summary>
		/// <param name="GrazeData">The graze data.</param>
		[EventSubscribe("Graze")]
		private void OnGraze(GrazeType GrazeData)
		{
			if ((!isAlive) || StandingWt == 0)
				return;

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
			double amountToRemove = Math.Min(amountRequired, HarvestableWt);

			// Do the actual removal
			if (amountRequired > 0.0)
				RemoveDM(amountToRemove);
		}

		/// <summary>
		/// Remove a given amount of DM (and N) from this plant (consider preferences for green/dead material)
		/// </summary>
		/// <param name="AmountToRemove">Amount to remove (kg/ha)</param>
		/// <exception cref="System.Exception">   + Name +  - removal of DM resulted in loss of mass balance</exception>
		public void RemoveDM(double AmountToRemove)
		{
			// check existing amount and what is harvestable
			double PreRemovalDM = dmShoot;
			double PreRemovalN = Nshoot;

			if (HarvestableWt > 0.0)
			{
				// get the DM weights for each pool, consider preference and available DM
				double tempPrefGreen = preferenceForGreenDM + (preferenceForDeadDM * (AmountToRemove / HarvestableWt));
				double tempPrefDead = preferenceForDeadDM + (preferenceForGreenDM * (AmountToRemove / HarvestableWt));
				double tempRemovableGreen = Math.Max(0.0, StandingLiveWt - minimumGreenWt);
				double tempRemovableDead = Math.Max(0.0, StandingDeadWt - MinimumDeadWt);

				// get partiton between dead and live materials
				double tempTotal = tempRemovableGreen * tempPrefGreen + tempRemovableDead * tempPrefDead;
				double fractionToHarvestGreen = 0.0;
				double fractionToHarvestDead = 0.0;
				if (tempTotal > 0.0)
				{
					fractionToHarvestGreen = tempRemovableGreen * tempPrefGreen / tempTotal;
					fractionToHarvestDead = tempRemovableDead * tempPrefDead / tempTotal;
				}

				// get amounts removed
				double RemovingGreenDM = AmountToRemove * fractionToHarvestGreen;
				double RemovingDeadDM = AmountToRemove * fractionToHarvestDead;

				// Fraction of DM remaining in the field
				double fractionRemainingGreen = 1.0;
				if (StandingLiveWt > 0.0)
					fractionRemainingGreen = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingGreenDM / StandingLiveWt));
				double fractionRemainingDead = 1.0;
				if (StandingDeadWt > 0.0)
					fractionRemainingDead = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingDeadDM / StandingDeadWt));

				// get digestibility of DM being harvested
				digestDefoliated = calcDigestibility();

				// update the various pools
				dmLeaf1 *= fractionRemainingGreen;
				dmLeaf2 *= fractionRemainingGreen;
				dmLeaf3 *= fractionRemainingGreen;
				dmLeaf4 *= fractionRemainingDead;
				dmStem1 *= fractionRemainingGreen;
				dmStem2 *= fractionRemainingGreen;
				dmStem3 *= fractionRemainingGreen;
				dmStem4 *= fractionRemainingDead;
				//No stolon remove

				// N remove
				Nleaf1 *= fractionRemainingGreen;
				Nleaf2 *= fractionRemainingGreen;
				Nleaf3 *= fractionRemainingGreen;
				Nleaf4 *= fractionRemainingDead;
				Nstem1 *= fractionRemainingGreen;
				Nstem2 *= fractionRemainingGreen;
				Nstem3 *= fractionRemainingGreen;
				Nstem4 *= fractionRemainingDead;

				//C and N remobilised are also removed proportionally
				double PreRemovalNRemob = NRemobilised;
				double PreRemovalCRemob = CRemobilised;
				NRemobilised *= fractionRemainingGreen;
				CRemobilised *= fractionRemainingGreen;

				// update Luxury N pools
				NLuxury2 *= fractionRemainingGreen;
				NLuxury3 *= fractionRemainingGreen;

				// update aggregate variables
				updateAggregated();

				// check mass balance and set outputs
				dmDefoliated = PreRemovalDM - dmShoot;
				Ndefoliated = PreRemovalN - Nshoot;
				if (Math.Abs(dmDefoliated - AmountToRemove) > 0.00001)
					throw new Exception("  " + Name + " - removal of DM resulted in loss of mass balance");
			}
		}

		/// <summary>Remove biomass from plant</summary>
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


			// get digestibility of DM being removed
			digestDefoliated = calcDigestibility();

			for (int i = 0; i < RemovalData.dm.Length; i++)			  // for each pool (green or dead)
			{
				string plantPool = RemovalData.dm[i].pool;
				for (int j = 0; j < RemovalData.dm[i].dlt.Length; j++)   // for each part (leaf or stem)
				{
					string plantPart = RemovalData.dm[i].part[j];
					double amountToRemove = RemovalData.dm[i].dlt[j] * 10.0;    // convert to kgDM/ha
					if (plantPool.ToLower() == "green" && plantPart.ToLower() == "leaf")
					{
						if (LeafGreenWt - amountToRemove > 0.0)
						{
							fractionToRemove = MathUtilities.Divide(amountToRemove, LeafGreenWt, 0.0);
							RemoveFractionDM(fractionToRemove, plantPool, plantPart);
						}
					}
					else if (plantPool.ToLower() == "green" && plantPart.ToLower() == "stem")
					{
						if (StemGreenWt - amountToRemove > 0.0)
						{
							fractionToRemove = MathUtilities.Divide(amountToRemove, StemGreenWt, 0.0);
							RemoveFractionDM(fractionToRemove, plantPool, plantPart);
						}
					}
					else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "leaf")
					{
						if (LeafDeadWt - amountToRemove > 0.0)
						{
							fractionToRemove = MathUtilities.Divide(amountToRemove, LeafDeadWt, 0.0);
							RemoveFractionDM(fractionToRemove, plantPool, plantPart);
						}
					}
					else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "stem")
					{
						if (StemDeadWt - amountToRemove > 0.0)
						{
							fractionToRemove = MathUtilities.Divide(amountToRemove, StemDeadWt, 0.0);
							RemoveFractionDM(fractionToRemove, plantPool, plantPart);
						}
					}
				}
			}
			RefreshAfterRemove();
		}

		/// <summary>Remove a fraction of DM from a given plant part</summary>
		/// <param name="fractionR">The fraction of DM and N to remove</param>
		/// <param name="pool">The pool to remove from (green or dead)</param>
		/// <param name="part">The part to remove from (leaf or stem)</param>
		public void RemoveFractionDM(double fractionR, string pool, string part)
		{
			if (pool.ToLower() == "green")
			{
				if (part.ToLower() == "leaf")
				{
					// removing green leaves
					dmDefoliated += LeafGreenWt * fractionR;
					Ndefoliated += LeafGreenN * fractionR;

					dmLeaf1 *= fractionR;
					dmLeaf2 *= fractionR;
					dmLeaf3 *= fractionR;

					Nleaf1 *= fractionR;
					Nleaf2 *= fractionR;
					Nleaf3 *= fractionR;
				}
				else if (part.ToLower() == "stem")
				{
					// removing green stems
					dmDefoliated += StemGreenWt * fractionR;
					Ndefoliated += StemGreenN * fractionR;

					dmStem1 *= fractionR;
					dmStem2 *= fractionR;
					dmStem3 *= fractionR;

					Nstem1 *= fractionR;
					Nstem2 *= fractionR;
					Nstem3 *= fractionR;
				}
			}
			else if (pool.ToLower() == "green")
			{
				if (part.ToLower() == "leaf")
				{
					// removing dead leaves
					dmDefoliated += LeafDeadWt * fractionR;
					Ndefoliated += LeafDeadN * fractionR;

					dmLeaf4 *= fractionR;
					Nleaf4 *= fractionR;
				}
				else if (part.ToLower() == "stem")
				{
					// removing dead stems
					dmDefoliated += StemDeadWt * fractionR;
					Ndefoliated += StemDeadN * fractionR;

					dmStem4 *= fractionR;
					Nstem4 *= fractionR;
				}
			}
		}

		/// <summary>Performs few actions to update variables after RemoveFractionDM</summary>
		public void RefreshAfterRemove()
		{
			// set values for fractionHarvest (in fact fraction harvested)
			fractionHarvested = MathUtilities.Divide(dmDefoliated, StandingWt + dmDefoliated, 0.0);

			// recalc the digestibility
			calcDigestibility();

			// update aggregated variables
			updateAggregated();
		}

		/// <summary>Reset this plant state to its initial values</summary>
		public void Reset()
		{
			SetInitialState();
			prevState = new SpeciesState();
		}

		/// <summary>Kills this plant (zero all variables and set to not alive)</summary>
		/// <param name="KillData">Fraction of crop to kill (here always 100%)</param>
		[EventSubscribe("KillCrop")]
		public void OnKillCrop(KillCropType KillData)
		{
			// Return all above ground parts to surface OM
			DoSurfaceOMReturn(dmShoot, Nshoot);

			// Incorporate all root mass to soil fresh organic matter
			DoIncorpFomEvent(dmRoot, Nroot);

			ResetZero();

			isAlive = false;
		}

		/// <summary>Reset this plant to zero (kill crop)</summary>
		public void ResetZero()
		{

			// Zero out the DM pools
			dmLeaf1 = dmLeaf2 = dmLeaf3 = dmLeaf4 = 0.0;
			dmStem1 = dmStem2 = dmStem3 = dmStem4 = 0.0;
			dmStolon1 = dmStolon2 = dmStolon3 = 0.0;
			dmRoot = 0.0;
			dmDefoliated = 0.0;

			// Zero out the N pools
			Nleaf1 = Nleaf2 = Nleaf3 = Nleaf4 = 0.0;
			Nstem1 = Nstem2 = Nstem3 = Nstem4 = 0.0;
			Nstolon1 = Nstolon2 = Nstolon3 = 0.0;
			Nroot = 0.0;
			Ndefoliated = 0.0;

			digestDefoliated = 0.0;

			updateAggregated();

			phenoStage = 0;

			prevState = new SpeciesState();
		}

		#endregion

		#region Functions  -------------------------------------------------------------------------------------------------

		/// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">soilstate</param>
		/// <returns></returns>
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

		/// <summary>
		/// Set the sw uptake for today
		/// </summary>
		public void SetSWUptake(List<ZoneWaterAndN> info)
		{ }
        /// <summary>
        /// Set the n uptake for today
        /// </summary>
        public void SetNUptake(List<ZoneWaterAndN> info)
        { }    

		/// <summary>Growth limiting factor due to temperature</summary>
		/// <param name="Temp">Temperature for which the limiting factor will be computed</param>
		/// <returns>The value for the limiting factor (0-1)</returns>
		/// <exception cref="System.Exception">Photosynthesis pathway is not valid</exception>
		private double TemperatureLimitingFactor(double Temp)
		{
			double result = 0.0;
			if (photosynthesisPathway == "C3")
			{
				if (Temp > growthTmin && Temp < growthTmax)
				{
					double growthTmax1 = growthTopt + (growthTopt - growthTmin) / growthTq;
					double val1 = Math.Pow((Temp - growthTmin), growthTq) * (growthTmax1 - Temp);
					double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (growthTmax1 - growthTopt);  // TODO: replace Topt with Tref here
					result = val1 / val2;
				}
			}
			else if (photosynthesisPathway == "C4")
			{
				if (Temp > growthTmin)
				{
					if (Temp > growthTopt)
						Temp = growthTopt;

					double growthTmax1 = growthTopt + (growthTopt - growthTmin) / growthTq;
					double val1 = Math.Pow((Temp - growthTmin), growthTq) * (growthTmax1 - Temp);
					double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (growthTmax1 - growthTopt);  // TODO: replace Topt with Tref here
					result = val1 / val2;
				}
			}
			else
				throw new Exception("Photosynthesis pathway is not valid");
			return result;
		}

		/// <summary>Effect of temperature on tissue turnover</summary>
		/// <param name="Temp">The temporary.</param>
		/// <returns>Temperature factor (0-1)</returns>
		private double TempFactorForTissueTurnover(double Temp)
		{
			double result = 0.0;
			if (Temp > tissueTurnoverTmin && Temp <= tissueTurnoverTopt)
			{
				result = (Temp - tissueTurnoverTmin) / (tissueTurnoverTopt - tissueTurnoverTmin);  // TODO: implement power function
			}
			else if (Temp > tissueTurnoverTopt)
			{
				result = 1.0;
			}
			return result;
		}

		/// <summary>Photosynthesis reduction factor due to high temperatures (heat stress)</summary>
		/// <returns>The reduction in photosynthesis rate (0-1)</returns>
		private double HeatStress()
		{
			// evaluate recovery from the previous high temperature effects
			double recoverF = 1.0;

			if (highTempEffect < 1.0)
			{
				if (referenceT4Heat > Tmean)
					accumT4Heat += (referenceT4Heat - Tmean);

				if (accumT4Heat < heatSumT)
					recoverF = highTempEffect + (1 - highTempEffect) * accumT4Heat / heatSumT;
			}

			// Evaluate the high temperature factor for today
			double newHeatF = 1.0;
			if (MetData.MaxT > heatFullT)
				newHeatF = 0;
			else if (MetData.MaxT > heatOnsetT)
				newHeatF = (MetData.MaxT - heatOnsetT) / (heatFullT - heatOnsetT);

			// If this new high temp. factor is smaller than 1.0, then it is compounded with the old one
			// also, the cumulative heat for recovery is re-started
			if (newHeatF < 1.0)
			{
				highTempEffect = recoverF * newHeatF;
				accumT4Heat = 0;
				recoverF = highTempEffect;
			}

			return recoverF;  // TODO: revise this function
		}

		/// <summary>Photosynthesis reduction factor due to low temperatures (cold stress)</summary>
		/// <returns>The reduction in potosynthesis rate (0-1)</returns>
		private double ColdStress()
		{
			//recover from the previous high temp. effect
			double recoverF = 1.0;
			if (lowTempEffect < 1.0)
			{
				if (Tmean > referenceT4Cold)
					accumT4Cold += (Tmean - referenceT4Cold);

				if (accumT4Cold < coldSumT)
					recoverF = lowTempEffect + (1 - lowTempEffect) * accumT4Cold / coldSumT;
			}

			//possible new low temp. effect
			double newColdF = 1.0;
			if (MetData.MinT < coldFullT)
				newColdF = 0;
			else if (MetData.MinT < coldOnsetT)
				newColdF = (MetData.MinT - coldFullT) / (coldOnsetT - coldFullT);

			// If this new cold temp. effect happens when serious cold effect is still on,
			// compound & then re-start of the recovery from the new effect
			if (newColdF < 1.0)
			{
				lowTempEffect = newColdF * recoverF;
				accumT4Cold = 0;
				recoverF = lowTempEffect;
			}

			return recoverF; // TODO: revise this function
		}

		/// <summary>Photosynthesis factor (reduction or increase) to eleveated [CO2]</summary>
		/// <returns>A factor to adjust photosynthesis due to CO2</returns>
		private double PCO2Effects()
		{
			if (Math.Abs(MetData.CO2 - referenceCO2) < 0.01)
				return 1.0;

			double Fp1 = MetData.CO2 / (coefficientCO2EffectOnPhotosynthesis + MetData.CO2);
			double Fp2 = (referenceCO2 + coefficientCO2EffectOnPhotosynthesis) / referenceCO2;

			return Fp1 * Fp2;
		}

		/// <summary>Effect on photosynthesis due to variations in optimum N concentration as affected by CO2</summary>
		/// <returns>A factor to adjust photosynthesis</returns>
		private double PmxNeffect()
		{
			if (isAnnual)
				return 0.0;
			else
			{
				double fN = NCO2Effects();

				double result = 1.0;
				double NcleafGreen = MathUtilities.Divide(Nleaf1 + Nleaf2 + Nleaf3, dmLeaf1 + dmLeaf2 + dmLeaf3, 0.0);
				if (NcleafGreen < leafNopt * fN)
				{
					if (NcleafGreen > leafNmin)
					{
						result = MathUtilities.Divide(NcleafGreen - leafNmin, (leafNopt * fN) - leafNmin, 1.0);
						result = Math.Min(1.0, Math.Max(0.0, result));
					}
					else
					{
						result = 0.0;
					}
				}

				return result;
			}
		}

		/// <summary>Plant nitrogen [N] decline to elevated [CO2]</summary>
		/// <returns>A factor to adjust N demand</returns>
		private double NCO2Effects()
		{
			if (Math.Abs(MetData.CO2 - referenceCO2) < 0.01)
				return 1.0;

			double termK = Math.Pow(offsetCO2EffectOnNuptake - referenceCO2, exponentCO2EffectOnNuptake);
			double termC = Math.Pow(MetData.CO2 - referenceCO2, exponentCO2EffectOnNuptake);
			double result = (1 - minimumCO2EffectOnNuptake) * termK / (termK + termC);

			return minimumCO2EffectOnNuptake + result;
		}

		//Canopy conductance decline to elevated [CO2]
		/// <summary>Conductances the c o2 effects.</summary>
		/// <returns></returns>
		private double ConductanceCO2Effects()
		{
			if (Math.Abs(MetData.CO2 - referenceCO2) < 0.5)
				return 1.0;
			//Hard coded here, not used, should go to Micromet!   - TODO
			double Gmin = 0.2;      //Fc = Gmin when CO2->unlimited
			double Gmax = 1.25;     //Fc = Gmax when CO2 = 0;
			double beta = 2.5;      //curvature factor,

			double aux1 = (1 - Gmin) * Math.Pow(referenceCO2, beta);
			double aux2 = (Gmax - 1) * Math.Pow(MetData.CO2, beta);
			double Fc = (Gmax - Gmin) * aux1 / (aux2 + aux1);
			return Gmin + Fc;
		}

		/// <summary>Growth limiting factor due to soil moisture deficit</summary>
		/// <returns>The limiting factor due to soil water deficit (0-1)</returns>
		internal double WaterDeficitFactor()
		{
			double result = 0.0;

			if (myWaterDemand <= 0.0001)         // demand should never be really negative, but might be slightly because of precision of float numbers
				result = 1.0;
			else
				result = mySoilWaterTakenUp.Sum() / myWaterDemand;

			return Math.Max(0.0, Math.Min(1.0, result));
		}

		/// <summary>Growth limiting factor due to excess of water in soil (logging/saturation)</summary>
		/// <returns>The limiting factor due to excess of soil water</returns>
		/// <remarks>Assuming that water above field capacity is not good</remarks>
		internal double WaterLoggingFactor()
		{
			double result = 1.0;

			// calculate soil moisture thresholds in the root zone
			double mySWater = 0.0;
			double mySaturation = 0.0;
			double myDUL = 0.0;
			double fractionLayer = 0.0;
			for (int layer = 0; layer <= myRootFrontier; layer++)
			{
				// fraction of layer with roots 
				fractionLayer = LayerFractionWithRoots(layer);
				// actual soil water content
				mySWater += Soil.Water[layer] * fractionLayer;
				// water content at saturation
				mySaturation += Soil.SoilWater.SATmm[layer] * fractionLayer;
				// water content at field capacity
				myDUL += Soil.SoilWater.DULmm[layer] * fractionLayer;
			}

			result = 1.0 - waterLoggingCoefficient * Math.Max(0.0, mySWater - myDUL) / (mySaturation - myDUL);

			return result;
		}

		/// <summary>Effect of water stress on tissue turnover</summary>
		/// <returns>Water stress factor (0-1)</returns>
		private double WaterFactorForTissueTurnover()
		{
			double result = 1.0;
			if (glfWater < tissueTurnoverGLFWopt)
			{
				result = (tissueTurnoverGLFWopt - glfWater) / tissueTurnoverGLFWopt;
				result = (tissueTurnoverWFactorMax - 1.0) * result;
				result = Math.Min(tissueTurnoverWFactorMax, Math.Max(1.0, 1 + result));
			}

			return result;
		}

		/// <summary>Computes the ground cover for the plant, or plant part</summary>
		/// <param name="thisLAI">The LAI for this plant or part</param>
		/// <returns>Fraction of ground effectively covered (0-1)</returns>
		private double CalcPlantCover(double thisLAI)
		{
			return (1.0 - Math.Exp(-lightExtentionCoeff * thisLAI));
		}

		/// <summary>Compute the distribution of roots in the soil profile (sum is equal to one)</summary>
		/// <returns>The proportion of root mass in each soil layer</returns>
		/// <exception cref="System.Exception">
		/// No valid method for computing root distribution was selected
		/// or
		/// Could not calculate root distribution
		/// </exception>
		private double[] RootProfileDistribution()
		{
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
							if (DepthTop >= myRootDepth)
								result[layer] = 0.0;
							else if (DepthTop + Soil.Thickness[layer] <= myRootDepth)
								result[layer] = 1.0;
							else
								result[layer] = (myRootDepth - DepthTop) / Soil.Thickness[layer];
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
						double DepthFirstStage = myRootDepth * expoLinearDepthParam;
						double DepthSecondStage = myRootDepth - DepthFirstStage;
						for (int layer = 0; layer < nLayers; layer++)
						{
							if (DepthTop >= myRootDepth)
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
								else if (DepthTop + Soil.Thickness[layer] <= myRootDepth)
									result[layer] += Math.Min(DepthTop + Soil.Thickness[layer], myRootDepth) - Math.Max(DepthTop, DepthFirstStage) / Soil.Thickness[layer];
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
					result[layer] = MathUtilities.Divide(result[layer], sumProportion, 0.0);
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
			if (layer > myRootFrontier)
				return 0.0;
			else
			{
				double depthAtTopThisLayer = 0;   // depth till the top of the layer being considered
				for (int z = 0; z < layer; z++)
					depthAtTopThisLayer += Soil.Thickness[z];
				double result = (myRootDepth - depthAtTopThisLayer) / Soil.Thickness[layer];
				return Math.Min(1.0, Math.Max(0.0, result));
			}
		}


		/// <summary>VPDs this instance.</summary>
		/// <returns></returns>
		/// The following helper functions [VDP and svp] are for calculating Fvdp
		private double VPD()
		{
			double VPDmint = svp(MetData.MinT) - MetData.VP;
			VPDmint = Math.Max(VPDmint, 0.0);

			double VPDmaxt = svp(MetData.MaxT) - MetData.VP;
			VPDmaxt = Math.Max(VPDmaxt, 0.0);

			double vdp = 0.66 * VPDmaxt + 0.34 * VPDmint;
			return vdp;
		}
		/// <summary>SVPs the specified temporary.</summary>
		/// <param name="temp">The temporary.</param>
		/// <returns></returns>
		private double svp(double temp)  // from Growth.for documented in MicroMet
		{
			return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
		}

		#endregion
	}

	/// <summary>Stores the state variables of a pasture species</summary>
	[Serializable]
	public class SpeciesState
	{
		/// <summary>Initializes a new instance of the species state<see cref="SpeciesState"/> class.</summary>
		public SpeciesState() { }

		// DM pools  ------------------------------------------------------------------------------
		/// <summary>The DM weight of leaves at stage1</summary>
		public double dmLeaf1;
		/// <summary>The DM weight of leaves at stage2</summary>
		public double dmLeaf2;
		/// <summary>The DM weight of leaves at stage3</summary>
		public double dmLeaf3;
		/// <summary>The DM weight of leaves at stage4</summary>
		public double dmLeaf4;

		/// <summary>The DM weight of stems at stage1</summary>
		public double dmStem1;
		/// <summary>The DM weight of stems at stage2</summary>
		public double dmStem2;
		/// <summary>The DM weight of stems at stage3</summary>
		public double dmStem3;
		/// <summary>The DM weight of stems at stage4</summary>
		public double dmStem4;

		/// <summary>The DM weight of stolons at stage1</summary>
		public double dmStolon1;
		/// <summary>The DM weight of stolons at stage2</summary>
		public double dmStolon2;
		/// <summary>The DM weight of stolons at stage3</summary>
		public double dmStolon3;

		/// <summary>The DM weight of roots</summary>
		public double dmRoot;

		/// <summary>The DM weight of all leaves</summary>
		public double dmLeaf
		{ get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmLeaf4; } }
		/// <summary>The DM weight of all stem and sheath</summary>
		public double dmStem
		{ get { return dmStem1 + dmStem2 + dmStem3 + dmStem4; } }
		/// <summary>The DM weight of all stolons</summary>
		public double dmStolon
		{ get { return dmStolon1 + dmStolon2 + dmStolon3; } }


		// N pools  -------------------------------------------------------------------------------
		/// <summary>The amount of N in leaves at stage 1</summary>
		public double Nleaf1;
		/// <summary>The amount of N in leaves at stage 2</summary>
		public double Nleaf2;
		/// <summary>The amount of N in leaves at stage 3</summary>
		public double Nleaf3;
		/// <summary>The amount of N in leaves at stage 4</summary>
		public double Nleaf4;

		/// <summary>The amount of N in stems at stage 1</summary>
		public double Nstem1;
		/// <summary>The amount of N in stems at stage 2</summary>
		public double Nstem2;
		/// <summary>The amount of N in stems at stage 3</summary>
		public double Nstem3;
		/// <summary>The amount of N in stems at stage 4</summary>
		public double Nstem4;

		/// <summary>The amount of N in stolons at stage 1</summary>
		public double Nstolon1;
		/// <summary>The amount of N in stolons at stage 2</summary>
		public double Nstolon2;
		/// <summary>The amount of N in stolons at stage 3</summary>
		public double Nstolon3;

		/// <summary>The amount of N in roots</summary>
		public double Nroot;

		// N concentrations  ----------------------------------------------------------------------
		/// <summary>The N concentration in leaves at stage 1</summary>
		public double NconcLeaf1
		{ get { return (dmLeaf1 > 0.0) ? Nleaf1 / dmLeaf1 : 0.0; } }
		/// <summary>The N concentration in leaves at stage 2</summary>
		public double NconcLeaf2
		{ get { return (dmLeaf2 > 0.0) ? Nleaf2 / dmLeaf2 : 0.0; } }
		/// <summary>The N concentration in leaves at stage 3</summary>
		public double NconcLeaf3
		{ get { return (dmLeaf3 > 0.0) ? Nleaf3 / dmLeaf3 : 0.0; } }
		/// <summary>The N concentration in leaves at stage 4</summary>
		public double NconcLeaf4
		{ get { return (dmLeaf4 > 0.0) ? Nleaf4 / dmLeaf4 : 0.0; } }

		/// <summary>The N concentration in stems at stage 1</summary>
		public double NconcStem1
		{ get { return (dmStem1 > 0.0) ? Nstem1 / dmStem1 : 0.0; } }
		/// <summary>The N concentration in stems at stage 2</summary>
		public double NconcStem2
		{ get { return (dmStem2 > 0.0) ? Nstem2 / dmStem2 : 0.0; } }
		/// <summary>The N concentration in stems at stage 3</summary>
		public double NconcStem3
		{ get { return (dmStem3 > 0.0) ? Nstem3 / dmStem3 : 0.0; } }
		/// <summary>The N concentration in stems at stage 4</summary>
		public double NconcStem4
		{ get { return (dmStem4 > 0.0) ? Nstem4 / dmStem4 : 0.0; } }

		/// <summary>The N concentration in stolons at stage 1</summary>
		public double NconcStolon1
		{ get { return (dmStolon1 > 0.0) ? Nstolon1 / dmStolon1 : 0.0; } }
		/// <summary>The N concentration in stolons at stage 2</summary>
		public double NconcStolon2
		{ get { return (dmStolon2 > 0.0) ? Nstolon2 / dmStolon2 : 0.0; } }
		/// <summary>The N concentration in stolons at stage 3</summary>
		public double NconcStolon3
		{ get { return (dmStolon3 > 0.0) ? Nstolon3 / dmStolon3 : 0.0; } }

		/// <summary>The N concentration in roots</summary>
		public double NconcRoot
		{ get { return (dmRoot > 0.0) ? Nroot / dmRoot : 0.0; } }
	}

	/// <summary>Defines a broken stick (piecewise) function</summary>
	[Serializable]
	public class BrokenStick
	{
		/// <summary>The x</summary>
		public double[] X;
		/// <summary>The y</summary>
		public double[] Y;

		/// <summary>Values the specified new x.</summary>
		/// <param name="newX">The new x.</param>
		/// <returns></returns>
		public double Value(double newX)
		{
			bool DidInterpolate = false;
			return MathUtilities.LinearInterpReal(newX, X, Y, out DidInterpolate);
		}
	}
}
