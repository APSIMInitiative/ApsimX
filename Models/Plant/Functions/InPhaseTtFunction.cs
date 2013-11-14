using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    [Description("Returns the thermal time accumulation from the current phase in phenology")]

    public class InPhaseTtFunction : Function
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
