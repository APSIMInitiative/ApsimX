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
        [Link] Factors Factors = null;
        [Link] Simulation Base = null;

        /// <summary>
        /// Create all simulations.
        /// </summary>
        public Simulation[] Create()
        {
            // remove event connections and links from the base simulation before we clone.
            Base.AllModels.ForEach(DisconnectEvents);
            Base.AllModels.ForEach(UnresolveLinks);

            List<List<FactorValue>> allCombinations = AllCombinations();

            List<Simulation> simulations = new List<Simulation>();
            foreach (List<FactorValue> combination in allCombinations)
            {
                Simulation newSimulation = Utility.Reflection.Clone<Simulation>(Base);
                newSimulation.Name = Name;

                // Connect events and links in our new  simulation.
                newSimulation.AllModels.ForEach(DisconnectEvents);
                newSimulation.AllModels.ForEach(UnresolveLinks);
                newSimulation.AllModels.ForEach(ConnectEventPublishers);
                newSimulation.AllModels.ForEach(ResolveLinks);
                newSimulation.AllModels.ForEach(CallOnLoaded);


                foreach (FactorValue value in combination)
                    value.ApplyToSimulation(newSimulation);

                simulations.Add(newSimulation);
            }

            // reconnect events and links in the base simulation.
            Base.AllModels.ForEach(ConnectEventPublishers);
            Base.AllModels.ForEach(ResolveLinks);

            return simulations.ToArray();
        }



        /// <summary>
        /// Return a list of simulation names.
        /// </summary>
        public string[] Names()
        {
            List<List<FactorValue>> allCombinations = AllCombinations();

            List<string> names = new List<string>();
            if (allCombinations != null)
            {
                foreach (List<FactorValue> combination in allCombinations)
                {
                    string newSimulationName = Name;

                    foreach (FactorValue value in combination)
                        value.AddToName(ref newSimulationName);

                    names.Add(newSimulationName);
                }
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
            if (Factors != null)
            {
                foreach (Factor factor in Factors.factors)
                {
                    List<FactorValue> values = new List<FactorValue>();
                    foreach (FactorValue factorValue in factor.FactorValues)
                        values.AddRange(factorValue.CreateValues());
                    if (values.Count > 0)
                        allValues.Add(values);
                }
                List<List<FactorValue>> allCombinations = AllCombinationsOf<FactorValue>(allValues.ToArray());
                return allCombinations;
            }
            return null;
        }

        /// <summary>
        /// From: http://stackoverflow.com/questions/545703/combination-of-listlistint
        /// </summary>
        private static List<List<T>> AllCombinationsOf<T>(params List<T>[] sets)
        {
            // need array bounds checking etc for production
            var combinations = new List<List<T>>();

            // prime the data
            if (sets.Length > 0)
            {
                foreach (var value in sets[0])
                    combinations.Add(new List<T> { value });

                foreach (var set in sets.Skip(1))
                    combinations = AddExtraSet(combinations, set);
            }
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
