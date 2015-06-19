using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// This function returns the daily delta for its child function
    /// </summary>
    [Serializable]
    [Description("Stores the value of its child function (called Integral) from yesterday and returns the difference between that and todays value of the child function")]
    public class DeltaFunction : Model, IFunction
    {
        //Class members
        /// <summary>The accumulated value</summary>
        private double YesterdaysValue = 0;
        
        /// <summary>The child function to return a delta for</summary>
        [Link]
        IFunction Integral = null;

        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            YesterdaysValue = 0;
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            YesterdaysValue = Integral.Value;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return Integral.Value - YesterdaysValue;
            }
        }
    }
}
