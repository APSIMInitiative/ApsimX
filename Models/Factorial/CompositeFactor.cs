namespace Models.Factorial
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class represents a series of paths and the same number of object values.
    /// Its sole purpose is to apply the object values to the model represented by the paths.
    /// If Specifications are specified then they are used instead of paths and values.
    /// </summary>
    [ValidParent(ParentType = typeof(Factor))]
    [Serializable]
    [ViewName("UserInterface.Views.CompositeFactorView")]
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
            Initialise();
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
            Initialise();
        }

        /// <summary>Constructor</summary>
        public CompositeFactor(Factor parentFactor, List<string> paths, List<object> values)
        {
            Parent = parentFactor;
            Paths = paths;
            Values = values;
            Initialise();
        }

        /// <summary>Gets or sets the specification to create overides for a simulation.</summary>
        public List<string> Specifications { get; set; }

        /// <summary>Gets all paths.</summary>
        public List<string> Paths { get; set; }

        /// <summary>Gets all values.</summary>
        public List<object> Values { get; set; }

        /// <summary>Gets a descriptor of this factor.</summary>
        public Tuple<string, string> Descriptor { get; private set; }

        /// <summary>Gets a simulation replacement for this factor.</summary>
        public IReplacement Replacement { get; private set; }

        /// <summary>Called when object creation has occurred.</summary>
        public override void OnCreated()
        {
            Initialise();
        }

        /// <summary>
        /// Apply this FactorValue to the specified simulation
        /// </summary>
        private void Initialise()
        {
            List<string> allPaths;
            List<object> allValues;
            if (Specifications != null)
            {
                // Compound factorvalue i.e. multiple specifications that all
                // work on a single simulation.
                allPaths = new List<string>();
                allValues = new List<object>();

                foreach (var specification in Specifications)
                    ParseSpecification(specification, allPaths, allValues);
            }
            else
            {
                allPaths = Paths;
                allValues = Values;
            }

            if (allPaths.Count > 1 && allPaths.Count != allValues.Count)
                throw new Exception("The number of factor paths does not match the number of factor values");

            // Add a simulation override for each path / value combination.
            for (int i = 0; i != allPaths.Count; i++)
            {
                if (allValues[i] is IModel)
                    Replacement = new ModelReplacement(allPaths[i], allValues[i] as IModel);
                else
                    Replacement = new PropertyReplacement(allPaths[i], allValues[i]);
            }

            // Set descriptors in simulation.
            if (Specifications != null && Specifications.Count > 0)
            {
                // compound factor value ie. one that has multiple specifications. 
                Descriptor = new Tuple<string, string>(Parent.Name, Name);
            }
            else
            {
                if (allValues[0] is IModel)
                    Descriptor = new Tuple<string, string>(Parent.Name, (allValues[0] as IModel).Name);
                else
                    Descriptor = new Tuple<string, string>(Parent.Name, allValues[0].ToString());
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
                Experiment experiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;
                var modelToReplace = Apsim.Get(experiment.BaseSimulation, path) as IModel;

                // Now find a child of that type.
                value = Apsim.Child(this, modelToReplace.GetType());

                allPaths.Add(path.Trim());
                allValues.Add(value);
            }
        }
    }
}