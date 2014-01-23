using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Factorial;

namespace Models.Factorial
{
    /// <summary>
    /// Encapsulates a factorial experiment.
    /// </summary>
    [Serializable]
    public class Experiment : ModelCollection
    {
        public Factors Factors { get; set; }
        public Simulation Simulation { get; set; }

        /// <summary>
        /// Create all simulations.
        /// </summary>
        public Simulation[] Create()
        {
            List<Simulation> simulations = new List<Simulation>();

            Simulation newSimulation = Utility.Reflection.Clone<Simulation>(Simulation);
            newSimulation.Name = "";
            for (int factorValueI = 0; factorValueI < Factors.factors[0].FactorValues.Count; factorValueI++)
            {
                CreatePermutation(0, factorValueI, newSimulation, simulations);
                simulations.Add(newSimulation);
                newSimulation = Utility.Reflection.Clone<Simulation>(Simulation);
                newSimulation.Name = "";
            }

            return simulations.ToArray();
        }




        internal string[] Names()
        {
            List<string> simulationNames = new List<string>();

            string simulationName = "";
            for (int factorValueI = 0; factorValueI < Factors.factors[0].FactorValues.Count; factorValueI++)
            {
                NamePermutation(0, factorValueI, ref simulationName, simulationNames);
                simulationNames.Add(simulationName);
                simulationName = "";
            }

            return simulationNames.ToArray();
        }



        private void CreatePermutation(int factorI, int factorValueI, Simulation newSimulation, List<Simulation> allSimulations)
        {
            Factors.factors[factorI].ApplyToSimulation(newSimulation, factorValueI);

            for (int factorJ = factorI + 1; factorJ < Factors.factors.Count; factorJ++)
            {
                for (int factorValueJ = 0; factorValueJ < Factors.factors[factorJ].FactorValues.Count; factorValueJ++)
                {
                    CreatePermutation(factorJ, factorValueJ, newSimulation, allSimulations);
                    allSimulations.Add(newSimulation);
                    newSimulation = Utility.Reflection.Clone<Simulation>(Simulation);
                    newSimulation.Name = "";
                }
            }

        }

        private void NamePermutation(int factorI, int factorValueI, ref string simulationName, List<string> simulationNames)
        {
            Factors.factors[factorI].AddToName(ref simulationName, factorValueI);

            for (int factorJ = factorI + 1; factorJ < Factors.factors.Count; factorJ++)
            {
                for (int factorValueJ = 0; factorValueJ < Factors.factors[factorJ].FactorValues.Count; factorValueJ++)
                {
                    NamePermutation(factorJ, factorValueJ, ref simulationName, simulationNames);
                    simulationNames.Add(simulationName);
                    simulationName = "";
                }
            }

        }
    }

     
}
