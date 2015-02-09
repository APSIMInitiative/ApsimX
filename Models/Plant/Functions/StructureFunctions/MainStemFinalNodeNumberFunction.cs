using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// Main stem final node number function
    /// </summary>
    [Serializable]
    [Description("This Function determines final leaf number for a crop.  If no childern are present final leaf number will be the same as primordia number, increasing at the same rate and reaching a fixed value when primordia initiation stops or when maximum leaf number is reached.  However, if a child function called 'FinalLeafNumber' is present that function will determine the increase and fixing of final leaf number")]
    public class MainStemFinalNodeNumberFunction : Model, IFunction
    {
        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The final leaf number</summary>
        [Link(IsOptional=true)]
        IFunction FinalLeafNumber = null;

        /// <summary>The maximum main stem node number</summary>
        public double MaximumMainStemNodeNumber = 0;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (FinalLeafNumber == null)
                {
                    if (Structure.MainStemPrimordiaNo != 0)
                        return Math.Min(MaximumMainStemNodeNumber, Structure.MainStemPrimordiaNo);
                    else 
                        return MaximumMainStemNodeNumber;
                }
                else
                    return Math.Min(FinalLeafNumber.Value, MaximumMainStemNodeNumber);

            }
        }
    }
}
