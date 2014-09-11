using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    public class PhaseLookupValue : Function
    {
        [Link]
        Phenology Phenology = null;

        private List<IModel> ChildFunctions;

        public string Start = "";

        public string End = "";

        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function));

                if (Start == "")
                    throw new Exception("Phase start name not set:" + Name);
                if (End == "")
                    throw new Exception("Phase end name not set:" + Name);

                if (Phenology.Between(Start, End) && ChildFunctions.Count > 0)
                {
                    Function Lookup = ChildFunctions[0] as Function;
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