using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
namespace Models.Factorial
{
    [Serializable]
    [AllowDropOn("Factor")]
    public class FactorValue : Model
    {
        [XmlElement("FactorValue")]
        public List<FactorValue> FactorValues { get; set; }

        public void ApplyToSimulation(Simulation newSimulation)
        {
            Factor parentFactor = ParentFactor;
            
            if (parentFactor.Paths.Count >= 1)
            {
                object originalValue = newSimulation.Get(parentFactor.Paths[0]);
                object newValue;
                if (originalValue is DateTime)
                    newValue = DateTime.Parse(Name);
                else if (originalValue is float)
                    newValue = Convert.ToSingle(originalValue);
                else if (originalValue is double)
                    newValue = Convert.ToDouble(originalValue);
                else if (originalValue is int)
                    newValue = Convert.ToInt32(originalValue);
                else if (originalValue is string)
                    newValue = Convert.ToString(originalValue);
                else
                    newValue = Name;
                newSimulation.Set(parentFactor.Paths[0], newValue);
                string newSimulationName = newSimulation.Name;
                AddToName(ref newSimulationName);
                newSimulation.Name = newSimulationName;
            }
        }

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


        public void AddToName(ref string simulationName)
        {
            if (simulationName.Length > 0)
                simulationName += ": ";
            simulationName += ParentFactor.Name + " " + Name;
        }
    }
}
