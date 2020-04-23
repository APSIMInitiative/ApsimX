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

        /// <summary>Get a list of all animal types.</summary>
        public IEnumerable<string> GetAnimalTypes()
        {
            return GetGenotypes().Select(genotype => genotype.Animal.ToString()).Distinct();
        }

        /// <summary>Get a list of genotype names for a specific animal type.</summary>
        /// <param name="animalType">The animal type.</param>
        public IEnumerable<string> GetGenotypeNamesForAnimalType(string animalType)
        {
            return GetGenotypes().Where(genotype => genotype.Animal.ToString() == animalType)
                                 .Select(genotype => genotype.Name);
        }

        /// <summary>
        /// Create a genotype cross.                                      
        /// </summary>
        /// <param name="nameOfNewGenotype">Name of new genotype. Can be null.</param>
        /// <param name="damBreedName">Dam breed name.</param>
        /// <param name="damProportion">Proportion dam.</param>
        /// <param name="sireBreedName">Sire breed name.</param>
        /// <param name="sireProportion">Proportion sire.</param>
        public AnimalParamSet CreateGenotypeCross(string nameOfNewGenotype,
                                                  string damBreedName, double damProportion,
                                                  string sireBreedName, double sireProportion)
        {
            if (damProportion + sireProportion != 1)
                throw new Exception("When creating a cross breed the total proportions must be equal to one.");

            var damBreed = GetGenotype(damBreedName);
            damBreed.EnglishName = damBreedName;
            damBreed.DeriveParams();
            damBreed.Initialise();
            var sireBreed = GetGenotype(sireBreedName);
            sireBreed.EnglishName = sireBreedName;
            sireBreed.DeriveParams();
            sireBreed.Initialise();

            var newGenotype = new AnimalParamSet();
            newGenotype.Name = nameOfNewGenotype;
            newGenotype.sEditor = damBreed.sEditor;
            newGenotype.sEditDate = damBreed.sEditDate;
            newGenotype.Animal = damBreed.Animal;
            newGenotype.bDairyBreed = damBreed.bDairyBreed;
            newGenotype.MaxYoung = damBreed.MaxYoung;
            newGenotype.OvulationPeriod = damBreed.OvulationPeriod;
            newGenotype.Puberty = damBreed.Puberty;

            newGenotype.FBreedSRW = damProportion * damBreed.FBreedSRW + sireProportion * sireBreed.FBreedSRW;
            newGenotype.FPotFleeceWt = damProportion * damBreed.FPotFleeceWt + sireProportion * sireBreed.FPotFleeceWt;
            newGenotype.FDairyIntakePeak = damProportion * damBreed.FDairyIntakePeak + sireProportion * sireBreed.FDairyIntakePeak;
            newGenotype.FleeceRatio = damProportion * damBreed.FleeceRatio + sireProportion * sireBreed.FleeceRatio;
            newGenotype.MaxFleeceDiam = damProportion * damBreed.MaxFleeceDiam + sireProportion * sireBreed.MaxFleeceDiam;
            newGenotype.PeakMilk = damProportion * damBreed.PeakMilk + sireProportion * sireBreed.PeakMilk;
            for (int idx = 1; idx <= 2; idx++)
                newGenotype.MortRate[idx] = damProportion * damBreed.MortRate[idx] + sireProportion * sireBreed.MortRate[idx];
            for (int idx = 1; idx <= 2; idx++)
                newGenotype.MortAge[idx] = damProportion * damBreed.MortAge[idx] + sireProportion * sireBreed.MortAge[idx];
            newGenotype.MortIntensity = damProportion * damBreed.MortIntensity + sireProportion * sireBreed.MortIntensity;
            newGenotype.MortCondConst = damProportion * damBreed.MortCondConst + sireProportion * sireBreed.MortCondConst;
            newGenotype.MortWtDiff = damProportion * damBreed.MortWtDiff + sireProportion * sireBreed.MortWtDiff;

            for (int idx = 0; idx < newGenotype.SRWScalars.Length; idx++) newGenotype.SRWScalars[idx] = damProportion * damBreed.SRWScalars[idx] + sireProportion * sireBreed.SRWScalars[idx];
            for (int idx = 1; idx < newGenotype.GrowthC.Length; idx++) newGenotype.GrowthC[idx] = damProportion * damBreed.GrowthC[idx] + sireProportion * sireBreed.GrowthC[idx];
            for (int idx = 1; idx < newGenotype.IntakeC.Length; idx++) newGenotype.IntakeC[idx] = damProportion * damBreed.IntakeC[idx] + sireProportion * sireBreed.IntakeC[idx];
            for (int idx = 0; idx < newGenotype.IntakeLactC.Length; idx++) newGenotype.IntakeLactC[idx] = damProportion * damBreed.IntakeLactC[idx] + sireProportion * sireBreed.IntakeLactC[idx];
            for (int idx = 1; idx < newGenotype.GrazeC.Length; idx++) newGenotype.GrazeC[idx] = damProportion * damBreed.GrazeC[idx] + sireProportion * sireBreed.GrazeC[idx];
            for (int idx = 1; idx < newGenotype.EfficC.Length; idx++) newGenotype.EfficC[idx] = damProportion * damBreed.EfficC[idx] + sireProportion * sireBreed.EfficC[idx];
            for (int idx = 1; idx < newGenotype.MaintC.Length; idx++) newGenotype.MaintC[idx] = damProportion * damBreed.MaintC[idx] + sireProportion * sireBreed.MaintC[idx];
            for (int idx = 1; idx < newGenotype.DgProtC.Length; idx++) newGenotype.DgProtC[idx] = damProportion * damBreed.DgProtC[idx] + sireProportion * sireBreed.DgProtC[idx];
            for (int idx = 1; idx < newGenotype.ProtC.Length; idx++) newGenotype.ProtC[idx] = damProportion * damBreed.ProtC[idx] + sireProportion * sireBreed.ProtC[idx];
            for (int idx = 1; idx < newGenotype.PregC.Length; idx++) newGenotype.PregC[idx] = damProportion * damBreed.PregC[idx] + sireProportion * sireBreed.PregC[idx];
            for (int idx = 1; idx < newGenotype.PregScale.Length; idx++) newGenotype.PregScale[idx] = damProportion * damBreed.PregScale[idx] + sireProportion * sireBreed.PregScale[idx];
            for (int idx = 1; idx < newGenotype.BirthWtScale.Length; idx++) newGenotype.BirthWtScale[idx] = damProportion * damBreed.BirthWtScale[idx] + sireProportion * sireBreed.BirthWtScale[idx];
            for (int idx = 1; idx < newGenotype.PeakLactC.Length; idx++) newGenotype.PeakLactC[idx] = damProportion * damBreed.PeakLactC[idx] + sireProportion * sireBreed.PeakLactC[idx];
            for (int idx = 1; idx < newGenotype.LactC.Length; idx++) newGenotype.LactC[idx] = damProportion * damBreed.LactC[idx] + sireProportion * sireBreed.LactC[idx];
            for (int idx = 1; idx < newGenotype.WoolC.Length; idx++) newGenotype.WoolC[idx] = damProportion * damBreed.WoolC[idx] + sireProportion * sireBreed.WoolC[idx];
            for (int idx = 1; idx < newGenotype.ChillC.Length; idx++) newGenotype.ChillC[idx] = damProportion * damBreed.ChillC[idx] + sireProportion * sireBreed.ChillC[idx];
            for (int idx = 1; idx < newGenotype.GainC.Length; idx++) newGenotype.GainC[idx] = damProportion * damBreed.GainC[idx] + sireProportion * sireBreed.GainC[idx];
            for (int idx = 1; idx < newGenotype.PhosC.Length; idx++) newGenotype.PhosC[idx] = damProportion * damBreed.PhosC[idx] + sireProportion * sireBreed.PhosC[idx];
            for (int idx = 1; idx < newGenotype.SulfC.Length; idx++) newGenotype.SulfC[idx] = damProportion * damBreed.SulfC[idx] + sireProportion * sireBreed.SulfC[idx];
            for (int idx = 1; idx < newGenotype.MethC.Length; idx++) newGenotype.MethC[idx] = damProportion * damBreed.MethC[idx] + sireProportion * sireBreed.MethC[idx];
            for (int idx = 1; idx < newGenotype.AshAlkC.Length; idx++) newGenotype.AshAlkC[idx] = damProportion * damBreed.AshAlkC[idx] + sireProportion * sireBreed.AshAlkC[idx];
            for (int idx = 1; idx < newGenotype.DayLengthConst.Length; idx++) newGenotype.DayLengthConst[idx] = damProportion * damBreed.DayLengthConst[idx] + sireProportion * sireBreed.DayLengthConst[idx];
            for (int idx = 0; idx < newGenotype.ToxaemiaSigs.Length; idx++) newGenotype.ToxaemiaSigs[idx] = damProportion * damBreed.ToxaemiaSigs[idx] + sireProportion * sireBreed.ToxaemiaSigs[idx];
            for (int idx = 0; idx < newGenotype.DystokiaSigs.Length; idx++) newGenotype.DystokiaSigs[idx] = damProportion * damBreed.DystokiaSigs[idx] + sireProportion * sireBreed.DystokiaSigs[idx];
            for (int idx = 0; idx < newGenotype.ExposureConsts.Length; idx++) newGenotype.ExposureConsts[idx] = damProportion * damBreed.ExposureConsts[idx] + sireProportion * sireBreed.ExposureConsts[idx];

            newGenotype.FertWtDiff = damProportion * damBreed.FertWtDiff + sireProportion * sireBreed.FertWtDiff;
            newGenotype.SelfWeanPropn = damProportion * damBreed.SelfWeanPropn + sireProportion * sireBreed.SelfWeanPropn;
            for (int idx = 1; idx < newGenotype.ConceiveSigs.Length; idx++)
                for (int Jdx = 0; Jdx < newGenotype.ConceiveSigs[idx].Length; Jdx++)
                    newGenotype.ConceiveSigs[idx][Jdx] = damProportion * damBreed.ConceiveSigs[idx][Jdx] + sireProportion * sireBreed.ConceiveSigs[idx][Jdx];

            for (int idx = 0; idx <= newGenotype.FParentage.Length - 1; idx++)
                newGenotype.FParentage[idx].fPropn = 0.0;

            SetParentage(damBreed, damProportion, newGenotype);
            SetParentage(sireBreed, sireProportion, newGenotype);

            // Add the new genotype into our list.
            if (userGenotypes == null)
                userGenotypes = new List<AnimalParamSet>();
            userGenotypes.Add(newGenotype);

            return newGenotype;
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
            ConvertArrayToCommands(parameterNode, "c-i-", "IntakeC", commands, 22);
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

        /// <summary>
        /// Set parentage of a genotype.
        /// </summary>
        /// <param name="parentBreed">The parent breed.</param>
        /// <param name="proportion">The proportion of the parent.</param>
        /// <param name="newGenotype">The genotype to set the parentage in.</param>
        private static void SetParentage(AnimalParamSet parentBreed, double proportion, AnimalParamSet newGenotype)
        {
            for (int k = 0; k <= parentBreed.FParentage.Length - 1; k++)
            {
                int i = 0;
                while ((i < newGenotype.FParentage.Length) && (parentBreed.FParentage[k].sBaseBreed != newGenotype.FParentage[i].sBaseBreed))
                    i++;
                if (i == newGenotype.FParentage.Length)
                {
                    Array.Resize(ref newGenotype.FParentage, i + 1);
                    newGenotype.FParentage[i].sBaseBreed = parentBreed.FParentage[k].sBaseBreed;
                    newGenotype.FParentage[i].fPropn = 0.0;
                }
                newGenotype.FParentage[i].fPropn = newGenotype.FParentage[i].fPropn
                                            + proportion * parentBreed.FParentage[k].fPropn;
            }
        }
    }
}