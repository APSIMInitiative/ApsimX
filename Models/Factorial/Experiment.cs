using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Factorial;

namespace Models.Factorial
{
    /// <summary>
    /// Encapsulates a factorial experiment.f
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MemoView")]
    [PresenterName("UserInterface.Presenters.ExperimentPresenter")]
    public class Experiment : ModelCollection
    {
        public Factors Factors { get; set; }
        public Simulation Simulation { get; set; }

        /// <summary>
        /// Create all simulations.
        /// </summary>
        public Simulation[] Create()
        {
            List<List<FactorValue>> allCombinations = AllCombinations();

            List<Simulation> simulations = new List<Simulation>();
            foreach (List<FactorValue> combination in allCombinations)
            {
                Simulation newSimulation = Utility.Reflection.Clone<Simulation>(Simulation);
                newSimulation.Name = "";

                foreach (FactorValue value in combination)
                    value.ApplyToSimulation(newSimulation);

                simulations.Add(newSimulation);
            }

            return simulations.ToArray();
        }



        /// <summary>
        /// Return a list of simulation names.
        /// </summary>
        public string[] Names()
        {
            List<List<FactorValue>> allCombinations = AllCombinations();

            List<string> names = new List<string>();
            foreach (List<FactorValue> combination in allCombinations)
            {
                string newSimulationName = "";

                foreach (FactorValue value in combination)
                    value.AddToName(ref newSimulationName);

                names.Add(newSimulationName);
            }

            return names.ToArray();
        }

        /// <summary>
        /// Return a list of list of factorvalue objects for all permutations.
        /// </summary>
        private List<List<FactorValue>> AllCombinations()
        {
            // Create a list of list of factorValuse so that we can do permutations of them.
            List<List<FactorValue>> allValues = new List<List<FactorValue>>();
            foreach (Factor factor in Factors.factors)
            {
                List<FactorValue> values = new List<FactorValue>();
                foreach (FactorValue factorValue in factor.FactorValues)
                    values.AddRange(factorValue.CreateValues());
                allValues.Add(values);
            }

            List<List<FactorValue>> allCombinations = AllCombinationsOf<FactorValue>(allValues.ToArray());
            return allCombinations;
        }

        /// <summary>
        /// From: http://stackoverflow.com/questions/545703/combination-of-listlistint
        /// </summary>
        private static List<List<T>> AllCombinationsOf<T>(params List<T>[] sets)
        {
            // need array bounds checking etc for production
            var combinations = new List<List<T>>();

            // prime the data
            foreach (var value in sets[0])
                combinations.Add(new List<T> { value });

            foreach (var set in sets.Skip(1))
                combinations = AddExtraSet(combinations, set);

            return combinations;
        }

        private static List<List<T>> AddExtraSet<T>
             (List<List<T>> combinations, List<T> set)
        {
            var newCombinations = from value in set
                                  from combination in combinations
                                  select new List<T>(combination) { value };

            return newCombinations.ToList();
        }

    }

     
}
