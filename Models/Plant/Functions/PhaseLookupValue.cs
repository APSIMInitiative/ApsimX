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

        public string Start = "";

        public string End = "";

        public override double FunctionValue
        {
            get
            {
                if (Start == "")
                    throw new Exception("Phase start name not set:" + Name);
                if (End == "")
                    throw new Exception("Phase end name not set:" + Name);

                object[] Children = this.Models;
                if (Phenology.Between(Start, End) && Children.Length > 0)
                {
                    Function Lookup = Children[0] as Function;
                    return Lookup.FunctionValue;
                }
                else
                    return 0.0;
            }
        }
        public override string ValueString
        {
            get
            {
                if (Start == "")
                    throw new Exception("Phase start name not set:" + Name);
                if (End == "")
                    throw new Exception("Phase end name not set:" + Name);

                object[] Children = this.Models;
                if (Phenology.Between(Start, End) && Children.Length > 0)
                {
                    Function Lookup = Children[0] as Function;
                    return Lookup.ValueString;
                }
                else
                    return "";
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