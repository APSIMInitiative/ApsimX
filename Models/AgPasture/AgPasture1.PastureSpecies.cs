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
    /// Describes a pasture species present in the sward
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PastureSpecies : Model, ICrop
    {
        #region Links, events and delegates  -------------------------------------------------------------------------------

        //- Links  ---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Link to APSIM's Clock (time information)
        /// </summary>
        [Link]
        private Clock Clock = null;

        /// <summary>
        /// Link to APSIM's WeatherFile (meteorological information)
        /// </summary>
        [Link]
        private WeatherFile MetData = null;

        /// <summary>
        /// Link to the soil info
        /// </summary>
        /// <remarks>
        /// This might not be necesasry in the near future if water and N uptake are controled by an
        ///  external module (AgPasture or Arbitrator)
        /// </remarks>
        [Link]
        private Soils.Soil Soil = null;

        /// <summary>
        /// Link to APSIM's Summary (For supplying information to user, warnings, etc)
        /// </summary>
        [Link]
        private ISummary Summary = null;

        //- Events  --------------------------------------------------------------------------------------------------------
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
            get { return speciesName; }
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
            get { return myWaterDemand; }
            set { myWaterDemand = value; }
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
            get { return 1.0; }
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
                EventData.crop_type = speciesName;
                EventData.sender = Name;
                NewCrop.Invoke(EventData);
            }
        }

        #endregion

        #region Model parameters  ------------------------------------------------------------------------------------------

        private string speciesName = "Ryegrass";
        [Description("Name of this plant species")]
        public string SpeciesName
        {
            get { return speciesName; }
            set { speciesName = value; }
        }

        private string speciesNType = "grass"; //speciesFamily = "grass";
        [Description("Family type for this plant species (grass/legume/brassica):")]
        public string SpeciesNType
        {
            get { return speciesNType; }
            set { speciesNType = value; }
        }

        private string speciesCType = "C3";
        [Description("Metabolic pathway for C fixation during photosynthesis (C3/C4/CAM")]
        public string SpeciesCType
        {
            get { return speciesCType; }
            set { speciesCType = value; }
        }

        private double inidmShoot = 1000.0;
        [Description("Initial above ground DM (leaf, stem, stolon, etc")]
        public double InitialdmShoot
        {
            get { return inidmShoot; }
            set { inidmShoot = value; }
        }

        private double inidmRoot = 250.0;
        [Description("Initial below ground DM (roots)")]
        public double InitialdmRoot
        {
            get { return inidmRoot; }
            set { inidmRoot = value; }
        }

        private double minimumGreenWt = 300.0;
        [Description("Minimum above ground green DM")]
        public double MinimumGreenWt
        {
            get { return minimumGreenWt; }
            set { minimumGreenWt = value; }
        }

        private double iniRootDepth = 750.0;
        [Description("Initial depth for roots")]
        public double InitialRootDepth
        {
            get { return iniRootDepth; }
            set { iniRootDepth = value; }
        }


        private double[] iniDMFraction = new double[] { 0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00 };
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
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

        private double maxPhotosynthesisRate = 1.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MaxPhotosynthesisRate
        {
            get { return maxPhotosynthesisRate; }
            set { maxPhotosynthesisRate = value; }
        }

        private double maintenanceRespirationCoef = 0.03;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MaintenanceRespirationCoef
        {
            get { return maintenanceRespirationCoef * 100; }
            set { maintenanceRespirationCoef = value / 100; }
        }

        private double growthEfficiency = 0.75;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double GrowthEfficiency
        {
            get { return growthEfficiency; }
            set { growthEfficiency = value; }
        }

        private double lightExtentionCoeff = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double LightExtentionCoeff
        {
            get { return lightExtentionCoeff; }
            set { lightExtentionCoeff = value; }
        }

        private double maxAssimilationRate = 330.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MaxAssimilationRate
        {
            get { return maxAssimilationRate; }
            set { maxAssimilationRate = value; }
        }

        private double growthTmin = 2.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double GrowthTmin
        {
            get { return growthTmin; }
            set { growthTmin = value; }
        }

        private double growthTmax = 32.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double GrowthTmax
        {
            get { return growthTmax; }
            set { growthTmax = value; }
        }

        private double growthTopt = 20.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double GrowthTopt
        {
            get { return growthTopt; }
            set { growthTopt = value; }
        }

        private double growthTq = 2.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double GrowthTq
        {
            get { return growthTq; }
            set { growthTq = value; }
        }

        private double massFluxTmin = 2.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MassFluxTmin
        {
            get { return massFluxTmin; }
            set { massFluxTmin = value; }
        }

        private double massFluxTopt = 20.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MassFluxTopt
        {
            get { return massFluxTopt; }
            set { massFluxTopt = value; }
        }

        private double massFluxW0 = 2.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MassFluxW0
        {
            get { return massFluxW0; }
            set { massFluxW0 = value; }
        }

        private double massFluxWopt = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MassFluxWopt
        {
            get { return massFluxWopt; }
            set { massFluxWopt = value; }
        }

        private double heatOnsetT = 75.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double HeatOnsetT
        {
            get { return heatOnsetT; }
            set { heatOnsetT = value; }
        }

        private double heatFullT = 100.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double HeatFullT
        {
            get { return heatFullT; }
            set { heatFullT = value; }
        }

        private double heatSumT = 50.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double HeatSumT
        {
            get { return heatSumT; }
            set { heatSumT = value; }
        }

        private double referenceT4Heat = 25.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ReferenceT4Heat
        {
            get { return referenceT4Heat; }
            set { referenceT4Heat = value; }
        }

        private double coldOnsetT = -20.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ColdOnsetT
        {
            get { return coldOnsetT; }
            set { coldOnsetT = value; }
        }

        private double coldFullT = -30.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ColdFullT
        {
            get { return coldFullT; }
            set { coldFullT = value; }
        }

        private double coldSumT = 20.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ColdSumT
        {
            get { return coldSumT; }
            set { coldSumT = value; }
        }

        private double referenceT4Cold = 25.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ReferenceT4Cold
        {
            get { return referenceT4Cold; }
            set { referenceT4Cold = value; }
        }

        private double specificLeafArea = 20.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double SpecificLeafArea
        {
            get { return specificLeafArea; }
            set { specificLeafArea = value; }
        }

        private double specificRootLength = 75.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double SpecificRootLength
        {
            get { return specificRootLength; }
            set { specificRootLength = value; }
        }

        private double maxRootFraction = 0.25;
        /// <summary>
        /// Maximum fraction of DM allocated to roots (from daily growth)
        /// </summary>
        [XmlIgnore]
        public double MaxRootFraction
        {
            get { return maxRootFraction; }
            set { maxRootFraction = value; }
        }

        private double shootAllocationIncrease = 0.8;
        /// <summary>
        /// Fraction by which DM allocation to shoot is increased during 'spring'
        /// </summary>
        /// <remarks>
        /// Allocation to shoot is typically given by 1-maxRootFraction, but for a certain 'spring' period it can be increased to simulate reproductive growth
        /// at this period shoot allocation is corrected by multiplying it by 1 + SeasonShootAllocationIncrease
        /// </remarks>
        [XmlIgnore]
        public double SeasonShootAllocationIncrease
        {
            get { return shootAllocationIncrease; }
            set { shootAllocationIncrease = value; }
        }

        private int doyIniHighShoot = 232;
        /// <summary>
        /// Day for the beginning of 'spring' (or for the period with higher shoot allocation)
        /// </summary>
        /// <remarks>
        /// Care must be taken as this varies with north or south hemisphere
        /// </remarks>
        [XmlIgnore]
        public int DayInitHigherShootAllocation
        {
            get { return doyIniHighShoot; }
            set { doyIniHighShoot = value; }
        }

        private int[] higherShootAllocationPeriods = new int[] { 35, 60, 30 };
        /// <summary>
        /// Number of days defining the periods for the three phases with higher DM allocation to shoot
        /// </summary>
        /// <remarks>
        /// Three numbers are needed, they define the duration of the phases for increase, plateau, and the deacrease in allocation
        /// The allocation to shoot is maximum at the plateau phase, it is 1 + SeasonShootAllocationIncrease times the value of maxSRratio
        /// </remarks>
        [XmlIgnore]
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

        private double fracToLeaf = 0.7;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double FracToLeaf
        {
            get { return fracToLeaf; }
            set { fracToLeaf = value; }
        }

        private double fracToStolon = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double FracToStolon
        {
            get { return fracToStolon; }
            set { fracToStolon = value; }
        }

        private double turnoverRateLive2Dead = 0.05;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double TurnoverRateLive2Dead
        {
            get { return turnoverRateLive2Dead; }
            set { turnoverRateLive2Dead = value; }
        }

        private double turnoverRateDead2Litter = 0.11;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double TurnoverRateDead2Litter
        {
            get { return turnoverRateDead2Litter; }
            set { turnoverRateDead2Litter = value; }
        }

        private double turnoverRateRootSenescence = 0.02;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double TurnoverRateRootSenescence
        {
            get { return turnoverRateRootSenescence; }
            set { turnoverRateRootSenescence = value; }
        }

        private double stockParameter = 0.05;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double StockParameter
        {
            get { return stockParameter; }
            set { stockParameter = value; }
        }

        private double digestibilityLive = 0.6;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double DigestibilityLive
        {
            get { return digestibilityLive; }
            set { digestibilityLive = value; }
        }

        private double digestibilityDead = 0.2;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double DigestibilityDead
        {
            get { return digestibilityDead; }
            set { digestibilityDead = value; }
        }

        private double minimumDeadWt = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MinimumDeadWt
        {
            get { return minimumDeadWt; }
            set { minimumDeadWt = value; }
        }

        private double leafNopt = 0.04;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double LeafNopt
        {
            get { return leafNopt * 100; }
            set { leafNopt = value / 100; }
        }

        private double leafNmax = 0.05;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double LeafNmax
        {
            get { return leafNmax * 100; }
            set { leafNmax = value / 100; }
        }

        private double leafNmin = 0.012;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double LeafNmin
        {
            get { return leafNmin * 100; }
            set { leafNmin = value / 100; }
        }

        private double relativeNStems = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double RelativeNStems
        {
            get { return relativeNStems; }
            set { relativeNStems = value; }
        }

        private double relativeNStolons = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double RelativeNStolons
        {
            get { return relativeNStolons; }
            set { relativeNStolons = value; }
        }

        private double relativeNRoots = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double RelativeNRoots
        {
            get { return relativeNRoots; }
            set { relativeNRoots = value; }
        }

        private double relativeNStage2 = 1.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double RelativeNStage2
        {
            get { return relativeNStage2; }
            set { relativeNStage2 = value; }
        }

        private double relativeNStage3 = 1.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double RelativeNStage3
        {
            get { return relativeNStage3; }
            set { relativeNStage3 = value; }
        }

        private double minimumNFixation = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MinimumNFixation
        {
            get { return minimumNFixation; }
            set { minimumNFixation = value; }
        }

        private double maximumNFixation = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MaximumNFixation
        {
            get { return maximumNFixation; }
            set { maximumNFixation = value; }
        }

        private double dillutionCoefN = 0.5;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double DillutionCoefN
        {
            get { return dillutionCoefN; }
            set { dillutionCoefN = value; }
        }

        private double kappaNRemob2 = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double KappaNRemob2
        {
            get { return kappaNRemob2; }
            set { kappaNRemob2 = value; }
        }
        private double kappaNRemob3 = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double KappaNRemob3
        {
            get { return kappaNRemob3; }
            set { kappaNRemob3 = value; }
        }

        private double kappaNRemob4 = 0.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double KappaNRemob4
        {
            get { return kappaNRemob4; }
            set { kappaNRemob4 = value; }
        }

        private double kappaCRemob = 0.0;
        /// <summary>
        /// Fraction of senescent DM that is remobilised (as carbohydrate)
        /// </summary>
        [XmlIgnore]
        public double KappaCRemob
        {
            get { return kappaCRemob; }
            set { kappaCRemob = value; }
        }

        private double facCNRemob = 0.0;
        /// <summary>
        /// Fraction of senescent DM that is remobilised (as carbohydrate)
        /// </summary>
        [XmlIgnore]
        public double FacCNRemob
        {
            get { return facCNRemob; }
            set { facCNRemob = value; }
        }

        private double glfGeneric = 1.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double GlfGeneric
        {
            get { return glfGeneric; }
            set { glfGeneric = value; }
        }

        private double waterStressFactor = 1.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double WaterStressFactor
        {
            get { return waterStressFactor; }
            set { waterStressFactor = value; }
        }

        private double waterLoggingEffect = 0.1;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double WaterLoggingEffect
        {
            get { return waterLoggingEffect; }
            set { waterLoggingEffect = value; }
        }

        private double referenceCO2 = 380.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ReferenceCO2
        {
            get { return referenceCO2; }
            set { referenceCO2 = value; }
        }

        private double offsetCO2EffectOnPhotosynthesis = 700.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double OffsetCO2EffectOnPhotosynthesis
        {
            get { return offsetCO2EffectOnPhotosynthesis; }
            set { offsetCO2EffectOnPhotosynthesis = value; }
        }

        private double offsetCO2EffectOnNuptake = 600.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double OffsetCO2EffectOnNuptake
        {
            get { return offsetCO2EffectOnNuptake; }
            set { offsetCO2EffectOnNuptake = value; }
        }

        private double minimumCO2EffectOnNuptake = 0.7;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double MinimumCO2EffectOnNuptake
        {
            get { return minimumCO2EffectOnNuptake; }
            set { minimumCO2EffectOnNuptake = value; }
        }

        private double exponentCO2EffectOnNuptake = 2.0;
        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public double ExponentCO2EffectOnNuptake
        {
            get { return exponentCO2EffectOnNuptake; }
            set { exponentCO2EffectOnNuptake = value; }
        }

        private double preferenceForGreenDM = 1.0;
        [XmlIgnore]
        public double PreferenceForGreenDM
        {
            get { return preferenceForGreenDM; }
            set { preferenceForGreenDM = value; }
        }

        private double preferenceForDeadDM = 1.0;
        [XmlIgnore]
        public double PreferenceForDeadDM
        {
            get { return preferenceForDeadDM; }
            set { preferenceForDeadDM = value; }
        }

        private string rootDistributionMethod = "ExpoLinear";
        /// <summary>
        /// Root distribution method
        /// </summary>
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

        [XmlIgnore]
        public BrokenStick HeightFromMass = new BrokenStick
        {
            X = new double[5] { 0, 1000, 2000, 3000, 4000 },
            Y = new double[5] { 0, 25, 75, 150, 250 }
        };

        [XmlIgnore]
        public BrokenStick FVPDFunction = new BrokenStick
        {
            X = new double[3] { 0.0, 10.0, 50.0 },
            Y = new double[3] { 1.0, 1.0, 1.0 }
        };


        internal string useSeparateCanopy = "no";

        internal string myWaterUptakeSource = "calc";
        internal string useAltWUptake = "no";
        internal double ReferenceKSuptake = 1000.0;
        internal string myNitrogenUptakeSource = "calc";
        internal string useAltNUptake = "no";
        internal double kuNH4 = 0.50;
        internal double kuNO3 = 0.95;
        internal double ReferenceRLD = 2.0;

        #endregion

        #region Model outputs  ---------------------------------------------------------------------------------------------

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

        [Description("Total plant dry matter weight")]
        [Units("kgDM/ha")]
        public double TotalWt
        {
            get { return dmShoot + dmRoot; }
        }

        [Description("Dry matter weight above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundWt
        {
            get { return dmShoot; }
        }

        [Description("Dry matter weight of alive plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGrounLivedWt
        {
            get { return dmGreen; }
        }

        [Description("Dry matter weight of dead plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundDeadWt
        {
            get { return dmDead; }
        }

        [Description("Dry matter weight below ground")]
        [Units("kgDM/ha")]
        public double BelowGroundWt
        {
            get { return dmRoot; }
        }

        [Description("Dry matter weight of standing herbage")]
        [Units("kgDM/ha")]
        public double StandingWt
        {
            get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmLeaf4 + dmStem1 + dmStem2 + dmStem3 + dmStem4; }
        }

        [Description("Dry matter weight of live standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingLiveWt
        {
            get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmStem1 + dmStem2 + dmStem3; }
        }

        [Description("Dry matter weight of dead standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingDeadWt
        {
            get { return dmLeaf4 + dmStem4; }
        }

        [Description("Dry matter weight of leaves")]
        [Units("kgDM/ha")]
        public double LeafWt
        {
            get { return dmLeaf1 + dmLeaf2 + dmLeaf3 + dmLeaf4; }
        }

        [Description("Dry matter weight of alive leaves")]
        [Units("kgDM/ha")]
        public double LeafGreenWt
        {
            get { return dmLeaf1 + dmLeaf2 + dmLeaf3; }
        }

        [Description("Dry matter weight of dead leaves")]
        [Units("kgDM/ha")]
        public double LeafDeadWt
        {
            get { return dmLeaf4; }
        }

        [Description("Dry matter weight of stems")]
        [Units("kgDM/ha")]
        public double StemWt
        {
            get { return dmStem1 + dmStem2 + dmStem3 + dmStem4; }
        }

        [Description("Dry matter weight of alive stems")]
        [Units("kgDM/ha")]
        public double StemGreenWt
        {
            get { return dmStem1 + dmStem2 + dmStem3; }
        }

        [Description("Dry matter weight of dead stems")]
        [Units("kgDM/ha")]
        public double StemDeadWt
        {
            get { return dmStem4; }
        }

        [Description("Dry matter weight of stolons")]
        [Units("kgDM/ha")]
        public double StolonWt
        {
            get { return dmStolon1 + dmStolon2 + dmStolon3; }
        }

        [Description("Dry matter weight of roots")]
        [Units("kgDM/ha")]
        public double RootWt
        {
            get { return dmRoot; }
        }

        [Description("Dry matter weight of leaves at stage 1 (young)")]
        [Units("kgDM/ha")]
        public double LeafStage1Wt
        {
            get { return dmLeaf1; }
        }

        [Description("Dry matter weight of leaves at stage 2 (developing)")]
        [Units("kgDM/ha")]
        public double LeafStage2Wt
        {
            get { return dmLeaf2; }
        }

        [Description("Dry matter weight of leaves at stage 3 (mature)")]
        [Units("kgDM/ha")]
        public double LeafStage3Wt
        {
            get { return dmLeaf3; }
        }

        [Description("Dry matter weight of leaves at stage 4 (dead)")]
        [Units("kgDM/ha")]
        public double LeafStage4Wt
        {
            get { return dmLeaf4; }
        }

        [Description("Dry matter weight of stems at stage 1 (young)")]
        [Units("kgDM/ha")]
        public double StemStage1Wt
        {
            get { return dmStem1; }
        }

        [Description("Dry matter weight of stems at stage 2 (developing)")]
        [Units("kgDM/ha")]
        public double StemStage2Wt
        {
            get { return dmStem2; }
        }

        [Description("Dry matter weight of stems at stage 3 (mature)")]
        [Units("kgDM/ha")]
        public double StemStage3Wt
        {
            get { return dmStem3; }
        }

        [Description("Dry matter weight of stems at stage 4 (dead)")]
        [Units("kgDM/ha")]
        public double StemStage4Wt
        {
            get { return dmStem4; }
        }

        [Description("Dry matter weight of stolons at stage 1 (young)")]
        [Units("kgDM/ha")]
        public double StolonStage1Wt
        {
            get { return dmStolon1; }
        }

        [Description("Dry matter weight of stolons at stage 2 (developing)")]
        [Units("kgDM/ha")]
        public double StolonStage2Wt
        {
            get { return dmStolon2; }
        }

        [Description("Dry matter weight of stolons at stage 3 (mature)")]
        [Units("kgDM/ha")]
        public double StolonStage3Wt
        {
            get { return dmStolon3; }
        }

        #endregion

        #region - C and DM flows  ------------------------------------------------------------------------------------------

        [Description("Potential C assimilation, corrected for extreme temperatures")]
        [Units("kgC/ha")]
        public double PotCarbonAssimilation
        {
            get { return Pgross; }
        }

        [Description("Loss of C via respiration")]
        [Units("kgC/ha")]
        public double CarbonLossRespiration
        {
            get { return Resp_m; }
        }

        [Description("C remobilised from senescent tissue")]
        [Units("kgC/ha")]
        public double CarbonRemobilised
        {
            get { return CRemobilised; }
        }

        [Description("Gross potential growth rate (potential C assimilation)")]
        [Units("kgDM/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return Pgross / CinDM; }
        }

        [Description("Respiration rate (DM lost via respiration)")]
        [Units("kgDM/ha")]
        public double RespirationWt
        {
            get { return Resp_m / CinDM; }
        }

        [Description("C remobilisation (DM remobilised from old tissue to new growth)")]
        [Units("kgDM/ha")]
        public double RemobilisationWt
        {
            get { return CRemobilised / CinDM; }
        }

        [Description("Net potential growth rate")]
        [Units("kgDM/ha")]
        public double NetPotentialGrowthWt
        {
            get { return dGrowthPot; }
        }

        [Description("Potential growth rate after water stress")]
        [Units("kgDM/ha")]
        public double PotGrowthWt_Wstress
        {
            get { return dGrowthW; }
        }

        [Description("Actual growth rate, after nutrient stress")]
        [Units("kgDM/ha")]
        public double ActualGrowthWt
        {
            get { return dGrowth; }
        }

        [Description("Effective growth rate, after turnover")]
        [Units("kgDM/ha")]
        public double EffectiveGrowthWt
        {
            get { return dGrowthEff; }
        }

        [Description("Effective herbage growth rate, above ground")]
        [Units("kgDM/ha")]
        public double HerbageGrowthWt
        {
            get { return dGrowthShoot; }
        }

        [Description("Effective root growth rate")]
        [Units("kgDM/ha")]
        public double RootGrowthWt
        {
            get { return dGrowthRoot; }
        }

        [Description("Litter amount deposited onto soil surface")]
        [Units("kgDM/ha")]
        public double LitterWt
        {
            get { return dLitter; }
        }

        [Description("Amount of senesced roots added to soil FOM")]
        [Units("kgDM/ha")]
        public double RootSenescedWt
        {
            get { return dRootSen; }
        }

        [Description("Gross primary productivity")]
        [Units("kgDM/ha")]
        public double GPP
        {
            get { return Pgross / CinDM; }
        }

        [Description("Net primary productivity")]
        [Units("kgDM/ha")]
        public double NPP
        {
            get { return (Pgross * growthEfficiency + Resp_m) / CinDM; }
        }

        [Description("Net above-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NAPP
        {
            get { return (Pgross * fShoot * growthEfficiency + Resp_m) / CinDM; }
        }

        #endregion

        #region - N amounts  -----------------------------------------------------------------------------------------------

        [Description("Total plant N amount")]
        [Units("kgN/ha")]
        public double TotalN
        {
            get { return Nshoot + Nroot; }
        }

        [Description("N amount of plant parts above ground")]
        [Units("kgN/ha")]
        public double AboveGroundN
        {
            get { return Nshoot; }
        }

        [Description("N amount of alive plant parts above ground")]
        [Units("kgN/ha")]
        public double AboveGroundLiveN
        {
            get { return Ngreen; }
        }

        [Description("N amount of dead plant parts above ground")]
        [Units("kgN/ha")]
        public double AboveGroundDeadN
        {
            get { return Ndead; }
        }

        [Description("N amount of standing herbage")]
        [Units("kgN/ha")]
        public double StandingN
        {
            get { return Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4 + Nstem1 + Nstem2 + Nstem3 + Nstem4; }
        }

        [Description("N amount of alive standing herbage")]
        [Units("kgN/ha")]
        public double StandingLiveN
        {
            get { return Nleaf1 + Nleaf2 + Nleaf3 + Nstem1 + Nstem2 + Nstem3; }
        }

        [Description("N amount of dead standing herbage")]
        [Units("kgN/ha")]
        public double StandingDeadN
        {
            get { return Nleaf4 + Nstem4; }
        }

        [Description("N amount of plant parts below ground")]
        [Units("kgN/ha")]
        public double BelowGroundN
        {
            get { return Nroot; }
        }

        [Description("N amount in the plant's leaves")]
        [Units("kgN/ha")]
        public double LeafN
        {
            get { return Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4; }
        }

        [Description("N amount in the plant's stems")]
        [Units("kgN/ha")]
        public double StemN
        {
            get { return Nstem1 + Nstem2 + Nstem3 + Nstem4; }
        }

        [Description("N amount in the plant's stolons")]
        [Units("kgN/ha")]
        public double StolonN
        {
            get { return Nstolon1 + Nstolon2 + Nstolon3; }
        }

        [Description("N amount in the plant's roots")]
        [Units("kgN/ha")]
        public double RootN
        {
            get { return Nroot; }
        }

        [Description("N amount in alive leaves")]
        [Units("kgN/ha")]
        public double LeafGreenN
        {
            get { return Nleaf1 + Nleaf2 + Nleaf3; }
        }

        [Description("N amount in dead leaves")]
        [Units("kgN/ha")]
        public double LeafDeadN
        {
            get { return Nleaf4; }
        }

        [Description("N amount in alive stems")]
        [Units("kgN/ha")]
        public double StemGreenN
        {
            get { return Nstem1 + Nstem2 + Nstem3; }
        }

        [Description("N amount in dead sytems")]
        [Units("kgN/ha")]
        public double StemDeadN
        {
            get { return Nstem4; }
        }

        [Description("N amount in leaves at stage 1 (young)")]
        [Units("kgN/ha")]
        public double LeafStage1N
        {
            get { return Nleaf1; }
        }

        [Description("N amount in leaves at stage 2 (developing)")]
        [Units("kgN/ha")]
        public double LeafStage2N
        {
            get { return Nleaf2; }
        }

        [Description("N amount in leaves at stage 3 (mature)")]
        [Units("kgN/ha")]
        public double LeafStage3N
        {
            get { return Nleaf3; }
        }

        [Description("N amount in leaves at stage 4 (dead)")]
        [Units("kgN/ha")]
        public double LeafStage4N
        {
            get { return Nleaf4; }
        }

        [Description("N amount in stems at stage 1 (young)")]
        [Units("kgN/ha")]
        public double StemStage1N
        {
            get { return Nstem1; }
        }

        [Description("N amount in stems at stage 2 (developing)")]
        [Units("kgN/ha")]
        public double StemStage2N
        {
            get { return Nstem2; }
        }

        [Description("N amount in stems at stage 3 (mature)")]
        [Units("kgN/ha")]
        public double StemStage3N
        {
            get { return Nstem3; }
        }

        [Description("N amount in stems at stage 4 (dead)")]
        [Units("kgN/ha")]
        public double StemStage4N
        {
            get { return Nstem4; }
        }

        [Description("N amount in stolons at stage 1 (young)")]
        [Units("kgN/ha")]
        public double StolonStage1N
        {
            get { return Nstolon1; }
        }

        [Description("N amount in stolons at stage 2 (developing)")]
        [Units("kgN/ha")]
        public double StolonStage2N
        {
            get { return Nstolon2; }
        }

        [Description("N amount in stolons at stage 3 (mature)")]
        [Units("kgN/ha")]
        public double StolonStage3N
        {
            get { return Nstolon3; }
        }

        #endregion

        #region - N concentrations  ----------------------------------------------------------------------------------------

        [Description("Average N concentration in standing plant parts")]
        [Units("kgN/kgDM")]
        public double StandingNConc
        {
            get { return StandingN / StandingWt; }
        }

        [Description("Average N concentration in leaves")]
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

        [Description("N concentration of leaves at stage 1 (young)")]
        [Units("kgN/kgDM")]
        public double LeafStage1NConc
        {
            get { return Nleaf1 / dmLeaf1; }
        }

        [Description("N concentration of leaves at stage 2 (developing)")]
        [Units("kgN/kgDM")]
        public double LeafStage2NConc
        {
            get { return Nleaf2 / dmLeaf2; }
        }

        [Description("N concentration of leaves at stage 3 (mature)")]
        [Units("kgN/kgDM")]
        public double LeafStage3NConc
        {
            get { return Nleaf3 / dmLeaf3; }
        }

        [Description("N concentration of leaves at stage 4 (dead)")]
        [Units("kgN/kgDM")]
        public double LeafStage4NConc
        {
            get { return Nleaf4 / dmLeaf4; }
        }

        [Description("N concentration of stems at stage 1 (young)")]
        [Units("kgN/kgDM")]
        public double StemStage1NConc
        {
            get { return Nstem1 / dmStem1; }
        }

        [Description("N concentration of stems at stage 2 (developing)")]
        [Units("kgN/kgDM")]
        public double StemStage2NConc
        {
            get { return Nstem2 / dmStem2; }
        }

        [Description("N concentration of stems at stage 3 (mature)")]
        [Units("kgN/kgDM")]
        public double StemStage3NConc
        {
            get { return Nstem3 / dmStem3; }
        }

        [Description("N concentration of stems at stage 4 (dead)")]
        [Units("kgN/kgDM")]
        public double StemStage4NConc
        {
            get { return Nstem4 / dmStem4; }
        }

        [Description("N concentration of stolons at stage 1 (young)")]
        [Units("kgN/kgDM")]
        public double StolonStage1NConc
        {
            get { return Nstolon1 / dmStolon1; }
        }

        [Description("N concentration of stolons at stage 2 (developing)")]
        [Units("kgN/kgDM")]
        public double StolonStage2NConc
        {
            get { return Nstolon2 / dmStolon2; }
        }

        [Description("N concentration of stolons at stage 3 (mature)")]
        [Units("kgN/kgDM")]
        public double StolonStage3NConc
        {
            get { return Nstolon3 / dmStolon3; }
        }

        [Description("Nitrogen concentration in new growth")]
        [Units("kgN/kgDM")]
        public double GrowthNconc
        {
            get
            {
                if (dGrowth > 0)
                    return newGrowthN / dGrowth;
                else
                    return 0.0;
            }
        }

        #endregion

        #region - N flows  -------------------------------------------------------------------------------------------------

        [Description("Amount of N remobilised from senesced material")]
        [Units("kgN/ha")]
        public double RemobilisedN
        {
            get { return Nremob2NewGrowth; }
        }

        [Description("Amount of N remobilisable from senesced material")]
        [Units("kgN/ha")]
        public double RemobilisableN
        {
            get { return NRemobilised; }
        }

        [Description("Amount of luxury N remobilised")]
        [Units("kgN/ha")]
        public double RemobilisedLuxuryN
        {
            get { return NFastRemob2 + NFastRemob3; }
        }

        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kgN/ha")]
        public double RemobilisableLuxuryN
        {
            get { return NLuxury2 + NLuxury3; }
        }

        [Description("Amount of luxury N potentially remobilisable from tissue 2")]
        [Units("kgN/ha")]
        public double RemobLuxuryN2
        {
            get { return NLuxury2; }
        }

        [Description("Amount of luxury N potentially remobilisable from tissue 3")]
        [Units("kgN/ha")]
        public double RemobLuxuryN3
        {
            get { return NLuxury3; }
        }

        [Description("Amount of atmospheric N fixed")]
        [Units("kgN/ha")]
        public double FixedN
        {
            get { return Nfixation; }
            set { Nfixation = value; }
        }

        [Description("Amount of N required with luxury uptake")]
        [Units("kgN/ha")]
        public double RequiredNLuxury
        {
            get { return NdemandLux; }
        }

        [Description("Amount of N required for optimum growth")]
        [Units("kgN/ha")]
        public double RequiredNOptimum
        {
            get { return NdemandOpt; }
        }

        [Description("Amount of N demanded from soil")]
        [Units("kgN/ha")]
        public double DemandN
        {
            get { return soilNdemand; }
        }

        [Description("Amount of N available in the soil")]
        [Units("kgN/ha")]
        public double[] SoilAvailableN
        {
            get { return mySoilAvailableN; }
        }

        [Description("Amount of N uptake")]
        [Units("kgN/ha")]
        public double[] UptakeN
        {
            get { return mySoilNUptake; }
        }

        [Description("Amount of N deposited as litter onto soil surface")]
        [Units("kgN/ha")]
        public double LitterN
        {
            get { return dNLitter; }
        }

        [Description("Amount of N from senesced roots added to soil FOM")]
        [Units("kgN/ha")]
        public double SenescedRootN
        {
            get { return dNrootSen; }
        }

        [Description("Amount of N in new growth")]
        [Units("kgN/ha")]
        public double ActualGrowthN
        {
            get { return newGrowthN; }
        }

        #endregion

        #region - Turnover rates and DM allocation  ------------------------------------------------------------------------

        [Description("Turnover rate for live DM (leaves and stem)")]
        [Units("0-1")]
        public double LiveDMTurnoverRate
        {
            get { return gama; }
        }

        [Description("Turnover rate for dead DM (leaves and stem)")]
        [Units("0-1")]
        public double DeadDMTurnoverRate
        {
            get { return gamaD; }
        }

        [Description("DM turnover rate for stolons")]
        [Units("0-1")]
        public double StolonDMTurnoverRate
        {
            get { return gamaS; }
        }

        [Description("DM turnover rate for roots")]
        [Units("0-1")]
        public double RootDMTurnoverRate
        {
            get { return gamaR; }
        }

        [Description("Fraction of DM allocated to Shoot")]
        [Units("0-1")]
        public double ShootDMAllocation
        {
            get { return fShoot; }
        }

        [Description("Fraction of DM allocated to roots")]
        [Units("0-1")]
        public double RootDMAllocation
        {
            get { return 1 - fShoot; }
        }

        #endregion

        #region - LAI and cover  -------------------------------------------------------------------------------------------

        [Description("Total leaf area index")]
        [Units("m^2/m^2")]
        public double TotalLAI
        {
            get { return greenLAI + deadLAI; }
        }

        [Description("Leaf area index of green leaves")]
        [Units("m^2/m^2")]
        public double GreenLAI
        {
            get { return greenLAI; }
        }

        [Description("Leaf area index of dead leaves")]
        [Units("m^2/m^2")]
        public double DeadLAI
        {
            get { return deadLAI; }
        }

        [Description("Irridance on the top of canopy")]
        [Units("W.m^2/m^2")]
        public double IrradianceTopCanopy
        {
            get { return IL; }
        }

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

        [Description("Plants average height")]                 //needed by micromet
        [Units("mm")]
        public double Height
        {
            get { return Math.Max(20.0, HeightFromMass.Value(StandingLiveWt)); }  // minimum = 20mm
        }
        #endregion

        #region - Root depth and distribution  -----------------------------------------------------------------------------

        [Description("Depth of roots")]
        [Units("mm")]
        public double RootDepth
        {
            get { return myRootDepth; }
        }

        [Description("Layer at bottom of root zone")]
        [Units("mm")]
        public double RootFrontier
        {
            get { return myRootFrontier; }
        }

        [Description("Fraction of root dry matter for each soil layer")]
        [Units("0-1")]
        public double[] RootWtFraction
        {
            get { return rootFraction; }
        }

        [Description("Root length density")]
        [Units("mm/mm^3")]
        public double[] RLV
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

        [Description("Soil water lower limit for plant uptake")]
        [Units("mm^3/mm^3")]
        public double[] SpeciesLL
        {
            get
            {
                SoilCrop soilInfo = (SoilCrop)Soil.Crop(Name);
                return soilInfo.LL;
            }
        }

        [Description("Plant water demand")]
        [Units("mm")]
        public double WaterDemand
        {
            get { return myWaterDemand; }
        }

        [Description("Plant availabe water")]
        [Units("mm")]
        public double[] SoilAvailableWater
        {
            get { return mySoilAvailableWater; }
        }

        [Description("Plant water demand")]
        [Units("mm")]
        public double[] WaterUptake
        {
            get { return mySoilWaterUptake; }
        }

        #endregion

        #region - Growth limiting factors  ---------------------------------------------------------------------------------

        [Description("Growth limiting factor due to nitrogen")]
        [Units("0-1")]
        public double GLFN
        {
            get { return glfn; }
        }

        [Description("Plant growth limiting factor due to plant N concentration")]
        [Units("0-1")]
        public double GLFnConcentration
        {
            get { return NcFactor; }
        }

        [Description("Growth limiting factor due to temperature")]
        [Units("0-1")]
        public double GLFTemp
        {
            get { return TemperatureLimitingFactor(Tmean); }
        }

        [Description("Growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GLFWater
        {
            get { return glfWater; }
        }

        [Description("Generic growth limiting factor")]
        [Units("0-1")]
        public double GLFGeneric
        {
            get { return glfGeneric; }
        }

        [Description("Effect of vapour pressure on growth (used by micromet)")]
        [Units("0-1")]
        public double FVPD
        {
            get { return FVPDFunction.Value(VPD()); }
        }
        #endregion

        #region - Harvest variables  ---------------------------------------------------------------------------------------

        [Description("Amount of dry matter harvestable (leaf+stem)")]
        [Units("kgDM/ha")]
        public double HarvestableWt
        {
            get { return Math.Max(0.0, StandingLiveWt - dmGreenmin) + Math.Max(0.0, StandingDeadWt - dmDeadmin); }
        }

        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kgDM/ha")]
        public double HarvestedWt
        {
            get { return dmDefoliated; }
        }

        [Description("Fraction harvested")]
        [Units("0-1")]
        public double HarvestedFraction
        {
            get { return fractionHarvest; }
            set { fractionHarvest = value; }
        }

        [Description("Amount of plant nitrogen removed by harvest")]
        [Units("kgN/ha")]
        public double HarvestedN
        {
            get { return Ndefoliated; }
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
            get { return digestHerbage; }
        }

        [Description("Average digestibility of harvested meterial")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get { return digestDefoliated; }
        }

        [Description("Average ME of herbage")]
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get { return 16 * digestHerbage * StandingWt; }
        }

        [Description("Average ME of harvested material")]
        [Units("(MJ/ha)")]
        public double HarvestedME
        {
            get { return 16 * digestDefoliated * HarvestedWt; }
        }

        #endregion

        #endregion

        #region Private variables  -----------------------------------------------------------------------------------------

        private SpeciesState prevState;				//for remember the status of previous day

        //private bool HaveInitialised = false;

        private double intRadnFrac;	 //fraction of Radn intercepted by this species = intRadn/Radn

        // defining the plant type
        private string micrometType = "grass";
        internal bool isAnnual = false;      //Species type (1=annual,0=perennial)
        internal bool isLegume;		//Legume (0=no,1=yes)
        internal string photoPath;	   //Phtosynthesis pathways: 3=C3, 4=C4; //no consideration for CAM(=3)

        // Parameters for annual species
        public int dayEmerg = 0; 		//Earlist day of emergence (for annuals only)
        public int monEmerg = 0;		//Earlist month of emergence (for annuals only)
        public int dayAnth = 0;			//Earlist day of anthesis (for annuals only)
        public int monAnth = 0;			//Earlist month of anthesis (for annuals only)
        public int daysToMature = 0;	//Days from anthesis to maturity (for annuals only)
        private int daysEmgToAnth = 0;   //Days from emergence to Anthesis (calculated, annual only)
        private int phenoStage = 1;  //pheno stages: 0 - pre_emergence, 1 - vegetative, 2 - reproductive
        private double phenoFactor = 1;
        private int daysfromEmergence = 0;   //days
        private int daysfromAnthesis = 0;	//days
        private bool bSown = false;
        private double DDSfromSowing = 0;
        private double dRootDepth = 50;		//Daily root growth (mm)
        private double maxRootDepth = 900;	//Maximum root depth (mm)

        private double maxSRratio;
        private double Tmean;

        private int nLayers = 0;
        private int myRootFrontier = 1;
        
        private double myRootDepth = 0.0;
        private double myWaterDemand = 0.0;
        private double myNitrogenDemand = 0.0;
        private double myGreenLAI = 0.0;
        private double myDeadLAI = 0.0;
        private double myTotalLAI = 0.0;
        private double myHeight = 0.0;
        private double myGreenCover = 0.0;
        private double myDeadCover = 0.0;
        private double myTotalCover = 0.0;

        private double[] mySoilAvailableWater;
        internal double[] mySoilWaterUptake;

        private double[] mySoilAvailableN;
        internal double[] mySoilNH4available;
        internal double[] mySoilNO3available;
        internal double[] mySoilNUptake;

        //private double InterceptedRadn;
        //private double LightExtCoeff;

        private bool isAlive = true;


        // N concentration thresholds for various tissues (are set relative to leaf N)
        private double NcStemOpt;	//sheath and stem
        private double NcStolonOpt;	//stolon
        private double NcRootOpt;	//root
        private double NcStemMax;	//sheath and stem
        private double NcStolonMax;	//stolon
        private double NcRootMax;	//root
        private double NcStemMin;
        private double NcStolonMin;
        private double NcRootMin;

        //N amount in various plant parts and tissue pools (kg N/ha)
        private double Ntotal;	//above-ground total N (kg/ha)
        private double Nshoot;	//above-ground total N (kg/ha)
        private double Ngreen;	//live N
        private double Ndead;	//in standing dead (kg/ha)
        private double Nleaf;	//leaf N
        private double Nstem;	//stem N
        private double NleafGreen;
        private double NstemGreen;
        private double Nstolon;	//stolon

        private double Nleaf1;	//leaf 1 (kg/ha)
        private double Nleaf2;	//leaf 2 (kg/ha)
        private double Nleaf3;	//leaf 3 (kg/ha)
        private double Nleaf4;	//leaf dead (kg/ha)
        private double Nstem1;	//sheath and stem 1 (kg/ha)
        private double Nstem2;	//sheath and stem 2 (kg/ha)
        private double Nstem3;	//sheath and stem 3 (kg/ha)
        private double Nstem4;	//sheath and stem dead (kg/ha)
        private double Nstolon1;	//stolon 1 (kg/ha)
        private double Nstolon2;	//stolon 2 (kg/ha)
        private double Nstolon3;	//stolon 3 (kg/ha)
        private double Nroot;	    //root (kg/ha)
        private double Nlitter;	//Litter pool (kg/ha)

        //private double NcLeaf1 = 0;
        //private double NcLeaf2 = 0;
        //private double NcLeaf3 = 0;
        //private double NcLeaf4 = 0;
        //private double NcStem1 = 0;
        //private double NcStem2 = 0;
        //private double NcStem3 = 0;
        //private double NcStem4 = 0;
        //private double NcStolon1 = 0;
        //private double NcStolon2 = 0;
        //private double NcStolon3 = 0;
        //private double NcRoot = 0;
        //private double NcLitter = 0;

        //DM in various plant parts and tissue pools (kg DM/ha)
        private double dmTotal;	  //=dmGreen + dmDead
        private double dmShoot;
        private double dmGreen;
        private double dmDead;
        private double dmLeaf;
        private double dmStem;
        private double dmStolon;
        private double dmRoot;

        private double dmGreenmin;
        private double dmDeadmin;

        private double dmLeaf1;	//leaf 1 (kg/ha)
        private double dmLeaf2;	//leaf 2 (kg/ha)
        private double dmLeaf3;	//leaf 3 (kg/ha)
        private double dmLeaf4;	//leaf dead (kg/ha)
        private double dmStem1;	//sheath and stem 1 (kg/ha)
        private double dmStem2;	//sheath and stem 2 (kg/ha)
        private double dmStem3;	//sheath and stem 3 (kg/ha)
        private double dmStem4;	//sheath and stem dead (kg/ha)
        private double dmStolon1;	//stolon 1 (kg/ha)
        private double dmStolon2;	//stolon 2 (kg/ha)
        private double dmStolon3;	//stolon 3 (kg/ha)
        private double dmLitter;	//Litter pool (kg/ha)

        private double NRemobilised = 0;	   //N remobiliesd N during senesing
        private double CRemobilised = 0;
        private double Nremob2NewGrowth = 0;

        private double NdemandLux;	  //N demand for new growth
        private double NdemandOpt;
        private double Nfixation;		 //N fixed by legumes
        private double soilNdemand;  //N demand from soil (=Ndemand-Nremob-Nfixed)
        internal double soilNuptake;  //N uptake of the day
        private double newGrowthN = 0;	//N plant-soil

        private double NLuxury2;		       // luxury N (above Nopt) potentially remobilisable
        private double NLuxury3;		       // luxury N (above Nopt)potentially remobilisable
        private double NFastRemob2 = 0.0;   // amount of luxury N remobilised from tissue 2
        private double NFastRemob3 = 0.0;   // amount of luxury N remobilised from tissue 3

        // harvest
        private double dmDefoliated;
        private double Ndefoliated;
        private double digestHerbage;
        private double digestDefoliated;
        // LAI
        private double greenLAI;
        private double deadLAI;

        //growth limiting factors
        private double glfWater;  //from water stress
        // private double glfTemp;   //from temperature
        private double glfn;	  //from N deficit
        private double NcFactor;

        // root 
        private double[] rootFraction;

        //calculated, species delta
        private double dGrowthPot;	//daily growth potential
        private double dGrowthW;	  //daily growth with water-deficit incorporated
        private double dGrowth;	   //daily growth
        private double dGrowthRoot;   //daily root growth
        private double dGrowthShoot; //daily growth shoot
        private double dGrowthEff;      // effective or net growth

        private double dLitter;	   //daily litter production
        private double dNLitter;	  //N in dLitter
        private double dRootSen;	  //daily root sennesce
        private double dNrootSen;	 //N in dRootSen

        private double fShoot;		 //actual fraction of dGrowth to shoot
        private double fLeaf;			//Fixed growth partition to leaf (0-1)
        private double fStolon;			//Fixed growth partition to stolon (0-1)

        // DM transfer coefficients (turnover)
        public double gama = 0.0;	// from tissue 1 to 2, then to 3, then to 4
        public double gamaS = 0.0;	// for stolons
        public double gamaD = 0.0;	// from dead (tissue 4) to litter
        public double gamaR = 0.0;	// for roots (to dead/FOM)

        private double IL;
        private double Pgross;
        private double Resp_m;

        private double highTempEffect = 1;  //fraction of growth rate due to high temp. effect
        private double lowTempEffect = 1;   //fraction of growth rate due to low temp. effect
        private double accumT4Heat = 0;          //accumulated temperature from previous heat strike = sum of '25-MeanT'(>0)
        private double accumT4Cold = 0;       //accumulated temperature from previous cold strike = sum of MeanT (>0)

        private double fractionHarvest;

        #endregion

        #region Constants  -------------------------------------------------------------------------------------------------

        const double CinDM = 0.4;			//C to DM convertion
        const double N2Protein = 6.25;	  //this is for plants... (higher amino acids)
        const double CNratioProtein = 3.5;	 //C:N in remobilised material
        const double CNratioCellWall = 100.0;

        #endregion

        #region Initialisation methods  ------------------------------------------------------------------------------------

        public PastureSpecies()
        {}

        /// <summary>
        /// Performs the initialisation procedures for this species (set DM, N, LAI, etc)
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // get the number of layers in the soil profile
            nLayers = Soil.Thickness.Length;

            // set initial plant state
            SetInitialState();

            // initialise the class which will hold info for yesterday's values
            prevState = new SpeciesState();

            // initialise arrays for water and N uptake
            mySoilAvailableWater = new double[nLayers];
            mySoilWaterUptake = new double[nLayers];

            // tell other modules about the existence of this species
            DoNewCropEvent();
            // tell other modules about this species' canopy
            DoNewCanopyEvent();
        }

        /// <summary>
        /// Set the initial parameters for this plant, including DM and N content of various pools plus plant height and root depth
        /// </summary>
        private void SetInitialState()
        {
            // 1. Initialise DM of various tissue pools, user should supply initial values for shoot and root
            dmTotal = dmShoot + dmRoot;
            dmLitter = 0.0;

            dmLeaf1 = iniDMFraction[0] * dmShoot;
            dmLeaf2 = iniDMFraction[1] * dmShoot;
            dmLeaf3 = iniDMFraction[2] * dmShoot;
            dmLeaf4 = iniDMFraction[3] * dmShoot;
            dmStem1 = iniDMFraction[4] * dmShoot;
            dmStem2 = iniDMFraction[5] * dmShoot;
            dmStem3 = iniDMFraction[6] * dmShoot;
            dmStem4 = iniDMFraction[7] * dmShoot;
            dmStolon1 = iniDMFraction[8] * dmShoot;
            dmStolon2 = iniDMFraction[9] * dmShoot;
            dmStolon3 = iniDMFraction[10] * dmShoot;

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
            Nlitter = dmLitter * leafNmin;

            // 4. Root depth and distribution
            myRootDepth = iniRootDepth;
            double cumDepth = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                cumDepth += Soil.Thickness[layer];
                if (cumDepth >= myRootDepth)
                {
                    myRootFrontier = layer;
                    layer = Soil.SoilWater.dlayer.Length;
                }
            }
            rootFraction = RootProfileDistribution();

            // 5. compute/set other variables
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// EventHandler - preparation befor the main process
        /// </summary>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // 1. Zero out several variables
            RefreshVariables();

            // mean air temperature for today
            Tmean = (MetData.MaxT + MetData.MinT) * 0.5;
        }

        /// <summary>
        /// Performs the plant growth calculations
        /// </summary>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (useSeparateCanopy == "yes")
            {
                if (isAlive)
                {
                    // stores the current state for this species
                    SaveState();

                    // step 01 - preparation and potential growth
                    CalcPotentialGrowth();

                    // Send information about this species canopy, MicroClimate will compute intercepted radiation and water demand
                    DoNewCanopyEvent();

                    // Water demand, supply, and uptake
                    if (myWaterUptakeSource == "calc")
                    {
                        DoWaterCalculations();
                        glfWater = WaterLimitingFactor() * WaterLoggingFactor();   // in reality only one of these is smaller than one
                    }
                    //else if myWaterUptakeSource == "AgPasture"
                    //      myWaterDemand should have been supplied by MicroClimate (supplied as PotentialEP)
                    //      water supply is hold by AgPasture only
                    //      myWaterUptake should have been computed by AgPasture (set directly)
                    //      glfWater is computed and set by AgPasture
                    //else
                    //      water uptake be calculated by other modules (e.g. SWIM) and supplied as
                    //  Note: when AgPasture is doing the water uptake, it can do it using its own calculations or other module's...

                    // step 02 - Potential growth after water limitations
                    CalcGrowthWithWaterLimitations();

                    // Nitrogen demand, supply, and uptake
                    if (myNitrogenUptakeSource == "calc")
                    {
                        DoNitrogenCalculations();
                        glfn = Math.Min(1.0, Math.Max(0.0, newGrowthN / NdemandOpt));
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
                    //else
                    //   N uptake is computed by another module (not implemented yet)

                    // step 03 - Actual growth after nutrient limitations, but before senescence
                    CalcActualGrowthAndPartition();

                    // step 04 - Effective growth after all limitations and senescence
                    CalcTurnoverAndEffectiveGrowth();
                }
            }
            //else
        }

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
                CRemobilised = 0.0;
                dGrowthPot = 0.0;
            }
            else
            {
                // Gross potential growth (kgC/ha/day)
                Pgross = DailyGrossPotentialGrowth();

                // Respiration (kgC/ha/day)
                Resp_m = DailyMaintenanceRespiration();

                // Remobilisation (kgC/ha/day) (got from previous day)

                // Net potential growth (kgDM/ha/day)
                dGrowthPot = DailyNetPotentialGrowth();
            }
        }

        internal void CalcGrowthWithWaterLimitations()
        {
            // Potential growth after water limitations
            dGrowthW = dGrowthPot * Math.Pow(glfWater, waterStressFactor);

            // allocation of todays growth
            fShoot = ToShootFraction();
            //   FL = UpdatefLeaf();
        }

        internal void CalcActualGrowthAndPartition()
        {
            // Actual daily growth
            dGrowth = DailyActualGrowth();

            // Partition growth into various tissues
            PartitionNewGrowth();
        }

        internal void CalcTurnoverAndEffectiveGrowth()
        {
            // Compute tissue turnover and remobilisation (C and N)
            TissueTurnoverAndRemobilisation();

            // Effective, or net, growth
            dGrowthEff = dGrowthShoot + dGrowthRoot;

            // Update aggregate variables and digetibility
            updateAggregated();

            digestHerbage = calcDigestibility();
        }

        #region - Handling and auxilary processes  -------------------------------------------------------------------------

        /// <summary>
        /// Refresh the value of several variables
        /// </summary>
        internal void RefreshVariables()
        {
            dmDefoliated = 0.0;
            Ndefoliated = 0.0;
            digestHerbage = 0.0;
            digestDefoliated = 0.0;

            NdemandOpt = 0.0;
            NdemandLux = 0.0;
            Nremob2NewGrowth = 0.0;
            Nfixation = 0.0;
            NcFactor = 0.0;
            newGrowthN = 0.0;
            soilNdemand = 0.0;
            soilNuptake = 0.0;
            dLitter = 0.0;
            dNLitter = 0.0;
            dRootSen = 0.0;
            dNrootSen = 0.0;
            Resp_m = 0.0;

            glfn = 1.0;
            //glfTemp = 1.0;
            glfWater = 1.0;
            phenoFactor = 1.0;
            intRadnFrac = 0.0;

            Pgross = 0.0;           // potential daily C assimilation (maximum growth)
            dGrowthPot = 0.0;	    // potential daily growth (DM after Pgross is corrected for ...)
            dGrowthW = 0.0;		    // potential daily growth after water limitation has been considered
            dGrowth = 0.0;		    // actual daily growth (after considering nutrient, and generic, limitations)
            dGrowthEff = 0.0;       // effective, or net, daily growth
            dGrowthShoot = 0.0;     // daily shoot growth, effective
            dGrowthRoot = 0.0;	    // daily root growth, effective
        }

        /// <summary>
        /// Stores the current state for this species
        /// </summary>
        internal void SaveState()
        {
            prevState.dmLeaf1 = dmLeaf;
            prevState.dmLeaf1 = dmLeaf1;
            prevState.dmLeaf2 = dmLeaf2;
            prevState.dmLeaf3 = dmLeaf3;
            prevState.dmLeaf4 = dmLeaf4;

            prevState.dmStem1 = dmStem;
            prevState.dmStem1 = dmStem1;
            prevState.dmStem2 = dmStem2;
            prevState.dmStem3 = dmStem3;
            prevState.dmStem4 = dmStem4;

            prevState.dmStolon1 = dmStolon;
            prevState.dmStolon1 = dmStolon1;
            prevState.dmStolon2 = dmStolon2;
            prevState.dmStolon3 = dmStolon3;

            prevState.dmLitter = dmLitter;
            prevState.dmRoot = dmRoot;

            prevState.dmDefoliated = dmDefoliated;

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
            prevState.Nlitter = Nlitter;
        }

        /// <summary>
        /// Computes the value of auxiliary variables (aggregates for DM and N content)
        /// </summary>
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
                throw new Exception("Loss of mass balance of shoot plant dry matter");

            // auxiliary N variables
            Nleaf = Nleaf1 + Nleaf2 + Nleaf3 + Nleaf4;
            Nstem = Nstem1 + Nstem2 + Nstem3 + Nstem4;
            Nstolon = Nstolon1 + Nstolon2 + Nstolon3;
            Nshoot = Nleaf + Nstem + Nstolon;

            Ngreen = Nleaf1 + Nleaf2 + Nleaf3
                   + Nstem1 + Nstem2 + Nstem3
                   + Nstolon1 + Nstolon2 + Nstolon3;
            Ndead = Nleaf4 + Nstem4;
            Ntotal = Nshoot + Nroot;

            if (Math.Abs((Ngreen + Ndead) - Nshoot) > 0.0001)
                throw new Exception("Loss of mass balance of shoot plant N");
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

        /// <summary>
        /// Reduction factor for potential growth due to phenology of annual species
        /// </summary>
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

            greenLAI = (dmLeaf1 + dmLeaf2 + dmLeaf3 + dmStolon * 0.3) * specificLeafArea / 1000;  // converted from kg/ha to kg/m2
            // assuming stolon have 0.3*SLA

            // Recover of grasses after unfavoured conditions:
            //  Consider cover will be bigger for the same amount of DM when DM is low, i.e. stems become important for photosynthesis.
            //  This can be explained by:
            //  - higher light extinction coefficient - plant leaves will be more horizontal than in dense, tall swards
            //  - more parts will turn green for photosysntheses?
            //  - quick response of plant shoots to favoured conditions after release of stress
            if (!isLegume && dmGreen < 1000)
                greenLAI += ((dmStem1 + dmStem2 + dmStem3) * specificLeafArea / 1000) * Math.Sqrt((1000 - dmGreen) / 1000);

            deadLAI = dmLeaf4 * specificLeafArea / 1000;
        }

        /// <summary>
        /// Compute the average digestibility of aboveground plant material
        /// </summary>
        /// <returns>The digestibility of plant material (0-1)</returns>
        private double calcDigestibility()
        {
            if ((dmLeaf + dmStem) <= 0.0)
            {
                return 0.0;
            }

            // fraction of sugar (soluble carbohydrates)  - RCichota: this seem to ignore any stored reserves
            double fSugar = 0.5 * dGrowth / dmGreen;

            //Live
            double digestLive = 0.0;
            if (dmGreen > 0.0 & Ngreen > 0.0)
            {
                double CNlive = (dmGreen * CinDM) / Ngreen;                                //CN ratio of live shoots
                double fProteinLive = (CNratioCellWall / CNlive - (1 - fSugar)) / (CNratioCellWall / CNratioProtein - 1); //Fraction of protein in living shoots
                double fWallLive = 1 - fSugar - fProteinLive;                          //Fraction of cell wall in living shoots
                digestLive = fSugar + fProteinLive + digestibilityLive * fWallLive;
            }

            //Dead
            double digestDead = 0;
            if (dmDead > 0 && Ndead > 0)
            {
                double CNdead = (dmDead * CinDM) / Ndead;                       //CN ratio of standing dead;
                double fProteinDead = (CNratioCellWall / CNdead - 1) / (CNratioCellWall / CNratioProtein - 1); //Fraction of protein in standing dead
                double fWallDead = 1 - fProteinDead;                        //Fraction of cell wall in standing dead
                digestDead = fProteinDead + digestibilityDead * fWallDead;
            }

            double deadFrac = dmDead / (dmLeaf + dmStem);
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
                for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
                {
                    cumDepth += Soil.SoilWater.dlayer[layer];
                    if (cumDepth >= myRootDepth)
                    {
                        myRootFrontier = layer;
                        layer = Soil.SoilWater.dlayer.Length;
                    }
                }
            }
            // else:  both myRootDepth and myRootFrontier have been set at initialisation and do not change
        }

        /// <summary>
        /// Computes the plant's gross potential growth rate
        /// </summary>
        /// <returns>The potential amount of C assimilated via photosynthesis (kgC/ha)</returns>
        private double DailyGrossPotentialGrowth()
        {
            // 1. compute photosynthesis rate for leaf area

            // to be moved to parameter section
            // Photochemical, or photosynthetic, efficiency (mg CO2/J) - typically with small variance and little effect
            const double alfa = 0.01;
            // Photosynthesis curvature parameter (J/kg^2/s) - typically with small variance and little effect
            const double theta = 0.8;

            // Temp effects to Pmax
            double Tday = Tmean + 0.5 * (MetData.MaxT - Tmean);
            double efTmean = TemperatureLimitingFactor(Tmean);
            double efTday = TemperatureLimitingFactor(Tday);

            // CO2 effects on Pmax
            double efCO2 = PCO2Effects();

            // N effects on Pmax
            NcFactor = PmxNeffect();

            // Maximum photosynthetic rate (mg CO2/m^2 leaf/s)
            double Pmax_EarlyLateDay = maxPhotosynthesisRate * efTmean * efCO2 * NcFactor;
            double Pmax_MiddleDay = maxPhotosynthesisRate * efTday * efCO2 * NcFactor;

            double myDayLength = 3600 * MetData.DayLength;  //conversion of hour to seconds
            // Photosynthetically active radiation, PAR = 0.5*Radn, converted from MJ/m2 to J/2 (10^6)
            double myPAR = 0.5 * interceptedRadn * 1000000;

            // Irradiance, or radiation, on the canopy at the middle of the day (W/m^2)
            IL = (4 / 3) * myPAR * lightExtentionCoeff / myDayLength;

            double IL2 = IL / 2;					  //IL for early & late period of a day

            // Photosynthesis per leaf area for middle and 'shoulders' of the day (mg CO2/m^2 leaf/s)
            double Pl_MiddleDay = (0.5 / theta) * (alfa * IL + Pmax_MiddleDay
                         - Math.Sqrt((alfa * IL + Pmax_MiddleDay) * (alfa * IL + Pmax_MiddleDay) - 4 * theta * alfa * IL * Pmax_MiddleDay));
            double Pl_EarlyLateDay = (0.5 / theta) * (alfa * 0.5 * IL + Pmax_EarlyLateDay
                         - Math.Sqrt((alfa * 0.5 * IL + Pmax_EarlyLateDay) * (alfa * 0.5 * IL + Pmax_EarlyLateDay) - 4 * theta * alfa * 0.5 * IL * Pmax_EarlyLateDay));

            // Photosynthesis per leaf area for the day (mg CO2/m^2 leaf/day)
            double Pl_Daily = myDayLength * (Pl_MiddleDay + Pl_EarlyLateDay) / 2;

            // Photosynthesis for whole canopy, per ground area (mg CO2/m^2/day)
            double Pc_Daily = Pl_Daily * CalcPlantCover(greenLAI) / lightExtentionCoeff;

            //  Carbon assimilation per leaf area (g C/m^2/day)
            double CarbonAssim = Pl_Daily * 0.001 * (12 / 44);        // Convert to from mgCO2 to kgC

            // Base gross photosynthesis, converted to kg C/ha/day)
            double BaseGrossGrowth = CarbonAssim * 10000 / 1000;

            // Upscaling from 'per LAI' to 'per ground area'
            //double carbon_m2 = 0.000001 * CD2C * 0.5 * myDayLength * (Pl_MiddleDay + Pl_EarlyLateDay) * PlantCoverGreen * intRadnFrac / lightExtentionCoeff;
            //carbon_m2 *= 1;// coverRF;					   //coverRF == 1 when puting species together
            //Pgross = 10000 * carbon_m2;				 //10000: 'kg/m^2' =>'kg/ha'

            // Consider the extreme temperature effects (in practice only one temp stress factor is < 1)
            double ExtremeTemperatureFactor = HeatStress() * ColdStress();

            // Actual gross photosynthesis (gross potential growth - kg C/ha/day)
            return BaseGrossGrowth * ExtremeTemperatureFactor;
        }

        /// <summary>
        /// Computes the plant's loss of C due to respiration
        /// </summary>
        /// <returns>The ampount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyMaintenanceRespiration()
        {
            // Temperature effects on respiration
            double Teffect = 0;
            if (Tmean > growthTmin)
            {
                if (Tmean < growthTopt)
                    Teffect = TemperatureLimitingFactor(Tmean);
                else
                    Teffect = Math.Min(1.25, Tmean / growthTopt);		// Using growthTopt as reference temperature, and maximum of 1.25
            }

            // Total DM converted to C (kg/ha)
            double dmLive = (dmGreen + dmRoot) * CinDM;
            double dResp = dmLive * maintenanceRespirationCoef * Teffect * NcFactor;
            return Math.Max(0.0, dResp);
        }

        /// <summary>
        /// Compute the plant's net potential growth
        /// </summary>
        /// <returns>The net potential growth (kg DM/ha)</returns>
        private double DailyNetPotentialGrowth()
        {
            // Net potential growth (C assimilation) for the day (excluding respiration)
            double NetPotGrowth = 0.0;
            NetPotGrowth = growthEfficiency * Pgross + CRemobilised - Resp_m;
            NetPotGrowth = Math.Max(0.0, dGrowthPot);

            // Net daily potential growth (kg DM/ha)
            NetPotGrowth /= CinDM;

            // phenologically related reduction in growth of annual species (from IJ)
            if (isAnnual)
                NetPotGrowth *= annualSpeciesReduction();

            return NetPotGrowth;
        }

        /// <summary>
        /// Computes the plant's potential growth rate
        /// </summary>
        private double DailyActualGrowth()
        {
            // Adjust GLF due to N deficiency. Many plants (grasses) can grow more by reducing the N concentration
            //  in its tissues. This is represented here by reducing the effect of N deficiency using a power function,
            //  when exponent is 1.0, the reduction in growth is proportional to N deficiency; for many plants the value
            //  should be smaller than that. For grasses, the exponent is typically around 0.5.
            double glfNit = Math.Pow(glfn, dillutionCoefN);

            // The generic limitation factor is assumed to be equivalent to a nutrient deficiency, so it is considered here
            dGrowth = dGrowthW * Math.Min(glfNit, glfGeneric);

            return dGrowth;
        }

        /// <summary>
        /// Update DM and N amounts of all tissues accounting for the new growth
        /// </summary>
        private void PartitionNewGrowth()
        {
            // Leaf appearance rate, as modified by temp & water stress  -  Not really used, should it??
            //double effTemp = TemperatureLimitingFactor(Tmean);
            //double effWater = Math.Pow(glfWater, 0.33333);
            //double rateLeafGrowth = leafRate * effTemp * effWater;
            //rateLeafGrowth = Math.Max(0.0, Math.Min(1.0, rateLeafGrowth));

            if (dGrowth > 0.0)
            {
                // Fractions of new growth for each plant part (fShoot was calculated in DoPlantGrowth)
                double toLeaf = fShoot * fLeaf;
                double toStolon = fShoot * fStolon;
                double toStem = fShoot * (1.0 - fStolon - fLeaf);
                double toRoot = 1.0 - fShoot;

                // Checking mass balance
                double ToAll = toLeaf + toStolon + toStem + toRoot;
                if (Math.Abs(ToAll - 1.0) > 0.0001)
                    throw new Exception("  AgPasture - Mass balance lost on partition of new growth");

                // New growth is allocated to the first tissue pools
                dmLeaf1 += toLeaf * dGrowth;
                dmStolon1 += toStolon * dGrowth;
                dmStem1 += toStem * dGrowth;
                dmRoot += toRoot * dGrowth;
                dGrowthShoot = (toLeaf + toStolon + toStem) * dGrowth;
                dGrowthRoot = toRoot * dGrowth;

                // Partitioning N based on DM fractions and on max [N] in plant parts
                double Nsum = toLeaf * leafNmax + toStem * NcStemMax + toStolon * NcStolonMax + toRoot * NcRootMax;
                double toLeafN = toLeaf * leafNmax / Nsum;
                double toStolonN = toStolon * NcStolonMax / Nsum;
                double toStemN = toStem * NcStemMax / Nsum;
                double toRootN = toRoot * NcRootMax / Nsum;

                // Checking mass balance
                ToAll = toRootN + toLeafN + toStolonN + toStemN;
                if (Math.Abs(ToAll - 1.0) > 0.0001)
                    throw new Exception("  AgPasture - Mass balance lost on partition of new growth N");

                // Allocate N from new growth to the first tissue pools
                Nleaf1 += toLeafN * newGrowthN;
                Nstem1 += toStemN * newGrowthN;
                Nstolon1 += toStolonN * newGrowthN;
                Nroot += toRootN * newGrowthN;

                // Fraction of Nremob not used in new growht that is returned to dead tissue
                double leftoverNremob = NRemobilised * kappaNRemob4;
                if (leftoverNremob > 0.0)
                {
                    Nleaf4 += leftoverNremob * prevState.Nleaf4 / (prevState.Nleaf4 + prevState.Nstem4);
                    Nstem4 += leftoverNremob * prevState.Nstem4 / (prevState.Nleaf4 + prevState.Nstem4);
                }
                // Note: this is only valid for leaf and stems, the remaining (1-kappaNRemob4) and the amounts in roots
                //  and stolon is disposed (added to soil FOM or Surface OM via litter)

                // Check whether luxury N was remobilised during N balance
                if (NFastRemob2 + NFastRemob3 > 0.0)
                {
                    // If N was remobilised, update the N content in tissues accordingly
                    //  partition between parts is assumed proportional to N content
                    if (NFastRemob2 > 0.0)
                    {
                        Nsum = prevState.Nleaf2 + prevState.Nstem2 + prevState.Nstolon2;
                        Nleaf2 += NFastRemob2 * prevState.Nleaf2 / Nsum;
                        Nstem2 += NFastRemob2 * prevState.Nstem2 / Nsum;
                        Nstolon2 += NFastRemob2 * prevState.Nstolon2 / Nsum;
                    }
                    if (NFastRemob3 > 0.0)
                    {
                        Nsum = prevState.Nleaf3 + prevState.Nstem3 + prevState.Nstolon3;
                        Nleaf3 += NFastRemob3 * prevState.Nleaf3 / Nsum;
                        Nstem3 += NFastRemob3 * prevState.Nstem3 / Nsum;
                        Nstolon3 += NFastRemob3 * prevState.Nstolon3 / Nsum;
                    }
                }
            }
        }

        /// <summary>
        /// Computes the fraction of today's growth allocated to shoot
        /// </summary>
        /// <remarks>
        /// Takes into consideration any seasonal variations and defoliation, this is done by
        /// targeting a given shoot:root ratio (that is the maxSRratio)
        /// </remarks>
        /// <returns>The fraction of DM growth allocated to shoot (0-1)</returns>
        private double ToShootFraction()
        {
            double result = 1.0;

            double fac = 1.0;
            int doyIncrease = doyIniHighShoot + higherShootAllocationPeriods[0];  //35;   //75
            int doyPlateau = doyIniHighShoot + higherShootAllocationPeriods[1];   // 95;   // 110;
            int doyDecrease = doyIniHighShoot + higherShootAllocationPeriods[2];  // 125;  // 140;
            int doy = Clock.Today.DayOfYear;

            if (doy > doyIniHighShoot)
            {
                if (doy < doyIncrease)
                    fac = 1 + shootAllocationIncrease * (doy - doyIniHighShoot) / higherShootAllocationPeriods[0];
                else if (doy <= doyPlateau)
                    fac = 1.0 + shootAllocationIncrease;
                else if (doy <= doyDecrease)
                    fac = 1 + shootAllocationIncrease * (1 - (doy - doyPlateau) / higherShootAllocationPeriods[2]);
            }
            else
            {
                if (doyDecrease > 365 && doy <= doyDecrease - 365)
                    fac = 1 + shootAllocationIncrease * (1 - (365 + doy - doyPlateau) / higherShootAllocationPeriods[2]);
            }

            if (prevState.dmRoot > 0.00001)
            {
                double presentSRratio = dmGreen / prevState.dmRoot;
                double targetedSRratio;

                if (presentSRratio > fac * maxSRratio)
                    targetedSRratio = glfWater * Math.Pow(fac * maxSRratio, 2.0) / (fac * maxSRratio);
                else
                    targetedSRratio = glfWater * Math.Pow(fac * maxSRratio, 2.0) / presentSRratio;

                result = targetedSRratio / (1.0 + targetedSRratio);
            }

            return result;
        }

        /// <summary>
        /// Tentative - correction for the fraction of DM allocated to leaves
        /// </summary>
        /// <returns></returns>
        private double UpdatefLeaf()
        {
            double result;
            if (isLegume)
            {
                if (dmGreen > 0.0 && (dmStolon / dmGreen) > fStolon)
                    result = 1.0;
                else if (dmGreen + dmStolon < 2000)
                    result = fLeaf + (1 - fLeaf) * (dmGreen + dmStolon) / 2000;
                else
                    result = fLeaf;
            }
            else
            {
                if (dmGreen < 2000)
                    result = fLeaf + (1 - fLeaf) * dmGreen / 2000;
                else
                    result = fLeaf;
            }
            return result;
        }

        /// <summary>
        /// Computes the turnover rate and update each tissue pool of all plant parts
        /// </summary>
        /// <remarks>
        /// The C and N amounts for remobilisation are also computed in here
        /// </remarks>
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
            gamaD = turnoverRateDead2Litter * WaterFac2Litter * digestibilityDead / 0.4 + StockFac2Litter;

            // Turnover rate for roots
            gamaR = turnoverRateRootSenescence * TempFac * WaterFac2Root;


            if (gama == 0.0) //if gama ==0 due to gftt or gfwt, then skip "turnover" part
            {
                // No turnover, so there is no new litter or root senenscing
                dLitter = 0.0;
                dNLitter = 0.0;
                dRootSen = 0.0;
                dNrootSen = 0.0;
            }
            else
            {
                if (isAnnual)
                {
                    if (phenoStage == 1)
                    { //vegetative
                        gama *= daysfromEmergence / daysEmgToAnth;
                        gamaR *= daysfromEmergence / daysEmgToAnth;
                    }
                    else if (phenoStage == 2)
                    { //reproductive
                        gama = 1 - (1 - gama) * (1 - Math.Pow(daysfromAnthesis / daysToMature, 2));
                    }
                }

                // Daily defoliation fraction
                double FracDefoliated = 0;
                if (prevState.dmDefoliated != 0.0 && (prevState.dmLeaf + prevState.dmStem + prevState.dmStolon) != 0.0)
                    FracDefoliated = prevState.dmDefoliated / (prevState.dmDefoliated + prevState.dmLeaf + prevState.dmStem + prevState.dmStolon);

                // Adjust stolon turnover due to defoliation (increase stolon senescence)
                gamaS = gama + FracDefoliated * (1 - gama);

                // Check whether todays senescence will result in dmGreen < dmGreenmin
                //   if that is the case then adjust (reduce) the turnover rates
                // TODO: possibly should skip this for annuals to allow them to die - phenololgy-related?
                double dmGreenToBe = dmGreen + dGrowthShoot - gama * (prevState.dmLeaf3 + prevState.dmStem3 + prevState.dmStolon3);
                if (dmGreenToBe < dmGreenmin)
                {
                    if (dmGreen + dGrowthShoot < dmGreenmin)
                    { // this should not happen anyway
                        gama = 0.0;
                        gamaS = 0.0;
                        gamaR = 0.0;
                    }
                    else
                    {
                        double gama_adj = (dmGreen + dGrowthShoot - dmGreenmin) / (prevState.dmLeaf3 + prevState.dmStem3 + prevState.dmStolon3);
                        gamaR = gamaR * gama_adj / gama;
                        gamaD = gamaD * gama_adj / gama;
                        gama = gama_adj;
                    }
                }
                if (dmRoot < 0.5 * dmGreenmin)          // set a minimum root too, probably not really needed
                    gamaR = 0;

                // Do actual DM turnover for all tissues
                double dDM_in = 0.0;
                double dDM_out = 2 * gama * prevState.dmLeaf1;
                dmLeaf1 += dDM_in - dDM_out;
                Nleaf1 += -dDM_out * prevState.Nleaf1 / prevState.dmLeaf1;
                dDM_in = dDM_out;
                dDM_out = gama * prevState.dmLeaf2;
                dmLeaf2 += dDM_in - dDM_out;
                Nleaf2 += dDM_in * prevState.Nleaf1 / prevState.dmLeaf1 - dDM_out * prevState.Nleaf2 / prevState.dmLeaf2;
                dDM_in = dDM_out;
                dDM_out = gama * prevState.dmLeaf3;
                dmLeaf3 += dDM_in - dDM_out;
                Nleaf3 += dDM_in * prevState.Nleaf2 / prevState.dmLeaf2 - dDM_out * prevState.Nleaf3 / prevState.dmLeaf3;
                dDM_in = dDM_out;
                dDM_out = gamaD * prevState.dmLeaf4;
                double ChRemobSugar = dDM_in * kappaCRemob;
                double ChRemobProtein = dDM_in * (prevState.Nleaf3 / prevState.dmLeaf3 - leafNmin) * CNratioProtein * facCNRemob;
                dDM_in -= ChRemobSugar + ChRemobProtein;
                if (dDM_in < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - leaf");
                dmLeaf4 += dDM_in - dDM_out;
                Nleaf4 += dDM_in * leafNmin - dDM_out * prevState.Nleaf4 / prevState.dmLeaf2;
                dLitter = dDM_out;
                dNLitter = dDM_out * prevState.Nleaf4 / prevState.dmLeaf2;
                dGrowthShoot -= dDM_out;
                double NRemobl = dDM_in * (prevState.Nleaf3 / prevState.dmLeaf3 - leafNmin);
                double ChRemobl = ChRemobSugar + ChRemobProtein;

                dDM_in = 0.0;
                dDM_out = 2 * gama * prevState.dmStem1;
                dmStem1 += dDM_in - dDM_out;
                Nstem1 += -dDM_out * prevState.Nstem1 / prevState.dmStem1;
                dDM_in = dDM_out;
                dDM_out = gama * prevState.dmStem2;
                dmStem2 += dDM_in - dDM_out;
                Nstem2 += dDM_in * prevState.Nstem1 / prevState.dmStem1 - dDM_out * prevState.Nstem2 / prevState.dmStem2;
                dDM_in = dDM_out;
                dDM_out = gama * prevState.dmStem3;
                dmStem3 += dDM_in - dDM_out;
                Nstem3 += dDM_in * prevState.Nstem2 / prevState.dmStem2 - dDM_out * prevState.Nstem3 / prevState.dmStem3;
                dDM_in = dDM_out;
                dDM_out = gamaD * prevState.dmStem4;
                ChRemobSugar = dDM_in * kappaCRemob;
                ChRemobProtein = dDM_in * (prevState.Nstem3 / prevState.dmStem3 - NcStemMin) * CNratioProtein * facCNRemob;
                dDM_in -= ChRemobSugar + ChRemobProtein;
                if (dDM_in < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - stem");
                dmStem4 += dDM_in - dDM_out;
                Nstem4 += dDM_in * NcStemMin - dDM_out * prevState.Nstem4 / prevState.dmStem2;
                dLitter = dDM_out;
                dNLitter = dDM_out * prevState.Nstem4 / prevState.dmStem2;
                dGrowthShoot -= dDM_out;
                NRemobl += dDM_in * (prevState.Nstem3 / prevState.dmStem3 - NcStemMin);
                ChRemobl += ChRemobSugar + ChRemobProtein;

                dDM_in = 0.0;
                dDM_out = 2 * gama * prevState.dmStolon1;
                dmStolon1 += dDM_in - dDM_out;
                Nstolon1 += -dDM_out * prevState.Nstolon1 / prevState.dmStolon1;
                dDM_in = dDM_out;
                dDM_out = gama * prevState.dmStolon2;
                dmStolon2 += dDM_in - dDM_out;
                Nstolon2 += dDM_in * prevState.Nstolon1 / prevState.dmStolon1 - dDM_out * prevState.Nstolon2 / prevState.dmStolon2;
                dDM_in = dDM_out;
                dDM_out = gama * prevState.dmStolon3;
                dmStolon3 += dDM_in - dDM_out;
                Nstolon3 += dDM_in * prevState.Nstolon2 / prevState.dmStolon2 - dDM_out * prevState.Nstolon3 / prevState.dmStolon3;
                dDM_in = dDM_out;
                ChRemobSugar = dDM_in * kappaCRemob;
                ChRemobProtein = dDM_in * (prevState.Nstolon3 / prevState.dmStolon3 - NcStolonMin) * CNratioProtein * facCNRemob;
                dDM_in -= ChRemobSugar + ChRemobProtein;
                if (dDM_in < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - stolon");
                dLitter = dDM_in;
                dNLitter = dDM_in * NcStolonMin;
                dGrowthShoot -= dDM_out;
                NRemobl += dDM_in * (prevState.Nstolon3 / prevState.dmStolon3 - NcStolonMin);
                ChRemobl += ChRemobSugar + ChRemobProtein;

                dRootSen = gamaR * prevState.dmRoot;
                dmRoot -= dRootSen;
                ChRemobSugar = dRootSen * kappaCRemob;
                ChRemobProtein = dRootSen * (prevState.Nroot / prevState.dmRoot - NcRootMin) * CNratioProtein * facCNRemob;
                dRootSen -= ChRemobSugar + ChRemobProtein;
                if (dRootSen < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - root");
                Nroot += -dRootSen * prevState.Nroot / dmRoot;
                dGrowthRoot -= dRootSen;
                NRemobl += dRootSen * (prevState.Nroot / dmRoot - NcRootMin);
                ChRemobl += ChRemobSugar + ChRemobProtein;

                // Remobilised C to be used in tomorrow's growth (converted from carbohydrates to C)
                CRemobilised = ChRemobl * CinDM;

                // Fraction of NRemobilised not used in new growth, added to today's litter
                double leftoverNremob = NRemobilised * (1 - kappaNRemob4);
                dNLitter += leftoverNremob;

                // Remobilised and remobilisable (luxury) N to be potentially used for growth tomorrow
                NRemobilised = NRemobl;
                NLuxury2 = Math.Max(0.0, Nleaf2 - dmLeaf2 * leafNopt * relativeNStage3)
                         + Math.Max(0.0, Nstem2 - dmStem2 * NcStemOpt * relativeNStage3)
                         + Math.Max(0.0, Nstolon2 - dmStolon2 * NcStolonOpt * relativeNStage3);
                NLuxury3 = Math.Max(0.0, Nleaf3 - dmLeaf3 * leafNopt * relativeNStage3)
                         + Math.Max(0.0, Nstem3 - dmStem3 * NcStemOpt * relativeNStage3)
                         + Math.Max(0.0, Nstolon3 - dmStolon3 * NcStolonOpt * relativeNStage3);
                // only a fraction of luxury N is available for remobilisation:
                NLuxury2 *= kappaNRemob2;
                NLuxury3 *= kappaNRemob3;
            }
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
                myCanopyData.sender = "grass";
                myCanopyData.lai = greenLAI;
                myCanopyData.lai_tot = TotalLAI;
                myCanopyData.height = Height;
                myCanopyData.depth = Height;
                myCanopyData.cover = GreenCover;
                myCanopyData.cover_tot = TotalCover;
                NewCanopy.Invoke(myCanopyData);
            }
        }

        /// <summary>
        /// Gets the water uptake for each layer as calculated by an external module (SWIM)
        /// </summary>
        /// <remarks>
        /// This method is only used when an external method is used to compute water uptake (this includes AgPasture)
        /// </remarks>
        /// <param name="SoilWater"></param>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
        {
            for (int iCrop = 0; iCrop < SoilWater.Uptakes.Length; iCrop++)
            {
                if (SoilWater.Uptakes[iCrop].Name == speciesName)
                {
                    for (int layer = 0; layer < SoilWater.Uptakes[iCrop].Amount.Length; layer++)
                        mySoilWaterUptake[layer] = SoilWater.Uptakes[iCrop].Amount[layer];
                }
            }
        }

        /// <summary>
        /// Gather the amount of available eater and computes the water uptake for this species
        /// </summary>
        /// <remarks>
        /// Using this routine is discourage as it ignores the presence of other species and thus 
        ///  might result in loss of mass balance or unbalanced supply, i.e. over-supply for one
        ///  while under-supply for other species (depending on the order that species are considered)
        /// </remarks>
        private void DoWaterCalculations()
        {
            mySoilAvailableWater = GetSoilAvailableWater();
            // myWaterDemand given by MicroClimate
            mySoilWaterUptake = DoSoilWaterUptake();
        }

        /// <summary>
        /// Finds out the amount soil water available for this plant (ignoring any other species)
        /// </summary>
        /// <returns>The amount of water available to plants in each layer</returns>
        internal double[] GetSoilAvailableWater()
        {
            double[] result = new double[nLayers];
            SoilCrop soilInfo = (SoilCrop)Soil.Crop(Name);
            if (useAltWUptake == "no")
            {
                for (int layer = 0; layer <= myRootFrontier; layer++)
                {
                    result[layer] = Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - soilInfo.LL[layer] * Soil.Thickness[layer])
                                  * LayerFractionWithRoots(layer);
                    result[layer] *= soilInfo.KL[layer];

                }
            }
            else
            { // Method implemented by RCichota
                // Available Water is function of root density, soil water content, and soil hydraulic conductivity
                // Assumptions: all factors are exponential functions and vary between 0 and 1;
                //   - If root density is equal to ReferenceRLD then plant can explore 90% of the water;
                //   - If soil Ksat is equal to ReferenceKSuptake then soil can supply 90% of its available water;
                //   - If soil water content is at DUL then 90% of its water is available;
                double[] myRLD = RLV;
                double facRLD = 0.0;
                double facCond = 0.0;
                double facWcontent = 0.0;
                for (int layer = 0; layer <= myRootFrontier; layer++)
                {
                    facRLD = 1 - Math.Pow(10, -myRLD[layer] / ReferenceRLD);
                    facCond = 1 - Math.Pow(10, -Soil.Water.KS[layer] / ReferenceKSuptake);
                    facWcontent = 1 - Math.Pow(10,
                                -(Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - Soil.SoilWater.ll15_dep[layer]))
                                / (Soil.SoilWater.dul_dep[layer] - Soil.SoilWater.ll15_dep[layer]));

                    // Theoretical total available water
                    result[layer] = Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - soilInfo.LL[layer] * Soil.Thickness[layer])
                                  * LayerFractionWithRoots(layer);
                    // Actual available water
                    result[layer] *= facRLD * facCond * facWcontent;
                }
            }

            return result;
        }

        /// <summary>
        /// Computes the actual water uptake and send the deltas to soil module
        /// </summary>
        /// <returns>The amount of water taken up for each soil layer</returns>
        private double[] DoSoilWaterUptake()
        {
            PMF.WaterChangedType WaterTakenUp = new PMF.WaterChangedType();
            WaterTakenUp.DeltaWater = new double[nLayers];

            double uptakeFraction = Math.Min(1.0, myWaterDemand / mySoilAvailableWater.Sum());
            double[] result = new double[nLayers];

            if (useAltWUptake == "no")
            {
                for (int layer = 0; layer < myRootFrontier; layer++)
                {
                    result[layer] = mySoilAvailableWater[layer] * uptakeFraction;
                    WaterTakenUp.DeltaWater[layer] = -result[layer];
                }
            }
            else
            { // Method implemented by RCichota
                // Uptake is distributed over the profile according to water availability,
                //  this means that water status and root distribution have been taken into account

                for (int layer = 0; layer < myRootFrontier; layer++)
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
        /// Performs the computations for N balance and uptake
        /// </summary>
        private void DoNitrogenCalculations()
        {
            // get soil available N
            if (myNitrogenUptakeSource.ToLower() == "calc")
            {
                GetSoilAvailableN();
                for (int layer = 0; layer < nLayers; layer++)
                    mySoilAvailableN[layer] = mySoilNH4available[layer] + mySoilNO3available[layer];
            }
            //else
            //    N available is computed in another module

            // get N demand (optimum and luxury)
            CalcNDemand();

            // get N fixation
            Nfixation = CalcNFixation();

            // evaluate the use of N remobilised and any soil demand
            if (NdemandLux - Nfixation > -0.0001)
            { // N demand is fulfilled by fixation alone
                Nfixation = NdemandLux;  // should not be needed, but just in case...
                Nremob2NewGrowth = 0.0;
                soilNdemand = 0.0;
            }
            else if (NdemandLux - (Nfixation + NRemobilised) > -0.0001)
            { // N demand is fulfilled by fixation plus remobilisation of senescent
                Nremob2NewGrowth = Math.Max(0.0, NRemobilised - (NdemandLux - Nfixation));
                NRemobilised -= Nremob2NewGrowth;
                soilNdemand = 0.0;
            }
            else
            { // N demand is greater than fixation and remobilisation of senescent, N uptake is needed
                Nremob2NewGrowth = NRemobilised;
                NRemobilised = 0.0;
                soilNdemand = NdemandLux - (Nfixation + Nremob2NewGrowth);
            }

            // get the amount of N taken up from soil
            soilNuptake = CalcSoilNUptake();
            newGrowthN = Nfixation + Nremob2NewGrowth + soilNuptake;

            // evaluate whether further remobilisation (from luxury N) is needed
            if (newGrowthN - NdemandOpt > -0.0001)
            { // total N available is not enough for optimum growth, check remobilisation of luxury N
                CalcNLuxuryRemob();
                newGrowthN += NFastRemob3 + NFastRemob2;
            }
            //else
            //    there is enough N for at least optimum N content, there is no need for further considerations

            // send delta N to the soil model
            DoSoilNitrogenUptake();
        }

        /// <summary>
        /// Computes the N demanded for optimum N content as well as luxury uptake
        /// </summary>
        internal void CalcNDemand()
        {
            double toRoot = dGrowthW * (1.0 - fShoot);
            double toStol = dGrowthW * fShoot * fStolon;
            double toLeaf = dGrowthW * fShoot * fLeaf;
            double toStem = dGrowthW * fShoot * (1.0 - fStolon - fLeaf);

            // N demand for new growth, with optimum N (kg/ha)
            NdemandOpt = toRoot * NcRootOpt + toStol * NcStolonOpt + toLeaf * leafNopt + toStem * NcStemOpt;

            double fN = NCO2Effects();
            NdemandOpt *= fN;    //reduce the demand under elevated CO2,
            // this will reduce the N stress under N limitation for the same soil N supply

            // N demand for new growth, with luxury uptake (maximum [N])
            NdemandLux = toRoot * NcRootMax + toStol * NcStolonMax + toLeaf * leafNmax + toStem * NcStemMax;
            // It is assumed that luxury uptake is not affected by CO2 variations
        }

        /// <summary>
        /// Computes the amount of N fixed from atmosphere
        /// </summary>
        /// <returns>The amount of N fixed (kgN/ha)</returns>
        internal double CalcNFixation()
        {
            double result = 0.0;

            if (isLegume)
            {
                // Start with minimum fixation
                result = minimumNFixation * NdemandLux;

                // evaluate N stress
                double Nstress = 1.0;
                if (NdemandLux > 0.0 && (NdemandLux > mySoilAvailableN.Sum() + result))
                    Nstress = mySoilAvailableN.Sum() / (NdemandLux - result);

                // Update N fixation if under N stress
                if (Nstress < 0.99)
                    result = (maximumNFixation - (maximumNFixation - minimumNFixation) * Nstress) * NdemandLux;

            }

            return result;
        }

        /// <summary>
        /// Find out the amount of Nitrogen (NH4 and NO3) in the soil available to plants for each soil layer
        /// </summary>
        internal void GetSoilAvailableN()
        {
            mySoilNH4available = new double[nLayers];
            mySoilNO3available = new double[nLayers];
            double facWtaken = 0.0;
            for (int layer = 0; layer <= myRootFrontier; layer++)
            {
                if (useAltNUptake == "no")
                {
                    // simple way, all N in the root zone is available
                    mySoilNH4available[layer] = Soil.SoilNitrogen.nh4[layer] * LayerFractionWithRoots(layer);
                    mySoilNO3available[layer] = Soil.SoilNitrogen.no3[layer] * LayerFractionWithRoots(layer);
                }
                else
                {
                    // Method implemented by RCichota,
                    // N is available following water and a given 'availability' factor (for each N form) and the fraction of water taken up

                    // fraction of available water taken up
                    facWtaken = mySoilWaterUptake[layer] / Math.Max(0.0, Soil.SoilWater.sw_dep[layer] - Soil.SoilWater.ll15_dep[layer]);

                    // Theoretical amount available
                    mySoilNH4available[layer] = Soil.SoilNitrogen.nh4[layer] * kuNH4 * LayerFractionWithRoots(layer);
                    mySoilNO3available[layer] = Soil.SoilNitrogen.no3[layer] * kuNO3 * LayerFractionWithRoots(layer);

                    // actual amount available
                    mySoilNH4available[layer] *= facWtaken;
                    mySoilNO3available[layer] *= facWtaken;
                }
            }
        }

        /// <summary>
        /// Computes the amount of N to be taken up from the soil
        /// </summary>
        /// <returns>The amount of N to be taken up from each soil layer</returns>
        private double CalcSoilNUptake()
        {
            double result;
            if (soilNdemand == 0.0)
            { // No demand, no uptake
                result = 0.0;
            }
            else
            {
                if (mySoilAvailableN.Sum() >= soilNdemand)
                { // soil can supply all remaining N needed
                    result = soilNdemand;
                }
                else
                { // soil cannot supply all N needed. Get the available N and partition between species
                    result = mySoilAvailableN.Sum() * soilNdemand;
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
            double Nmissing = NdemandOpt - newGrowthN;
            if (Nmissing <= NLuxury2 + NLuxury3)
            {
                // There is luxury N that can be used for optimum growth, first from tissue 3
                if (Nmissing <= NLuxury3)
                {
                    NFastRemob3 = Nmissing;
                    NFastRemob2 = 0.0;
                    Nmissing = 0.0;
                }
                else
                {
                    // first from tissue 3
                    NFastRemob3 = NLuxury3;
                    Nmissing -= NLuxury3;

                    // remaining from tissue 2
                    NFastRemob2 = Nmissing;
                    Nmissing = 0.0;
                }
            }
            else
            {
                // N luxury is not enough for optimum growth, use up all there is
                if (NLuxury2 + NLuxury3 > 0)
                {
                    NFastRemob3 = NLuxury3;
                    NFastRemob2 = NLuxury2;
                    Nmissing -= (NLuxury3 + NLuxury2);
                }
            }
        }

        /// <summary>
        /// Computes the distribution of N uptake over the soil profile and send the delta to soil module
        /// </summary>
        private void DoSoilNitrogenUptake()
        {
            if (myNitrogenUptakeSource == "calc")
            {
                Soils.NitrogenChangedType NUptake = new Soils.NitrogenChangedType();
                NUptake.Sender = speciesName;
                NUptake.SenderType = "Plant";
                NUptake.DeltaNO3 = new double[nLayers];
                NUptake.DeltaNH4 = new double[nLayers];

                double Fraction = 0;
                double n_uptake = 0;

                if (useAltNUptake == "no")
                {
                    if (mySoilAvailableN.Sum() > 0.0)
                        Fraction = Math.Min(1.0, soilNuptake / mySoilAvailableN.Sum());

                    for (int layer = 0; layer < myRootFrontier; layer++)
                    {
                        mySoilNUptake[layer] = (Soil.SoilNitrogen.nh4[layer] + Soil.SoilNitrogen.no3[layer]) * Fraction;
                        n_uptake += mySoilNUptake[layer];

                        NUptake.DeltaNH4[layer] = -Soil.SoilNitrogen.nh4[layer] * Fraction;
                        NUptake.DeltaNO3[layer] = -Soil.SoilNitrogen.no3[layer] * Fraction;
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
                    double totWuptake = mySoilWaterUptake.Sum();
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        fNH4Avail[layer] = mySoilAvailableN[layer] / totNH4Available;
                        fNO3Avail[layer] = mySoilAvailableN[layer] / totNO3Available;
                        fWUptake[layer] = mySoilWaterUptake[layer] / totWuptake;
                    }
                    double totFacNH4 = fNH4Avail.Sum() + fWUptake.Sum();
                    double totFacNO3 = fNO3Avail.Sum() + fWUptake.Sum();
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        Fraction = (fNH4Avail[layer] + fWUptake[layer]) / totFacNH4;
                        NUptake.DeltaNH4[layer] = -Soil.SoilNitrogen.nh4[layer] * Fraction;

                        Fraction = (fNO3Avail[layer] + fWUptake[layer]) / totFacNO3;
                        NUptake.DeltaNO3[layer] = -Soil.SoilNitrogen.no3[layer] * Fraction;
                    }
                }

                // do the actual N changes
                NitrogenChanged.Invoke(NUptake);
            }
            else
            {
                // N uptake calculated by other modules (e.g., SWIM)
                string msg = "Only one option for N uptake is implemented in AgPasture. Please specify N uptake source as either \"AgPasture\" or \"calc\".";
                throw new Exception(msg);
            }
        }

        #endregion

        #region - Organic matter processes  --------------------------------------------------------------------------------

        /// <summary>
        /// Return a given amount of DM (and N) to surface organic matter
        /// </summary>
        /// <param name="amountDM">DM amount to return</param>
        /// <param name="amountN">N amount to return</param>
        private void DoSurfaceOMReturn(double amountDM, double amountN)
        {
            if (BiomassRemoved != null)
            {
                Single dDM = (Single)amountDM;

                PMF.BiomassRemovedType BR = new PMF.BiomassRemovedType();
                String[] type = new String[] { "grass" };
                Single[] dltdm = new Single[] { (Single)amountDM };
                Single[] dltn = new Single[] { (Single)amountN };
                Single[] dltp = new Single[] { 0 };         // P not considered here
                Single[] fraction = new Single[] { 1 };     // fraction is always 1.0 here

                BR.crop_type = speciesName;
                BR.dm_type = type;
                BR.dlt_crop_dm = dltdm;
                BR.dlt_dm_n = dltn;
                BR.dlt_dm_p = dltp;
                BR.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(BR);
            }
        }

        /// <summary>
        /// Return scenescent roots to fresh organic matter pool in the soil
        /// </summary>
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
                fomData.C = amountDM * rootFraction[layer] * CinDM;
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
                FOMData.Type = speciesName;
                FOMData.Layer = FOMdataLayer;
                IncorpFOM.Invoke(FOMData);
            }
        }

        #endregion

        #endregion

        #region Other processes  -------------------------------------------------------------------------------------------

        public void Harvest(string type, double amount)
        {
            GrazeType GrazeData = new GrazeType();
            GrazeData.amount = amount;
            GrazeData.type = type;
            OnGraze(GrazeData);
        }

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
        public void RemoveDM(double AmountToRemove)
        {
            // check existing amount and what is harvestable
            double PreRemovalDM = dmShoot;
            double PreRemovalN = Nshoot;

            if (HarvestableWt > 0.0)
            {
                // get the weights for each pool, consider preference and available DM
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
                prevState.dmDefoliated = dmDefoliated;
                Ndefoliated = PreRemovalN - Nshoot;
                if (Math.Abs(dmDefoliated - AmountToRemove) > 0.00001)
                    throw new Exception("  " + speciesName + " - removal of DM resulted in loss of mass balance");
            }
        }

        /// <summary>
        /// Remove biomass from plant
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
                        if (LeafGreenWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = amountToRemove / LeafGreenWt;
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "green" && plantPart.ToLower() == "stem")
                    {
                        if (StemGreenWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = amountToRemove / StemGreenWt;
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "leaf")
                    {
                        if (LeafDeadWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = amountToRemove / LeafDeadWt;
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "stem")
                    {
                        if (StemDeadWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = amountToRemove / StemDeadWt;
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                }
            }
            RefreshAfterRemove();
        }

        /// <summary>
        /// Remove a fraction of DM from a given plant part
        /// </summary>
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

        /// <summary>
        /// Performs few actions to update variables after RemoveFractionDM
        /// </summary>
        public void RefreshAfterRemove()
        {
            // set values for fractionHarvest (in fact fraction harvested)
            fractionHarvest = dmDefoliated / (StandingWt + dmDefoliated);

            // recalc the digestibility
            calcDigestibility();

            // update aggregated variables
            updateAggregated();
        }

        /// <summary>
        /// Reset this plant state to its initial values
        /// </summary>
        public void Reset()
        {
            SetInitialState();
            prevState = new SpeciesState();
        }

        /// <summary>
        /// Kills this plant (zero all variables and set to not alive)
        /// </summary>
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

        /// <summary>
        /// Reset this plant to zero (kill crop)
        /// </summary>
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

            updateAggregated();

            phenoStage = 0;

            prevState = new SpeciesState();
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
        /// Growth limiting factor due to temperature
        /// </summary>
        /// <param name="Temp">Temperature for which the limiting factor will be computed</param>
        /// <returns>The value for the limiting factor (0-1)</returns>
        private double TemperatureLimitingFactor(double Temp)
        {
            double result = 0.0;
            if (photoPath == "C3")
            {
                if (Temp > growthTmin && Temp < growthTmax)
                {
                    double nTmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                    double val1 = Math.Pow((Temp - growthTmin), growthTq) * (nTmax - Temp);
                    double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (nTmax - growthTopt);
                    result = val1 / val2;
                }
            }
            else if (photoPath == "C4")
            {
                if (Temp > growthTmin)		 // same as GFTempC3 for [Tmin,Topt], but T as Topt if T > Topt
                {
                    if (Temp > growthTopt)
                        Temp = growthTopt;

                    double nTmax = growthTopt + (growthTopt - growthTmin) / growthTq;
                    double val1 = Math.Pow((Temp - growthTmin), growthTq) * (nTmax - Temp);
                    double val2 = Math.Pow((growthTopt - growthTmin), growthTq) * (nTmax - growthTopt);
                    result = val1 / val2;
                }
            }
            else
                throw new Exception("Photosynthesis pathway is not valid");
            return result;
        }

        /// <summary>
        /// Effect of temperature on tissue turnover
        /// </summary>
        /// <returns>Temperature factor (0-1)</returns>
        private double TempFactorForTissueTurnover(double Temp)
        {
            double result = 0.0;
            if (Temp > massFluxTmin && Temp <= massFluxTopt)
            {
                result = (Temp - massFluxTmin) / (massFluxTopt - massFluxTmin);
            }
            else if (Temp > massFluxTopt)
            {
                result = 1.0;
            }
            return result;
        }

        /// <summary>
        /// Photosynthesis reduction factor due to high temperatures (heat stress)
        /// </summary>
        /// <returns>The reduction in potosynthesis rate (0-1)</returns>
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

            return recoverF;
        }

        /// <summary>
        /// Photosynthesis reduction factor due to low temperatures (cold stress)
        /// </summary>
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

            return recoverF;
        }

        /// <summary>
        /// Photosynthesis factor (reduction or increase) to eleveated [CO2]
        /// </summary>
        /// <returns>A factor to adjust photosynthesis due to CO2</returns>
        private double PCO2Effects()
        {
            if (Math.Abs(MetData.CO2 - referenceCO2) < 0.01)
                return 1.0;

            double Fp1 = (MetData.CO2 / (offsetCO2EffectOnPhotosynthesis + MetData.CO2));
            double Fp2 = ((referenceCO2 + offsetCO2EffectOnPhotosynthesis) / referenceCO2);

            return Fp1 * Fp2;
        }

        /// <summary>
        /// N effect on maximum photosynthesis, affected by CO2 as well
        /// </summary>
        /// <returns></returns>
        private double PmxNeffect()
        {

            if (isAnnual)
                return 0.0;
            else
            {
                double Fn = NCO2Effects();

                double result = 1.0;

                if (dmLeaf1 + dmLeaf2 + dmLeaf3 > 0.0)
                {
                    double NcleafGreen = (Nleaf1 + Nleaf2 + Nleaf3) / (dmLeaf1 + dmLeaf2 + dmLeaf3);
                    if (NcleafGreen < leafNopt * Fn)
                    {
                        if (NcleafGreen > leafNmin)
                            result = Math.Min(1.0, (NcleafGreen - leafNmin) / (leafNopt * Fn - leafNmin));
                        else
                            result = 0.0;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Plant nitrogen [N] decline to elevated [CO2]
        /// </summary>
        /// <returns></returns>
        private double NCO2Effects()
        {
            if (Math.Abs(MetData.CO2 - referenceCO2) < 0.5)
                return 1.0;

            double interM = Math.Pow((offsetCO2EffectOnNuptake - referenceCO2), exponentCO2EffectOnNuptake);
            double result = minimumCO2EffectOnNuptake
                          + (1 - minimumCO2EffectOnNuptake) * interM
                          / (interM + Math.Pow((MetData.CO2 - referenceCO2), exponentCO2EffectOnNuptake));

            return result;
        }

        //Canopy conductance decline to elevated [CO2]
        private double ConductanceCO2Effects()
        {
            if (Math.Abs(MetData.CO2 - referenceCO2) < 0.5)
                return 1.0;
            //Hard coded here, not used, should go to Micromet!
            double Gmin = 0.2;      //Fc = Gmin when CO2->unlimited
            double Gmax = 1.25;     //Fc = Gmax when CO2 = 0;
            double beta = 2.5;      //curvature factor,

            double Fc = Gmin + (Gmax - Gmin) * (1 - Gmin) * Math.Pow(referenceCO2, beta) /
            ((Gmax - 1) * Math.Pow(MetData.CO2, beta) + (1 - Gmin) * Math.Pow(referenceCO2, beta));
            return Fc;
        }

        /// <summary>
        /// Growth limiting factor due to soil moisture deficit
        /// </summary>
        /// <returns>The limiting factor due to soil water deficit (0-1)</returns>
        private double WaterLimitingFactor()
        {
            double result = 0.0;

            if (myWaterDemand == 0)
                result = 1.0;
            if (myWaterDemand > 0.0 && mySoilWaterUptake.Sum() == 0.0)
                result = 0.0;
            else
                result = mySoilWaterUptake.Sum() / myWaterDemand;

            return result;
        }

        /// <summary>
        /// Growth limiting factor due to excess of water in soil (logging/saturation)
        /// </summary>
        /// <remarks>
        /// Assuming that water above field capacity is not good
        /// </remarks>
        /// <returns>The limiting factor due to excess of soil water</returns>
        private double WaterLoggingFactor()
        {
            double result = 1.0;

            // calculate soil moisture thresholds in the root zone
            double mySWater = 0.0;
            double mySaturation = 0.0;
            double myDUL = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                // actual soil water content
                mySWater += Soil.SW[layer] * Soil.Thickness[layer] * LayerFractionWithRoots(layer);
                // water content at saturation
                mySaturation += Soil.SAT[layer] * Soil.Thickness[layer] * LayerFractionWithRoots(layer);
                // water content at field capacity
                myDUL += Soil.DUL[layer] * Soil.Thickness[layer] * LayerFractionWithRoots(layer);
            }

            if (mySWater > myDUL)
                result = 1 - waterLoggingEffect * (mySWater - myDUL) / (mySaturation - myDUL);

            return result;
        }

        /// <summary>
        /// Effect of water stress on tissue turnover
        /// </summary>
        /// <returns>Water stress factor (0-1)</returns>
        private double WaterFactorForTissueTurnover()
        {
            double result = 1.0;
            if (glfWater < massFluxWopt)
                result = 1 + (massFluxW0 - 1.0) * ((massFluxWopt - glfWater) / massFluxWopt);
            result = Math.Max(massFluxW0, Math.Min(1.0, result));

            return result;
        }

        /// <summary>
        /// Computes the ground cover for the plant, or plant part
        /// </summary>
        /// <param name="thisLAI">The LAI for this plant or part</param>
        /// <returns>Fraction of ground effectively covered (0-1)</returns>
        private double CalcPlantCover(double thisLAI)
        {
            return (1.0 - Math.Exp(-lightExtentionCoeff * thisLAI));
        }

        /// <summary>
        /// Compute the distribution of roots in the soil profile (sum is equal to one)
        /// </summary>
        /// <returns>The proportion of root mass in each soil layer</returns>
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
                    result[layer] = result[layer] * Soil.Thickness[layer] / sumProportion;
            else
                throw new Exception("Could not calculate root distribution");
            return result;
        }

        /// <summary>
        /// Compute how much of the layer is actually explored by roots (considering depth only)
        /// </summary>
        /// <param name="layer">The index for the layer being considered</param>
        /// <param name="root_depth">The depth of the bottom of the root zone</param>
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
        private double svp(double temp)  // from Growth.for documented in MicroMet
        {
            return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
        }

        #endregion
    }


    /// <summary>
    /// Stores the state variables of a pasture species
    /// </summary>
    [Serializable]
    public class SpeciesState
    {
        public SpeciesState() { }

        // DM pools
        public double dmLeaf;
        public double dmLeaf1;
        public double dmLeaf2;
        public double dmLeaf3;
        public double dmLeaf4;

        public double dmStem;
        public double dmStem1;
        public double dmStem2;
        public double dmStem3;
        public double dmStem4;

        public double dmStolon;
        public double dmStolon1;
        public double dmStolon2;
        public double dmStolon3;

        public double dmLitter;
        public double dmRoot;

        public double dmDefoliated;

        // N pools
        public double Nleaf1;
        public double Nleaf2;
        public double Nleaf3;
        public double Nleaf4;

        public double Nstem1;
        public double Nstem2;
        public double Nstem3;
        public double Nstem4;

        public double Nstolon1;
        public double Nstolon2;
        public double Nstolon3;

        public double Nlitter;
        public double Nroot;
    }

    /// <summary>
    /// Defines a broken stick (piecewise) function
    /// </summary>
    [Serializable]
    public class BrokenStick
    {
        public double[] X;
        public double[] Y;

        public double Value(double newX)
        {
            bool DidInterpolate = false;
            return Utility.Math.LinearInterpReal(newX, X, Y, out DidInterpolate);
        }
    }
}
