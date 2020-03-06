namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// # [Name]
    /// Encapsulates a factorial experiment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ExperimentView")]
    [PresenterName("UserInterface.Presenters.ExperimentPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ScopedModel]
    public class Experiment : Model, ISimulationDescriptionGenerator, ICustomDocumentation
    {
        /// <summary>
        /// List of names of the disabled simulations. Any simulation name not in this list is assumed to be enabled.
        /// </summary>
        public List<string> DisabledSimNames { get; set; }

        /// <summary>Gets a list of simulation descriptions.</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions()
        {
            var simulationDescriptions = new List<SimulationDescription>();

            // Calculate all combinations.
            var allCombinations = CalculateAllCombinations();
            if (allCombinations != null)
            {
                // Find base simulation.
                var baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;

                // Loop through all combinations and add a simulation description to the
                // list of simulations descriptions being returned to the caller.
                foreach (var combination in allCombinations)
                {
                    // Create a simulation.
                    var simulationName = GetName(combination);
                    var simDescription = new SimulationDescription(baseSimulation, simulationName, true);

                    // Add an experiment descriptor.
                    simDescription.Descriptors.Add(new SimulationDescription.Descriptor("Experiment", Name));

                    // Add a simulation descriptor.
                    simDescription.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", simulationName));

                    // Don't need to add a folderName descriptor, as this will be added by the base simulation.
                    foreach (var simulationDescriptor in baseSimulation.GenerateSimulationDescriptions())
                    {
                        foreach (var descriptor in simulationDescriptor.Descriptors)
                            if (descriptor.Name != "SimulationName")
                                simDescription.Descriptors.Add(descriptor);
                    }

                    // Apply each composite factor of this combination to our simulation description.
                    combination.ForEach(c => c.ApplyToSimulation(simDescription));

                    // Add simulation description to the return list of descriptions
                    simulationDescriptions.Add(simDescription);
                }
            }

            return simulationDescriptions;
        }

        /// <summary>
        /// Calculate a list of fall combinations of factors.
        /// </summary>
        private List<List<CompositeFactor>> CalculateAllCombinations()
        {
           Factors Factors = Apsim.Child(this, typeof(Factors)) as Factors;

            // Create a list of list of factorValues so that we can do permutations of them.
            List<List<CompositeFactor>> allValues = new List<List<CompositeFactor>>();
            if (Factors != null)
            {
                foreach (CompositeFactor compositeFactor in Apsim.Children(Factors, typeof(CompositeFactor)))
                {
                    if (compositeFactor.Enabled)
                        allValues.Add(new List<CompositeFactor>() { compositeFactor });
                }
                foreach (Factor factor in Factors.factors)
                {
                    if (factor.Enabled)
                        foreach (var compositeFactor in factor.GetCompositeFactors())
                            allValues.Add(new List<CompositeFactor>() { compositeFactor });
                }
                foreach (Permutation factor in Apsim.Children(Factors, typeof(Permutation)))
                {
                    if (factor.Enabled)
                        allValues.AddRange(factor.GetPermutations());
                }
                
                // Remove disabled simulations.
                if (DisabledSimNames != null)
                    allValues.RemoveAll(comb => DisabledSimNames.Contains(GetName(comb)));

                return allValues;
            }
            else
                return null;
        }

        /// <summary>
        /// Generates the name for a combination of FactorValues.
        /// </summary>
        /// <param name="factors"></param>
        /// <returns></returns>
        private string GetName(List<CompositeFactor> factors)
        {
            string newName = null;
            string permutationName = null;
            foreach (var factor in factors)
            {
                if (!(factor.Parent is Factors) && !(factor.Parent is Permutation) )
                    newName += factor.Parent.Name;
                if (factor.Parent.Parent is Permutation)
                    permutationName = factor.Parent.Parent.Name;
                newName += factor.Name;
            }
            if (permutationName == null || permutationName == "Permutation")
                return Name + newName;
            else
                return Name + permutationName + newName;
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
