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

        public void ApplyToSimulation(Simulation newSimulation, List<string> Paths)
        {
            if (Paths.Count >= 1)
            {
                object originalValue = newSimulation.Get(Paths[0]);
                object newValue;
                if (originalValue is DateTime)
                    newValue = DateTime.Parse(Name);
                else
                    newValue = Name;
                newSimulation.Set(Paths[0], newValue);
            }
        }
    }
}
