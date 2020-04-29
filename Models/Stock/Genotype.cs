namespace Models.GrazPlan
{
    using APSIM.Shared.Utilities;
    using Models.Core.Run;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Encapsulates a single genotype.
    /// </summary>
    [Serializable]
    public class Genotype
    {
        private List<string> parameterXmlSections = new List<string>();

        private AnimalParamSet parameters;

        /// <summary>Constructor for a genotype from ruminant.prm.</summary>
        /// <param name="parameterNode">The ruminant.prm xml node where this genotype is defined.</param>
        public Genotype(XmlNode parameterNode)
        {
            parameterXmlSections.Add(parameterNode.OuterXml);
            var parent = parameterNode.ParentNode;
            while (!(parent is XmlDocument))
            {
                parameterXmlSections.Add(parent.OuterXml);
                parent = parent.ParentNode;
            }

            Name = XmlUtilities.Attribute(parameterNode, "name").Replace(".", "");
            AnimalType = XmlUtilities.Value(parameterNode, "animal");
            var parentNode = parameterNode.ParentNode;
            while (AnimalType == string.Empty && parentNode != null)
            {
                AnimalType = XmlUtilities.Value(parentNode, "animal");
                parentNode = parentNode.ParentNode;
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="animalParameterSet"></param>
        public Genotype(AnimalParamSet animalParameterSet)
        {
            parameters = animalParameterSet;
            Name = animalParameterSet.Name;
            AnimalType = animalParameterSet.Animal.ToString();
        }

        /// <summary>Animal type</summary>
        public string AnimalType { get; }

        /// <summary>Name of genotype.</summary>
        public string Name { get; }

        /// <summary>Gets genotype parameters.</summary>
        public AnimalParamSet Parameters
        {
            get
            {
                if (parameters != null)
                    return parameters;
                if (parameterXmlSections.Count > 0)
                    return ReadParametersFromPRM(parameterXmlSections);
                throw new Exception($"Cannot find any stock parameters for genotype {Name}");
            }
        }

        /// <summary>
        /// Get an animal parameter set for the given genotype name. Will throw if cannot find genotype.
        /// </summary>
        /// <param name="parameterXmlSections">The xml node sections to read from.</param>
        private AnimalParamSet ReadParametersFromPRM(List<string> parameterXmlSections)
        {
            // Parse the xml
            var overrides = new List<PropertyReplacement>();
            foreach (var parameterXml in parameterXmlSections)
            {
                // Load XML
                var doc = new XmlDocument();
                doc.LoadXml(parameterXml);
                var parameterNode = doc.DocumentElement as XmlNode;

                // Read prm section from XML and add to our overrides list.
                overrides.AddRange(ReadPRMSection(parameterNode));
            }

            // Reverse the overrides so the the general ones are first and the more specfic ones are last.
            overrides.Reverse();

            // Convert the overrides into an AnimalParamSet by applying all overrides from general to specific.
            parameters = new AnimalParamSet();
            parameters.Name = Name;
            if (AnimalType == "cattle")
                parameters.Animal = GrazType.AnimalType.Cattle;
            else if (AnimalType == "sheep")
                parameters.Animal = GrazType.AnimalType.Sheep;
            overrides.ForEach(genotype => genotype.Replace(parameters));

            return parameters;
        }

        /// <summary>
        /// Read and convert a single section in a .prm file to a set of command overrides.
        /// </summary>
        /// <param name="parameterNode"></param>
        /// <returns></returns>
        private List<PropertyReplacement> ReadPRMSection(XmlNode parameterNode)
        {
            var commands = new List<PropertyReplacement>();
            ConvertScalarToCommand(parameterNode, "editor", "sEditor", commands);
            ConvertScalarToCommand(parameterNode, "edited", "sEditDate", commands);
            var dairyString = XmlUtilities.Value(parameterNode, "dairy");
            if (dairyString == "true")
                commands.Add(new PropertyReplacement("bDairyBreed", true));
            else if (dairyString == "false")
                commands.Add(new PropertyReplacement("bDairyBreed", false));
            ConvertScalarToCommand(parameterNode, "srw", "BreedSRW", commands);
            ConvertScalarToCommand(parameterNode, "c-pfw", "FleeceRatio", commands);
            ConvertScalarToCommand(parameterNode, "c-mu", "MaxFleeceDiam", commands);
            ConvertArrayToCommands(parameterNode, "c-srs-", "SRWScalars", commands, 2);
            ConvertScalarToCommand(parameterNode, "c-swn", "SelfWeanPropn", commands);
            ConvertArrayToCommands(parameterNode, "c-n-", "GrowthC", commands, 5);
            ConvertArrayToCommands(parameterNode, "c-i-", "IntakeC", commands, 22);
            ConvertArrayToScalars(parameterNode, "c-idy-", new string[] { "FDairyIntakePeak", "FDairyIntakeTime", "FDairyIntakeShape" }, commands);
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
            ConvertArrayToScalars(parameterNode, "c-d-",
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
            return commands;
        }


        /// <summary>
        /// Convert an XML scalar parameter into a command.
        /// </summary>
        /// <param name="parameterNode">The XML parameter node.</param>
        /// <param name="parameterName">The name of the XML child parameter.</param>
        /// <param name="animalParamName">The name of a GrazPlan parameter.</param>
        /// <param name="commands">The list of comamnds to add to.</param>
        private static void ConvertScalarToCommand(XmlNode parameterNode, string parameterName, string animalParamName, List<PropertyReplacement> commands)
        {
            var value = XmlUtilities.Value(parameterNode, parameterName);
            if (!string.IsNullOrEmpty(value))
                commands.Add(new PropertyReplacement(animalParamName, value));
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
                                                   string animalParamName, List<PropertyReplacement> commands,
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
                            commands.Add(new PropertyReplacement($"{animalParamName}[{i+2}]", values[i]));  // 1 based array indexing before equals sign.
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
                        commands.Add(new PropertyReplacement($"{animalParamName}[{index + 1}]", stringValue));   // 1 based array indexing before equals sign.
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
                        commands.Add(new PropertyReplacement(animalParamName, stringValue));
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
                                                  string[] animalParamNames, List<PropertyReplacement> commands)
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
                        commands.Add(new PropertyReplacement(animalParamNames[i], values[i]));
                }
            }
        }

    }
}
