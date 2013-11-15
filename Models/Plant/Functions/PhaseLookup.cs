using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Determines which PhaseLookupValue child functions start and end stages bracket the current phenological stage and returns the value of the grand child function decending from the applicable PhaseLookupValue function.")]
    public class PhaseLookup : Function
    {
        public List<Function> Children { get; set; }

        public override double FunctionValue
        {
            get
            {
                foreach (Function F in Children)
                {
                    PhaseLookupValue P = F as PhaseLookupValue;
                    if (P.InPhase)
                        return P.FunctionValue;
                }
                return 0;  // Default value is zero
            }
        }

        public override string ValueString
        {
            get
            {
                foreach (Function F in Children)
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


