﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Models.Core;
using Models.Climate;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Models.Surface;
using APSIM.Numerics;
using APSIM.Core;
using Models.Soils;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Data structure that contains the information to set up a specific crop type of SCRUM
    /// </summary>
    /// <remarks>
    /// This includes parameters for the crop/cultivar as well as basic management (planting, harvesting, and residue management)
    /// </remarks>
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Simulations))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumCropInstance : Model, ILocatorDependency
    {
        [NonSerialized] private ILocator locator;

        /// <summary>Harvesting Event.</summary>
        public event EventHandler<EventArgs> Harvesting;


        /// <summary>Connection to the simulation clock.</summary>
        [Link(Type = LinkType.Scoped)]
        private Clock clock = null;

        /// <summary>Connection to the zone this crop is growing in.</summary>
        [Link(Type = LinkType.Ancestor)]
        private Zone myZone = null;

        /// <summary>Connection to the weather data model.</summary>
        [Link]
        Weather weather = null;

        /// <summary>Connection to the SCRUM plant model.</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        public Plant scrum = null;

        /// <summary>Connection to the plant phenology model.</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        /// <summary>Connection to the sigmoid function.</summary>
        [Link] private SigmoidFunction sigmoid = null;

        /// <summary>Connection to the surface organic model.</summary>
        [Link(Type = LinkType.Scoped)]
        private SurfaceOrganicMatter surfaceOM = null;

        [Link(Type = LinkType.Scoped)]
        private Soil soil = null;

        /// <summary>Connection to the summary model.</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Connection to the interface with the product 'organ'.</summary>
        [Link(ByName = true)]
        private IHasDamageableBiomass product = null;

        /// <summary>Connection to the interface with the stover 'organ'.</summary>
        [Link(ByName = true)]
        private IHasDamageableBiomass stover = null;

        private double _harvestIndex = 0.4;
        private double _moistureContent = 0.15;
        private double _rootProportion = 0.1;
        private double _maxRootDepth = 1500;
        private double _surfaceKL = 0.1;
        private double _maxHeight = 900;
        private double _maxCover = 0.97;
        private double _extinctionCoefficient = 0.7;
        private double _seedlingNConc = 0.05;
        private double _productHarvestNconc = 0.015;
        private double _rootNconc = 0.01;
        private double _stoverHarvestNconc = 0.01;
        private double _legumeFactor = 0;
        private double _baseTemperature = 5;
        private double _optimumTemperature = 25;
        private double _maxTemperature = 35;
        private double _tt_sowToEmergence = 100;
        private double _gSMax = 0.006;
        private double _r50 = 150;

        //-------------------------------------------------------------------------------------------------------------
        // Parameters defining the crop/cultivar to be simulated
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Name of the crop being simulated.</summary>
        public string CropName { get { return Name; } }

        /// <summary>Harvest index for the crop (proportion of the plant biomass that is product, 0.01-0.99).</summary>
        [Separator("Setup to simulate an instance of a crop using SCRUM - Enter values defining the crop in the sections below\n" +
            " Parameters defining growth pattern and biomass partition")]
        [Description(" Harvest Index (0.01-0.99):")]
        [Units("0-1")]
        public double HarvestIndex
        {
            get { return _harvestIndex; }
            set { _harvestIndex = constrain(value, 0.01, 0.99); }
        }

        /// <summary>Moisture content of product at harvest (0-0.99 g/g).</summary>
        [Description(" Product moisture content (0-0.99 g/g):")]
        [Units("g/g")]
        public double MoistureContent
        {
            get { return _moistureContent; }
            set { _moistureContent = constrain(value, 0, 0.99); }
        }

        /// <summary>Proportion of biomass allocated to roots (0.01-0.9).</summary>
        [Description(" Root biomass proportion (0.01-0.9):")]
        [Units("0-1")]
        public double RootProportion
        {
            get { return _rootProportion; }
            set { _rootProportion = constrain(value,0.01,0.9); }
        }

        /// <summary>Root depth at maturity (100 - 3000 mm).</summary>
        [Description(" Root depth at maturity (100 - 3000 mm):")]
        [Units("mm")]
        public double MaxRootDepth
        {
            get { return _maxRootDepth; }
            set { _maxRootDepth = constrain(value, 300, 3000); }
        }

        /// <summary>KL in top soil layer (0.01 - 0.2)</summary>
        [Description(" KL in top soil layer (0.01 - 0.2):")]
        [Bounds(Lower = 0.01, Upper = 0.2)]
        [Units("0-1")]
        public double SurfaceKL
        {
            get { return _surfaceKL; }
            set { _surfaceKL = constrain(value, 0.01, 0.2); }
        }

        /// <summary>Crop height at maturity (100 - 3000).</summary>
        [Description(" Crop height at maturity (100 - 3000 mm):")]
        [Units("mm")]
        public double MaxHeight

        {
            get { return _maxHeight; }
            set { _maxHeight = constrain(value, 100, 3000); }
        }

        /// <summary>Maximum crop green cover (0.01-0.97).</summary>
        [Description(" Maximum green cover (0.01-0.97):")]
        [Units("0-1")]
        public double MaxCover
        {
            get { return _maxCover; }
            set { _maxCover = constrain(value, 0.01, 0.97); }
        }

        /// <summary>Crop extinction coefficient (0.1-1.0).</summary>
        [Description(" Crop extinction coefficient (0.1-1.0):")]
        [Units("0-1")]
        public double ExtinctionCoefficient
        {
            get { return _extinctionCoefficient; }
            set { _extinctionCoefficient = constrain(value, 0.1, 1.0); }
        }

        /// <summary>Phenology stage at which plant Nconc is measured.</summary>
        /// <remarks>Used to adjust Nconc at harvest if a harvest is earlier or later that the stage when Nconc is specified.</remarks>
        [Separator(" Parameters defining crop nitrogen requirements")]
        [Description(" Stage for Nconc parameters:")]
        [Display(Type = DisplayType.ScrumHarvestStages)]
        public string TypicalHarvestStage { get; set; }

        /// <summary>Nitrogen concentration of plant at seedling stage (0.01 - 0.1 g/g).</summary>
        [Description(" Nitrogen concentration of plant at seedling stage (0.01 - 0.1 g/g):")]
        [Units("g/g")]
        public double SeedlingNConc
        {
            get { return _seedlingNConc; }
            set { _seedlingNConc = constrain(value, 0.01, 0.1); }
        }

        /// <summary>Nitrogen concentration of product at maturity (0.001 - 0.1 g/g).</summary>
        [Description(" Nitrogen concentration of product at harvest (0.001 - 0.1 g/g):")]
        [Units("g/g")]
        public double ProductHarvestNConc
        {
            get { return _productHarvestNconc; }
            set { _productHarvestNconc = constrain(value, 0.001, 0.1); }
        }

        /// <summary>Nitrogen concentration of stover at maturity (0.001 - 0.1 g/g).</summary>
        [Description(" Nitrogen concentration of stover at harvest (0.001 - 0.1 g/g):")]
        [Units("g/g")]
        public double StoverHarvestNConc
        {
            get { return _stoverHarvestNconc; }
            set { _stoverHarvestNconc = constrain(value, 0.001, 0.1); }
        }

        /// <summary>Nitrogen concentration of roots (0.001 - 0.1 g/g).</summary>
        [Description(" Nitrogen concentration of roots (0.001 - 0.1 g/g):")]
        [Units("g/g")]
        public double RootNConc
        {
            get { return _rootNconc; }
            set { _rootNconc = constrain(value, 0.001, 0.1); }
        }

        /// <summary>Proportion of potential N fixation for this crop (used for simulating legumes).</summary>
        [Description(" Proportion of potential N fixation for this crop, if a legume (0-1):")]
        [Units("0-1")]
        public double LegumeFactor
        {
            get { return _legumeFactor; }
            set { _legumeFactor = constrain(value, 0.0, 1.0); }
        }

        /// <summary>Base temperature for the crop (-10 - 20 oC).</summary>
        [Separator(" Parameters defining crop development as function of temperature")]
        [Description(" Crop base temperature (oC):")]
        [Units("^oC")]
        public double BaseTemperature
        {
            get { return _baseTemperature; }
            set { _baseTemperature = constrain(value, -10, 20); }
        }

        /// <summary>Optimum temperature for the crop (0 - 40 oC).</summary>
        [Description(" Crop optimum temperature (oC):")]
        [Units("^oC")]
        public double OptimumTemperature
        {
            get { return _optimumTemperature; }
            set { _optimumTemperature = constrain(value, 0, 40); }
        }

        /// <summary>Maximum temperature for the crop (10 - 60 oC).</summary>
        [Description(" Crop maximum temperature (oC):")]
        [Units("^oC")]
        public double MaxTemperature
        {
            get { return _maxTemperature; }
            set { _maxTemperature = constrain(value, 10, 60); }
        }

        /// <summary>Thermal time required from sowing to emergence (0-1000 oCd).</summary>
        [Description(" Thermal time required from sowing to emergence (0-1000 oCd):")]
        [Units("^oCd")]
        public double Tt_SowToEmergence
        {
            get { return _tt_sowToEmergence; }
            set { _tt_sowToEmergence = constrain(value, 0, 1000); }
        }

        /// <summary>Maximum canopy conductance (typically varies between 0.001 and 0.016 m/s).</summary>
        [Separator(" Parameters defining crop water requirements")]
        [Description(" Maximum canopy conductance (between 0.001 and 0.016 m/s):")]
        [Units("m/s")]
        public double GSMax
        {
            get { return _gSMax; }
            set { _gSMax = constrain(value, 0.001, 0.016); }
        }

        /// <summary>Net radiation at 50% of maximum conductance (typically varies between 50 and 200 W/m2).</summary>
        [Description(" Net radiation at 50% of maximum conductance (between 50 and 200 W/m^2):")]
        [Units("W/m^2")]
        public double R50
        {
            get { return _r50; }
            set { _r50 = constrain(value, 50, 200); }
        }

        /// <summary>Flag whether the crop responds to water stress.</summary>
        [Description(" Does the crop respond to water stress?")]
        public bool ConsiderWaterStress { get; set; }

        //-------------------------------------------------------------------------------------------------------------
        // Parameters defining the basic management for this crop, need to be set by a manager
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Date to establish the crop in the field.</summary>
        [JsonIgnore]
        public Nullable<DateTime> EstablishDate { get; set; }

        /// <summary>Stage at which the crop is established in the field.</summary>
        [JsonIgnore]
        public string EstablishStage { get; set; }

        /// <summary>Depth at which the crop/seeds are planted (mm).</summary>
        [JsonIgnore]
        public double PlantingDepth { get; set; }

        /// <summary>Expected yield for the crop, assumed to be fresh weight (t/ha).</summary>
        /// <remarks>The user is expected to consider genotype, location and growing conditions.</remarks>
        [JsonIgnore]
        public double ExpectedYield { get; set; }

        /// <summary>Date to harvest the crop.</summary>
        [JsonIgnore]
        public Nullable<DateTime> HarvestDate { get; set; }

        /// <summary>Thermal time required from establishment to reach harvest stage (oCd).</summary>
        [JsonIgnore]
        public double Tt_EstablishmentToHarvest { get; set; }

        /// <summary>Stage at which the crop is harvested from the field.</summary>
        [JsonIgnore]
        public string HarvestStage { get; set; }

        /// <summary>Proportion of expected yield that is left in the field at harvest.</summary>
        /// <remarks>This may be due to disease, poor quality, or lack of market.</remarks>
        [JsonIgnore]
        public double FieldLoss { get; set; }

        /// <summary>Proportion of stover that is removed from the field at harvest.</summary>
        /// <remarks>This may be used to mimic bailing or some other means of removal.</remarks>
        [JsonIgnore]
        public double ResidueRemoval { get; set; }

        /// <summary>Proportion of residues that are incorporated in the soil by cultivation at harvest.</summary>
        [JsonIgnore]
        public double ResidueIncorporation { get; set; }

        /// <summary>Depth down to which the residues are incorporated into the soil by cultivation.</summary>
        [JsonIgnore]
        public double ResidueIncorporationDepth { get; set; }

        //-------------------------------------------------------------------------------------------------------------
        // Parameters defining the shape of the growth curves for this crop, can to be set by a manager
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Factor to estimate the value of Xo_Biomass, proportion of thermal time where the inflection point of biomass curve happens.</summary>
        [JsonIgnore]
        public double Factor_XoBiomass { get; set; } = 0.5;

        /// <summary>Factor to estimate the value of b_Biomass from Xo_biomass. Controls the slope at the inflection point of biomass curve.</summary>
        [JsonIgnore]
        public double Factor_bBiomass { get; set; } = 0.2;

        /// <summary>Factor to estimate the value of Xo_Cover from Xo_biomass. Controls where the inflection point of canopy cover curve happens.</summary>
        [JsonIgnore]
        public double Factor_XoCover { get; set; } = 0.4;

        /// <summary>Factor to estimate the value of b_Cover from Xo_Cover. Controls the slope at the inflection point of canopy cover curve.</summary>
        [JsonIgnore]
        public double Factor_bCover { get; set; } = 0.2;

        /// <summary>Factor to estimate the value of Xo_Height from Xo_biomass. Controls where the inflection point of crop height curve happens.</summary>
        [JsonIgnore]
        public double Factor_XoHeight { get; set; } = 0.7;

        /// <summary>Factor to estimate the value of b_Height from Xo_Height. Controls the slope at the inflection point of crop height curve.</summary>
        [JsonIgnore]
        public double Factor_bHeight { get; set; } = 0.2;

        //-------------------------------------------------------------------------------------------------------------
        // Outputs from this model
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Thermal time from emergence to maturity (oCd).</summary>
        [JsonIgnore]
        public double Tt_EmergenceToMaturity { get; set; }

        /// <summary>Product biomass removed at harvested.</summary>
        [JsonIgnore]
        public Biomass ProductHarvested { get; private set; }

        /// <summary>Stover biomass removed at harvested.</summary>
        [JsonIgnore]
        public Biomass StoverRemoved { get; private set; }

        /// <summary>Publicises the Nitrogen demand for this crop instance. Occurs when a plant is sown.</summary>
        public event EventHandler<ScrumFertDemandData> SCRUMTotalNDemand;

        /// <summary>Locator supplied by APSIM kernel.</summary>
        public void SetLocator(ILocator locator) => this.locator = locator;

        /// <summary>Calculates the amount of N required to grow the expected yield.</summary>
        /// <param name="yieldExpected">Fresh yield expected at harvest (t/ha)</param>
        /// <returns>The amount of N required by the crop (kg/ha)</returns>
        private double calcTotalNDemand(double yieldExpected)
        {
            double dryMatterContent = 1.0 - MoistureContent;
            yieldExpected = yieldExpected * 100.0; // convert to g/m2
            double totalCropDM = (yieldExpected * dryMatterContent / HarvestIndex) / (1.0 - RootProportion);
            double productDM = totalCropDM * (1.0 - RootProportion) * HarvestIndex;
            double stoverDM = totalCropDM * (1.0 - RootProportion) * (1.0 - HarvestIndex);
            double rootDM = totalCropDM * RootProportion;
            double productN = productDM * ProductHarvestNConc;
            double stoverN = stoverDM * StoverHarvestNConc;
            double rootN = rootDM * RootNConc;
            double cropNDemand = (productN + stoverN + rootN) * 10; // convert to kg/ha
            return cropNDemand;
        }

        /// <summary>The cultivar object representing the current instance of the SCRUM crop.</summary>
        private Cultivar currentCrop = null;

        /// <summary>Thermal time from establishment to harvest (oCd).</summary>
        private double tt_EstablishmentToHarvest { get; set; }

        /// <summary>Names and indices for each predefined crop phenology stage.</summary>
        [JsonIgnore]
        public static Dictionary<string, int> StageNumbers = new Dictionary<string, int>()
        {
            { "Seed", 1 },
            { "Emergence", 2 },
            { "Seedling", 3 },
            { "Vegetative", 4 },
            { "EarlyReproductive", 5 },
            { "MidReproductive", 6 },
            { "LateReproductive", 7 },
            { "Maturity", 8 },
            { "Ripe", 9 }
        };

        /// <summary>Proportion of maximum DM that occurs at each predefined phenology stage.</summary>
        /// <remarks>Computed based on a logistic function.</remarks>
        [JsonIgnore]
        public static Dictionary<string, double> ProportionMaxDM = new Dictionary<string, double>()
        {
            { "Seed", 0.004 },
            { "Emergence", 0.0067 },
            { "Seedling", 0.011 },
            { "Vegetative", 0.5 },
            { "EarlyReproductive", 0.7 },
            { "MidReproductive", 0.86 },
            { "LateReproductive", 0.95 },
            { "Maturity", 0.99325 },
            { "Ripe", 0.99965 }
        };

        /// <summary>Proportion of thermal time accumulated at each phenology stage.</summary>
        /// <remarks>Derived from the proportion of DM using a logistic function re-arranged.</remarks>
        [JsonIgnore]
        public static Dictionary<string, double> ProportionThermalTime = new Dictionary<string, double>()
        {
            { "Seed", -0.0517 },
            { "Emergence", 0.0001 },
            { "Seedling", 0.0501 },
            { "Vegetative", 0.5 },
            { "EarlyReproductive", 0.5847 },
            { "MidReproductive", 0.6815 },
            { "LateReproductive", 0.7944 },
            { "Maturity", 0.9991 },
            { "Ripe", 1.2957 }
        };

        [JsonIgnore]
        private Dictionary<string, string> scrumParams = new Dictionary<string, string>()
        {
            {"InvertedRelativeDM","[SCRUM].Stover.Photosynthesis.UnStressedBiomass.Integral.Ymax.InvertedRelativeDMAtHarvest.FixedValue = " },
            {"ExpectedYield","[Product].ExpectedYield.FixedValue = "},
            {"HarvestIndex","[Product].HarvestIndex.FixedValue = "},
            {"DryMatterContent","[Product].DryMatterContent.FixedValue = "},
            {"RootProportion","[Root].RootProportion.FixedValue = "},
            {"SurfaceKL","[SCRUM].Root.KLModifier.SurfaceKL.FixedValue = " },
            {"ProductNConc","[Product].MaxNConcAtMaturity.FixedValue = "},
            {"StoverNConc","[Stover].MaxNConcAtMaturity.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"SeedlingNConc","[SCRUM].MaxNConcAtSeedling.FixedValue = " },
            {"LegumeFactor","[SCRUM].LegumeFactor.FixedValue = "},
            {"ExtinctCoeff","[Stover].ExtinctionCoefficient.FixedValue = "},
            {"XoCover","[SCRUM].Stover.Cover.Growth.Expansion.Delta.Integral.Xo.FixedValue = " },
            {"bCover","[SCRUM].Stover.Cover.Growth.Expansion.Delta.Integral.b.FixedValue = " },
            {"ACover","[SCRUM].Stover.Cover.Growth.Expansion.Delta.Integral.Ymax.FixedValue =" },
            {"XoBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.Xo.FixedValue = "},
            {"bBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.b.FixedValue = " },
            {"MaxHeight","[Stover].HeightFunction.Ymax.FixedValue = "},
            {"XoHeight","[Stover].HeightFunction.Xo.FixedValue = " },
            {"bHeight","[Stover].HeightFunction.b.FixedValue = " },
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"TtSeed","[Phenology].Seed.Target.FixedValue ="},
            {"TtSeedling","[Phenology].Seedling.Target.FixedValue ="},
            {"TtVegetative","[Phenology].Vegetative.Target.FixedValue ="},
            {"TtEarlyReproductive","[Phenology].EarlyReproductive.Target.FixedValue ="},
            {"TtMidReproductive","[Phenology].MidReproductive.Target.FixedValue ="},
            {"TtLateReproductive","[Phenology].LateReproductive.Target.FixedValue ="},
            {"TtMaturity","[Phenology].Maturity.Target.FixedValue ="},
            {"TtRipe","[Phenology].Ripe.Target.FixedValue ="},
            {"InitialStoverWt","[Stover].InitialWt.FixedValue = "},
            {"InitialProductWt","[Product].InitialWt.Structural.FixedValue = "},
            {"InitialRootWt", "[Root].InitialWt.Structural.FixedValue = " },
            {"InitialCover","[SCRUM].Stover.Cover.InitialCover.FixedValue = " },
            {"BaseTemperature","[Phenology].ThermalTime.XYPairs.X[1] = "},
            {"OptimumTemperature","[Phenology].ThermalTime.XYPairs.X[2] = " },
            {"MaxTemperature","[Phenology].ThermalTime.XYPairs.X[3] = " },
            {"MaxThermalTime","[Phenology].ThermalTime.XYPairs.Y[2] = "},
            {"GSMax","[SCRUM].Stover.Gsmax350 = " },
            {"R50","[SCRUM].Stover.R50 = " },
            {"WaterStressPhoto","[SCRUM].Stover.Photosynthesis.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressCover","[SCRUM].Stover.Cover.Growth.Expansion.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[SCRUM].Root.NUptakeSWFactor.XYPairs.Y[1] = "},
        };

        /// <summary>Flag whether this crop instance has been established.</summary>
        private bool cropEstablished = false;

        /// <summary>Flag whether this crop instance is being terminated.</summary>
        private bool cropTerminating = false;

        /// <summary>Sets the management for this crop instance.</summary>
        private ScrumManagementInstance setManagementInstance(ScrumManagementInstance management = null)
        {
            if (management != null)
            {
                EstablishDate = management.EstablishDate;
                EstablishStage = management.EstablishStage;
                PlantingDepth = management.PlantingDepth;
                HarvestStage = management.HarvestStage;
                ExpectedYield = management.ExpectedYield;
                HarvestDate = management.HarvestDate;
                Tt_EstablishmentToHarvest = management.Tt_EstablishmentToHarvest;
                FieldLoss = management.FieldLoss;
                ResidueRemoval = management.ResidueRemoval;
                ResidueIncorporation = management.ResidueIncorporation;
                ResidueIncorporationDepth = management.ResidueIncorporationDepth;
            }
            else
            {
                management = new ScrumManagementInstance(
                    cropName: CropName,
                    establishDate: (DateTime)EstablishDate,
                    establishStage: EstablishStage,
                    harvestStage: HarvestStage,
                    expectedYield: ExpectedYield,
                    harvestDate: HarvestDate,
                    ttEstablishmentToHarvest: Tt_EstablishmentToHarvest,
                    plantingDepth: PlantingDepth,
                    fieldLoss: FieldLoss,
                    residueRemoval: ResidueRemoval,
                    residueIncorporation: ResidueIncorporation,
                    residueIncorporationDepth: ResidueIncorporationDepth);
            }

            return management;
        }

        /// <summary>Establishes this crop instance (sets SCRUM running).</summary>
        public void Establish(ScrumManagementInstance management)
        {
            var soilCrop = soil.FindDescendant<SoilCrop>(scrum.Name + "Soil");

            // SPRUM sets soil KL to 1 and uses the KL modifier to determine appropriate kl based on root depth
            for (int d = 0; d < soilCrop.KL.Length; d++)
                soilCrop.KL[d] = 1.0;

            management = setManagementInstance(management);
            currentCrop = SetCropCoefficients(management);

            // check some parameters
            if (ExpectedYield == 0.0)
            {
                throw new Exception(Name + " must have an expected yield greater than zero or SCRUM can't simulate it.");
            }

            // compute N requirements
            ScrumFertDemandData fertDemandData = new ScrumFertDemandData(
                name: CropName,
                totalNDemand: management.IsFertilised ? calcTotalNDemand(ExpectedYield) : 0.0,
                establishDate: (DateTime)EstablishDate,
                firstFertdate: (DateTime)management.FirstFertDate,
                harvestDate: (DateTime)HarvestDate,
                tt_EmergenceToMaturity: Tt_EmergenceToMaturity);

            // invoke the SCRUM TotalNDemand event
            if (SCRUMTotalNDemand != null)
            {
                SCRUMTotalNDemand.Invoke(this, fertDemandData);
            }

            // initialise this crop instance in SCRUM
            scrum.Children.Add(currentCrop);
            double cropPopulation = 1.0;
            double rowWidth = 0.0;
            scrum.Sow(cultivar: CropName, population: cropPopulation, depth: PlantingDepth, rowSpacing: rowWidth, maxCover: MaxCover);
            if (management.EstablishStage.ToString() != "Seed")
            {
                phenology.SetToStage(StageNumbers[management.EstablishStage.ToString()]);
            }

            ProductHarvested = new Biomass();
            StoverRemoved = new Biomass();

            cropEstablished = true;
            summary.WriteMessage(this, "Some of the message above is not relevant as SCRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + management.CropName + " is established as " + management.EstablishStage +
                " and will be harvested at " + management.HarvestStage + ". The potential yield is set to " + management.ExpectedYield.ToString("#0.0") +
                " t/ha, with a moisture content of " + MoistureContent.ToString("#0.00") + " g/g and harvest index of " + HarvestIndex.ToString("#0.00") +
                ". It will be harvested on " + HarvestDate?.ToString("dd-MMM-yyyy") + ", requiring " + tt_EstablishmentToHarvest.ToString("#0.0") +
                " oCd from now on.", MessageType.Information);
        }

        /// <summary>Data structure that holds the parameters that define the crop/cultivar being simulated.</summary>
        /// <remarks>These will overwrite those already defined in SCRUM.</remarks>
        public Cultivar SetCropCoefficients(ScrumManagementInstance management)
        {
            Dictionary<string, string> cropParams = new Dictionary<string, string>(scrumParams);

            if (ConsiderWaterStress)
            {
                cropParams["WaterStressPhoto"] += "0.0";
                cropParams["WaterStressCover"] += "0.0";
                cropParams["WaterStressNUptake"] += "0.0";
            }
            else
            {
                cropParams["WaterStressPhoto"] += "1.0";
                cropParams["WaterStressCover"] += "1.0";
                cropParams["WaterStressNUptake"] += "1.0";
            }

            if (MoistureContent > 1.0)
            {
                throw new Exception("Moisture content of " + Name + " ScrumCropInstance has a moisture content > 1.0 g/g.  Value must be less than 1.0");
            }

            double dryMatterContent = 1.0 - MoistureContent;
            cropParams["DryMatterContent"] += dryMatterContent.ToString();
            double yieldExpected = management.ExpectedYield * 100.0; // convert to g/m2
            cropParams["ExpectedYield"] += yieldExpected.ToString();
            cropParams["HarvestIndex"] += HarvestIndex.ToString();
            cropParams["RootNConc"] += RootNConc.ToString();
            cropParams["SeedlingNConc"] += SeedlingNConc.ToString();
            cropParams["MaxRootDepth"] += MaxRootDepth.ToString();
            cropParams["SurfaceKL"] += SurfaceKL.ToString();
            cropParams["MaxHeight"] += MaxHeight.ToString();
            cropParams["RootProportion"] += RootProportion.ToString();
            cropParams["ACover"] += Math.Min(MaxCover,0.97).ToString();
            cropParams["ExtinctCoeff"] += ExtinctionCoefficient.ToString();
            cropParams["LegumeFactor"] += LegumeFactor.ToString();
            cropParams["GSMax"] += GSMax.ToString();
            cropParams["R50"] += R50.ToString();

            // Derive some crop parameters
            double typicalHarvestStageCode = StageNumbers[TypicalHarvestStage];
            double exponent = Math.Exp(-2.0 * (typicalHarvestStageCode - 3.0));
            cropParams["ProductNConc"] += ((ProductHarvestNConc - SeedlingNConc * exponent) / (1.0 - exponent)).ToString();
            cropParams["StoverNConc"] += ((StoverHarvestNConc - SeedlingNConc * exponent) / (1.0 - exponent)).ToString();

            tt_EstablishmentToHarvest = 0.0;
            if (double.IsNaN(management.Tt_EstablishmentToHarvest) || (management.Tt_EstablishmentToHarvest == 0))
            {
                tt_EstablishmentToHarvest = GetThermalTimeSum(management.EstablishDate, (DateTime)management.HarvestDate, BaseTemperature, OptimumTemperature, MaxTemperature);
            }
            else
            {
                tt_EstablishmentToHarvest = management.Tt_EstablishmentToHarvest;
            }

            if ((management.HarvestDate == DateTime.MinValue) || (management.HarvestDate == null))
            {
                HarvestDate = GetHarvestDate(management.EstablishDate, tt_EstablishmentToHarvest, BaseTemperature, OptimumTemperature, MaxTemperature);
            }
            else
            {
                HarvestDate = (DateTime)management.HarvestDate;
            }

            double tt_SowToEmergence = 0;
            if (management.EstablishStage == "Seed")
            {
                tt_SowToEmergence = Tt_SowToEmergence;
            }

            double PropnTt_EstablishmentToHarvest = ProportionThermalTime[management.HarvestStage] - Math.Max(ProportionThermalTime[management.EstablishStage], ProportionThermalTime["Emergence"]);
            Tt_EmergenceToMaturity = (tt_EstablishmentToHarvest - tt_SowToEmergence) / PropnTt_EstablishmentToHarvest;

            double Xo_Biomass = Tt_EmergenceToMaturity * Factor_XoBiomass;
            double b_Biomass = Xo_Biomass * Factor_bBiomass;
            double Xo_cover = Xo_Biomass * Factor_XoCover /(ExtinctionCoefficient * 2);  //Extinction coefficient affects how quickly cover increases relative to phenology.  To capture this affect we adjust Xo_cover for extinction coefficient);
            double b_cover = Xo_cover * Factor_bCover;
            double Xo_height = Xo_Biomass * Factor_XoHeight;
            double b_height = Xo_height * Factor_bHeight;

            cropParams["XoBiomass"] += Xo_Biomass.ToString();
            cropParams["bBiomass"] += b_Biomass.ToString();
            cropParams["XoCover"] += Xo_cover.ToString();
            cropParams["bCover"] += b_cover.ToString();
            cropParams["XoHeight"] += Xo_height.ToString();
            cropParams["bHeight"] += b_height.ToString();

            double tt_PreEstablishment = Math.Max(ProportionThermalTime[management.EstablishStage], ProportionThermalTime["Emergence"]) * Tt_EmergenceToMaturity;
            if (management.EstablishStage != "Seed")
            {
                tt_PreEstablishment += tt_SowToEmergence;
            }

            double irdm = 1.0 / sigmoid.Function(tt_EstablishmentToHarvest + tt_PreEstablishment - tt_SowToEmergence, Xo_Biomass, b_Biomass);
            cropParams["InvertedRelativeDM"] += irdm.ToString();
            cropParams["TtSeed"] += tt_SowToEmergence;
            cropParams["TtSeedling"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["Seedling"] - ProportionThermalTime["Emergence"])).ToString();
            cropParams["TtVegetative"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["Vegetative"] - ProportionThermalTime["Seedling"])).ToString();
            cropParams["TtEarlyReproductive"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["EarlyReproductive"] - ProportionThermalTime["Vegetative"])).ToString();
            cropParams["TtMidReproductive"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["MidReproductive"] - ProportionThermalTime["EarlyReproductive"])).ToString();
            cropParams["TtLateReproductive"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["LateReproductive"] - ProportionThermalTime["MidReproductive"])).ToString();
            cropParams["TtMaturity"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["Maturity"] - ProportionThermalTime["LateReproductive"])).ToString();
            cropParams["TtRipe"] += (Tt_EmergenceToMaturity * (ProportionThermalTime["Ripe"] - ProportionThermalTime["Maturity"])).ToString();

            double abovegroundDM = (yieldExpected * dryMatterContent / HarvestIndex) * irdm;
            double cropTotalDM = abovegroundDM + (abovegroundDM * RootProportion);
            double initialDM = cropTotalDM * ProportionMaxDM[management.EstablishStage];
            cropParams["InitialStoverWt"] += (initialDM * (1 - RootProportion) * (1 - HarvestIndex)).ToString();
            cropParams["InitialProductWt"] += (initialDM * (1 - RootProportion) * HarvestIndex).ToString();
            cropParams["InitialRootWt"] += Math.Max(0.01, initialDM * RootProportion).ToString(); //Need to have some root mass at start or SCRUM throws an error
            cropParams["InitialCover"] += (MaxCover / (1 + Math.Exp(-(tt_PreEstablishment - Xo_cover) / b_cover))).ToString();

            if (OptimumTemperature < BaseTemperature)
                throw new Exception("Optimum Temperature is less than Base.  Check your Base and Optimum temperatures for " + this.CropName);
            if (OptimumTemperature > MaxTemperature)
                throw new Exception("Optimum Temperature is greater than Max.  Check your Max and Optimum temperatures for " + this.CropName);
            cropParams["BaseTemperature"] += BaseTemperature.ToString();
            cropParams["OptimumTemperature"] += OptimumTemperature.ToString();
            cropParams["MaxTemperature"] += MaxTemperature.ToString();
            cropParams["MaxThermalTime"] += (OptimumTemperature - BaseTemperature).ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar CropValues = new Cultivar(Name, commands);
            return CropValues;
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if ((myZone != null) && (clock != null))
            {
                // check whether the crop was terminated yesterday (do some clean up)
                if (cropTerminating)
                {
                    ProductHarvested.Clear();
                    StoverRemoved.Clear();
                    cropTerminating = false;
                }

                // check whether the crop can be harvested/terminated
                if ((clock.Today == HarvestDate) && (cropEstablished == true))
                {
                    HarvestScrumCrop();
                    EndScrumCrop();

                    // incorporate some of the residue to the soil
                    ScrumManagementInstance management = setManagementInstance();
                    if (management.ResidueIncorporation > 0.0)
                    {
                        surfaceOM.Incorporate(management.ResidueIncorporation, management.ResidueIncorporationDepth);
                    }
                }
            }
        }

        /// <summary>Triggers the removal of biomass from various organs.</summary>
        public void HarvestScrumCrop()
        {
            Biomass initialCropBiomass = (Biomass)locator.Get("[SCRUM].Product.Total");
            product.RemoveBiomass(liveToRemove: 1.0 - FieldLoss,
                                  deadToRemove: 1.0 - FieldLoss,
                                  liveToResidue: FieldLoss,
                                  deadToResidue: FieldLoss);
            Biomass finalCropBiomass = (Biomass)locator.Get("[SCRUM].Product.Total");
            ProductHarvested = initialCropBiomass - finalCropBiomass;

            initialCropBiomass = (Biomass)locator.Get("[SCRUM].Stover.Total");
            stover.RemoveBiomass(liveToRemove: ResidueRemoval,
                                 deadToRemove: ResidueRemoval,
                                 liveToResidue: 1.0 - ResidueRemoval,
                                 deadToResidue: 1.0 - ResidueRemoval);
            finalCropBiomass = (Biomass)locator.Get("[SCRUM].Stover.Total");
            StoverRemoved = initialCropBiomass - finalCropBiomass;
            if (Harvesting != null)
            { Harvesting.Invoke(this, new EventArgs()); }

        }

        /// <summary>Performs some tasks to end this instance of SCRUM.</summary>
        public void EndScrumCrop()
        {
            // end this crop instance
            scrum.EndCrop();

            // remove this crop instance from SCRUM and reset parameters
            scrum.Children.Remove(currentCrop);
            cropEstablished = false;
            cropTerminating = true;
        }

        /// <summary>Calculates the accumulated thermal time between two dates.</summary>
        /// <param name="startDate">Start or establishment date</param>
        /// <param name="endDate">End or harvest date</param>
        /// <param name="BaseTemperature">Crop's base Temperature</param>
        /// <param name="OptTemperature">Crop's optimum temperature</param>
        /// <param name="MaxTemperature">Crop's maximum Temperature</param>
        public double GetThermalTimeSum(DateTime startDate, DateTime endDate, double BaseTemperature, double OptTemperature, double MaxTemperature)
        {
            double[] xs = new double[] { BaseTemperature, OptTemperature, MaxTemperature };
            double[] ys = new double[] { 0, OptTemperature - BaseTemperature, 0 };
            XYPairs TtResponse = new XYPairs() { X = xs, Y = ys };

            double TtSum = 0;
            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
            {
                DailyMetDataFromFile TodaysMetData = weather.GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
            }
            return TtSum;
        }

        /// <summary>Calculates the date at end of period that takes to accumulate a given thermal time.</summary>
        /// <param name="startDate">Start or establishment date</param>
        /// <param name="tt_EstablishmentToHarvest">Thermal time from establishment to Harvest</param>
        /// <param name="BaseTemperature">Crop's base Temperature</param>
        /// <param name="OptTemperature">Crop's optimum temperature</param>
        /// <param name="MaxTemperature">Crop's maximum Temperature</param>
        public DateTime GetHarvestDate(DateTime startDate, double tt_EstablishmentToHarvest, double BaseTemperature, double OptTemperature, double MaxTemperature)
        {
            double[] xs = new double[] { BaseTemperature, OptTemperature, MaxTemperature };
            double[] ys = new double[] { 0, OptTemperature - BaseTemperature, 0 };
            XYPairs TtResponse = new XYPairs { X = xs, Y = ys };

            double TtSum = 0;
            DateTime d = startDate;
            while (TtSum < tt_EstablishmentToHarvest)
            {
                DailyMetDataFromFile TodaysMetData = weather.GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
                d = d.AddDays(1);
            }
            return d;
        }

        private double constrain(double value, double min, double max)
        {
            if (value < min)
                ErrorMessage = value.ToString() + " is lower than minimum allowed so has been constrained to the minimum (" + min.ToString() + ")";
            else if (value > max)
                ErrorMessage = value.ToString() + " is higher than maximum allowed so has been constrained to the maximum (" + min.ToString() + ")";
            else
                ErrorMessage = string.Empty;

            return MathUtilities.Bound(value, min, max);
        }

        /// <summary>
        /// Provides an error message to display if something is wrong.
        /// Used by the UserInterface to give a warning of what is wrong
        ///
        /// When the user selects a file using the browse button in the UserInterface
        /// and the file can not be displayed for some reason in the UserInterface.
        /// </summary>
        [JsonIgnore]
        public string ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Data structure that contains information for calculating the N demand for a SCRUM instance.
    /// </summary>
    [Serializable]
    public class ScrumFertDemandData : EventArgs
    {
        /// <summary>The name of the crop being simulated.</summary>
        public string Crop { get; set; }

        /// <summary>The amount of N required to grow to expected yield (g/m2).</summary>
        public double TotalNDemand { get; set; }

        /// <summary>The duration of the no-fertiliser application window (days).</summary>
        public int NonFertWindowDuration { get; set; }

        /// <summary>The duration of the fertiliser application window (days).</summary>
        public int FertWindowDuration { get; set; }

        /// <summary>The date the crop is to be harvested.</summary>
        public DateTime HarvestDate { get; set; }

        /// <summary>The thermal time required by crop from establishment to maturity (oCd).</summary>
        public double Tt_EmergenceToMaturity { get; set; }

        /// <summary>The constructor</summary>
        public ScrumFertDemandData(string name, double totalNDemand, DateTime establishDate,
                                   DateTime firstFertdate, DateTime harvestDate, double tt_EmergenceToMaturity)
        {
            Crop = name;
            TotalNDemand = totalNDemand;
            NonFertWindowDuration = (firstFertdate - establishDate).Days;
            FertWindowDuration = (harvestDate - firstFertdate).Days;
            HarvestDate = harvestDate;
            Tt_EmergenceToMaturity = tt_EmergenceToMaturity;
        }
    }
}
