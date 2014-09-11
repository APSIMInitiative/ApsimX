using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
namespace Models.Factorial
{
    [ValidParent(typeof(Factor))]
    public class FactorValue : Model
    {
        private List<string> FactorPaths;
        private string FactorName;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FactorValue() { }

        /// <summary>
        /// Constructor used by range FactorValues.
        /// </summary>
        public FactorValue(List<string> paths, string factorName)
        {
            FactorPaths = paths;
            FactorName = factorName;
        }

        /// <summary>
        /// A FactorValue that is a range will return multiple FactorValue objects.
        /// </summary>
        public FactorValue[] CreateValues()
        {
            // Grab the parent Factor name and paths.
            FactorPaths = ParentFactor.Paths;
            FactorName = ParentFactor.Name;
            if (IsRange)
            {
                // Format of a range:
                //    value1 to value2 step increment.
                string[] rangeBits = Name.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                double from = Convert.ToDouble(rangeBits[0]);
                double to = Convert.ToDouble(rangeBits[2]);
                double step = Convert.ToDouble(rangeBits[4]);

                List<FactorValue> newValues = new List<FactorValue>();
                for (double value = from; value <= to; value += step)
                {
                    FactorValue newValue = new FactorValue(FactorPaths, FactorName);
                    newValue.Name = value.ToString();
                    newValues.Add(newValue);
                }
                return newValues.ToArray();
            }
            else
                return new FactorValue[1] { this };
        }

        /// <summary>
        /// Apply this FactorValue to the specified simulation
        /// </summary>
        public void ApplyToSimulation(Simulation newSimulation)
        {
            if (FactorPaths.Count > 1 && FactorPaths.Count != Children.Count)
                throw new ApsimXException(this, "The number of factor paths does not match the number of factor values");

            if (FactorPaths.Count == 1)
            {
                if (Children != null && Children.Count > 1)
                    throw new ApsimXException(this, "One factor path was specified with multiple child factor values.");

                if (Children == null || Children.Count == 0)
                    ApplyNameAsValue(newSimulation, FactorPaths[0], Name);
                else
                    ApplyModelReplacement(newSimulation, FactorPaths[0], Children[0]);
            }
            else if (Children != null)
            {
                // Multiple child factor values specified - apply each one.
                for (int i = 0; i != FactorPaths.Count; i++)
                {
                    if (Children[i] is FactorValue)
                    {
                        FactorValue factorValue = Children[i] as FactorValue;
                        if (factorValue != null)
                            ApplyNameAsValue(newSimulation, FactorPaths[i], factorValue.Name);
                        else
                            ApplyModelReplacement(newSimulation, FactorPaths[i], factorValue.Children[0]);
                    }
                    else
                        ApplyModelReplacement(newSimulation, FactorPaths[i], Children[i]);

                    
                }
            }

            string newSimulationName = newSimulation.Name;
            AddToName(ref newSimulationName);
            newSimulation.Name = newSimulationName;
        }

        /// <summary>
        /// Use the name of this object as a value to insert into the specified 'newSimulation'
        /// </summary>
        private void ApplyNameAsValue(Simulation newSimulation, string path, string name)
        {
            object originalValue = newSimulation.Get(path);
            object newValue;
            if (originalValue is DateTime)
                newValue = DateTime.Parse(name);
            else if (originalValue is float)
                newValue = Convert.ToSingle(name);
            else if (originalValue is double)
                newValue = Convert.ToDouble(name);
            else if (originalValue is int)
                newValue = Convert.ToInt32(name);
            else if (originalValue is string)
                newValue = Convert.ToString(name);
            else
                newValue = name;
            newSimulation.Set(path, newValue);
        }

        /// <summary>
        /// Replace the object specified by 'path' in 'newSimulation' with the specified 'value'
        /// </summary>
        private void ApplyModelReplacement(Simulation newSimulation, string path, Model value)
        {
            Model newModel = Apsim.Clone(value) as Model;
            Model modelToReplace = newSimulation.Get(path) as Model;
            if (modelToReplace == null)
                throw new ApsimXException(this, "Cannot find model to replace. Model path: " + path);

            int index = modelToReplace.Parent.Children.IndexOf(modelToReplace);
            if (index == -1)
                throw new ApsimXException(this, "Cannot find model to replace. Model path: " + path);

            modelToReplace.Parent.Children.RemoveAt(index);
            modelToReplace.Parent.Children.Insert(index, newModel);
            newModel.Name = modelToReplace.Name;
            newModel.Parent = modelToReplace.Parent;
        }

        /// <summary>
        /// Add this FactorValues name to the specified simulation.
        /// </summary>
        /// <param name="simulationName"></param>
        public void AddToName(ref string simulationName)
        {
            simulationName += FactorName + Name;
        }

        /// <summary>
        /// Return true if this FactorValue is a range.
        /// </summary>
        public bool IsRange 
        { 
            get 
            { 
                // Format of a range:
                //    value1 to value2 step increment.

                string[] rangeBits = Name.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (rangeBits.Length == 5 && 
                    rangeBits[1].Equals("to", StringComparison.CurrentCultureIgnoreCase) &&
                    rangeBits[3].Equals("step", StringComparison.CurrentCultureIgnoreCase))
                    return true;
                else
                    return false;
            } 
        }

        /// <summary>
        /// Return the parent factor.
        /// </summary>
        private Factor ParentFactor
        {
            get
            {
                Factor parentFactor = this.Parent as Factor;
                if (parentFactor == null)
                    throw new ApsimXException(this, "Cannot find a parent factor");
                return parentFactor;
            }
        }

    }
}
