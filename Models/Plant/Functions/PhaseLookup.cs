using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Determines which PhaseLookupValue child functions start and end stages bracket the current phenological stage and returns the value of the grand child function decending from the applicable PhaseLookupValue function.")]
    public class PhaseLookup : Function
    {
        private List<Function> Children { get; set; }

        public override double Value
        {
            get
            {
                if (Children == null)
                    Children = ModelsMatching<Function>();

                foreach (Function F in Children)
                {
                    PhaseLookupValue P = F as PhaseLookupValue;
                    if (P.InPhase)
                        return P.Value;
                }
                return 0;  // Default value is zero
            }
        }

    }
}


