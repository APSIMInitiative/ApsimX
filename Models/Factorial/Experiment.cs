namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Runners;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;

    /// <summary>
    /// Encapsulates a factorial experiment.f
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.FactorControlView")]
    [PresenterName("UserInterface.Presenters.FactorControlPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Experiment : Model, ISimulationGenerator
    {
        [Link]
        IStorageReader storage = null;

        private List<List<FactorValue>> allCombinations;
        private Stream serialisedBase;
        private Simulations parentSimulations;

        /// <summary>Simulation runs are about to begin.</summary>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun(IEnumerable<string> knownSimulationNames = null, IEnumerable<string> simulationNamesBeingRun = null)
        {
        }

        /// <summary>Gets the next job to run</summary>
        public Simulation NextSimulationToRun()
        {
            if (allCombinations == null || allCombinations.Count == 0)
                return null;

            if (serialisedBase == null)
            {
                allCombinations = AllCombinations();
                parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
                serialisedBase = Apsim.SerialiseToStream(baseSimulation) as Stream;
            }

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

            // Make substitutions.
            parentSimulations.MakeSubstitutions(newSimulation);

            // Call OnLoaded in all models.
            Events events = new Events(newSimulation);
            LoadedEventArgs loadedArgs = new LoadedEventArgs();
            events.Publish("Loaded", new object[] { newSimulation, loadedArgs });

            foreach (FactorValue value in combination)
                value.ApplyToSimulation(newSimulation);

            PushFactorsToReportModels(newSimulation, combination);
            StoreFactorsInDataStore(newSimulation, combination);
            return newSimulation;
        }

        /// <summary>Gets a list of simulation names</summary>
        public IEnumerable<string> GetSimulationNames()
        {
            List<string> names = new List<string>();
            allCombinations = AllCombinations();
            foreach (List<FactorValue> combination in allCombinations)
            {
                string newSimulationName = Name;

                foreach (FactorValue value in combination)
                    newSimulationName += value.Name;

                names.Add(newSimulationName);
            }
            return names;
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
        private static void GetFactorNamesAndValues(List<FactorValue> factorValues, List<string> names, List<string> values)
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

                    // Make substitutions.
                    parentSimulations.MakeSubstitutions(newSimulation);

                    // Connect events and links in our new  simulation.
                    Events events = new Events(newSimulation);
                    LoadedEventArgs loadedArgs = new LoadedEventArgs();
                    events.Publish("Loaded", new object[] { newSimulation, loadedArgs });

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
                bool doFullFactorial = false;
                foreach (Factor factor in Factors.factors)
                {
                    List<FactorValue> factorValues = factor.CreateValues();
                    allValues.Add(factorValues);
                    doFullFactorial = doFullFactorial || factorValues.Count > 1;
                }
                if (doFullFactorial)
                    return MathUtilities.AllCombinationsOf<FactorValue>(allValues.ToArray());
                else
                    return allValues;
            }
            return null;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                foreach (IModel child in Children)
                {
                    if (!(child is Simulation) && !(child is Factors))
                        child.Document(tags, headingLevel + 1, indent);
                }
            }
        }

    }
}
