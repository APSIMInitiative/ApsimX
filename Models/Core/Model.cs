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
        [XmlElement(typeof(Models.SurfaceOM.SurfaceOrganicMatter))]
        [XmlElement(typeof(Models.SoluteManager))]
        [XmlElement(typeof(Models.AgPasture.Sward))]
        [XmlElement(typeof(Models.AgPasture.PastureSpecies))]
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
        [XmlElement(typeof(Models.PMF.Organs.RootSWIM))]
        [XmlElement(typeof(Models.PMF.Organs.SimpleLeaf))]
        [XmlElement(typeof(Models.PMF.Organs.PerennialLeaf))]
        [XmlElement(typeof(Models.PMF.Phen.Phenology))]
        [XmlElement(typeof(Models.PMF.Phen.EmergingPhase))]
        [XmlElement(typeof(Models.PMF.Phen.EndPhase))]
        [XmlElement(typeof(Models.PMF.Phen.ExpressionPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GenericPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GerminatingPhase))]
        [XmlElement(typeof(Models.PMF.Phen.GotoPhase))]
        [XmlElement(typeof(Models.PMF.Phen.LeafAppearancePhase))]
        [XmlElement(typeof(Models.PMF.Phen.LeafDeathPhase))]
        [XmlElement(typeof(Models.PMF.Phen.MolecularPhenology))]
        [XmlElement(typeof(Models.PMF.Phen.NodeNumberPhase))]
        [XmlElement(typeof(Models.PMF.Phen.PhaseSetFunction))]
        [XmlElement(typeof(Models.PMF.Phen.Vernalisation))]
        [XmlElement(typeof(Models.PMF.Phen.VernalisationC))]
        [XmlElement(typeof(Models.PMF.Phen.VernalisationCW))]
        [XmlElement(typeof(Models.PMF.Phen.QualitativePPEffect))]
        [XmlElement(typeof(Models.PMF.Phen.ZadokPMF))]
        [XmlElement(typeof(Models.PMF.Phen.BuddingPhase))]
        [XmlElement(typeof(Models.PMF.Functions.ArrayFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AccumulateFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MovingAverageFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MovingSumFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AddFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AgeCalculatorFunction))]
        [XmlElement(typeof(Models.PMF.Functions.AirTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.BellCurveFunction))]
        [XmlElement(typeof(Models.PMF.Functions.Constant))]
        [XmlElement(typeof(Models.PMF.Functions.DeltaFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DivideFunction))]
        [XmlElement(typeof(Models.PMF.Functions.ExponentialFunction))]
        [XmlElement(typeof(Models.PMF.Functions.ExpressionFunction))]
        [XmlElement(typeof(Models.PMF.Functions.ExternalVariable))]
        [XmlElement(typeof(Models.PMF.Functions.HoldFunction))]
        [XmlElement(typeof(Models.PMF.Functions.InPhaseTtFunction))]
        [XmlElement(typeof(Models.PMF.Functions.LessThanFunction))]
        [XmlElement(typeof(Models.PMF.Functions.LinearInterpolationFunction))]
        [XmlElement(typeof(Models.PMF.Functions.BoundFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MaximumFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MinimumFunction))]
        [XmlElement(typeof(Models.PMF.Functions.MultiplyFunction))]
        [XmlElement(typeof(Models.PMF.Functions.OnEventFunction))]
        [XmlElement(typeof(Models.PMF.Functions.PhaseBasedSwitch))]
        [XmlElement(typeof(Models.PMF.Functions.PhaseLookup))]
        [XmlElement(typeof(Models.PMF.Functions.PhaseLookupValue))]
        [XmlElement(typeof(Models.PMF.Functions.PhotoperiodDeltaFunction))]
        [XmlElement(typeof(Models.PMF.Functions.PhotoperiodFunction))]
        [XmlElement(typeof(Models.PMF.Functions.PowerFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SigmoidFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SoilWaterScale))]
        [XmlElement(typeof(Models.PMF.Functions.SoilTemperatureDepthFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SoilTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SoilTemperatureWeightedFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SplineInterpolationFunction))]
        [XmlElement(typeof(Models.PMF.Functions.StageBasedInterpolation))]
        [XmlElement(typeof(Models.PMF.Functions.SubtractFunction))]
        [XmlElement(typeof(Models.PMF.Functions.TrackerFunction))]
        [XmlElement(typeof(Models.PMF.Functions.VariableReference))]
        [XmlElement(typeof(Models.PMF.Functions.WeightedTemperatureFunction))]
        [XmlElement(typeof(Models.PMF.Functions.WangEngelTempFunction))]
        [XmlElement(typeof(Models.PMF.Functions.XYPairs))]
        [XmlElement(typeof(Models.PMF.Functions.SupplyFunctions.CanopyPhotosynthesis))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.AllometricDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.TEWaterDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.InternodeDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.InternodeCohortDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.PartitionFractionDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.PopulationBasedDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.PotentialSizeDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.RelativeGrowthRateDemandFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.FillingRateFunction))]
        [XmlElement(typeof(Models.PMF.Functions.DemandFunctions.BerryFillingRateFunction))]
        [XmlElement(typeof(Models.PMF.Functions.SupplyFunctions.Photosynthesis))]
        [XmlElement(typeof(Models.PMF.Functions.SupplyFunctions.RUECO2Function))]
        [XmlElement(typeof(Models.PMF.Functions.SupplyFunctions.RUEModel))]
        [XmlElement(typeof(Models.PMF.SimpleTree))]
        [XmlElement(typeof(Models.PMF.Cultivar))]
        [XmlElement(typeof(Models.PMF.CultivarFolder))]
        [XmlElement(typeof(Models.PMF.OrganBiomassRemovalType))]
        [XmlElement(typeof(Models.PMF.Library.BiomassRemoval))]
        [XmlElement(typeof(Models.PMF.Struct.Structure))]
        [XmlElement(typeof(Models.PMF.Struct.HeightFunction))]
        [XmlElement(typeof(Models.PMF.Struct.ApexStandard))]
        [XmlElement(typeof(Models.PMF.Struct.ApexTiller))]
        [XmlElement(typeof(Alias))]
        [XmlElement(typeof(Models.Zones.CircularZone))]
        [XmlElement(typeof(Models.Zones.RectangularZone))]
        [XmlElement(typeof(Models.Aqua.PondWater))]
        [XmlElement(typeof(Models.Aqua.FoodInPond))]
        [XmlElement(typeof(Models.Aqua.Prawns))]
		[XmlElement(typeof(Models.WholeFarm.Activities.ActivitiesHolder))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.ActivityLabourRequirement))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.ActivityLabourRequirementGroup))]
		[XmlElement(typeof(Models.WholeFarm.Activities.ActivityFolder))]
		[XmlElement(typeof(Models.WholeFarm.Resources.AnimalFoodStore))]
        [XmlElement(typeof(Models.WholeFarm.Activities.ControlledMatingSettings))]
		[XmlElement(typeof(Models.WholeFarm.Resources.AnimalFoodStoreType))]
		[XmlElement(typeof(Models.WholeFarm.Activities.FinanceActivityCalculateInterest))]
		[XmlElement(typeof(Models.WholeFarm.Activities.FinanceActivityPayOverhead))]
		[XmlElement(typeof(Models.WholeFarm.Resources.Finance))]
		[XmlElement(typeof(Models.WholeFarm.Resources.FinanceType))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.FodderLimitsFilterGroup))]
		[XmlElement(typeof(Models.WholeFarm.Resources.GrazeFoodStore))]
		[XmlElement(typeof(Models.WholeFarm.Resources.GrazeFoodStoreType))]
		[XmlElement(typeof(Models.WholeFarm.Resources.HumanFoodStore))]
        [XmlElement(typeof(Models.WholeFarm.Resources.HumanFoodStoreType))]
		[XmlElement(typeof(Models.WholeFarm.Activities.LabourActivityOffFarm))]
		[XmlElement(typeof(Models.WholeFarm.Resources.Labour))]
        [XmlElement(typeof(Models.WholeFarm.Resources.LabourType))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.LabourFilter))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.LabourFilterGroup))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.LabourDefineFilterGroup))]
		[XmlElement(typeof(Models.WholeFarm.LabourOffFarmFilterGroup))]
		[XmlElement(typeof(Models.WholeFarm.LabourHired))]
        [XmlElement(typeof(Models.WholeFarm.LabourHiredType))]
        [XmlElement(typeof(Models.WholeFarm.Resources.Land))]
        [XmlElement(typeof(Models.WholeFarm.Resources.LandType))]
		[XmlElement(typeof(Models.WholeFarm.Resources.OtherAnimals))]
		[XmlElement(typeof(Models.WholeFarm.Activities.OtherAnimalsActivityBreed))]
		[XmlElement(typeof(Models.WholeFarm.Activities.OtherAnimalsActivityFeed))]
		[XmlElement(typeof(Models.WholeFarm.Activities.OtherAnimalsActivityGrow))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.OtherAnimalsFilter))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.OtherAnimalsFilterGroup))]
		[XmlElement(typeof(Models.WholeFarm.Resources.OtherAnimalsType))]
		[XmlElement(typeof(Models.WholeFarm.Resources.OtherAnimalsTypeCohort))]
		[XmlElement(typeof(Models.WholeFarm.Activities.PastureActivityManage))]
		[XmlElement(typeof(Models.WholeFarm.Resources.ResourcesHolder))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityBreed))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityFeed))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityGraze))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityGrow))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityManage))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityMuster))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityPredictiveStocking))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityBuySell))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivitySellDryBreeders))]
		[XmlElement(typeof(Models.WholeFarm.Activities.RuminantActivityTrade))]
		[XmlElement(typeof(Models.WholeFarm.RuminantArbitratorFeed))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.RuminantFilter))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.RuminantFilterGroup))]
		[XmlElement(typeof(Models.WholeFarm.Groupings.RuminantMusterGroup))]
		[XmlElement(typeof(Models.WholeFarm.Resources.RuminantPricing))]
		[XmlElement(typeof(Models.WholeFarm.Resources.RuminantPriceEntry))]
		[XmlElement(typeof(Models.WholeFarm.Resources.RuminantHerd))]
        [XmlElement(typeof(Models.WholeFarm.Resources.RuminantType))]
        [XmlElement(typeof(Models.WholeFarm.Resources.RuminantTypeCohort))]
		[XmlElement(typeof(Models.WholeFarm.SummariseRuminantHerd))]
		[XmlElement(typeof(Models.WholeFarm.Transmutation))]
		[XmlElement(typeof(Models.WholeFarm.TransmutationCost))]
		[XmlElement(typeof(Models.WholeFarm.WholeFarm))]
		[XmlElement(typeof(Models.Lifecycle.Lifecycle))]
        [XmlElement(typeof(Models.Lifecycle.Lifestage))]
        [XmlElement(typeof(Models.Lifecycle.LifestageProcess))]
        [XmlElement(typeof(Models.Lifecycle.LifestageReproductionProcess))]
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

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public virtual void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModel(this, tags, headingLevel, indent, true);

                //foreach (IModel model in Children)
                //    model.Document(tags, headingLevel+1, indent);
            }
        }

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
    }
}
