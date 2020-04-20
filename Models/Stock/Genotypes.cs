namespace Models.GrazPlan
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.PMF;
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Linq;

    /// <summary>
    /// Encapsulates a collection of stock genotype parameters. It can read the GrazPlan .prm
    /// files as well as the APSIM ruminant JSON file format.
    /// </summary>
    [Serializable]
    public class Genotypes
    {
        /// <summary>
        /// 'Standard' genotypes. By default these are read from the Ruminant.json resource.
        /// They can also be replaced by a GrazPlan PRM file.
        /// </summary>
        private Cultivar standardGenotypes;

        /// <summary>
        /// User supplied genotypes. These are searched first when looking for genotypes.
        /// </summary>
        private List<AnimalParamSet> userGenotypes;

        /// <summary>Set the standard genotypes from the specified GrazPlan PRM XML string</summary>
        /// <param name="xmlString">The GrazPlan PRM XML string.</param>
        public Cultivar LoadPRMXml(string xmlString)
        {
            var xml = new XmlDocument();
            xml.LoadXml(xmlString);
            var parameters = xml.DocumentElement;

            standardGenotypes = ReadPRM(parameters);
            Apsim.ParentAllChildren(standardGenotypes);
            return standardGenotypes;
        }

        /// <summary>Set the user specified genotypes.</summary>
        /// <param name="genotypes">The user specified genotypes.</param>
        public void SetUserGenotypes(IEnumerable<AnimalParamSet> genotypes)
        {
            userGenotypes = genotypes.ToList();
        }

        /// <summary>
        /// Get an animal parameter set for the given genotype name. Will throw if cannot find genotype.
        /// </summary>
        /// <param name="genotypeName">Name of genotype to locate and return.</param>
        public AnimalParamSet GetGenotype(string genotypeName)
        {
            // Look in user genotypes first.
            if (userGenotypes != null)
            {
                var animalParamSet = userGenotypes.Find(genotype => genotype.Name == genotypeName);
                if (animalParamSet != null)
                    return animalParamSet;
            }
            // Didn't find a user genotype. Look for it in standard genotypes.
            EnsureStandardGenotypesLoaded();
            var specificGenotype = Apsim.Find(standardGenotypes, genotypeName) as Cultivar;

            // Did we find the genotype? If not throw exception.
            if (specificGenotype == null)
                throw new Exception($"Cannot find stock genotype {genotypeName}");

            // Convert the genotype into an AnimalParamSet
            var animal = new AnimalParamSet();
            animal.Name = specificGenotype.Name;

            // From root genotype to specific genotype, apply genotype overrides.
            // Get a list of all genotypes to apply from specific genotype to more general parent genotypes.
            var genotypesToApply = new List<Cultivar>();
            genotypesToApply.Add(specificGenotype);
            var moreGeneralGenotype = specificGenotype.Parent as Cultivar;
            while (moreGeneralGenotype != null)
            {
                genotypesToApply.Add(moreGeneralGenotype);
                moreGeneralGenotype = moreGeneralGenotype.Parent as Cultivar;
            }

            // Now reverse the list so that we apply them from general to specific.
            genotypesToApply.Reverse();

            // Apply all genotypes from general to specific.
            genotypesToApply.ForEach(genotype => genotype.Apply(animal));

            return animal;
        }

        /// <summary>
        /// Return a collection of all genotypes.
        /// </summary>
        public IEnumerable<AnimalParamSet> GetGenotypes()
        {
            var genotypes = new List<AnimalParamSet>();

            // Get a list of genotype names.
            var genotypeNames = new List<string>();
            if (userGenotypes != null)
                genotypeNames.AddRange(userGenotypes.Select(genotype => genotype.Name));

            EnsureStandardGenotypesLoaded();
            genotypeNames.AddRange(Apsim.ChildrenRecursively(standardGenotypes).Select(genotype => genotype.Name));

            // Convert each name into a genotype and store in return list.
            foreach (var genotypeName in genotypeNames.Distinct())
                genotypes.Add(GetGenotype(genotypeName));
            return genotypes;
        }

        /// <summary>
        /// Read a parameter set and append to the json array.
        /// </summary>
        /// <param name="parameterNode">The XML parameter node to convert.</param>
        private Cultivar ReadPRM(XmlNode parameterNode)
        {
            var animalParamSet = new Cultivar();
            animalParamSet.Name = XmlUtilities.Attribute(parameterNode, "Name").Replace(".", "");

            var commands = new List<string>();
            ConvertScalarToCommand(parameterNode, "editor", "sEditor", commands);
            ConvertScalarToCommand(parameterNode, "edited", "sEditDate", commands);
            var dairyString = XmlUtilities.Value(parameterNode, "dairy");
            if (dairyString == "true")
                commands.Add($"bDairyBreed = True");
            else if(dairyString == "false")
                commands.Add($"bDairyBreed = False");
            ConvertScalarToCommand(parameterNode, "srw", "BreedSRW", commands);
            ConvertScalarToCommand(parameterNode, "c-pfw", "FleeceRatio", commands);
            ConvertScalarToCommand(parameterNode, "c-mu", "MaxFleeceDiam", commands);
            ConvertArrayToCommands(parameterNode, "c-srs-", "SRWScalars", commands, 2);
            ConvertScalarToCommand(parameterNode, "c-swn", "SelfWeanPropn", commands);
            ConvertArrayToCommands(parameterNode, "c-n-", "GrowthC", commands, 5);
            ConvertArrayToCommands(parameterNode, "c-i-", "IntakeC", commands, 21);
            ConvertArrayToScalars(parameterNode,  "c-idy-", new string[] { "FDairyIntakePeak", "FDairyIntakeTime", "FDairyIntakeShape" }, commands);
            ConvertArrayToCommands(parameterNode, "c-imx-", "IntakeLactC", commands, 4);
            ConvertArrayToCommands(parameterNode, "c-r-", "GrazeC", commands, 21);
            ConvertArrayToCommands(parameterNode, "c-k-", "EfficC", commands, 17);
            ConvertArrayToCommands(parameterNode, "c-m-", "MaintC", commands, 18);
            ConvertArrayToCommands(parameterNode, "c-rd-", "DgProtC", commands, 9);
            ConvertArrayToCommands(parameterNode, "c-a-", "ProtC", commands, 10);
            ConvertArrayToCommands(parameterNode, "c-p-", "PregC", commands, 14);
            ConvertArrayToCommands(parameterNode, "c-p14-", "PregScale", commands, 4);
            ConvertArrayToCommands(parameterNode, "c-p15-", "BirthWtScale", commands, 4);
            ConvertArrayToCommands(parameterNode, "c-l0-", "PeakLactC", commands, 4);
            ConvertArrayToCommands(parameterNode, "c-l-", "LactC", commands, 26);
            ConvertArrayToCommands(parameterNode, "c-w-", "WoolC", commands, 15);
            ConvertArrayToCommands(parameterNode, "c-c-", "ChillC", commands, 17);
            ConvertArrayToCommands(parameterNode, "c-g-", "GainC", commands, 19);
            ConvertArrayToCommands(parameterNode, "c-ph-", "PhosC", commands, 16);
            ConvertArrayToCommands(parameterNode, "c-su-", "SulfC", commands, 5);
            ConvertArrayToCommands(parameterNode, "c-h-", "MethC", commands, 8);
            ConvertArrayToCommands(parameterNode, "c-aa-", "AshAlkC", commands, 4);
            ConvertArrayToCommands(parameterNode, "c-f1-", "DayLengthConst", commands, 4);

            ConvertArrayToCommands(parameterNode, "c-f2-", "F2", commands, 4);  // ConceiveSigs[][0]
            ConvertArrayToCommands(parameterNode, "c-f3-", "F3", commands, 4);  // ConceiveSigs[][1]
            ConvertArrayToScalars(parameterNode,  "c-d-", 
                                 new string[]
                                 {
                                     "MortRate[2]", "MortIntensity", "MortCondConst",
                                     "ToxaemiaSigs[1]", "ToxaemiaSigs[2]",              // NOTE 1 based array indexing.
                                     "DystokiaSigs[1]", "DystokiaSigs[2]",
                                     "ExposureConsts[1]", "ExposureConsts[2]", "ExposureConsts[3]", "ExposureConsts[4]",
                                     "MortWtDiff", "MortRate[3]", "MortAge[2]", "MortAge[3]"
                                 }, commands);
            ConvertScalarToCommand(parameterNode, "c-f4", "OvulationPeriod", commands);
            ConvertArrayToCommands(parameterNode, "c-pbt-", "Puberty", commands, 2);
            var animalType = XmlUtilities.Value(parameterNode, "animal");
            if (string.Equals(animalType, "Cattle", StringComparison.InvariantCultureIgnoreCase))
                commands.Add($"Animal = Cattle");
            else if (string.Equals(animalType, "Sheep", StringComparison.InvariantCultureIgnoreCase))
                commands.Add($"Animal = Sheep");
            else if (!string.IsNullOrEmpty(animalType))
                throw new Exception($"Invalid animal type {animalType}");                

            animalParamSet.Command = commands.ToArray();

            // recurse through child parameter sets.
            foreach (var child in XmlUtilities.ChildNodes(parameterNode, "set"))
                animalParamSet.Children.Add(ReadPRM(child));

            return animalParamSet;
        }

        /// <summary>
        /// Convert an XML scalar parameter into a command.
        /// </summary>
        /// <param name="parameterNode">The XML parameter node.</param>
        /// <param name="parameterName">The name of the XML child parameter.</param>
        /// <param name="animalParamName">The name of a GrazPlan parameter.</param>
        /// <param name="commands">The list of comamnds to add to.</param>
        private static void ConvertScalarToCommand(XmlNode parameterNode, string parameterName, string animalParamName, List<string> commands)
        {
            var value = XmlUtilities.Value(parameterNode, parameterName);
            if (!string.IsNullOrEmpty(value))
                commands.Add($"{animalParamName} = {value}");
        }

        /// <summary>
        /// Convert an XML parameter array into a series of commands.
        /// </summary>
        /// <param name="parentNode">The XML parameter node.</param>
        /// <param name="parameterName">The name of the XML child parameter.</param>
        /// <param name="animalParamName">The name of a GrazPlan parameter.</param>
        /// <param name="commands">The list of comamnds to add to.</param>
        /// <param name="numValuesInArray">The number of values that should be in the array.</param>
        private static void ConvertArrayToCommands(XmlNode parentNode, string parameterName, 
                                                   string animalParamName, List<string> commands,
                                                   int numValuesInArray)
        {
            var parameterNode = FindChildWithPrefix(parentNode, parameterName);
            if (parameterNode != null)
            {
                var stringValue = parameterNode.InnerText;
                bool hasMissingValues = stringValue.StartsWith(",") || stringValue.EndsWith(",") || stringValue.Contains(",,");
                if (hasMissingValues)
                {
                    var values = stringValue.Split(',');
                    for (int i = 0; i != values.Length; i++)
                    {
                        if (values[i] != string.Empty)
                            commands.Add($"{animalParamName}[{i+2}] = {values[i]}");  // 1 based array indexing before equals sign.
                    }
                }
                else
                {
                    // See if an index was specified as part of the parameter name.
                    var nodeName = XmlUtilities.Attribute(parameterNode, "name");
                    if (nodeName != parameterName)
                    {
                        // There must be an index specified e.g. c-w-0
                        var index = Convert.ToInt32(nodeName.Replace(parameterName, ""));
                        commands.Add($"{animalParamName}[{index + 1}] = {stringValue}");   // 1 based array indexing before equals sign.
                    }
                    else
                    {
                        // Determine if we need to add another value to the top of the values list 
                        // so that the number of values matches the array length definition in the animprm.cs code.
                        var values = stringValue.Split(',');
                        if (values.Length != numValuesInArray)
                        {
                            // Add a zero to the top of the list of values.
                            var valuesList = new List<string>(values);
                            valuesList.Insert(0, "0");
                            values = valuesList.ToArray();
                        }
                        // We build the string value.
                        stringValue = StringUtilities.BuildString(values, ",");

                        // Create the command.
                        commands.Add($"{animalParamName} = {stringValue}");
                    }
                }
            }
        }

        /// <summary>Finds a direct child of the specified node that has a name starting with a prefix.</summary>
        /// <param name="node">The node.</param>
        /// <param name="namePrefix">The beginning of a name attribute to find.</param>
        /// <returns></returns>
        private static XmlNode FindChildWithPrefix(XmlNode node, string namePrefix)
        {
            if (node != null)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    var name = XmlUtilities.Attribute(child, "name");
                    if (name != null && name.StartsWith(namePrefix))
                        return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Convert an XML parameter array into a series of different animal parameters.
        /// </summary>
        /// <param name="parameterNode">The XML parameter node.</param>
        /// <param name="parameterName">The name of the XML child parameter.</param>
        /// <param name="animalParamNames">The names of a multiple GrazPlan paramaters, one for each parameter value.</param>
        /// <param name="commands">The list of comamnds to add to.</param>
        private static void ConvertArrayToScalars(XmlNode parameterNode, string parameterName,
                                                  string[] animalParamNames, List<string> commands)
        {
            var stringValue = XmlUtilities.Value(parameterNode, parameterName);
            if (!string.IsNullOrEmpty(stringValue))
            {
                var values = stringValue.Split(',');
                if (values.Length != animalParamNames.Length)
                    throw new Exception($"Invalid number of values found for parameter {parameterName}");
                for (int i = 0; i < values.Length; i++)
                {
                    if (!string.IsNullOrEmpty(values[i]))
                        commands.Add($"{animalParamNames[i]} = {values[i]}");
                }
            }
        }

        /// <summary>
        /// Ensure the standard genotypes are loaded.
        /// </summary>
        private void EnsureStandardGenotypesLoaded()
        {
            if (standardGenotypes == null)
            {
                // No - load them now.
                LoadPRMXml(ReflectionUtilities.GetResourceAsString("Models.Resources.ruminant.prm"));
            }
        }
    }
}