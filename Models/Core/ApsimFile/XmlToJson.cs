

namespace Models.Core.ApsimFile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// XML to JSON converter
    /// </summary>
    public class XmlToJson
    {
        private static string[] knownModelNames = new string[]
        {
            "Simulation",
            "Simulations",
            "Zone",
            "Model",
            "ModelCollectionFromResource",
            "Models.Agroforestry.LocalMicroClimate",
            "Models.Agroforestry.TreeProxy",
            "Models.Agroforestry.AgroforestrySystem",
            "Models.Graph.Graph",
            "Models.Graph.Series",
            "Models.Graph.Regression",
            "Models.Graph.EventNamesOnGraph",
            "Models.PMF.Plant",
            "Models.PMF.OilPalm.OilPalm",
            "Models.Soils.Soil",
            "Models.Surface.SurfaceOrganicMatter",
            "Models.Surface.ResidueTypes",
            "Models.SoluteManager",
            "Models.AgPasture.Sward",
            "Models.AgPasture.PastureSpecies",
            "Models.AgPasture.PastureAboveGroundOrgan",
            "Models.AgPasture.GenericTissue",
            "Clock",
            "DataStore",
            "Fertiliser",
            "Models.PostSimulationTools.Input",
            "Models.PostSimulationTools.PredictedObserved",
            "Models.PostSimulationTools.TimeSeriesStats",
            "Models.PostSimulationTools.Probability",
            "Models.PostSimulationTools.ExcelInput",
            "Irrigation",
            "Manager",
            "MicroClimate",
            "Operations",
            "Models.Report.Report",
            "Summary",
            "Tests",
            "Weather",
            "ControlledEnvironment",
            "Log",
            "Models.Factorial.Experiment",
            "Models.Factorial.Factors",
            "Models.Factorial.Factor",
            "Memo",
            "Folder",
            "Replacements",
            "Soils.Evapotranspiration",
            "Soils.HydraulicProperties",
            "Soils.MRSpline",
            "Soils.WEIRDO",
            "Soils.Water",
            "Soils.SoilCrop",
            "Soils.SoilCropOilPalm",
            "Soils.SoilWater",
            "Soils.SoilNitrogen",
            "Soils.SoilOrganicMatter",
            "Soils.Analysis",
            "Soils.InitialWater",
            "Soils.Phosphorus",
            "Soils.Swim3",
            "Soils.SwimSoluteParameters",
            "Soils.LayerStructure",
            "Soils.CERESSoilTemperature",
            "Soils.SoilTemperature",
            "Soils.SoilTemperature2",
            "Soils.OutputLayers",
            "Soils.Arbitrator.SoilArbitrator",
            "Soils.Sample",
            "Soils.Nutrient.Nutrient",
            "Soils.Nutrient.NutrientPool",
            "Soils.Nutrient.CarbonFlow",
            "Soils.Nutrient.NFlow",
            "Soils.Nutrient.Solute",
            "Soils.Nutrient.Chloride",
            "WaterModel.CNReductionForCover",
            "WaterModel.CNReductionForTillage",
            "WaterModel.EvaporationModel",
            "WaterModel.LateralFlowModel",
            "WaterModel.RunoffModel",
            "WaterModel.SaturatedFlowModel",
            "WaterModel.SoilModel",
            "WaterModel.UnsaturatedFlowModel",
            "WaterModel.WaterTableModel",
            "Models.Sugarcane",
            "Models.GrazPlan.Stock",
            "Models.GrazPlan.Supplement",
            "Models.PMF.OrganArbitrator",
            "Models.PMF.RelativeAllocation",
            "Models.PMF.RelativeAllocationSinglePass",
            "Models.PMF.PrioritythenRelativeAllocation",
            "Models.PMF.PriorityAllocation",
            "Models.PMF.Biomass",
            "Models.PMF.BiomassDemand",
            "Models.PMF.CompositeBiomass",
            "Models.PMF.ArrayBiomass",
            "Models.PMF.Organs.GenericOrgan",
            "Models.PMF.Organs.HIReproductiveOrgan",
            "Models.PMF.Organs.Leaf",
            "Models.PMF.Organs.LeafCohort",
            "Models.PMF.Organs.Leaf.LeafCohortParameters",
            "Models.PMF.Organs.Nodule",
            "Models.PMF.Organs.ReproductiveOrgan",
            "Models.PMF.Organs.Root",
            "Models.PMF.Organs.SimpleLeaf",
            "Models.PMF.Organs.PerennialLeaf",
            "Models.PMF.Phen.BBCH",
            "Models.PMF.Phen.Phenology",
            "Models.PMF.Phen.EmergingPhase",
            "Models.PMF.Phen.EndPhase",
            "Models.PMF.Phen.GenericPhase",
            "Models.PMF.Phen.GerminatingPhase",
            "Models.PMF.Phen.GotoPhase",
            "Models.PMF.Phen.LeafAppearancePhase",
            "Models.PMF.Phen.LeafDeathPhase",
            "Models.PMF.Phen.NodeNumberPhase",
            "Models.PMF.Phen.Vernalisation",
            "Models.PMF.Phen.ZadokPMF",
            "Models.Functions.ArrayFunction",
            "Models.Functions.AccumulateFunction",
            "Models.Functions.AccumulateByDate",
            "Models.Functions.AccumulateByNumericPhase"  ,
            "Models.Functions.MovingAverageFunction",
            "Models.Functions.MovingSumFunction",
            "Models.Functions.AddFunction",
            "Models.Functions.AgeCalculatorFunction",
            "Models.Functions.AirTemperatureFunction",
            "Models.Functions.BellCurveFunction",
            "Models.Functions.Constant",
            "Models.Functions.DeltaFunction",
            "Models.Functions.DivideFunction",
            "Models.Functions.ExponentialFunction",
            "Models.Functions.ExpressionFunction",
            "Models.Functions.ExternalVariable",
            "Models.Functions.HoldFunction",
            "Models.Functions.LessThanFunction",
            "Models.Functions.LinearInterpolationFunction",
            "Models.Functions.BoundFunction",
            "Models.Functions.MaximumFunction",
            "Models.Functions.MinimumFunction",
            "Models.Functions.MultiplyFunction",
            "Models.Functions.OnEventFunction",
            "Models.Functions.PhaseBasedSwitch",
            "Models.Functions.PhaseLookup",
            "Models.Functions.PhaseLookupValue",
            "Models.Functions.PhotoperiodDeltaFunction",
            "Models.Functions.PhotoperiodFunction",
            "Models.Functions.PowerFunction",
            "Models.Functions.QualitativePPEffect",
            "Models.Functions.SigmoidFunction",
            "Models.Functions.SoilWaterScale",
            "Models.Functions.SoilTemperatureDepthFunction",
            "Models.Functions.SoilTemperatureFunction",
            "Models.Functions.SoilTemperatureWeightedFunction",
            "Models.Functions.SplineInterpolationFunction",
            "Models.Functions.StageBasedInterpolation",
            "Models.Functions.SubtractFunction",
            "Models.Functions.TrackerFunction",
            "Models.Functions.VariableReference",
            "Models.Functions.WeightedTemperatureFunction",
            "Models.Functions.WangEngelTempFunction",
            "Models.Functions.XYPairs",
            "Models.Functions.SupplyFunctions.CanopyPhotosynthesis",
            "Models.Functions.DemandFunctions.AllometricDemandFunction",
            "Models.Functions.DemandFunctions.TEWaterDemandFunction",
            "Models.Functions.DemandFunctions.InternodeDemandFunction",
            "Models.Functions.DemandFunctions.InternodeCohortDemandFunction",
            "Models.Functions.DemandFunctions.PartitionFractionDemandFunction",
            "Models.Functions.DemandFunctions.PopulationBasedDemandFunction",
            "Models.Functions.DemandFunctions.PotentialSizeDemandFunction",
            "Models.Functions.DemandFunctions.RelativeGrowthRateDemandFunction",
            "Models.Functions.DemandFunctions.FillingRateFunction",
            "Models.Functions.DemandFunctions.BerryFillingRateFunction",
            "Models.Functions.SupplyFunctions.RUECO2Function",
            "Models.Functions.SupplyFunctions.RUEModel",
            "Models.Functions.DemandFunctions.StorageDMDemandFunction",
            "Models.Functions.DemandFunctions.StorageNDemandFunction",
            "Models.PMF.SimpleTree",
            "Models.PMF.Cultivar",
            "Models.PMF.CultivarFolder",
            "Models.PMF.OrganBiomassRemovalType",
            "Models.PMF.Library.BiomassRemoval",
            "Models.PMF.Struct.Structure",
            "Models.PMF.Struct.BudNumberFunction",
            "Models.PMF.Struct.HeightFunction",
            "Models.PMF.Struct.ApexStandard",
            "Models.PMF.Struct.ApexTiller",
            "Alias",
            "Morris",
            "Models.Zones.CircularZone",
            "Models.Zones.RectangularZone",
            "Models.Zones.StripCropZone",
            "Models.Aqua.PondWater",
            "Models.Aqua.FoodInPond",
            "Models.Aqua.Prawns",
            "Models.CLEM.Activities.ActivitiesHolder",
            "Models.CLEM.Activities.ActivityFolder",
            "Models.CLEM.Activities.ActivityTimerCropHarvest",
            "Models.CLEM.Activities.ActivityTimerDateRange",
            "Models.CLEM.Activities.ActivityTimerInterval",
            "Models.CLEM.Activities.ActivityTimerMonthRange",
            "Models.CLEM.Resources.AnimalFoodStore",
            "Models.CLEM.Resources.AnimalFoodStoreType",
            "Models.CLEM.Resources.AnimalPricing",
            "Models.CLEM.Resources.AnimalPriceEntry",
            "Models.CLEM.Activities.CropActivityFee",
            "Models.CLEM.Activities.CropActivityManageCrop",
            "Models.CLEM.Activities.CropActivityManageProduct",
            "Models.CLEM.Activities.CropActivityTask",
            "Models.CLEM.Resources.Equipment",
            "Models.CLEM.Resources.EquipmentType",
            "Models.CLEM.FileCrop",
            "Models.CLEM.FileGRASP",
            "Models.CLEM.FileSQLiteGRASP",
            "Models.CLEM.Resources.Finance",
            "Models.CLEM.Activities.FinanceActivityCalculateInterest",
            "Models.CLEM.Activities.FinanceActivityPayExpense",
            "Models.CLEM.Resources.FinanceType",
            "Models.CLEM.Groupings.FodderLimitsFilterGroup",
            "Models.CLEM.Resources.GrazeFoodStore",
            "Models.CLEM.Resources.GrazeFoodStoreType",
            "Models.CLEM.Resources.GreenhouseGases",
            "Models.CLEM.Resources.GreenhouseGasesType",
            "Models.CLEM.Resources.HumanFoodStore",
            "Models.CLEM.Resources.HumanFoodStoreType",
            "Models.CLEM.Activities.IATCropLand",
            "Models.CLEM.Activities.IATGrowCrop",
            "Models.CLEM.Activities.IATGrowCropCost",
            "Models.CLEM.Activities.IATGrowCropCostAndLabour",
            "Models.CLEM.Activities.IATGrowCropLabour",
            "Models.CLEM.Resources.Labour",
            "Models.CLEM.Activities.LabourActivityOffFarm",
            "Models.CLEM.Groupings.LabourFilter",
            "Models.CLEM.Groupings.LabourFilterGroup",
            "Models.CLEM.Groupings.LabourFilterGroupDefine",
            "Models.CLEM.Groupings.LabourFilterGroupSpecified",
            "Models.CLEM.Groupings.LabourFilterGroupUnit",
            "Models.CLEM.Resources.LabourType",
            "Models.CLEM.Resources.Land",
            "Models.CLEM.Resources.LandType",
            "Models.CLEM.Resources.OtherAnimals",
            "Models.CLEM.Activities.OtherAnimalsActivityBreed",
            "Models.CLEM.Activities.OtherAnimalsActivityFeed",
            "Models.CLEM.Activities.OtherAnimalsActivityGrow",
            "Models.CLEM.Groupings.OtherAnimalsFilter",
            "Models.CLEM.Groupings.OtherAnimalsFilterGroup",
            "Models.CLEM.Resources.OtherAnimalsType",
            "Models.CLEM.Resources.OtherAnimalsTypeCohort",
            "Models.CLEM.Activities.PastureActivityBurn",
            "Models.CLEM.Activities.PastureActivityManage",
            "Models.CLEM.Resources.ProductStore",
            "Models.CLEM.Resources.ProductStoreType",
            "Models.CLEM.Resources.ProductStoreTypeManure",
            "Models.CLEM.Activities.Relationship",
            "Models.CLEM.Reporting.ReportRuminantHerd",
            "Models.CLEM.Activities.ResourceActivitySell",
            "Models.CLEM.Reporting.ReportActivitiesPerformed",
            "Models.CLEM.Reporting.ReportPasturePoolDetails",
            "Models.CLEM.Reporting.ReportResourceBalances",
            "Models.CLEM.Reporting.ReportResourceShortfalls",
            "Models.CLEM.Resources.ResourcesHolder",
            "Models.CLEM.Activities.RuminantActivityBuySell",
            "Models.CLEM.Activities.RuminantActivityBreed",
            "Models.CLEM.Activities.RuminantActivityCollectManureAll",
            "Models.CLEM.Activities.RuminantActivityCollectManurePaddock",
            "Models.CLEM.Activities.RuminantActivityFeed",
            "Models.CLEM.Activities.RuminantActivityGraze",
            "Models.CLEM.Activities.RuminantActivityGrow",
            "Models.CLEM.Activities.RuminantActivityHerdCost",
            "Models.CLEM.Activities.RuminantActivityManage",
            "Models.CLEM.Activities.RuminantActivityMilking",
            "Models.CLEM.Activities.RuminantActivityMuster",
            "Models.CLEM.Activities.RuminantActivityPredictiveStocking",
            "Models.CLEM.Activities.RuminantActivityPredictiveStockingENSO",
            "Models.CLEM.Activities.RuminantActivitySellDryBreeders",
            "Models.CLEM.Activities.RuminantActivityTrade",
            "Models.CLEM.Activities.RuminantActivityWean",
            "Models.CLEM.Groupings.RuminantFeedGroup",
            "Models.CLEM.Groupings.RuminantFilter",
            "Models.CLEM.Groupings.RuminantFilterGroup",
            "Models.CLEM.Resources.RuminantHerd",
            "Models.CLEM.Resources.RuminantInitialCohorts",
            "Models.CLEM.Activities.RuminantActivityFee",
            "Models.CLEM.Resources.RuminantType",
            "Models.CLEM.Resources.RuminantTypeCohort",
            "Models.CLEM.SummariseRuminantHerd",
            "Models.CLEM.Transmutation",
            "Models.CLEM.TransmutationCost",
            "Models.CLEM.Activities.TruckingSettings",
            "Models.CLEM.Resources.WaterStore",
            "Models.CLEM.Resources.WaterType",
            "Models.CLEM.ZoneCLEM",
            "Models.LifeCycle.LifeCycle",
            "Models.LifeCycle.LifeStage",
            "Models.LifeCycle.LifeStageProcess",
            "Models.LifeCycle.LifeStageReproductionProcess",
            "Models.LifeCycle.LifeStageImmigrationProcess",
            "Map"
        };

        /// <summary>
        /// Convert APSIM Next Generation xml to json.
        /// </summary>
        /// <param name="xml">XML string to convert.</param>
        /// <returns>The equivalent JSON.</returns>
        public static string Convert(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string json = JsonConvert.SerializeXmlNode(doc);
            JObject root = JObject.Parse(json);

            JObject newRoot = AddModelsToChildren(root);

            json = newRoot.ToString();

            return json;
        }

        private static JObject AddModelsToChildren(JObject root)
        {
            JObject newRoot = new JObject();
            JArray children = new JArray();
            newRoot["Children"] = children;

            JToken child = root.First;
            while (child != null)
            {
                if (child.Path == "?xml")
                {
                    // do nothing
                }
                else if (GetModelTypeName(child.Path) == null)
                {
                    WriteProperty(child as JProperty, newRoot);

                }
                else
                {
                    foreach (JToken simulationsChild in child.Children())
                    {
                        JObject simulationsChildAsObject = simulationsChild as JObject;
                        foreach (JProperty property in simulationsChildAsObject.Children())
                        {
                            if (GetModelTypeName(property.Name) != null)
                            {
                                var newChild = AddModelsToChildren(property.Value as JObject);
                                children.Add(newChild);
                            }
                            else
                                WriteProperty(property, newRoot);
                        }
                    }
                }

                child = child.Next;
            }

            return newRoot;
        }

        private static string GetModelTypeName(string modelNameToFind)
        {
            string[] modelWords = modelNameToFind.Split(".".ToCharArray());
            string m = modelWords[modelWords.Length - 1];
            foreach (var modelName in knownModelNames)
            {
                string[] words = modelName.Split(".".ToCharArray());
                if (m == words[words.Length - 1])
                    return modelName;
            }
            return null;
        }

        private static void WriteProperty(JProperty property, JObject toObject)
        {
            string propertyName = property.Name;
            if (propertyName == "@Version")
                propertyName = "Version";
            if (!propertyName.StartsWith("@"))
            {
                JToken valueToken = property.Value;
                if (valueToken.HasValues)
                {
                    if (property.First.First.First is JValue)
                    {
                        JValue value = property.First.First.First as JValue;
                        string elementType = (value.Parent as JProperty).Name;
                        JArray newArray = new JArray();
                        newArray.Add(new JValue(value.ToString()));
                        toObject[propertyName] = newArray;
                    }
                    else if (property.First.First.First is JArray)
                    {
                        JArray array = property.First.First.First as JArray;

                        string elementType = (array.Parent as JProperty).Name;
                        JArray newArray = new JArray();
                        foreach (var value in array.Values())
                        {
                            if (elementType == "string")
                                newArray.Add(new JValue(value.ToString()));
                            else if (elementType == "double")
                                newArray.Add(new JValue(double.Parse(value.ToString())));
                        }
                        toObject[propertyName] = newArray;
                    }
                }
                else
                {
                    string value = valueToken.Value<string>();
                    int intValue;
                    double doubleValue;
                    bool boolValue;
                    DateTime dateValue;
                    if (int.TryParse(value, out intValue))
                        toObject[propertyName] = intValue;
                    else if (double.TryParse(value, out doubleValue))
                        toObject[propertyName] = doubleValue;
                    else if (bool.TryParse(value, out boolValue))
                        toObject[propertyName] = boolValue;
                    else if (DateTime.TryParseExact(value, "MM/dd/yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateValue))
                        toObject[propertyName] = dateValue;
                    else
                        toObject[propertyName] = value;
                }
            }
        }
    }
}
