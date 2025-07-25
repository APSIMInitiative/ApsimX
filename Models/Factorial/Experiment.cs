﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Core.Run;
using Models.Optimisation;
using APSIM.Core;

namespace Models.Factorial
{

    /// <summary>
    /// Encapsulates a factorial experiment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ExperimentView")]
    [PresenterName("UserInterface.Presenters.ExperimentPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(CroptimizR))]
    public class Experiment : Model, ISimulationDescriptionGenerator, IScopedModel
    {
        /// <summary>
        /// List of names of the disabled simulations. Any simulation name not in this list is assumed to be enabled.
        /// </summary>
        public List<string> DisabledSimNames { get; set; }

        /// <summary>Gets a list of simulation descriptions.</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions() => GetSimulationDescriptions(false).ToList();

        /// <summary>
        /// Get the total number of simulations generated by this experiment.
        /// </summary>
        public int NumSimulations() => CalculateAllCombinations()?.Count ?? 0;

        /// <summary>Gets a list of simulation descriptions.</summary>
        public IEnumerable<SimulationDescription> GetSimulationDescriptions(bool includeDisabled = true)
        {
            // Calculate all combinations.
            var allCombinations = CalculateAllCombinations();
            if (!includeDisabled && DisabledSimNames != null) {
                allCombinations.RemoveAll(comb => DisabledSimNames.Contains(GetName(comb)));
            }

            if (allCombinations != null)
            {
                // Find base simulation.
                var baseSimulation = this.FindChild<Simulation>();

                // Loop through all combinations and add a simulation description to the
                // list of simulations descriptions being returned to the caller.
                foreach (var combination in allCombinations)
                {
                    // Create a simulation.
                    var simulationName = GetName(combination);
                    var simDescription = new SimulationDescription(baseSimulation, simulationName);

                    // Add an experiment descriptor.
                    simDescription.Descriptors.Add(new SimulationDescription.Descriptor("Experiment", Name));

                    // Add a simulation descriptor.
                    simDescription.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", simulationName));

                    // Don't need to add a folderName descriptor, as this will be added by the base simulation.
                    IEnumerable<SimulationDescription> descriptions = baseSimulation?.GenerateSimulationDescriptions();
                    if (descriptions == null)
                        descriptions = Enumerable.Empty<SimulationDescription>();
                    foreach (var simulationDescriptor in descriptions)
                    {
                        foreach (var descriptor in simulationDescriptor.Descriptors)
                            if (descriptor.Name != "SimulationName")
                                simDescription.Descriptors.Add(descriptor);
                    }

                    // Apply each composite factor of this combination to our simulation description.
                    combination.ForEach(c => c.ApplyToSimulation(simDescription));

                    // Add simulation description to the return list of descriptions
                    yield return simDescription;
                }
            }
        }

        /// <summary>
        /// Get a human-readable description of the experiment (e.g. "NRate x Water").
        /// </summary>
        public string GetDesign()
        {
            Factors factors = FindChild<Factors>();
            StringBuilder design = new StringBuilder(GetTreatmentDescription(factors));
            foreach (Permutation permutation in factors.FindAllChildren<Permutation>())
                design.Append(GetTreatmentDescription(permutation));

            var simulationNames = GenerateSimulationDescriptions().Select(s => s.Name);
            design.Append($" ({simulationNames.Count()})");
            return design.ToString();
        }

        private string GetTreatmentDescription(IModel factors)
        {
            return string.Join(" x ", factors.FindAllChildren<Factor>().Select(f => f.Name));
        }

        /// <summary>
        /// Calculate a list of fall combinations of factors.
        /// </summary>
        private List<List<CompositeFactor>> CalculateAllCombinations()
        {
            Factors Factors = this.FindChild<Factors>();

            // Create a list of list of factorValues so that we can do permutations of them.
            List<List<CompositeFactor>> allValues = new List<List<CompositeFactor>>();
            if (Factors != null)
            {
                foreach (CompositeFactor compositeFactor in Factors.FindAllChildren<CompositeFactor>())
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
                foreach (Permutation factor in Factors.FindAllChildren<Permutation>())
                {
                    if (factor.Enabled)
                        allValues.AddRange(factor.GetPermutations());
                }

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
                if (!(factor.Parent is Factors) && !(factor.Parent is Permutation))
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
    }
}
