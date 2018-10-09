namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Runners;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// # [Name]
    /// Encapsulates a factorial experiment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ExperimentView")]
    [PresenterName("UserInterface.Presenters.ExperimentPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Experiment : Model, ISimulationGenerator, ICustomDocumentation
    {
        [Link]
        IStorageReader storage = null;

        private List<List<FactorValue>> allCombinations;
        private Stream serialisedBase;
        private Simulations parentSimulations;

        /// <summary>
        /// List of names of the disabled simulations. Any simulation name not in this list is assumed to be enabled.
        /// </summary>
        public List<string> DisabledSimNames { get; set; }

        /// <summary>Simulation runs are about to begin.</summary>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun()
        {
            Initialise();
        }

        /// <summary>Gets the next job to run</summary>
        public Simulation NextSimulationToRun(bool fullFactorial = true)
        {
            if (allCombinations == null || allCombinations.Count == 0)
                return null;

            if (serialisedBase == null)
                Initialise(fullFactorial);

            var combination = allCombinations[0];
            allCombinations.RemoveAt(0);
            string newSimulationName = Name;
            foreach (FactorValue value in combination)
                newSimulationName += value.Name;

            Simulation newSimulation = Apsim.DeserialiseFromStream(serialisedBase) as Simulation;
            newSimulation.Name = newSimulationName;
            newSimulation.Parent = null;
            newSimulation.FileName = parentSimulations.FileName;
            Apsim.ParentAllChildren(newSimulation);

            // Make substitutions and issue Loaded event
            parentSimulations.MakeSubsAndLoad(newSimulation);

            foreach (FactorValue value in combination)
                value.ApplyToSimulation(newSimulation);

            PushFactorsToReportModels(newSimulation, combination);
            StoreFactorsInDataStore(newSimulation, combination);
            return newSimulation;
        }

        /// <summary>
        /// Generates an .apsimx file for each simulation in the experiment and returns an error message (if it fails).
        /// </summary>
        /// <param name="path">Full path including filename and extension.</param>
        /// <returns>Empty string if successful, error message if it fails.</returns>
        public void GenerateApsimXFile(string path)
        {
            if (allCombinations == null || allCombinations.Count < 1)
                allCombinations = EnabledCombinations();
            Simulation sim = NextSimulationToRun();
            while (sim != null)
            {
                Simulations sims = Simulations.Create(new List<IModel> { sim, new Models.Storage.DataStore() });

                string xml = Apsim.Serialise(sims);
                File.WriteAllText(Path.Combine(path, sim.Name + ".apsimx"), xml);
                sim = NextSimulationToRun();
            }
        }
        
        /// <summary>Gets a list of simulation names</summary>
        public IEnumerable<string> GetSimulationNames(bool fullFactorial = true)
        {
            List<string> names = new List<string>();
            allCombinations = fullFactorial ? AllCombinations() : EnabledCombinations();
            foreach (List<FactorValue> combination in allCombinations)
            {
                string newSimulationName = Name;

                foreach (FactorValue value in combination)
                    newSimulationName += value.Name;

                names.Add(newSimulationName);
            }
            return names;
        }

        /// <summary>Gets a list of factors</summary>
        public List<ISimulationGeneratorFactors> GetFactors()
        {
            if (serialisedBase == null || allCombinations.Count == 0)
                Initialise(true);

            List<ISimulationGeneratorFactors> factors = new List<ISimulationGeneratorFactors>();

            List<string> simulationNames = new List<string>();
            foreach (List<FactorValue> combination in allCombinations)
            {
                // Work out a simulation name for this combination
                string simulationName = Name;
                foreach (FactorValue value in combination)
                    simulationName += value.Name;
                SimulationGeneratorFactors simulationFactors = new SimulationGeneratorFactors("SimulationName", simulationName);
                factors.Add(simulationFactors);
                simulationFactors.AddFactor("Experiment", Name);

                foreach (FactorValue value in combination)
                {
                    string factorName = value.Factor.Name;
                    if (value.Factor.Parent is Factor)
                        factorName = value.Factor.Parent.Name;
                    string factorValue = value.Name.Replace(factorName, "");
                    simulationFactors.AddFactor(factorName, factorValue);
                }
            }
            return factors;
        }

        /// <summary>
        /// Initialise the experiment ready for creating simulations.
        /// </summary>
        private void Initialise(bool fullFactorial = false)
        {            
            parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
            allCombinations = fullFactorial ? AllCombinations() : EnabledCombinations();
            Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
            serialisedBase = Apsim.SerialiseToStream(baseSimulation) as Stream;
        }

        /// <summary>Find all report models and give them the factor values.</summary>
        /// <param name="factorValues">The factor values to send to each report model.</param>
        /// <param name="simulation">The simulation to search for report models.</param>
        private void PushFactorsToReportModels(Simulation simulation, List<FactorValue> factorValues)
        {
            List<string> names = new List<string>();
            List<string> values = new List<string>();

            GetFactorNamesAndValues(factorValues, names, values);

            foreach (Report.Report report in Apsim.ChildrenRecursively(simulation, typeof(Report.Report)))
            {
                report.ExperimentFactorNames = names;
                report.ExperimentFactorValues = values;
            }
        }

        /// <summary>Get a list of factor names and values.</summary>
        /// <param name="factorValues">The factor value instances</param>
        /// <param name="names">The return list of factor names</param>
        /// <param name="values">The return list of factor values</param>
        public static void GetFactorNamesAndValues(List<FactorValue> factorValues, List<string> names, List<string> values)
        {
            foreach (FactorValue factorValue in factorValues)
            {
                Factor topLevelFactor = factorValue.Factor;
                if (topLevelFactor.Parent is Factor)
                    topLevelFactor = topLevelFactor.Parent as Factor;
                string name = topLevelFactor.Name;
                string value = factorValue.Name.Replace(topLevelFactor.Name, "");
                if (value == string.Empty)
                {
                    name = "Factors";
                    value = factorValue.Name;
                }
                names.Add(name);
                values.Add(value);
            }
        }

        /// <summary>Find all report models and give them the factor values.</summary>
        /// <param name="factorValues">The factor values to send to each report model.</param>
        /// <param name="simulation">The simulation to search for report models.</param>
        private void StoreFactorsInDataStore(Simulation simulation, List<FactorValue> factorValues)
        {
            if (storage != null)
            {
                List<string> names = new List<string>();
                List<string> values = new List<string>();

                GetFactorNamesAndValues(factorValues, names, values);

                string parentFolderName = null;
                IModel parentFolder = Apsim.Parent(this, typeof(Folder));
                if (parentFolder != null)
                    parentFolderName = parentFolder.Name;

                DataTable factorTable = new DataTable();
                factorTable.TableName = "_Factors";
                factorTable.Columns.Add("ExperimentName", typeof(string));
                factorTable.Columns.Add("SimulationName", typeof(string));
                factorTable.Columns.Add("FolderName", typeof(string));
                factorTable.Columns.Add("FactorName", typeof(string));
                factorTable.Columns.Add("FactorValue", typeof(string));
                for (int i = 0; i < names.Count; i++)
                {
                    DataRow row = factorTable.NewRow();
                    row[0] = Name;
                    row[1] = simulation.Name;
                    row[2] = parentFolderName;
                    row[3] = names[i];
                    row[4] = values[i];
                    factorTable.Rows.Add(row);
                }
                storage.WriteTable(factorTable);
            }
        }

        /// <summary>
        /// Gets the base simulation
        /// </summary>
        public Simulation BaseSimulation
        {
            get
            {
                return Apsim.Child(this, typeof(Simulation)) as Simulation;
            }
        }

        /// <summary>
        /// Create a specific simulation.
        /// </summary>
        public Simulation CreateSpecificSimulation(string name)
        {
            List<List<FactorValue>> allCombinations = AllCombinations();
            Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
            Simulations parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;

            foreach (List<FactorValue> combination in allCombinations)
            {
                string newSimulationName = Name;
                foreach (FactorValue value in combination)
                    newSimulationName += value.Name;

                if (newSimulationName == name)
                {
                    Simulation newSimulation = Apsim.Clone(baseSimulation) as Simulation;
                    newSimulation.Name = newSimulationName;
                    newSimulation.Parent = null;
                    newSimulation.FileName = parentSimulations.FileName;
                    Apsim.ParentAllChildren(newSimulation);

                    // Make substitutions and issue "Loaded" event
                    parentSimulations.MakeSubsAndLoad(newSimulation);

                    foreach (FactorValue value in combination)
                        value.ApplyToSimulation(newSimulation);

                    PushFactorsToReportModels(newSimulation, combination);

                    return newSimulation;
                }
            }

            return null;
        }

        /// <summary>
        /// Return a list of list of factorvalue objects for all permutations.
        /// </summary>
        public List<List<FactorValue>> AllCombinations()
        {
            Factors Factors = Apsim.Child(this, typeof(Factors)) as Factors;

            // Create a list of list of factorValues so that we can do permutations of them.
            List<List<FactorValue>> allValues = new List<List<FactorValue>>();
            if (Factors != null)
            {
                bool doFullFactorial = true;
                foreach (Factor factor in Factors.factors)
                {
                    if (factor.Enabled)
                    {
                        List<FactorValue> factorValues = factor.CreateValues();

                        // Iff any of the factors modify the same model (e.g. have a duplicate path), then we do not want to do a full factorial.
                        // This code should check if there are any such duplicates by checking each path in each factor value in the list of factor
                        // values for the current factor against each path in each list of factor values in the list of all factors which we have
                        // already added to the global list of list of factor values.
                        foreach (FactorValue currentFactorValue in factorValues)
                            foreach (string currentFactorPath in currentFactorValue.Paths)
                                foreach (List<FactorValue> allFactorValues in allValues)
                                    foreach (FactorValue globalValue in allFactorValues)
                                        foreach (string globalPath in globalValue.Paths)
                                            if (string.Equals(globalPath, currentFactorPath, StringComparison.CurrentCulture))
                                                doFullFactorial = false;

                        allValues.Add(factorValues);
                    }
                }
                if (doFullFactorial)
                    return MathUtilities.AllCombinationsOf<FactorValue>(allValues.ToArray());
                else
                    return allValues;
            }
            return null;
        }

        /// <summary>
        /// Generates a partial factorial list of lists of factor values, based on the list of enabled factor names.
        /// If this list is empty, this function will return a full factorial list of simulations.
        /// </summary>
        /// <returns></returns>
        public List<List<FactorValue>> EnabledCombinations()
        {
            if (DisabledSimNames == null || DisabledSimNames.Count < 1)
                return AllCombinations();

            // easy but inefficient method (for testing purposes)
            return AllCombinations().Where(x => (DisabledSimNames.IndexOf(GetName(x)) < 0)).ToList();
        }

        /// <summary>
        /// Generates the name for a combination of FactorValues.
        /// </summary>
        /// <param name="factors"></param>
        /// <returns></returns>
        private string GetName(List<FactorValue> factors)
        {
            string str = Name;
            foreach (FactorValue factor in factors)
            {
                str += factor.Name;
            }
            return str;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                foreach (IModel child in Children)
                {
                    if (!(child is Simulation) && !(child is Factors))
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
                }
            }
        }

    }
}
