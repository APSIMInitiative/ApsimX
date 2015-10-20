// -----------------------------------------------------------------------
// <copyright file="FactorValue.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Factorial
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using System.Globalization;

    /// <summary>
    /// This class represents a series of paths and the same number of object values.
    /// Its sole purpose is to apply the object values to the model represented by the paths.
    /// </summary>
    [ValidParent(ParentModels = new Type[] { typeof(Factor), typeof(FactorValue) })]
    public class FactorValue
    {
        /// <summary>
        /// Name of factor value
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The paths to the models.
        /// </summary>
        private List<string> paths;

        /// <summary>
        /// The values for each path.
        /// </summary>
        private List<object> values;

        /// <summary>
        /// Constructor
        /// </summary>
        public FactorValue(string name, string path, object value)
        {
            this.Name = name;
            this.paths = new List<string>();
            paths.Add(path);
            values = new List<object>();
            this.values.Add(value);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FactorValue(string name, List<string> paths, List<object> values)
        {
            this.Name = name;
            this.paths = paths;
            this.values = values;
        }

        /// <summary>Gets all values.</summary>
        public List<object> Values { get { return values; } }

        /// <summary>
        /// Apply this FactorValue to the specified simulation
        /// </summary>
        public void ApplyToSimulation(Simulation newSimulation)
        {
            if (paths.Count > 1 && paths.Count != values.Count)
                throw new Exception("The number of factor paths does not match the number of factor values");

            // Multiple child factor values specified - apply each one.
            for (int i = 0; i != paths.Count; i++)
            {
                if (values[i] is string)
                    ApplyStringAsValue(newSimulation, paths[i], values[i].ToString());
                else
                    ApplyModelReplacement(newSimulation, paths[i], values[i] as IModel);

                    
            }

            string newSimulationName = newSimulation.Name;
            newSimulation.Name = newSimulationName;
        }

        /// <summary>
        /// Use the name of this object as a value to insert into the specified 'newSimulation'
        /// </summary>
        private static void ApplyStringAsValue(Simulation newSimulation, string path, string name)
        {
            object originalValue = newSimulation.Get(path);
            object newValue;
            if (originalValue is DateTime)
                newValue = DateTime.Parse(name, CultureInfo.InvariantCulture);
            else if (originalValue is float)
                newValue = Convert.ToSingle(name, CultureInfo.InvariantCulture);
            else if (originalValue is double)
                newValue = Convert.ToDouble(name, CultureInfo.InvariantCulture);
            else if (originalValue is int)
                newValue = Convert.ToInt32(name, CultureInfo.InvariantCulture);
            else if (originalValue is string)
                newValue = Convert.ToString(name);
            else
                newValue = name;
            newSimulation.Set(path, newValue);
        }

        /// <summary>
        /// Replace the object specified by 'path' in 'newSimulation' with the specified 'value'
        /// </summary>
        private static void ApplyModelReplacement(Simulation newSimulation, string path, IModel value)
        {
            IModel newModel = Apsim.Clone(value);
            IModel modelToReplace = newSimulation.Get(path) as IModel;
            if (modelToReplace == null)
                throw new Exception("Cannot find model to replace. Model path: " + path);

            int index = modelToReplace.Parent.Children.IndexOf(modelToReplace as Model);
            if (index == -1)
                throw new Exception("Cannot find model to replace. Model path: " + path);

            modelToReplace.Parent.Children.RemoveAt(index);
            modelToReplace.Parent.Children.Insert(index, newModel as Model);
            newModel.Name = modelToReplace.Name;
            newModel.Parent = modelToReplace.Parent;

            Apsim.CallEventHandler(newModel, "Loaded", null);
        }
    }
}
