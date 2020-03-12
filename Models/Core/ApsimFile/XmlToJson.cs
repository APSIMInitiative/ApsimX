

namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// XML to JSON converter
    /// </summary>
    public class XmlToJson
    {
        private static string[] builtinTypeNames = new string[] { "string", "int", "double", "dateTime", "ArrayOfString" };
        private static string[] arrayVariableNames = new string[] { "AcceptedStats", "Operation", "Parameters", "cultivars", "Nodes", "Stores", "PaddockList" };
        private static string[] arrayVariables = new[] { "Command", "Alias", "Leaves", "ZoneNamesToGrowRootsIn", "ZoneRootDepths", "ZoneInitialDM" };
        private static string[] propertiesToIgnore = new[] { "ParameterValues", "Nodes", "Arcs", "Weirdo" };
        private static string[] modelTypes = new string[]
        {
            "Models.Sobol","Models.Morris",
            "Models.Map","Models.MicroClimate",
            "Models.Memo","Models.Sugarcane",
            "Models.Fertiliser","Models.Irrigation",
            "Models.Log","Models.Manager",
            "Models.Operations","Models.Summary",
            "Models.Tests","Models.Clock",
            "Models.ControlledEnvironment","Models.Weather",
            "Models.WaterModel.CNReductionForCover","Models.WaterModel.CNReductionForTillage",
            "Models.WaterModel.EvaporationModel","Models.WaterModel.LateralFlowModel",
            "Models.WaterModel.RunoffModel","Models.WaterModel.SaturatedFlowModel",
            "Models.WaterModel.WaterBalance","Models.WaterModel.UnsaturatedFlowModel",
            "Models.WaterModel.WaterTableModel","Models.Surface.SurfaceOrganicMatterCollectionFromResource",
            "Models.Surface.ResidueTypes","Models.Report",
            "Models.PostSimulationTools.Probability","Models.PostSimulationTools.ExcelInput",
            "Models.PostSimulationTools.TimeSeriesStats","Models.PostSimulationTools.PredictedObserved",
            "Models.PostSimulationTools.Input","Models.GrazPlan.Supplement",
            "Models.GrazPlan.Stock","Models.LifeCycle.LifeStageImmigrationProcess",
            "Models.LifeCycle.LifeCycle","Models.LifeCycle.LifeStage",
            "Models.LifeCycle.LifeStageProcess","Models.LifeCycle.LifeStageReproductionProcess",
            "Models.Storage.DataStore","Models.Soils.SoilNitrogenPlantAvailableNH4",
            "Models.Soils.SoilNitrogenPlantAvailableNO3","Models.Soils.SoilNitrogenUrea",
            "Models.Soils.SoilNitrogenNH4","Models.Soils.SoilNitrogenNO3",
            "Models.Soils.HydraulicProperties","Models.Soils.OutputLayers",
            "Models.Soils.Evapotranspiration","Models.Soils.MRSpline",
            "Models.Soils.HourlyData","Models.Soils.WEIRDO",
            "Models.Soils.Pore","Models.Soils.SoilNitrogen",
            "Models.Soils.Analysis","Models.Soils.InitialWater",
            "Models.Soils.LayerStructure","Models.Soils.Phosphorus",
            "Models.Soils.Sample","Models.Soils.CERESSoilTemperature",
            "Models.Soils.Soil","Models.Soils.SoilCrop",
            "Models.Soils.SoilOrganicMatter","Models.Soils.Swim3",
            "Models.Soils.SwimSoluteParameters","Models.Soils.SwimSubsurfaceDrain",
            "Models.Soils.SwimWaterTable","Models.Soils.TillageType",
            "Models.Soils.Water","Models.Soils.SoilWater",
            "Models.Soils.Arbitrator.SoilArbitrator","Models.Soils.SoilTemp.SoilTemperature",
            "Models.Soils.Nutrients.Chloride","Models.Soils.Nutrients.NFlow",
            "Models.Soils.Nutrients.CarbonFlow","Models.Soils.Nutrients.NutrientPool",
            "Models.Soils.Nutrients.NutrientCollectionFromResource","Models.Soils.Nutrients.Solute",
            "Models.Factorial.CompositeFactor","Models.Factorial.Factor",
            "Models.Factorial.Experiment","Models.Factorial.Factors",
            "Models.PMF.RetranslocateAvailableN","Models.PMF.BaseArbitrator",
            "Models.PMF.RetranslocateNonStructural","Models.PMF.SorghumArbitratorN",
            "Models.PMF.BiomassDemand","Models.PMF.PrioritythenRelativeAllocation",
            "Models.PMF.PriorityAllocation","Models.PMF.RelativeAllocationSinglePass",
            "Models.PMF.RelativeAllocation","Models.PMF.CultivarFolder",
            "Models.PMF.OrganBiomassRemovalType","Models.PMF.Cultivar",
            "Models.PMF.SimpleTree","Models.PMF.ArrayBiomass",
            "Models.PMF.Biomass","Models.PMF.PlantCollectionFromResource",
            "Models.PMF.OilPalm.OilPalmCollectionFromResource","Models.PMF.Library.BiomassRemoval",
            "Models.PMF.Struct.CulmStructure","Models.PMF.Struct.BudNumberFunction",
            "Models.PMF.Struct.ApexBase","Models.PMF.Struct.HeightFunction",
            "Models.PMF.Struct.Structure","Models.PMF.Organs.Culm",
            "Models.PMF.Organs.EnergyBalance","Models.PMF.Organs.SorghumLeaf",
            "Models.PMF.Organs.PerennialLeaf","Models.PMF.Organs.GenericOrgan",
            "Models.PMF.Organs.HIReproductiveOrgan","Models.PMF.Organs.Leaf",
            "Models.PMF.Organs.LeafCohort","Models.PMF.Organs.Nodule",
            "Models.PMF.Organs.ReproductiveOrgan","Models.PMF.Organs.Root",
            "Models.PMF.Organs.SimpleLeaf","Models.PMF.Phen.DAWSPhase",
            "Models.PMF.Phen.Age","Models.PMF.Phen.PhotoperiodPhase",
            "Models.PMF.Phen.Vernalisation","Models.PMF.Phen.BBCH",
            "Models.PMF.Phen.NodeNumberPhase","Models.PMF.Phen.ZadokPMF",
            "Models.PMF.Phen.EmergingPhase","Models.PMF.Phen.EndPhase",
            "Models.PMF.Phen.GenericPhase","Models.PMF.Phen.GerminatingPhase",
            "Models.PMF.Phen.GotoPhase","Models.PMF.Phen.LeafAppearancePhase",
            "Models.PMF.Phen.LeafDeathPhase","Models.PMF.Phen.Phenology",
            "Models.EventNamesOnGraph","Models.Regression",
            "Models.Surface.SurfaceOrganicMatter",
    		"Models.Soils.Nutrients.Nutrient,",
    		"Models.PMF.Plant",
    		"Models.PMF.OilPalm.OilPalm,",
            "Models.Series","Models.Graph",
            "Models.Functions.DecumulateFunction","Models.Functions.EndOfDayFunction",
            "Models.Functions.AccumulateAtEvent","Models.Functions.DailyMeanVPD",
            "Models.Functions.CERESDenitrificationWaterFactor","Models.Functions.CERESDenitrificationTemperatureFactor",
            "Models.Functions.CERESMineralisationFOMCNRFactor","Models.Functions.DayCentN2OFractionModel",
            "Models.Functions.CERESNitrificationpHFactor","Models.Functions.CERESNitrificationWaterFactor",
            "Models.Functions.CERESUreaHydrolysisModel","Models.Functions.CERESMineralisationWaterFactor",
            "Models.Functions.CERESMineralisationTemperatureFactor","Models.Functions.CERESNitrificationModel",
            "Models.Functions.StringComparisonFunction","Models.Functions.AccumulateByDate",
            "Models.Functions.AccumulateByNumericPhase","Models.Functions.TrackerFunction",
            "Models.Functions.ArrayFunction","Models.Functions.WangEngelTempFunction",
            "Models.Functions.BoundFunction","Models.Functions.LinearAfterThresholdFunction",
            "Models.Functions.SoilWaterScale","Models.Functions.MovingAverageFunction",
            "Models.Functions.HoldFunction","Models.Functions.DeltaFunction",
            "Models.Functions.MovingSumFunction","Models.Functions.QualitativePPEffect",
            "Models.Functions.AccumulateFunction","Models.Functions.AddFunction",
            "Models.Functions.AgeCalculatorFunction","Models.Functions.AirTemperatureFunction",
            "Models.Functions.BellCurveFunction","Models.Functions.Constant",
            "Models.Functions.DivideFunction","Models.Functions.ExponentialFunction",
            "Models.Functions.ExpressionFunction","Models.Functions.ExternalVariable",
            "Models.Functions.LessThanFunction","Models.Functions.LinearInterpolationFunction",
            "Models.Functions.MaximumFunction","Models.Functions.MinimumFunction",
            "Models.Functions.MultiplyFunction","Models.Functions.OnEventFunction",
            "Models.Functions.PhaseBasedSwitch","Models.Functions.PhaseLookup",
            "Models.Functions.PhaseLookupValue","Models.Functions.PhotoperiodDeltaFunction",
            "Models.Functions.PhotoperiodFunction","Models.Functions.PowerFunction",
            "Models.Functions.SigmoidFunction","Models.Functions.SoilTemperatureDepthFunction",
            "Models.Functions.SoilTemperatureFunction","Models.Functions.SoilTemperatureWeightedFunction",
            "Models.Functions.SplineInterpolationFunction","Models.Functions.StageBasedInterpolation",
            "Models.Functions.SubtractFunction","Models.Functions.VariableReference",
            "Models.Functions.WeightedTemperatureFunction","Models.Functions.XYPairs",
            "Models.Functions.SupplyFunctions.LeafLightUseEfficiency","Models.Functions.SupplyFunctions.LeafMaxGrossPhotosynthesis",
            "Models.Functions.SupplyFunctions.CanopyGrossPhotosynthesisHourly","Models.Functions.SupplyFunctions.CanopyPhotosynthesis",
            "Models.Functions.SupplyFunctions.RUECO2Function","Models.Functions.SupplyFunctions.RUEModel",
            "Models.Functions.DemandFunctions.StorageDMDemandFunction","Models.Functions.DemandFunctions.StorageNDemandFunction",
            "Models.Functions.DemandFunctions.InternodeCohortDemandFunction","Models.Functions.DemandFunctions.BerryFillingRateFunction",
            "Models.Functions.DemandFunctions.TEWaterDemandFunction","Models.Functions.DemandFunctions.FillingRateFunction",
            "Models.Functions.DemandFunctions.AllometricDemandFunction","Models.Functions.DemandFunctions.InternodeDemandFunction",
            "Models.Functions.DemandFunctions.PartitionFractionDemandFunction","Models.Functions.DemandFunctions.PopulationBasedDemandFunction",
            "Models.Functions.DemandFunctions.PotentialSizeDemandFunction","Models.Functions.DemandFunctions.RelativeGrowthRateDemandFunction",
            "Models.Core.Alias","Models.Core.Replacements",
            "Models.Core.ModelCollectionFromResource","Models.Core.Folder",
            "Models.Core.Zone","Models.Core.Simulations",
            "Models.Core.Simulation","Models.CLEM.CLEMModel",
            "Models.CLEM.Reporting.CustomQuery","Models.CLEM.Reporting.PivotTable",
            "Models.CLEM.Reporting.ReportLabourRequirements","Models.CLEM.Activities.Relationship",
            "Models.Aqua.FoodInPond","Models.Aqua.PondWater",
            "Models.Aqua.Prawns","Models.Agroforestry.LocalMicroClimate",
            "Models.Agroforestry.TreeProxy","Models.AgPasture.PastureSpecies",
            "Models.AgPasture.PastureAboveGroundOrgan","Models.AgPasture.GenericTissue",
            "Models.AgPasture.Sward","Models.Soils.SoilWater+TillageTypesList",
            "Models.PMF.Organs.Leaf+LeafCohortParameters",
            "Models.Zones.CircularZone",
            "Models.Zones.RectangularZone",
            "Models.CLEM.ZoneCLEM",
            "Models.Agroforestry.AgroforestrySystem",
            "Models.PMF.OrganArbitrator"
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
            var settings = new JsonSerializerSettings()
            {
                // This will tell the serializer not to attempt to localise dates.
                DateParseHandling = DateParseHandling.None
            };
            JObject root = (JObject)JsonConvert.DeserializeObject(json, settings);

            JToken newRoot = CreateObject(root[doc.DocumentElement.Name]);

            // The order of child nodes can be wrong. Newtonsoft XML to JSON will
            // group child nodes of the same type into an array. This alters the
            // order of children. Need to reorder children.
            ReorderChildren(newRoot, doc.DocumentElement);

            json = newRoot.ToString();

            return json;
        }

        /// <summary>
        /// Create an object (or an array)
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static JToken CreateObject(JToken root)
        {
            JObject newRoot = new JObject();
            if (root.Parent != null)
                AddTypeToObject(root, newRoot);

            JToken child = root.First;
            while (child != null)
            {
                JProperty property = child as JProperty;
                if (property != null && !propertiesToIgnore.Contains(property.Name))
                {
                    if (property.Value is JArray)
                    {
                        if (builtinTypeNames.Contains(property.Name))
                        {
                            // array of string / double etc/
                            return CreateArray(property.Name, property.Value as JArray, newRoot);
                        }
                        else
                        {
                            // array of models.
                            JArray arrayOfModels = CreateArray(property.Name, property.Value as JArray, newRoot);
                            if (arrayOfModels.Count > 0)
                                newRoot[property.Name] = arrayOfModels;
                        }
                    }
                    else if (property.Value is JValue)
                    {
                        if (builtinTypeNames.Contains(property.Name))
                        {
                            // Should be treated as an array. e.g report event names are often
                            // only one element. The NewtonSoft JSON converter treats this as
                            // a normal property. We need to convert the property into an array
                            // with one element.
                            return CreateArray(property.Name, property.Value, newRoot);
                        }
                        else if (GetModelFullName(property.Name) != null && 
                            property.Name != "Parameter")  // CLEM.LabourFilter has a Parameter property.
                        {
                            // a model without any child nodes.
                            AddNewChild(property, newRoot);
                        }
                        else if (arrayVariables.Contains(property.Name))
                        {
                            JArray arrayOfModels = CreateArray(property.Name, property.Value, newRoot);
                            if (arrayOfModels.Count > 0)
                                newRoot[property.Name] = arrayOfModels;
                        }
                        else 
                            WriteProperty(property, newRoot);
                    }
                    else if (property.Value is JObject)
                        ProcessObject(property.Name, property.Value, newRoot);
                }

                child = child.Next;
            }

            return newRoot;
        }

        private static void AddTypeToObject(JToken root, JToken newRoot)
        {
            string modelType;
            if (root["@xsi:type"] != null)
                modelType = root["@xsi:type"].ToString();
            else
                modelType = root.Parent.Path;

            Type fullNameType = GetTypeFromName(modelType);
            string fullName;
            if (fullNameType == null)
                fullName = GetModelFullName(modelType);
            else
                fullName = fullNameType.FullName;
            if (fullName != null)
                newRoot["$type"] = fullName + ", Models";
        }

        private static void ProcessObject(string name, JToken obj, JObject newRoot)
        {
            // Look for an array of something e.g. variable names in report.
            if (name == "Code")
            {
                JValue childAsValue = obj.First.First as JValue;
                newRoot["Code"] = childAsValue.Value.ToString();
            }
            else if (name == "MemoText")
            {
                JValue childAsValue = obj.First.First as JValue;
                newRoot["Text"] = childAsValue.Value.ToString();
            }
            else if (name.Equals("Script", StringComparison.CurrentCultureIgnoreCase))
            {
                // manager parameters.
                JArray parameters = new JArray();
                foreach (JProperty parameter in obj.Children())
                {
                    JObject newParameter = new JObject();
                    newParameter["Key"] = parameter.Name;
                    newParameter["Value"] = parameter.Value.ToString();
                    parameters.Add(newParameter);
                }
                newRoot["Parameters"] = parameters;
            }
            else if (name.Equals("PaddockList", StringComparison.CurrentCultureIgnoreCase))
            {
                // manager parameters.
                JArray values = new JArray();
                foreach (var child in obj.Children())
                {
                    var newObject = CreateObject(child.First);
                    values.Add(newObject);
                }
                newRoot[name] = values;
            }
            else
            {
                if (GetModelFullName(name) == null)
                {
                    var modelType = GetTypeFromName(JsonUtilities.Type(newRoot));
                    var property = modelType?.GetProperty(name);
                    var newObject = CreateObject(obj);
                    // If the new obejct is NOT a JArray, and this object is supposed to be an array...
                    if (!(newObject is JArray) && (arrayVariableNames.Contains(name) || (property != null && property.PropertyType.IsArray)))
                    {
                        // Should be an array of objects.
                        if (newObject.First.First is JArray)
                            newObject = newObject.First.First;
                        else
                        {
                            JArray array = new JArray();
                            if (newObject.Count() == 1 && newObject.First is JProperty)
                                array.Add(newObject.First.First);
                            else
                                array.Add(newObject);
                            newObject = array;
                        }
                    }

                    if (!(newObject is JArray) && newObject["$type"] != null && 
                        GetModelFullName(newObject["$type"].ToString()) != null)
                        AddNewChild(newObject, newRoot);
                    else if (newObject.Children().Count() == 1 && newObject.First.Path == "#text")
                        newRoot[name] = newObject.First.First;
                    else
                        newRoot[name] = newObject;
                }
                else
                    AddNewChild(obj, newRoot);
            }
        }

        private static JArray CreateArray(string name, JToken array, JObject newRoot)
        {
            JArray newArray = new JArray();
            if (array is JArray)
            {
                // Array of non models. e.g. array of Axis.
                foreach (var element in array.Children())
                {
                    var modelType = GetModelFullName(name);
                    if (name == "string" || name == "Command")
                        newArray.Add(new JValue(element.ToString()));
                    else if (name == "double")
                        newArray.Add(new JValue(double.Parse(element.ToString(), CultureInfo.InvariantCulture)));
                    else if (name == "int")
                        newArray.Add(new JValue(int.Parse(element.ToString(), CultureInfo.InvariantCulture)));
                    else if (name == "dateTime")
                        newArray.Add(new JValue(DateTime.Parse(element.ToString(), CultureInfo.InvariantCulture)));
                    else if (name == "ArrayOfString")
                    {
                        JArray nestedArray = new JArray();
                        foreach (var value in element.First.Values<JArray>())
                            newArray.Add(value);
                        //newArray.Add(nestedArray);
                    }
                    else if (element is JValue)
                        newArray.Add(element);
                    else if (modelType == null)
                        newArray.Add(CreateObject(element));
                    else
                        AddNewChild(element, newRoot);
                }
            }
            else if (array is JValue)
            {
                // Simply put the single property into the array. e.g. report event names
                JValue value = array as JValue;
                if (name == "string" || name == "Command" || name == "Alias")
                    newArray.Add(new JValue(value.ToString()));
                else if (name == "double")
                    newArray.Add(new JValue(double.Parse(value.ToString(), CultureInfo.InvariantCulture)));
            }

            return newArray;
        }

        private static void AddNewChild(JToken element, JObject newRoot)
        {
            JToken newChild;
            if (element is JProperty)
            {
                newChild = new JObject();
                var fullName = GetModelFullName((element as JProperty).Name);
                if (fullName != null)
                    newChild["$type"] = fullName + ", Models";

                if (newChild["Name"] == null)
                    newChild["Name"] = (element as JProperty).Name;
            }
            else
            {
                newChild = CreateObject(element as JObject);
                if (newChild["Name"] == null && newChild["$type"] != null)
                {
                    string type = newChild["$type"].Value<string>().Replace(", Models", "");
                    string[] words = type.Split(".".ToCharArray());
                    newChild["Name"] = words.Last();
                }
            }


            if (newRoot["Children"] == null)
                newRoot["Children"] = new JArray();

            (newRoot["Children"] as JArray).Add(newChild);
        }

        private static void WriteProperty(JProperty property, JObject toObject)
        {
            string propertyName = property.Name;
            if (propertyName == "@Version")
                propertyName = "Version";
            if (propertyName == "#text" && property.Path.Contains("Memo"))
                return; // Old memo have #text, we don't want them.

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
                                newArray.Add(new JValue(double.Parse(value.ToString(), CultureInfo.InvariantCulture)));
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
                    if (property.Name == "Name")
                    {
                        if (JsonUtilities.Type(toObject) == "SoilCrop")
                            toObject["Name"] = GetSoilCropName(property.Value.ToString());
                        else
                            toObject[propertyName] = value;
                    }
                    else if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
                        toObject[propertyName] = intValue;
                    else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                        toObject[propertyName] = doubleValue;
                    else if (value == "-INF")
                        toObject[propertyName] = double.NaN;
                    else if (bool.TryParse(value, out boolValue))
                        toObject[propertyName] = boolValue;
                    else if (DateTime.TryParseExact(value, "MM/dd/yyyy hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                        toObject[propertyName] = dateValue.ToString("yyyy-MM-dd");
                    else
                        toObject[propertyName] = value;
                }
            }
            else if (propertyName == "@name") // Name attribute.
            {
                // SoilCrops copied from Apsim classic need to be renamed to CropNameSoil e.g. WheatSoil.
                if (toObject["$type"]?.ToString() == "Models.Soils.SoilCrop, Models")
                    toObject["Name"] = property.Value.ToString() + "Soil";
                else if (toObject["Name"] == null)
                    toObject["Name"] = property.Value;
            }
        }

        private static string GetModelFullName(string modelNameToFind)
        {
            if (modelNameToFind == null)
                return null;

            string[] modelWords = modelNameToFind.Replace(", Models", "").Split(".".ToCharArray());
            string m = modelWords[modelWords.Length - 1];

            return modelTypes.FirstOrDefault(t => t.EndsWith("." + m));
         }

        private static Type GetTypeFromName(string modelNameToFind)
        {
            if (modelNameToFind == null)
                return null;

            string[] modelWords = modelNameToFind.Split(".".ToCharArray());
            string m = modelWords[modelWords.Length - 1];

            Type[] types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (type.Name == m)
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// Make sure the child nodes of JToken are the same as for the original XML document.
        /// Do this recursively.
        /// </summary>
        /// <param name="jsonNode">The JSON node.</param>
        /// <param name="xmlNode">The XML node.</param>
        private static void ReorderChildren(JToken jsonNode, XmlNode xmlNode)
        {
            if (jsonNode["Children"] != null)
            {
                JArray newArray = new JArray();

                // Some simulations can have a 2 child models with same name.
                List<string> childNamesDone = new List<string>();

                JArray children = jsonNode["Children"] as JArray;
                foreach (var childXmlNode in XmlUtilities.ChildNodes(xmlNode, null))
                {
                    string childXmlName = XmlUtilities.Value(childXmlNode, "Name");

                    if (!childNamesDone.Contains(childXmlName))
                    {
                        if (childXmlName == string.Empty)
                        {
                            string nameAttribute = XmlUtilities.NameAttr(childXmlNode);
                            if (nameAttribute != null)
                                childXmlName = nameAttribute;
                            else if (GetModelFullName(childXmlNode.Name) != null)
                                childXmlName = childXmlNode.Name;
                        }
                        if (childXmlName != string.Empty || GetTypeFromName(childXmlNode.Name) != null)
                        {
                            int i = 1;
                            foreach (var childJsonNode in children.Where(c => !(c is JArray) && c["Name"].ToString() == childXmlName || (c["$type"].ToString().Contains("SoilCrop") && c["Name"].ToString() == GetSoilCropName(childXmlName))))
                            {
                                bool alreadyAdded = newArray.FirstOrDefault(c => c["Name"].ToString() == childXmlName) != null;

                                if (childJsonNode != null)
                                {
                                    if (alreadyAdded)
                                    {
                                        string name = childJsonNode["Name"].ToString();
                                        string newName = name + i.ToString();
                                        childJsonNode["Name"] = newName;
                                        i++;
                                    }

                                    ReorderChildren(childJsonNode, childXmlNode);
                                    newArray.Add(childJsonNode);
                                }
                            }
                            childNamesDone.Add(childXmlName);
                        }
                    }
                }

                jsonNode["Children"] = newArray;
            }
        }

        /// <summary>
        /// Gets the name of a SoilCrop. This should start with an upper case
        /// letter and end with "Soil". e.g. WheatSoil.
        /// </summary>
        /// <param name="name">Name of the crop.</param>
        /// <returns></returns>
        /// <remarks>
        /// todo: rework the SoilCrop class so that this isn't necessary?
        /// </remarks>
        private static string GetSoilCropName(string name)
        {
            name = name.First().ToString().ToUpper() + name.Substring(1);
            if (!name.EndsWith("Soil"))
                name += "Soil";
            return name;
        }
    }
}
