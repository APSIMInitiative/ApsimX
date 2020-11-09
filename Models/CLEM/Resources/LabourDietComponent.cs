using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Stores the details of a dietary component in the time step
    /// </summary>
    public class LabourDietComponent
    {
        /// <summary>
        /// Link to the food store consumed
        /// </summary>
        public HumanFoodStoreType FoodStore { get; set; }

        /// <summary>
        /// Amount consumed
        /// </summary>
        public double AmountConsumed { get; set; }

        /// <summary>
        /// Inital metric values from other food sources
        /// </summary>
        private Dictionary<string, double> otherMetricAmounts = new Dictionary<string, double>();

        /// <summary>
        /// A method to add addition metric amount at the start of the month to represent value non food store consumption
        /// </summary>
        /// <param name="metric">Name of the metric</param>
        /// <param name="amount">Amount for time step for AE</param>
        public void AddOtherSource(string metric, double amount)
        {
            double val;
            if (otherMetricAmounts.TryGetValue(metric, out val))
            {
                otherMetricAmounts[metric] = val + amount;
            }
            else
            {
                otherMetricAmounts.Add(metric, amount);
            }
        }

        /// <summary>
        /// Return the total of the specified metric in the diet
        /// </summary>
        /// <param name="metric"></param>
        /// <returns></returns>
        public double GetTotal(string metric)
        {
            double result = 0;
            if(FoodStore is null)
            {
                // check if initial value provided
                otherMetricAmounts.TryGetValue(metric, out result);
            }
            else
            {
                // convert to metric if possible
                var amount = FoodStore.ConvertTo(metric, AmountConsumed);
                if (amount != null)
                {
                    Double.TryParse(amount.ToString(), out result);
                }
            }
            return result;
        }
    }
}
