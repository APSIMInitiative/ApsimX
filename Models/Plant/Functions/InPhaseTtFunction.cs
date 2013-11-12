using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Phen;

namespace Models.Plant.Functions
{
    [Description("Returns the thermal time accumulation from the current phase in phenology")]

    public class InPhaseTtFunction : Function
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
