using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
namespace Models.Factorial
{
    [AllowDropOn("Factor")]
    public class FactorValue : Model
    {
        private List<string> FactorPaths;
        private string FactorName;

        [XmlElement("FactorValue")]
        public List<FactorValue> FactorValues { get; set; }

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
            if (FactorPaths.Count >= 1)
            {
                object originalValue = newSimulation.Get(FactorPaths[0]);
                object newValue;
                if (originalValue is DateTime)
                    newValue = DateTime.Parse(Name);
                else if (originalValue is float)
                    newValue = Convert.ToSingle(Name);
                else if (originalValue is double)
                    newValue = Convert.ToDouble(Name);
                else if (originalValue is int)
                    newValue = Convert.ToInt32(Name);
                else if (originalValue is string)
                    newValue = Convert.ToString(Name);
                else
                    newValue = Name;
                newSimulation.Set(FactorPaths[0], newValue);
                string newSimulationName = newSimulation.Name;
                AddToName(ref newSimulationName);
                newSimulation.Name = newSimulationName;
            }
        }

        /// <summary>
        /// Add this FactorValues name to the specified simulation.
        /// </summary>
        /// <param name="simulationName"></param>
        public void AddToName(ref string simulationName)
        {
            if (simulationName.Length > 0)
                simulationName += ": ";
            simulationName += FactorName + " " + Name;
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
                    throw new ApsimXException(FullPath, "Cannot find a parent factor");
                return parentFactor;
            }
        }

    }
}
