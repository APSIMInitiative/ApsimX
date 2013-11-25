using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions.StructureFunctions
{
    [Description("This Function determines final leaf number for a crop.  If no childern are present final leaf number will be the same as primordia number, increasing at the same rate and reaching a fixed value when primordia initiation stops or when maximum leaf number is reached.  However, if a child function called 'FinalLeafNumber' is present that function will determine the increase and fixing of final leaf number")]
    public class MainStemFinalNodeNumberFunction : Function
    {
        [Link]
        Structure Structure = null;

        public Function FinalLeafNumber { get; set; }

        double _FinalNodeNumber = 0;

        public double MaximumMainStemNodeNumber = 0;

        public override void UpdateVariables(string initial)
        {
            if (initial == "yes")
                _FinalNodeNumber = MaximumMainStemNodeNumber;
            else
            {
                if (FinalLeafNumber == null)
                    _FinalNodeNumber = Math.Min(MaximumMainStemNodeNumber, Structure.MainStemPrimordiaNo);
                else
                    _FinalNodeNumber = Math.Min(FinalLeafNumber.Value, MaximumMainStemNodeNumber);
            }
        }

        public void Clear()
        {
            _FinalNodeNumber = 0;
        }

        
        public override double Value
        {
            get
            {
                return _FinalNodeNumber;
            }
        }
    }
}
