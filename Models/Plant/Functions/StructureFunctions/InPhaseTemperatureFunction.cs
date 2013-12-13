using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions.StructureFunctions
{
    [Serializable]
    [Description("Returns the curreent InPhase tempature accumulation")]

    public class InPhaseTemperatureFunction : Function
    {
        [Link]
        Phenology Phenology = null;


        public override double Value
        {
            get
            {
                return Phenology.CurrentPhase.TTinPhase;
            }
        }
    }
}
