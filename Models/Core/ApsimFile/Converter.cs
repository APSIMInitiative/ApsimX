namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Models.PMF;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Converts the .apsim file from one version to the next
    /// </summary>
    public class Converter
    {
        /// <summary>Gets the latest .apsimx file format version.</summary>
        public static int LatestVersion { get { return 61; } }

        /// <summary>Converts a .apsimx string to the latest version.</summary>
        /// <param name="st">XML or JSON string to convert.</param>
        /// <param name="toVersion">The optional version to convert to.</param>
        /// <param name="fileName">The optional filename where the string came from.</param>
        /// <returns>Returns true if something was changed.</returns>
        public static ConverterReturnType DoConvert(string st, int toVersion = -1, string fileName = null)
        {
            ConverterReturnType returnData = new ConverterReturnType();

            if (toVersion == -1)
                toVersion = LatestVersion;

            int offset = st.TakeWhile(c => char.IsWhiteSpace(c)).Count();
            char firstNonBlankChar = st[offset];

            if (firstNonBlankChar == '<')
            {
                bool changed = XmlConverters.DoConvert(ref st, Math.Min(toVersion, XmlConverters.LastVersion), fileName);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(st);
                int fileVersion = Convert.ToInt32(XmlUtilities.Attribute(doc.DocumentElement, "Version"), CultureInfo.InvariantCulture);
                if (fileVersion == toVersion)
                    return new ConverterReturnType()
                    { DidConvert = changed, RootXml = doc };

                st = ConvertToJSON(st, fileName);
                returnData.Root = JObject.Parse(st);
            }
            else if (firstNonBlankChar == '{')
            {
                // json
                returnData.Root = JObject.Parse(st);
            }
            else
            {
                throw new Exception("Unknown string encountered. Not JSON or XML. String: " + st);
            }

            if (returnData.Root.ContainsKey("Version"))
            {
                int fileVersion = (int)returnData.Root["Version"];

                if (fileVersion > LatestVersion)
                    throw new Exception(string.Format("Unable to open file '{0}'. File version is greater than the latest file version. Has this file been opened in a more recent version of Apsim?", fileName));

                // Run converters if not at the latest version.
                while (fileVersion < toVersion)
                {
                    returnData.DidConvert = true;

                    // Find the method to call to upgrade the file by one version.
                    int versionFunction = fileVersion + 1;
                    MethodInfo method = typeof(Converter).GetMethod("UpgradeToVersion" + versionFunction, BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null)
                        throw new Exception("Cannot find converter to go to version " + versionFunction);

                    // Found converter method so call it.
                    method.Invoke(null, new object[] { returnData.Root, fileName });

                    fileVersion++;
                }

                if (returnData.DidConvert)
                {
                    returnData.Root["Version"] = fileVersion;
                    st = returnData.Root.ToString();
                }
            }
            returnData.DidConvert = EnsureSoilHasInitWaterAndSample(returnData.Root) || returnData.DidConvert;

            return returnData;
        }

        /// <summary>
        /// If root is a soil then make sure it has a sample or init water.
        /// </summary>
        /// <param name="root">The root node of the JSON to look at.</param>
        /// <returns>True if model was changed.</returns>
        private static bool EnsureSoilHasInitWaterAndSample(JObject root)
        {
            string rootType = JsonUtilities.Type(root, true);

            if (rootType != null && rootType == "Models.Soils.Soil")
            {
                JArray soilChildren = root["Children"] as JArray;
                if (soilChildren != null && soilChildren.Count > 0)
                {
                    var initWater = soilChildren.FirstOrDefault(c => c["$type"].Value<string>().Contains(".InitWater"));
                    var sample = soilChildren.FirstOrDefault(c => c["$type"].Value<string>().Contains(".Sample"));

                    if (sample == null && initWater == null)
                    {
                        // Add in an initial water and initial conditions models.
                        initWater = new JObject();
                        initWater["$type"] = "Models.Soils.InitialWater, Models";
                        initWater["Name"] = "Initial water";
                        initWater["PercentMethod"] = "FilledFromTop";
                        initWater["FractionFull"] = 1;
                        initWater["DepthWetSoil"] = "NaN";
                        soilChildren.Add(initWater);

                        sample = new JObject();
                        sample["$type"] = "Models.Soils.Sample, Models";
                        sample["Name"] = "Initial conditions";
                        sample["Thickness"] = new JArray(new double[] { 1800 });
                        sample["NO3N"] = new JArray(new double[] { 3 });
                        sample["NH4"] = new JArray(new double[] { 1 });
                        sample["SWUnits"] = "Volumetric";
                        soilChildren.Add(sample);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Upgrades to version 47 - the first JSON version.</summary>
        private static string ConvertToJSON(string st, string fileName)
        {
            string json = XmlToJson.Convert(st);
            JObject j = JObject.Parse(json);
            j["Version"] = 47;
            return j.ToString();
        }

        private static void UpgradeToVersion47(JObject root, string fileName)
        {
            // Nothing to do as conversion to JSON has already happened.
        }

        /// <summary>
        /// Upgrades to version 48. Iterates through all manager scripts, and replaces
        /// all instances of the text "DisplayTypeEnum" with "DisplayType".
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion48(JObject root, string fileName)
        {
            foreach (JObject manager in JsonUtilities.ChildrenRecursively(root, "Manager"))
                JsonUtilities.ReplaceManagerCode(manager, "DisplayTypeEnum", "DisplayType");
        }


        /// <summary>
        /// Upgrades to version 49. Renames Models.Morris+Parameter to Models.Sensitivity.Parameter.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion49(JObject root, string fileName)
        {
            foreach (JObject morris in JsonUtilities.ChildrenRecursively(root, "Models.Morris"))
                foreach (var parameter in morris["Parameters"])
                    parameter["$type"] = parameter["$type"].ToString().Replace("Models.Morris+Parameter", "Models.Sensitivity.Parameter");
        }

        ///<summary>
        /// Upgrades to version 50. Fixes the RelativeTo property of 
        /// InitialWater components of soils copied from Apsim Classic.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        /// <remarks>
        /// ll15 must be renamed to LL15.
        /// Wheat must be renamed to WheatSoil.
        /// Maize must be renamed to MaizeSoil.
        /// </remarks>
        private static void UpgradeToVersion50(JObject root, string fileName)
        {
            foreach (JObject initialWater in JsonUtilities.ChildrenRecursively(root, "InitialWater"))
            {
                if (initialWater["RelativeTo"] != null)
                {
                    if (initialWater["RelativeTo"].ToString().ToUpper().Contains("LL15"))
                        initialWater["RelativeTo"] = initialWater["RelativeTo"].ToString().Replace("ll15", "LL15");
                    else if (!string.IsNullOrEmpty(initialWater["RelativeTo"].ToString()) && !initialWater["RelativeTo"].ToString().EndsWith("Soil"))
                        initialWater["RelativeTo"] = initialWater["RelativeTo"].ToString() + "Soil";
                }
            }
        }
        /// <summary>
        /// Changes GsMax to Gsmax350 in all models that implement ICanopy.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion51(JObject root, string fileName)
        {
            // Create a list of models that might have gsmax.
            // Might need to add in other models that implement ICanopy 
            // e.g. OilPalm, AgPastureSpecies, SimpleTree, Sugarcane

            var models = new List<JObject>();
            models.AddRange(JsonUtilities.ChildrenOfType(root, "Leaf"));
            models.AddRange(JsonUtilities.ChildrenOfType(root, "SimpleLeaf"));
            models.AddRange(JsonUtilities.ChildrenOfType(root, "PerennialLeaf"));
            models.AddRange(JsonUtilities.ChildrenOfType(root, "SorghumLeaf"));

            // Loop through all models and rename Gsmax to Gsmax350.
            foreach (var model in models)
            {
                JsonUtilities.RenameProperty(model, "Gsmax", "Gsmax350");
                JsonUtilities.AddConstantFunctionIfNotExists(model, "StomatalConductanceCO2Modifier", "1.0");
            }
        }
        
        /// <summary>
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion52(JObject root, string fileName)
        {
            foreach (var SOM in JsonUtilities.ChildrenOfType(root, "SoilOrganicMatter"))
            {
                double rootWt;
                if (SOM["RootWt"] is JArray)
                    rootWt = Convert.ToDouble(SOM["RootWt"][0], CultureInfo.InvariantCulture); // This can happen when importing old APSIM file.
                else
                    rootWt = Convert.ToDouble(SOM["RootWt"], CultureInfo.InvariantCulture);
                SOM.Remove("RootWt");
                double[] thickness = MathUtilities.StringsToDoubles(JsonUtilities.Values(SOM, "Thickness"));

                double profileDepth = MathUtilities.Sum(thickness);
                double cumDepth = 0;
                double[] rootWtFraction = new double[thickness.Length];

                for (int layer = 0; layer < thickness.Length; layer++)
                {
                    double fracLayer = Math.Min(1.0, MathUtilities.Divide(profileDepth - cumDepth, thickness[layer], 0.0));
                    cumDepth += thickness[layer];
                    rootWtFraction[layer] = fracLayer * Math.Exp(-3.0 * Math.Min(1.0, MathUtilities.Divide(cumDepth, profileDepth, 0.0)));
                }
                // get the actuall FOM distribution through layers (adds up to one)
                double totFOMfraction = MathUtilities.Sum(rootWtFraction);
                for (int layer = 0; layer < thickness.Length; layer++)
                    rootWtFraction[layer] /= totFOMfraction;
                double[] rootWtVector = MathUtilities.Multiply_Value(rootWtFraction, rootWt);

                JsonUtilities.SetValues(SOM, "RootWt", rootWtVector);
            }

        }

        /// <summary>
        /// Adds solutes under SoilNitrogen.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion53(JObject root, string fileName)
        {
            foreach (var soilNitrogen in JsonUtilities.ChildrenOfType(root, "SoilNitrogen"))
            {
                JsonUtilities.CreateNewChildModel(soilNitrogen, "NO3", "Models.Soils.SoilNitrogenNO3");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "NH4", "Models.Soils.SoilNitrogenNH4");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "Urea", "Models.Soils.SoilNitrogenUrea");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "PlantAvailableNO3", "Models.Soils.SoilNitrogenPlantAvailableNO3");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "PlantAvailableNH4", "Models.Soils.SoilNitrogenPlantAvailableNH4");
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NO3", "SoilNitrogen.NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NH4", "SoilNitrogen.NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.urea", "SoilNitrogen.Urea.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNO3", "SoilNitrogen.PlantAvailableNO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNH4", "SoilNitrogen.PlantAvailableNH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].no3", "[SoilNitrogen].NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].nh4", "[SoilNitrogen].NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].urea", "[SoilNitrogen].Urea.kgha");
            }
            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                var originalCode = manager.ToString();
                if (originalCode != null)
                {
                    if (originalCode.Contains("SoilNitrogen.NO3"))
                    {
                        manager.Replace("Soil.SoilNitrogen.NO3", "NO3.kgha");
                        manager.Replace("SoilNitrogen.NO3", "NO3.kgha");
                        manager.AddDeclaration("ISolute", "NO3", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.NH4"))
                    {
                        manager.Replace("Soil.SoilNitrogen.NH4", "NH4.kgha");
                        manager.Replace("SoilNitrogen.NH4", "NH4.kgha");
                        manager.AddDeclaration("ISolute", "NH4", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.urea"))
                    {
                        manager.Replace("Soil.SoilNitrogen.urea", "Urea.kgha");
                        manager.Replace("SoilNitrogen.urea", "Urea.kgha");
                        manager.AddDeclaration("ISolute", "Urea", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.PlantAvailableNO3"))
                    {
                        manager.Replace("Soil.SoilNitrogen.PlantAvailableNO3", "PlantAvailableNO3.kgha");
                        manager.Replace("SoilNitrogen.PlantAvailableNO3", "PlantAvailableNO3.kgha");
                        manager.AddDeclaration("ISolute", "PlantAvailableNO3", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.PlantAvailableNH4"))
                    {
                        manager.Replace("Soil.SoilNitrogen.PlantAvailableNH4", "PlantAvailableNH4.kgha");
                        manager.Replace("SoilNitrogen.PlantAvailableNH4", "PlantAvailableNH4.kgha");
                        manager.AddDeclaration("ISolute", "PlantAvailableNH4", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode != manager.ToString())
                    {
                        var usingLines = manager.GetUsingStatements().ToList();
                        usingLines.Add("Models.Interfaces");
                        manager.SetUsingStatements(usingLines);
                        manager.Save();
                    }
                }
            }

            foreach (var series in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (series["XFieldName"] != null)
                {
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NO3", "SoilNitrogen.NO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NH4", "SoilNitrogen.NH4.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.urea", "SoilNitrogen.Urea.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3", "SoilNitrogen.PlantAvailableNO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4", "SoilNitrogen.PlantAvailableNH4.kgha");
                }
                if (series["YFieldName"] != null)
                {
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NO3", "SoilNitrogen.NO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NH4", "SoilNitrogen.NH4.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.urea", "SoilNitrogen.Urea.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3", "SoilNitrogen.PlantAvailableNO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4", "SoilNitrogen.PlantAvailableNH4.kgha");
                }
            }
        }

        /// <summary>
        /// Remove SoluteManager.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion54(JObject root, string fileName)
        {
            foreach (var soluteManager in JsonUtilities.ChildrenOfType(root, "SoluteManager"))
                soluteManager.Remove();

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].NO3N", "[Soil].SoilNitrogen.NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].NH4N", "[Soil].SoilNitrogen.NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].UreaN", "[Soil].SoilNitrogen.Urea.kgha");
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                bool managerChanged = false;
                if (manager.Replace("mySoil.NO3N", "NO3.kgha"))
                {
                    manager.AddDeclaration("ISolute", "NO3", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("mySoil.NH4N", "NH4.kgha"))
                {
                    manager.AddDeclaration("ISolute", "NH4", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("mySoil.UreaN", "Urea.kgha"))
                {
                    manager.AddDeclaration("ISolute", "Urea", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("Soil.NO3N", "NO3.kgha"))
                {
                    manager.AddDeclaration("ISolute", "NO3", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("Soil.NH4N", "NH4.kgha"))
                {
                    manager.AddDeclaration("ISolute", "NH4", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("Soil.UreaN", "Urea.kgha"))
                {
                    manager.AddDeclaration("ISolute", "Urea", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("mySoil.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("Soil.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("soil.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Replace("soil1.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                var declarations = manager.GetDeclarations();
                if (declarations.RemoveAll(declaration => declaration.TypeName == "SoluteManager") > 0)
                {
                    manager.SetDeclarations(declarations);
                    managerChanged = true;
                }

                if (managerChanged)
                {
                    var usingLines = manager.GetUsingStatements().ToList();
                    usingLines.Add("Models.Interfaces");
                    manager.SetUsingStatements(usingLines);
                    manager.Save();
                }
            }
        }


        /// <summary>
        /// Changes initial Root Wt to an array.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion55(JObject root, string fileName)
        {
            foreach (var SOM in JsonUtilities.ChildrenOfType(root, "SoilOrganicMatter"))
            {
                double soilcnr;
                if (SOM["SoilCN"] is JArray)
                    soilcnr = Convert.ToDouble(SOM["SoilCN"][0], CultureInfo.InvariantCulture); // This can happen when importing old APSIM file.
                else
                    soilcnr = Convert.ToDouble(SOM["SoilCN"], CultureInfo.InvariantCulture);
                SOM.Remove("SoilCN");
                double[] thickness = MathUtilities.StringsToDoubles(JsonUtilities.Values(SOM, "Thickness"));

                double[] SoilCNVector = new double[thickness.Length];

                for (int layer = 0; layer < thickness.Length; layer++)
                    SoilCNVector[layer] = soilcnr;

                JsonUtilities.SetValues(SOM, "SoilCN", SoilCNVector);
            }

        }

        /// <summary>
        /// Change Factor.Specifications to Factor.Specification. Also FactorValue
        /// becomes CompositeFactor.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion56(JToken root, string fileName)
        {
            foreach (var factor in JsonUtilities.ChildrenRecursively(root as JObject, "Factor"))
            {
                var parent = JsonUtilities.Parent(factor);

                string parentModelType = JsonUtilities.Type(parent);
                if (parentModelType == "Factors")
                {
                    var specifications = factor["Specifications"] as JArray;
                    if (specifications != null)
                    {
                        if (specifications.Count > 1)
                        {
                            // must be a compound factor. 

                            // Change our Factor to a CompositeFactor
                            factor["$type"] = "Models.Factorial.CompositeFactor, Models";

                            // Remove the Factor from it's parent.
                            var parentChildren = parent["Children"] as JArray;
                            parentChildren.Remove(factor);

                            // Create a new site factor and add our CompositeFactor to the children list.
                            var siteFactor = JsonUtilities.ChildWithName(parent as JObject, "Site") as JObject;
                            if (siteFactor == null)
                            {
                                // Create a site factor 
                                siteFactor = new JObject();
                                siteFactor["$type"] = "Models.Factorial.Factor, Models";
                                siteFactor["Name"] = "Site";
                                JArray siteFactorChildren = new JArray();
                                siteFactor["Children"] = siteFactorChildren;

                                // Add our new site factor to our models parent.
                                parentChildren.Add(siteFactor);
                            }
                            (siteFactor["Children"] as JArray).Add(factor);

                        }
                        else
                        {
                            // Convert array to string.
                            if (specifications.Count > 0)
                                factor["Specification"] = specifications[0].ToString();
                        }
                    }
                }
                else if (parentModelType == "Factor")
                {
                    factor["$type"] = "Models.Factorial.CompositeFactor, Models";
                }
            }

            foreach (var series in JsonUtilities.ChildrenRecursively(root as JObject, "Series"))
            {
                var factorToVaryColours = series["FactorToVaryColours"];
                if (factorToVaryColours != null && factorToVaryColours.Value<string>() == "Simulation")
                    series["FactorToVaryColours"] = "SimulationName";
                var factorToVaryMarkers = series["FactorToVaryMarkers"];
                if (factorToVaryMarkers != null && factorToVaryMarkers.Value<string>() == "Simulation")
                    series["FactorToVaryMarkers"] = "SimulationName";
                var factorToVaryLines = series["FactorToVaryLines"];
                if (factorToVaryLines != null && factorToVaryLines.Value<string>() == "Simulation")
                    series["FactorToVaryLines"] = "SimulationName";
            }
        }

        /// <summary>
        /// Upgrades to version 57. Adds a RetranslocateNonStructural node to
        /// all GenericOrgans which do not have a child called
        /// RetranslocateNitrogen.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion57(JObject root, string fileName)
        {
            foreach (JObject organ in JsonUtilities.ChildrenRecursively(root, "GenericOrgan"))
                if (JsonUtilities.ChildWithName(organ, "RetranslocateNitrogen") == null)
                    JsonUtilities.AddModel(organ, typeof(RetranslocateNonStructural), "RetranslocateNitrogen");
        }

        /// <summary>
        /// Upgrades to version 58. Renames 'ParamThickness' to 'Thickness' in Weirdo.
        /// Also change calls to property soil.SWAtWaterThickness to soil.Thickness.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion58(JObject root, string fileName)
        {
            foreach (JObject weirdo in JsonUtilities.ChildrenRecursively(root, "WEIRDO"))
            {
                var paramThicknessNode = weirdo["ParamThickness"];
                if (paramThicknessNode != null)
                {
                    weirdo["Thickness"] = paramThicknessNode;
                    weirdo.Remove("ParamThickness");
                }
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                if (manager.Replace(".SWAtWaterThickness", ".Thickness"))
                    manager.Save();
            }
        }

        /// <summary>
        /// Upgrades to version 59. Renames 'SoilCropOilPalm' to 'SoilCrop'.
        /// Renames Soil.SoilOrganicMatter.OC to Soil.Initial.OC
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion59(JObject root, string fileName)
        {
            foreach (var sample in JsonUtilities.ChildrenRecursively(root, "Sample"))
            {
                var array = sample["NO3"] as JArray;
                if (array != null)
                {
                    var nitrogenValue = new JObject();
                    nitrogenValue["$type"] = "Models.Soils.NitrogenValue, Models";

                    var storedAsPPM = sample["NO3Units"]?.ToString() == "0" ||
                                      sample["NO3Units"]?.ToString() == "ppm" ||
                                      sample["NO3Units"] == null; 

                    nitrogenValue["Values"] = array;
                    nitrogenValue["StoredAsPPM"] = storedAsPPM;
                    sample.Remove("NO3");
                    sample["NO3N"] = nitrogenValue;
                }

                array = sample["NH4"] as JArray;
                if (array != null)
                {
                    var nitrogenValue = new JObject();
                    nitrogenValue["$type"] = "Models.Soils.NitrogenValue, Models";

                    var storedAsPPM = sample["NH4Units"]?.ToString() == "0" ||
                                      sample["NH4Units"]?.ToString() == "ppm" ||
                                      sample["NH4Units"] == null;

                    nitrogenValue["Values"] = array;
                    nitrogenValue["StoredAsPPM"] = storedAsPPM;
                    sample.Remove("NH4");
                    sample["NH4N"] = nitrogenValue;
                }
            }
            foreach (var soilCropOilPalmNode in JsonUtilities.ChildrenRecursively(root, "SoilCropOilPalm"))
                soilCropOilPalmNode["$type"] = "Models.Soils.SoilCrop, Models";

            foreach (var report in JsonUtilities.ChildrenRecursively(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilOrganicMatter.OC", ".Initial.OC");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].PH", "[Soil].Initial.PH");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].EC", "[Soil].Initial.EC");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].ESP", "[Soil].Initial.ESP");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].Cl", "[Soil].Initial.CL");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].OC", "[Soil].Initial.OC");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].InitialNO3N", "[Soil].Initial.NO3N.PPM");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].InitialNH4N", "[Soil].Initial.NH4N.PPM");
            }

            foreach (var series in JsonUtilities.ChildrenRecursively(root, "Series"))
            {
                if (series["XFieldName"] != null)
                    series["XFieldName"] = series["XFieldName"].ToString().Replace(".SoilOrganicMatter.OC", ".Initial.OC");
                if (series["YFieldName"] != null)
                    series["YFieldName"] = series["YFieldName"].ToString().Replace(".SoilOrganicMatter.OC", ".Initial.OC");
            }

            foreach (var expressionFunction in JsonUtilities.ChildrenRecursively(root, "ExpressionFunction"))
            {
                var expression = expressionFunction["Expression"].ToString();
                expression = expression.Replace(".SoilOrganicMatter.OC", ".Initial.OC");
                expressionFunction["Expression"] = expression;
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                var changeMade = manager.Replace("Soil.ToCumThickness(soil.Thickness)", "soil.ThicknessCumulative");

                if (manager.Replace("mySoil.Depth.Length", "mySoil.Thickness.Length"))
                    changeMade = true;

                if (manager.Replace("soil.Depth.Length", "soil.Thickness.Length"))
                    changeMade = true;

                if (changeMade)
                    manager.Save();
            }
        }

        /// <summary>
        /// Convert no3 and nh4 parameters from ppm to kg/ha.
        /// </summary>
        /// <param name="values"></param>
        private static void ConvertToPPM(JArray values)
        {
            var sample = JsonUtilities.Parent(JsonUtilities.Parent(values));
            var soil = JsonUtilities.Parent(sample) as JObject;
            var water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "Water");
            if (water == null)
                water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "WEIRDO");

            // Get soil thickness and bulk density.
            var soilThickness = water["Thickness"].Values<double>().ToArray();
            var soilBD = water["BD"].Values<double>().ToArray();

            // Get sample thickness and bulk density.
            var sampleThickness = sample["Thickness"].Values<double>().ToArray();
            var sampleBD = Soils.Standardiser.Layers.MapConcentration(soilBD, soilThickness, sampleThickness, soilBD.Last());

            for (int i = 0; i < values.Count; i++)
                values[i] = values[i].Value<double>() * 100 / (sampleBD[i] * sampleThickness[i]);
        }

        /// <summary>
        /// Does the specified array have non NaN values?
        /// </summary>
        /// <param name="no3Values">The array to remove them from.</param>
        private static bool HasValues(JArray no3Values)
        {
            foreach (var value in no3Values)
                if (value.ToString() != "NaN")
                    return true;
            return false;
        }

        /// <summary>
        /// Upgrades to version 60. Move NO3 and NH4 from sample to Analaysis node
        /// and always store as ppm.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion60(JObject root, string fileName)
        {
            foreach (var sample in JsonUtilities.ChildrenRecursively(root, "Sample"))
            {
                var soil = JsonUtilities.Parent(sample) as JObject;
                var analysis = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "Analysis");
                var water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "Water");
                if (water == null)
                    water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "WEIRDO");

                var no3Node = sample["NO3N"];
                if (no3Node != null && no3Node.HasValues)
                {
                    if (analysis == null)
                        throw new Exception("Cannot find an analysis node while converting a soil sample.");

                    // Convert units to ppm if necessary.
                    var no3Values = no3Node["Values"] as JArray;

                    // Only overlay values if they are not NaN values.
                    if (HasValues(no3Values))
                    {
                        if (!no3Node["StoredAsPPM"].Value<bool>())
                            ConvertToPPM(no3Values);

                        // Make sure layers match analysis layers.
                        var analysisThickness = analysis["Thickness"].Values<double>().ToArray();
                        var sampleThickness = sample["Thickness"].Values<double>().ToArray();
                        var values = no3Values.Values<double>().ToArray();
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, sampleThickness, analysisThickness, 1.0);
                        no3Values = new JArray(mappedValues);

                        // Move from sample to analysis
                        analysis["NO3N"] = no3Values;
                    }
                }
                sample["NO3N"] = null;
                var nh4Node = sample["NH4N"];
                if (nh4Node != null && nh4Node.HasValues)
                {
                    if (analysis == null)
                        throw new Exception("Cannot find an analysis node while converting a soil sample.");

                    // Convert units to ppm if necessary.
                    var nh4Values = nh4Node["Values"] as JArray;

                    // Only overlay values if they are not NaN values.
                    if (HasValues(nh4Values))
                    {
                        if (!nh4Node["StoredAsPPM"].Value<bool>())
                            ConvertToPPM(nh4Values);

                        // Make sure layers match analysis layers.
                        var analysisThickness = analysis["Thickness"].Values<double>().ToArray();
                        var sampleThickness = sample["Thickness"].Values<double>().ToArray();
                        var values = nh4Values.Values<double>().ToArray();
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, sampleThickness, analysisThickness, 0.2);
                        nh4Values = new JArray(mappedValues);

                        // Move from sample to analysis
                        analysis["NH4N"] = nh4Values;
                    }
                }
                sample["NH4N"] = null;
            }
        }
        /// <summary>
        /// Upgrades to version 61. Rename the 'Water' node under soil to 'Physical'
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion61(JObject root, string fileName)
        {
            foreach (var water in JsonUtilities.ChildrenRecursively(root, "Water"))
            {
                water["$type"] = "Models.Soils.Physical, Models";
                water["Name"] = "Physical";
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".Water.", ".Physical.");
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "Factor"))
            {
                var specifications = factor["Specifications"];
                if (specifications != null)
                {
                    var specificationsString = specifications.ToString();
                    if (specificationsString.Contains(".Water."))
                    {
                        specificationsString = specificationsString.Replace(".Water.", ".Physical.");
                        factor["Specifications"] = specificationsString;
                    }
                }
            }
        }

        /// <summary>
        /// Upgrades to version 61. Ensures that a micromet model is within every simulation.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion61(JObject root, string fileName)
        {

            foreach (JObject Sim in JsonUtilities.ChildrenRecursively(root, "Simulation"))
            {
                List<JObject> MicroClimates = JsonUtilities.ChildrenRecursively(root, "MicroClimate");
                if (MicroClimates.Count == 0)
                    AddMicroClimate(Sim);
            }

        }
        /// <summary>
        /// Add a MicroClimate model to the specified JSON model token.
        /// </summary>
        /// <param name="simulation">An APSIM Simulation</param>
        public static void AddMicroClimate(JObject simulation)
        {
            JArray children = simulation["Children"] as JArray;
            if (children == null)
            {
                children = new JArray();
                simulation["Children"] = children;
            }

            JObject microClimateModel = new JObject();
            microClimateModel["$type"] = "Models.MicroClimate, Models";
            microClimateModel["Name"] = "MicroClimate";
            microClimateModel["a_interception"] = "0.0";
            microClimateModel["b_interception"] = "1.0";
            microClimateModel["c_interception"] = "0.0";
            microClimateModel["d_interception"] = "0.0";
            microClimateModel["soil_albedo"] = "0.13";
            microClimateModel["SoilHeatFluxFraction"] = "0.4";
            microClimateModel["NightInterceptionFraction"] = "0.5";
            microClimateModel["ReferenceHeight"] = "2.0";
            microClimateModel["IncludeInDocumentation"] = "true";
            microClimateModel["Enabled"] = "true";
            microClimateModel["ReadOnly"] = "false";
            var weathers = JsonUtilities.ChildrenOfType(simulation, "Weather");

            // Don't bother with microclimate if no weather component
            if (weathers.Count != 0)
            {
                var weather = weathers.First();
                int index = children.IndexOf(weather);
                children.Insert(index+1,microClimateModel);
            }
        }


        /// <summary>
        /// Changes initial Root Wt to an array.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion99(JObject root, string fileName)
        {
            // Delete all alias children.
            foreach (var soilNitrogen in JsonUtilities.ChildrenOfType(root, "SoilNitrogen"))
            {
                var parent = JsonUtilities.Parent(soilNitrogen);
                var nutrient = JsonUtilities.CreateNewChildModel(parent, "Nutrient", "Models.Soils.Nutrients.Nutrient");
                nutrient["ResourceName"] = "Nutrient";
                soilNitrogen.Remove();
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                manager.Replace("using Models.Soils;", "using Models.Soils;\r\nusing Models.Soils.Nutrients;");

                manager.Replace("SoilNitrogen.FOMN", ".Nutrient.FOMN");
                manager.Replace("SoilNitrogen.FOMC", ".Nutrient.FOMC");

                if (manager.Replace("Soil.SoilNitrogen.HumicN", "Humic.N"))
                    manager.AddDeclaration("NutrientPool", "Humic", new string[] { "[ScopedLinkByName]" });
                if (manager.Replace("Soil.SoilNitrogen.HumicC", "Humic.C"))
                    manager.AddDeclaration("NutrientPool", "Humic", new string[] { "[ScopedLinkByName]" });

                if (manager.Replace("Soil.SoilNitrogen.MicrobialN", "Microbial.N"))
                    manager.AddDeclaration("NutrientPool", "Microbial", new string[] { "[ScopedLinkByName]" });
                if (manager.Replace("Soil.SoilNitrogen.MicrobialC", "Microbial.C"))
                    manager.AddDeclaration("NutrientPool", "Microbial", new string[] { "[ScopedLinkByName]" });

                if (manager.Replace("Soil.SoilNitrogen.dlt_n_min_res", "SurfaceResidueDecomposition.MineralisedN"))
                    manager.AddDeclaration("CarbonFlow", "SurfaceResidueDecomposition", new string[] { "[LinkByPath(Path=\"[Nutrient].SurfaceResidue.Decomposition\")]" });
                
                manager.Replace("SoilNitrogen.MineralisedN", "Nutrient.MineralisedN");

                manager.Replace("SoilNitrogen.TotalN", "Nutrient.TotalN");
                if (manager.Replace("SoilNitrogen.TotalN", "Nutrient.TotalN"))
                {
                    manager.RemoveDeclaration("SoilNitrogen");
                    manager.AddDeclaration("INutrient", "Nutrient", new string[] { "[ScopedLinkByName]" });
                }

                manager.Replace("SoilNitrogen.TotalC", "Nutrient.TotalC");
                if (manager.Replace("SoilNitrogen.TotalC", "Nutrient.TotalC"))
                {
                    manager.RemoveDeclaration("SoilNitrogen");
                    manager.AddDeclaration("INutrient", "Nutrient", new string[] { "[ScopedLinkByName]" });
                }

                manager.Replace("SoilNitrogen.mineral_n", "Nutrient.MineralN");
                manager.Replace("SoilNitrogen.Denitrification", "Nutrient.Natm");
                manager.Replace("SoilNitrogen.n2o_atm", "Nutrient.N2Oatm");
                manager.Save();
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NO3.kgha", "Nutrient.NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NH4.kgha", "Nutrient.NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.Urea.kgha", "Nutrient.Urea.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNO3.kgha", "Nutrient.PlantAvailableNO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNH4.kgha", "Nutrient.PlantAvailableNH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].NO3.kgha", "[Nutrient].NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].NH4.kgha", "[Nutrient].NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].Urea.kgha", "[Nutrient].Urea.kgha");

                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.FOMN", ".Nutrient.FOMN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.FOMC", ".Nutrient.FOMC");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.HumicN", ".Nutrient.Humic.N");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.HumicC", ".Nutrient.Humic.C");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.MicrobialN", ".Nutrient.Microbial.N");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.MicrobialC", ".Nutrient.Microbial.C");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.urea", ".Nutrient.Urea.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.dlt_n_min_res", ".Nutrient.SurfaceResidue.Decomposition.MineralisedN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.MineralisedN", ".Nutrient.MineralisedN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.Denitrification", ".Nutrient.Natm");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.n2o_atm", ".Nutrient.N2Oatm");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.TotalC", ".Nutrient.TotalC");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.TotalN", ".Nutrient.TotalN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.mineral_n", ".Nutrient.MineralN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.Nitrification", ".Nutrient.NH4.Nitrification");
            }

            foreach (var series in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (series["XFieldName"] != null)
                {
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NO3.kgha", "Nutrient.NO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NH4.kgha", "Nutrient.NH4.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.Urea.kgha", "Nutrient.Urea.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3.kgha", "Nutrient.PlantAvailableNO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4.kgha", "Nutrient.PlantAvailableNH4.kgha");
                }
                if (series["YFieldName"] != null)
                {
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NO3.kgha", "Nutrient.NO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NH4.kgha", "Nutrient.NH4.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.Urea.kgha", "Nutrient.Urea.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3.kgha", "Nutrient.PlantAvailableNO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4.kgha", "Nutrient.PlantAvailableNH4.kgha");
                }
            }

        }
    }
}

