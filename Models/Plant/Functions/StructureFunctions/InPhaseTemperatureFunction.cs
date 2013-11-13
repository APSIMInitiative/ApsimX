using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Phen;

namespace Models.Plant.Functions.StructureFunctions
{
    [Description("Returns the curreent InPhase tempature accumulation")]

    public class InPhaseTemperatureFunction : Function
    {
        [Link]
        Phenology Phenology = null;


        public override double FunctionValue
        {
            get
            {
                return Phenology.CurrentPhase.TTinPhase;
            }
        }
    }
}
