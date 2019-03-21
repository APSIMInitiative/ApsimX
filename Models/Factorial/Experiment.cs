namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Runners;
    using Models.Storage;
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
    [ScopedModel]
    public class Experiment : Model, ISimulationGenerator, ICustomDocumentation
    {
        [Link]
        IDataStore storage = null;

        private Stream serialisedBase;
        private Simulations parentSimulations;

        /// <summary>A list of all fctorial combinations.</summary>
        public List<List<CompositeFactor>> AllCombinations { get; private set; }

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
        public Simulation NextSimulationToRun()
        {
		 //   // NEW BIT
			//// If index is out of bounds then return null to indicate we don't 
   //         // have any more simulations to return.
   //         if (allCombinations == null || simulationIndex >= allCombinations.Count)
   //             return null;

   //         // Create a simulation.
   //         var sim = new RunnableSimulation(BaseSimulation, GetName(allCombinations[simulationIndex]), true);

   //         // Add an experiment descriptor.
   //         sim.Descriptors.Add(new Tuple<string, string>("Experiment", Name));

   //         // Apply factor overrides for this combination / simulation.
   //         allCombinations[simulationIndex].ForEach(c => c.ApplyToSimulation(sim));
   //         return sim;
		
		
		
            if (serialisedBase == null)
                Initialise();

            if (AllCombinations == null || AllCombinations.Count == 0)
                return null;

            var combination = AllCombinations[0];
            AllCombinations.RemoveAt(0);
            string newSimulationName = GetName(combination);

            Simulation newSimulation = Apsim.DeserialiseFromStream(serialisedBase) as Simulation;
            newSimulation.Name = newSimulationName;
            newSimulation.Parent = null;
            newSimulation.FileName = parentSimulations.FileName;
            Apsim.ParentAllChildren(newSimulation);

            // Make substitutions and issue Loaded event
            parentSimulations.MakeSubsAndLoad(newSimulation);

            combination.ForEach(c => c.Replacement.Replace(newSimulation));

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
            if (AllCombinations == null || AllCombinations.Count < 1)
                AllCombinations = EnabledCombinations();
            Simulation sim = NextSimulationToRun();
            while (sim != null)
            {
                Simulations sims = Simulations.Create(new List<IModel> { sim, new Models.Storage.DataStore() });

                string st = FileFormat.WriteToString(sims);
                File.WriteAllText(Path.Combine(path, sim.Name + ".apsimx"), st);
                sim = NextSimulationToRun();
            }
        }
        
        /// <summary>Gets a list of simulation names</summary>
        public IEnumerable<string> GetSimulationNames(bool fullFactorial = true)
        {
            var simulationNames = new List<string>();
            foreach (var combination in AllCombinations)
                simulationNames.Add(GetName(combination));
            return simulationNames;
        }

        /// <summary>Gets a list of factors</summary>
        public List<ISimulationGeneratorFactors> GetFactors()
        {
            List<ISimulationGeneratorFactors> factors = new List<ISimulationGeneratorFactors>();

            List<string> simulationNames = new List<string>();
            foreach (var combination in AllCombinations)
            {
                // Work out a simulation name for this combination
                string simulationName = GetName(combination);

                SimulationGeneratorFactors simulationFactors = new SimulationGeneratorFactors("SimulationName", simulationName);
                factors.Add(simulationFactors);
                simulationFactors.AddFactor("Experiment", Name);

                foreach (var value in combination)
                    simulationFactors.AddFactor(value.Descriptor.Item1, value.Descriptor.Item2);
            }
            return factors;
        }

        /// <summary>
        /// Initialise the experiment ready for creating simulations.
        /// </summary>
        private void Initialise()
        {            
            parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
            CalculateAllCombinations();
            Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
            serialisedBase = Apsim.SerialiseToStream(baseSimulation) as Stream;
        }

        /// <summary>Find all report models and give them the factor values.</summary>
        /// <param name="combination">The factor values to send to each report model.</param>
        /// <param name="simulation">The simulation to search for report models.</param>
        private void PushFactorsToReportModels(Simulation simulation, List<CompositeFactor> combination)
        {
            List<string> names = new List<string>();
            List<string> values = new List<string>();

            GetFactorNamesAndValues(combination, names, values);

            foreach (Report.Report report in Apsim.ChildrenRecursively(simulation, typeof(Report.Report)))
            {
                report.ExperimentFactorNames = names;
                report.ExperimentFactorValues = values;
            }
        }

        /// <summary>Get a list of factor names and values.</summary>
        /// <param name="combination">The factor value instances</param>
        /// <param name="names">The return list of factor names</param>
        /// <param name="values">The return list of factor values</param>
        public static void GetFactorNamesAndValues(List<CompositeFactor> combination, List<string> names, List<string> values)
        {
            foreach (var factorValue in combination)
            {
                names.Add(factorValue.Descriptor.Item1);
                values.Add(factorValue.Descriptor.Item2);
            }
        }

        /// <summary>Find all report models and give them the factor values.</summary>
        /// <param name="factorValues">The factor values to send to each report model.</param>
        /// <param name="simulation">The simulation to search for report models.</param>
        private void StoreFactorsInDataStore(Simulation simulation, List<CompositeFactor> factorValues)
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
                storage.Writer.WriteTable(factorTable);
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
            Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
            Simulations parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;

            foreach (var combination in AllCombinations)
            {
                string newSimulationName = GetName(combination);

                if (newSimulationName == name)
                {
                    Simulation newSimulation = Apsim.Clone(baseSimulation) as Simulation;
                    newSimulation.Name = newSimulationName;
                    newSimulation.Parent = null;
                    newSimulation.FileName = parentSimulations.FileName;
                    Apsim.ParentAllChildren(newSimulation);

                    // Make substitutions
                    parentSimulations.MakeSubsAndLoad(newSimulation);

                    foreach (var value in combination)
                        value.Replacement.Replace(newSimulation);

                    PushFactorsToReportModels(newSimulation, combination);

                    return newSimulation;
                }
            }

            return null;
        }

        /// <summary>
        /// Return a list of list of factorvalue objects for all permutations.
        /// </summary>
        public void CalculateAllCombinations()
        {
           Factors Factors = Apsim.Child(this, typeof(Factors)) as Factors;

            // Create a list of list of factorValues so that we can do permutations of them.
            List<List<CompositeFactor>> allValues = new List<List<CompositeFactor>>();
            if (Factors != null)
            {
                foreach (Factor factor in Factors.factors)
                {
                    if (factor.Enabled)
                        allValues.Add(factor.GetCompositeFactors());
                }
                AllCombinations =  MathUtilities.AllCombinationsOf<CompositeFactor>(allValues.ToArray());

                // Remove disabled simulations.
                if (DisabledSimNames != null)
                    AllCombinations.RemoveAll(comb => DisabledSimNames.Contains(GetName(comb)));
            }
        }

        /// <summary>
        /// Generates a partial factorial list of lists of factor values, based on the list of enabled factor names.
        /// If this list is empty, this function will return a full factorial list of simulations.
        /// </summary>
        /// <returns></returns>
        public List<List<CompositeFactor>> EnabledCombinations()
        {
            if (DisabledSimNames == null || DisabledSimNames.Count < 1)
                return AllCombinations;

            // easy but inefficient method (for testing purposes)
            return AllCombinations.Where(x => (DisabledSimNames.IndexOf(GetName(x)) < 0)).ToList();
        }

        /// <summary>
        /// Generates the name for a combination of FactorValues.
        /// </summary>
        /// <param name="factors"></param>
        /// <returns></returns>
        private string GetName(List<CompositeFactor> factors)
        {
            string newName = Name;
            factors.ForEach(factor => newName += factor.Parent.Name + factor.Name);
            return newName;
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
