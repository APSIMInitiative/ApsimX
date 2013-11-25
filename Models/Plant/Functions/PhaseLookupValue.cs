using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    public class PhaseLookupValue : Function
    {
        [Link]
        Phenology Phenology = null;

        public List<Function> Children { get; set; }

        public string Start = "";

        public string End = "";

        public override double Value
        {
            get
            {
                if (Start == "")
                    throw new Exception("Phase start name not set:" + Name);
                if (End == "")
                    throw new Exception("Phase end name not set:" + Name);

                if (Phenology.Between(Start, End) && Children.Count > 0)
                {
                    Function Lookup = Children[0] as Function;
                    return Lookup.Value;
                }
                else
                    return 0.0;
            }
        }

        public bool InPhase
        {
            get
            {
                return Phenology.Between(Start, End);
            }
        }
    }

}