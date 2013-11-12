using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Determines which PhaseLookupValue child functions start and end stages bracket the current phenological stage and returns the value of the grand child function decending from the applicable PhaseLookupValue function.")]
    public class PhaseLookup : Function
    {
        
        public override double Value
        {
            get
            {
                foreach (Function F in this.Models)
                {
                    PhaseLookupValue P = F as PhaseLookupValue;
                    if (P.InPhase)
                        return P.Value;
                }
                return 0;  // Default value is zero
            }
        }

        public override string ValueString
        {
            get
            {
                foreach (Function F in this.Models)
                {
                    PhaseLookupValue P = F as PhaseLookupValue;
                    if (P.InPhase)
                        return P.ValueString;
                }
                return "";
            }
        }

    }
}


