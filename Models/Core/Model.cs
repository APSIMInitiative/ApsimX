// -----------------------------------------------------------------------
// <copyright file="Model.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using Storage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Base class for all models
    /// </summary>
    [Serializable]
    public class Model : IModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Model" /> class.
        /// </summary>
        public Model()
        {
            this.Name = GetType().Name;
            this.IsHidden = false;
            this.Children = new List<Model>();
            IncludeInDocumentation = true;
            Enabled = true;
        }

        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a list of child models.   
        /// </summary>
        [XmlElement(typeof(Simulation))]
        [XmlElement(typeof(Simulations))]
        [XmlElement(typeof(Zone))]
        [XmlElement(typeof(Model))]
        [XmlElement(typeof(ModelCollectionFromResource))]
        [XmlElement(typeof(Models.Agroforestry.LocalMicroClimate))]
        [XmlElement(typeof(Models.Agroforestry.TreeProxy))]
        [XmlElement(typeof(Models.Agroforestry.AgroforestrySystem))]
        [XmlElement(typeof(Models.Graph.Graph))]
        [XmlElement(typeof(Models.Graph.Series))]
        [XmlElement(typeof(Models.Graph.Regression))]
        [XmlElement(typeof(Models.Graph.EventNamesOnGraph))]
        [XmlElement(typeof(Models.PMF.Plant))]
        [XmlElement(typeof(Models.PMF.OilPalm.OilPalm))]
        [XmlElement(typeof(Models.Soils.Soil))]
        [XmlElement(typeof(Models.Surface.SurfaceOrganicMatter))]
        [XmlElement(typeof(Models.Surface.ResidueTypes))]
        [XmlElement(typeof(Models.SoluteManager))]
        [XmlElement(typeof(Models.AgPasture.Sward))]
        [XmlElement(typeof(Models.AgPasture.PastureSpecies))]
        [XmlElement(typeof(Models.AgPasture.PastureAboveGroundOrgan))]
        [XmlElement(typeof(Models.AgPasture.GenericTissue))]
        [XmlElement(typeof(Clock))]
        [XmlElement(typeof(DataStore))]
        [XmlElement(typeof(Fertiliser))]
        [XmlElement(typeof(Models.PostSimulationTools.Input))]
        [XmlElement(typeof(Models.PostSimulationTools.PredictedObserved))]
        [XmlElement(typeof(Models.PostSimulationTools.TimeSeriesStats))]
        [XmlElement(typeof(Models.PostSimulationTools.Probability))]
        [XmlElement(typeof(Models.PostSimulationTools.ExcelInput))]
        [XmlElement(typeof(Irrigation))]
        [XmlElement(typeof(Manager))]
        [XmlElement(typeof(MicroClimate))]
        [XmlElement(typeof(Operations))]
        [XmlElement(typeof(Models.Report.Report))]
        [XmlElement(typeof(Summary))]
        [XmlElement(typeof(NullSummary))]
        [XmlElement(typeof(Tests))]
        [XmlElement(typeof(Weather))]
        [XmlElement(typeof(ControlledEnvironment))]
        [XmlElement(typeof(Log))]
        [XmlElement(typeof(Models.Factorial.Experiment))]
        [XmlElement(typeof(Models.Factorial.Factors))]
        [XmlElement(typeof(Models.Factorial.Factor))]
        [XmlElement(typeof(Memo))]
        [XmlElement(typeof(Folder))]
        [XmlElement(typeof(Replacements))]
        [XmlElement(typeof(Soils.Evapotranspiration))]
        [XmlElement(typeof(Soils.HydraulicProperties))]
        [XmlElement(typeof(Soils.MRSpline))]
        [XmlElement(typeof(Soils.WEIRDO))]
        [XmlElement(typeof(Soils.Water))]
        [XmlElement(typeof(Soils.SoilCrop))]
        [XmlElement(typeof(Soils.SoilCropOilPalm))]
        [XmlElement(typeof(Soils.SoilWater))]
        [XmlElement(typeof(Soils.SoilNitrogen))]
        [XmlElement(typeof(Soils.SoilOrganicMatter))]
        [XmlElement(typeof(Soils.Analysis))]
        [XmlElement(typeof(Soils.InitialWater))]
        [XmlElement(typeof(Soils.Phosphorus))]
        [XmlElement(typeof(Soils.Swim3))]
        [XmlElement(typeof(Soils.SwimSoluteParameters))]
        [XmlElement(typeof(Soils.LayerStructure))]
        [XmlElement(typeof(Soils.CERESSoilTemperature))]
        [XmlElement(typeof(Soils.SoilTemperature))]
        [XmlElement(typeof(Soils.SoilTemperature2))]
        [XmlElement(typeof(Soils.OutputLayers))]
        [XmlElement(typeof(Soils.Arbitrator.SoilArbitrator))]
        [XmlElement(typeof(Soils.Sample))]
        [XmlElement(typeof(Soils.Nutrient.Nutrient))]
        [XmlElement(typeof(Soils.Nutrient.NutrientPool))]
        [XmlElement(typeof(Soils.Nutrient.CarbonFlow))]
        [XmlElement(typeof(Soils.Nutrient.NFlow))]
        [XmlElement(typeof(Soils.Nutrient.Solute))]
        [XmlElement(typeof(Soils.Nutrient.Chloride))]
        [XmlElement(typeof(WaterModel.CNReductionForCover))]
        [XmlElement(typeof(WaterModel.CNReductionForTillage))]
        [XmlElement(typeof(WaterModel.EvaporationModel))]
        [XmlElement(typeof(WaterModel.LateralFlowModel))]
        [XmlElement(typeof(WaterModel.RunoffModel))]
        [XmlElement(typeof(WaterModel.SaturatedFlowModel))]
        [XmlElement(typeof(WaterModel.SoilModel))]
        [XmlElement(typeof(WaterModel.UnsaturatedFlowModel))]
        [XmlElement(typeof(WaterModel.WaterTableModel))]
        [XmlElement(typeof(Models.Sugarcane))]
        [XmlElement(typeof(Models.GrazPlan.Stock))]
        [XmlElement(typeof(Models.GrazPlan.Supplement))]
        [XmlElement(typeof(Models.PMF.OrganArbitrator))]
        [XmlElement(typeof(Models.PMF.RelativeAllocation))]
        [XmlElement(typeof(Models.PMF.RelativeAllocationSinglePass))]
        [XmlElement(typeof(Models.PMF.PrioritythenRelativeAllocation))]
        [XmlElement(typeof(Models.PMF.PriorityAllocation))]
        [XmlElement(typeof(Models.PMF.Biomass))]
        [XmlElement(typeof(Models.PMF.BiomassDemand))]
        [XmlElement(typeof(Models.PMF.CompositeBiomass))]
        [XmlElement(typeof(Models.PMF.ArrayBiomass))]
        [XmlElement(typeof(Models.PMF.Organs.GenericOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.HIReproductiveOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.Leaf))]
        [XmlElement(typeof(Models.PMF.Organs.LeafCohort))]
        [XmlElement(typeof(Models.PMF.Organs.Leaf.LeafCohortParameters))]
        [XmlElement(typeof(Models.PMF.Organs.Nodule))]
        [XmlElement(typeof(Models.PMF.Organs.ReproductiveOrgan))]
        [XmlElement(typeof(Models.PMF.Organs.Root))]
        [XmlElement(typeof(Models.PMF.Organs.SimpleLeaf))]
        [XmlElement(typeof(Models.PMF.Organs.PerennialLeaf))]
        [XmlElement(typeof(Models.PMF.Phen.BBCH))]
        [XmlElement(typeof(Models.PMF.Phen.Phenology))]
        [XmlElement(typeof(Models.PMF.Phen.EmergingPhase))]
        [XmlElement(typeof(Models.PMF.Phen.EndPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GenericPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GerminatingPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GotoPhase))]
        [XmlElement(typeof(Models.PMF.Phen.LeafAppearancePhase))]
        [XmlElement(typeof(Models.PMF.Phen.LeafDeathPhase))]
        [XmlElement(typeof(Models.PMF.Phen.NodeNumberPhase))]
        [XmlElement(typeof(Models.PMF.Phen.Vernalisation))]
        [XmlElement(typeof(Models.PMF.Phen.ZadokPMF))]
        [XmlElement(typeof(Models.Functions.ArrayFunction))]
        [XmlElement(typeof(Models.Functions.AccumulateFunction))]
        [XmlElement(typeof(Models.Functions.AccumulateByDate))]
        [XmlElement(typeof(Models.Functions.AccumulateByNumericPhase))]  
        [XmlElement(typeof(Models.Functions.MovingAverageFunction))]
        [XmlElement(typeof(Models.Functions.MovingSumFunction))]
        [XmlElement(typeof(Models.Functions.AddFunction))]
        [XmlElement(typeof(Models.Functions.AgeCalculatorFunction))]
        [XmlElement(typeof(Models.Functions.AirTemperatureFunction))]
        [XmlElement(typeof(Models.Functions.BellCurveFunction))]
        [XmlElement(typeof(Models.Functions.Constant))]
        [XmlElement(typeof(Models.Functions.DeltaFunction))]
        [XmlElement(typeof(Models.Functions.DivideFunction))]
        [XmlElement(typeof(Models.Functions.ExponentialFunction))]
        [XmlElement(typeof(Models.Functions.ExpressionFunction))]
        [XmlElement(typeof(Models.Functions.ExternalVariable))]
        [XmlElement(typeof(Models.Functions.HoldFunction))]
        [XmlElement(typeof(Models.Functions.LessThanFunction))]
        [XmlElement(typeof(Models.Functions.LinearInterpolationFunction))]
        [XmlElement(typeof(Models.Functions.BoundFunction))]
        [XmlElement(typeof(Models.Functions.MaximumFunction))]
        [XmlElement(typeof(Models.Functions.MinimumFunction))]
        [XmlElement(typeof(Models.Functions.MultiplyFunction))]
        [XmlElement(typeof(Models.Functions.OnEventFunction))]
        [XmlElement(typeof(Models.Functions.PhaseBasedSwitch))]
        [XmlElement(typeof(Models.Functions.PhaseLookup))]
        [XmlElement(typeof(Models.Functions.PhaseLookupValue))]
        [XmlElement(typeof(Models.Functions.PhotoperiodDeltaFunction))]
        [XmlElement(typeof(Models.Functions.PhotoperiodFunction))]
        [XmlElement(typeof(Models.Functions.PowerFunction))]
        [XmlElement(typeof(Models.Functions.QualitativePPEffect))]
        [XmlElement(typeof(Models.Functions.SigmoidFunction))]
        [XmlElement(typeof(Models.Functions.SoilWaterScale))]
        [XmlElement(typeof(Models.Functions.SoilTemperatureDepthFunction))]
        [XmlElement(typeof(Models.Functions.SoilTemperatureFunction))]
        [XmlElement(typeof(Models.Functions.SoilTemperatureWeightedFunction))]
        [XmlElement(typeof(Models.Functions.SplineInterpolationFunction))]
        [XmlElement(typeof(Models.Functions.StageBasedInterpolation))]
        [XmlElement(typeof(Models.Functions.SubtractFunction))]
        [XmlElement(typeof(Models.Functions.TrackerFunction))]
        [XmlElement(typeof(Models.Functions.VariableReference))]
        [XmlElement(typeof(Models.Functions.WeightedTemperatureFunction))]
        [XmlElement(typeof(Models.Functions.WangEngelTempFunction))]
        [XmlElement(typeof(Models.Functions.XYPairs))]
        [XmlElement(typeof(Models.Functions.SupplyFunctions.CanopyPhotosynthesis))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.AllometricDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.TEWaterDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.InternodeDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.InternodeCohortDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.PartitionFractionDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.PopulationBasedDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.PotentialSizeDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.RelativeGrowthRateDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.FillingRateFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.BerryFillingRateFunction))]
        [XmlElement(typeof(Models.Functions.SupplyFunctions.RUECO2Function))]
        [XmlElement(typeof(Models.Functions.SupplyFunctions.RUEModel))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.StorageDMDemandFunction))]
        [XmlElement(typeof(Models.Functions.DemandFunctions.StorageNDemandFunction))]
        [XmlElement(typeof(Models.PMF.SimpleTree))]
        [XmlElement(typeof(Models.PMF.Cultivar))]
        [XmlElement(typeof(Models.PMF.CultivarFolder))]
        [XmlElement(typeof(Models.PMF.OrganBiomassRemovalType))]
        [XmlElement(typeof(Models.PMF.Library.BiomassRemoval))]
        [XmlElement(typeof(Models.PMF.Struct.Structure))]
        [XmlElement(typeof(Models.PMF.Struct.BudNumberFunction))]
        [XmlElement(typeof(Models.PMF.Struct.HeightFunction))]
        [XmlElement(typeof(Models.PMF.Struct.ApexStandard))]
        [XmlElement(typeof(Models.PMF.Struct.ApexTiller))]
        [XmlElement(typeof(Alias))]
        [XmlElement(typeof(Morris))]
        [XmlElement(typeof(Models.Zones.CircularZone))]
        [XmlElement(typeof(Models.Zones.RectangularZone))]
        [XmlElement(typeof(Models.Zones.StripCropZone))]
        [XmlElement(typeof(Models.Aqua.PondWater))]
        [XmlElement(typeof(Models.Aqua.FoodInPond))]
        [XmlElement(typeof(Models.Aqua.Prawns))]
        [XmlElement(typeof(Models.CLEM.Activities.ActivitiesHolder))]
        [XmlElement(typeof(Models.CLEM.Activities.ActivityFolder))]
        [XmlElement(typeof(Models.CLEM.Activities.ActivityTimerCropHarvest))]
        [XmlElement(typeof(Models.CLEM.Activities.ActivityTimerDateRange))]
        [XmlElement(typeof(Models.CLEM.Activities.ActivityTimerInterval))]
        [XmlElement(typeof(Models.CLEM.Activities.ActivityTimerMonthRange))]
        [XmlElement(typeof(Models.CLEM.Resources.AnimalFoodStore))]
        [XmlElement(typeof(Models.CLEM.Resources.AnimalFoodStoreType))]
        [XmlElement(typeof(Models.CLEM.Resources.AnimalPricing))]
        [XmlElement(typeof(Models.CLEM.Resources.AnimalPriceEntry))]
        [XmlElement(typeof(Models.CLEM.Activities.CropActivityFee))]
        [XmlElement(typeof(Models.CLEM.Activities.CropActivityManageCrop))]
        [XmlElement(typeof(Models.CLEM.Activities.CropActivityManageProduct))]
        [XmlElement(typeof(Models.CLEM.Activities.CropActivityTask))]
        [XmlElement(typeof(Models.CLEM.Resources.Equipment))]
        [XmlElement(typeof(Models.CLEM.Resources.EquipmentType))]
        [XmlElement(typeof(Models.CLEM.FileCrop))]
        [XmlElement(typeof(Models.CLEM.FileGRASP))]
        [XmlElement(typeof(Models.CLEM.FileSQLiteGRASP))]
        [XmlElement(typeof(Models.CLEM.Resources.Finance))]
        [XmlElement(typeof(Models.CLEM.Activities.FinanceActivityCalculateInterest))]
        [XmlElement(typeof(Models.CLEM.Activities.FinanceActivityPayExpense))]
        [XmlElement(typeof(Models.CLEM.Resources.FinanceType))]
        [XmlElement(typeof(Models.CLEM.Groupings.FodderLimitsFilterGroup))]
        [XmlElement(typeof(Models.CLEM.Resources.GrazeFoodStore))]
        [XmlElement(typeof(Models.CLEM.Resources.GrazeFoodStoreType))]
        [XmlElement(typeof(Models.CLEM.Resources.GreenhouseGases))]
        [XmlElement(typeof(Models.CLEM.Resources.GreenhouseGasesType))]
        [XmlElement(typeof(Models.CLEM.Resources.HumanFoodStore))]
        [XmlElement(typeof(Models.CLEM.Resources.HumanFoodStoreType))]
        [XmlElement(typeof(Models.CLEM.Activities.IATCropLand))]
        [XmlElement(typeof(Models.CLEM.Activities.IATGrowCrop))]
        [XmlElement(typeof(Models.CLEM.Activities.IATGrowCropCost))]
        [XmlElement(typeof(Models.CLEM.Activities.IATGrowCropCostAndLabour))]
        [XmlElement(typeof(Models.CLEM.Activities.IATGrowCropLabour))]
        [XmlElement(typeof(Models.CLEM.Resources.Labour))]
        [XmlElement(typeof(Models.CLEM.Activities.LabourActivityOffFarm))]
        [XmlElement(typeof(Models.CLEM.Groupings.LabourFilter))]
        [XmlElement(typeof(Models.CLEM.Groupings.LabourFilterGroup))]
        [XmlElement(typeof(Models.CLEM.Groupings.LabourFilterGroupDefine))]
        [XmlElement(typeof(Models.CLEM.Groupings.LabourFilterGroupSpecified))]
        [XmlElement(typeof(Models.CLEM.Groupings.LabourFilterGroupUnit))]
        [XmlElement(typeof(Models.CLEM.Resources.LabourType))]
        [XmlElement(typeof(Models.CLEM.Resources.Land))]
        [XmlElement(typeof(Models.CLEM.Resources.LandType))]
        [XmlElement(typeof(Models.CLEM.Resources.OtherAnimals))]
        [XmlElement(typeof(Models.CLEM.Activities.OtherAnimalsActivityBreed))]
        [XmlElement(typeof(Models.CLEM.Activities.OtherAnimalsActivityFeed))]
        [XmlElement(typeof(Models.CLEM.Activities.OtherAnimalsActivityGrow))]
        [XmlElement(typeof(Models.CLEM.Groupings.OtherAnimalsFilter))]
        [XmlElement(typeof(Models.CLEM.Groupings.OtherAnimalsFilterGroup))]
        [XmlElement(typeof(Models.CLEM.Resources.OtherAnimalsType))]
        [XmlElement(typeof(Models.CLEM.Resources.OtherAnimalsTypeCohort))]
        [XmlElement(typeof(Models.CLEM.Activities.PastureActivityBurn))]
        [XmlElement(typeof(Models.CLEM.Activities.PastureActivityManage))]
        [XmlElement(typeof(Models.CLEM.Resources.ProductStore))]
        [XmlElement(typeof(Models.CLEM.Resources.ProductStoreType))]
        [XmlElement(typeof(Models.CLEM.Resources.ProductStoreTypeManure))]
        [XmlElement(typeof(Models.CLEM.Activities.Relationship))]
        [XmlElement(typeof(Models.CLEM.Reporting.ReportRuminantHerd))]
        [XmlElement(typeof(Models.CLEM.Activities.ResourceActivitySell))]
        [XmlElement(typeof(Models.CLEM.Reporting.ReportActivitiesPerformed))]
        [XmlElement(typeof(Models.CLEM.Reporting.ReportPasturePoolDetails))]
        [XmlElement(typeof(Models.CLEM.Reporting.ReportResourceBalances))]
        [XmlElement(typeof(Models.CLEM.Reporting.ReportResourceShortfalls))]
        [XmlElement(typeof(Models.CLEM.Resources.ResourcesHolder))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityBuySell))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityBreed))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityCollectManureAll))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityCollectManurePaddock))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityFeed))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityGraze))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityGrow))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityHerdCost))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityManage))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityMilking))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityMuster))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityPredictiveStocking))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityPredictiveStockingENSO))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivitySellDryBreeders))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityTrade))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityWean))]
        [XmlElement(typeof(Models.CLEM.Groupings.RuminantFeedGroup))]
        [XmlElement(typeof(Models.CLEM.Groupings.RuminantFilter))]
        [XmlElement(typeof(Models.CLEM.Groupings.RuminantFilterGroup))]
        [XmlElement(typeof(Models.CLEM.Resources.RuminantHerd))]
        [XmlElement(typeof(Models.CLEM.Resources.RuminantInitialCohorts))]
        [XmlElement(typeof(Models.CLEM.Activities.RuminantActivityFee))]
        [XmlElement(typeof(Models.CLEM.Resources.RuminantType))]
        [XmlElement(typeof(Models.CLEM.Resources.RuminantTypeCohort))]
        [XmlElement(typeof(Models.CLEM.SummariseRuminantHerd))]
        [XmlElement(typeof(Models.CLEM.Transmutation))]
        [XmlElement(typeof(Models.CLEM.TransmutationCost))]
        [XmlElement(typeof(Models.CLEM.Activities.TruckingSettings))]
        [XmlElement(typeof(Models.CLEM.Resources.WaterStore))]
        [XmlElement(typeof(Models.CLEM.Resources.WaterType))]
        [XmlElement(typeof(Models.CLEM.ZoneCLEM))]
		[XmlElement(typeof(Models.LifeCycle.LifeCycle))]
        [XmlElement(typeof(Models.LifeCycle.LifeStage))]
        [XmlElement(typeof(Models.LifeCycle.LifeStageProcess))]
        [XmlElement(typeof(Models.LifeCycle.LifeStageReproductionProcess))]
        [XmlElement(typeof(Models.LifeCycle.LifeStageImmigrationProcess))]
        [XmlElement(typeof(Map))]
        public List<Model> Children { get; set; }

        /// <summary>
        /// Gets or sets the parent of the model.
        /// </summary>
        [XmlIgnore]
        public IModel Parent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        [XmlIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in the auto-doc documentation.
        /// </summary>
        public bool IncludeInDocumentation { get; set; }

        /// <summary>
        /// A cleanup routine, in which we clear our child list recursively
        /// </summary>
        public void ClearChildLists()
        {
            foreach (Model child in Children)
                child.ClearChildLists();
            Children.Clear();
        }

        /// <summary>
        /// Gets or sets whether the model is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Return the current APSIM version number.
        /// </summary>
        public string ApsimVersion
        {
            get
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                FileInfo info = new FileInfo(Assembly.GetExecutingAssembly().Location);
                string buildDate = info.LastWriteTime.ToString("yyyy-MM-dd");
                return "Version " + version + ", built " + buildDate;
            }
        }

        /// <summary>
        /// Controls whether the model can be modified.
        /// </summary>
        public bool ReadOnly { get; set; }
    }
}
