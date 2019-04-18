namespace Models.Core.Run
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encapsulates all the bits that are need to construct a simulation
    /// and the associated metadata describing a simulation.
    /// </summary>
    [Serializable]
    public class SimulationDescription
    {
        /// <summary>The base simulation.</summary>
        private Simulation baseSimulation;

        /// <summary>A list of all replacements to apply to simulation to run.</summary>
        private List<IReplacement> replacementsToApply = new List<IReplacement>();

        /// <summary>Do we clone the simulation before running?</summary>
        private bool doClone;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sim">The simulation to run.</param>
        /// <param name="name">The name of the simulation.</param>
        /// <param name="clone">Clone the simulation passed in before running?</param>
        public SimulationDescription(Simulation sim, string name = null, bool clone = true)
        {
            baseSimulation = sim;
            if (name == null && baseSimulation != null)
                Name = baseSimulation.Name;
            else
                Name = name;
            doClone = clone;
        }

        /// <summary>name</summary>
        public string Name { get; }

        /// <summary>Gets / sets the list of descriptors for this simulaton.</summary>
        public List<Descriptor> Descriptors { get; set; } = new List<Descriptor>();

        /// <summary>
        /// Add an override to replace an existing model, as specified by the
        /// path, with a replacement model.
        /// </summary>
        /// <param name="replacement">An instance of a replacement that needs to be applied when simulation is run.</param>
        public void AddOverride(IReplacement replacement)
        {
            replacementsToApply.Add(replacement);
        }

        /// <summary>
        /// Add a property override to replace an existing value, as specified by a
        /// path.
        /// </summary>
        /// <param name="path">The path to use to locate the model to replace.</param>
        /// <param name="replacement">The model to use as the replacement.</param>
        public void AddOverride(string path, object replacement)
        {
            replacementsToApply.Add(new PropertyReplacement(path, replacement));
        }

        /// <summary>
        /// Convert the simulation decription to a simulation.
        /// path.
        /// </summary>
        /// <param name="simulations">The top level simulations model.</param>
        public Simulation ToSimulation(Simulations simulations = null)
        {
            AddReplacements(simulations);

            Simulation newSimulation;
            if (doClone)
                newSimulation = Apsim.Clone(baseSimulation) as Simulation;
            else
                newSimulation = baseSimulation;

            if (Name == null)
                newSimulation.Name = baseSimulation.Name;
            else
                newSimulation.Name = Name;
            newSimulation.Parent = null;
            Apsim.ParentAllChildren(newSimulation);
            replacementsToApply.ForEach(r => r.Replace(newSimulation));

            // Give the simulation the descriptors.
            newSimulation.Descriptors = Descriptors;

            return newSimulation;
        }

        /// <summary>Add any replacements to all simulation descriptions.</summary>
        /// <param name="simulations">The top level simulations model.</param>
        private void AddReplacements(Simulations simulations)
        {
            if (simulations != null)
            {
                IModel replacements = Apsim.Child(simulations, "Replacements");
                if (replacements != null)
                {
                    foreach (IModel replacement in replacements.Children)
                    {
                        var modelReplacement = new ModelReplacement(null, replacement);
                        replacementsToApply.Insert(0, modelReplacement);
                    }
                }
            }
        }

        /// <summary>Encapsulates a descriptor for a simulation.</summary>
        [Serializable]
        public class Descriptor
        {
            /// <summary>The name of the descriptor.</summary>
            public string Name { get; }

            /// <summary>The value of the descriptor.</summary>
            public string Value { get; }

            /// <summary>Constructor</summary>
            /// <param name="name">Name of the descriptor.</param>
            /// <param name="value">Value of the descriptor.</param>
            public Descriptor(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

    }
}
