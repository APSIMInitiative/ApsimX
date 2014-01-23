using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using System.Xml;

namespace Models.Factorial
{
    [Serializable]
    [ViewName("UserInterface.Views.FactorView")]
    [PresenterName("UserInterface.Presenters.FactorPresenter")]
    [AllowDropOn("Factors")]
    public class Factor : ModelCollection
    {
        public List<string> Paths { get; set; }

        [XmlElement("FactorValue")]
        public List<FactorValue> FactorValues { get; set; }

        public void ApplyToSimulation(Simulation newSimulation, int factorValueI)
        {
            if (factorValueI < FactorValues.Count && Paths.Count >= 1)
            {
                FactorValues[factorValueI].ApplyToSimulation(newSimulation, Paths);
                string newName = newSimulation.Name;
                AddToName(ref newName, factorValueI);
                newSimulation.Name = newName;
            }
        }

        public void AddToName(ref string simulationName, int factorValueI)
        {
            simulationName += Name + " " + FactorValues[factorValueI].Name; // append the right most path bit.
        }
    }
}
