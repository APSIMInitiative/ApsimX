namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class represents a series of paths and the same number of object values.
    /// Its sole purpose is to apply the object values to the model represented by the paths.
    /// If Specifications are specified then they are used instead of paths and values.
    /// </summary>
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Factor))]
    [ValidParent(ParentType = typeof(Permutation))]
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.CompositeFactorPresenter")]
    public class CompositeFactor : Model
    {
        /// <summary>Parameterless constrctor needed for serialisation</summary>
        public CompositeFactor()
        {
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(string name, string path, object value)
        {
            Paths = new List<string> { path };
            Values = new List<object> { value };
            Name = name;
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(Factor parentFactor, string path, object value)
        {
            Parent = parentFactor;
            Paths = new List<string> { path };
            Values = new List<object> { value };
            if (value is IModel)
                Name = (value as IModel).Name;
            else
                Name = value.ToString();
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(Factor parentFactor, List<string> paths, List<object> values)
        {
            Parent = parentFactor;
            Paths = paths;
            Values = values;
        }

        /// <summary>Gets or sets the specification to create overides for a simulation.</summary>
        public List<string> Specifications { get; set; }

        /// <summary>Gets all paths.</summary>
        public List<string> Paths { get; set; }

        /// <summary>Gets all values.</summary>
        public List<object> Values { get; set; }

        /// <summary>
        /// Apply this CompositeFactor to the specified simulation
        /// </summary>
        /// <param name="simulationDescription">A description of a simulation.</param>
        public void ApplyToSimulation(SimulationDescription simulationDescription)
        {
            List<string> allPaths = new List<string>();
            List<object> allValues = new List<object>();
            if (Specifications != null)
            {
                // Compound factorvalue i.e. multiple specifications that all
                // work on a single simulation.
                foreach (var specification in Specifications)
                    ParseSpecification(specification, allPaths, allValues);
            }
            if (Paths != null)
            {
                allPaths.AddRange(Paths);
                allValues.AddRange(Values);
            }

            if (allPaths.Count > 1 && allPaths.Count != allValues.Count)
                throw new Exception("The number of factor paths does not match the number of factor values");

            // Add a simulation override for each path / value combination.
            for (int i = 0; i != allPaths.Count; i++)
            {
                if (allValues[i] is IModel)
                    simulationDescription.AddOverride(new ModelReplacement(allPaths[i], allValues[i] as IModel));
                else
                    simulationDescription.AddOverride(new PropertyReplacement(allPaths[i], allValues[i]));
            }

            if (!(Parent is Factors))
            {
                // Set descriptors in simulation.
                string descriptorName = Name;
                if (Parent != null)
                    descriptorName = Parent.Name;
                if (Specifications != null && Specifications.Count > 0)
                {
                    // compound factor value ie. one that has multiple specifications. 
                    simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor(descriptorName, Name));
                }
                else
                {
                    if (allValues[0] is IModel)
                        simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor(descriptorName, (allValues[0] as IModel).Name));
                    else
                        simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor(descriptorName, allValues[0].ToString()));
                }
            }
        }

        /// <summary>
        /// Parse the specification into paths and values.
        /// </summary>
        /// <param name="specification">The specification to parse.</param>
        /// <param name="allPaths">The list of paths to add to.</param>
        /// <param name="allValues">The list of values to add to.</param>
        private void ParseSpecification(string specification, List<string> allPaths, List<object> allValues)
        {
            string path = specification;
            object value;
            if (path.Contains("="))
            {
                value = StringUtilities.SplitOffAfterDelimiter(ref path, "=").Trim();
                if (value == null)
                    throw new Exception("Cannot find any values on the specification line: " + specification);

                allPaths.Add(path.Trim());
                allValues.Add(value.ToString().Trim());
            }
            else
            {
                // Find the model that we are to replace.
                var experiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;
                var baseSimulation = Apsim.Child(experiment, typeof(Simulation));
                var modelToReplace = Apsim.Get(baseSimulation, path) as IModel;

                // Now find a child of that type.
                var possibleMatches = Apsim.Children(this, modelToReplace.GetType());
                if (possibleMatches.Count > 1)
                    value = possibleMatches.Find(m => m.Name == modelToReplace.Name);
                else
                    value = possibleMatches[0];

                allPaths.Add(path.Trim());
                allValues.Add(value);
            }
        }
    }
}