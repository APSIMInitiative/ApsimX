using Newtonsoft.Json;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A food pool of given age
    /// </summary>
    [Serializable]
    public class HumanFoodStorePool
    {
        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
        public double Amount { get { return amount; } }
        private double amount = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="amount">Amount of food</param>
        /// <param name="age">Age of food</param>
        public HumanFoodStorePool(double amount, int age)
        {
            this.amount = amount;
            Age = age;
        }

        /// <summary>
        /// Age of pool in months
        /// </summary>
        [JsonIgnore]
        public int Age { get; set; }

        #region transactions

        /// <summary>
        /// Add to Resource method.
        /// This style is used when a pool needs to be added to the current pool
        /// </summary>
        /// <param name="pool">HumanFoodStorePool to add to this pool</param>
        public void Add(HumanFoodStorePool pool)
        {
            if (pool.Amount > 0)
                amount += pool.Amount;
        }

        /// <summary>
        /// Add to Resource method.
        /// This style is used when a pool needs to be added to the current pool
        /// </summary>
        /// <param name="amount">Amount to add to this pool</param>
        public void Add(double amount)
        {
            if (amount > 0)
                this.amount += amount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="removeAmount"></param>
        /// <param name="activity"></param>
        /// <param name="reason"></param>
        public double Remove(double removeAmount, CLEMModel activity, string reason)
        {
            removeAmount = Math.Min(this.amount, removeAmount);
            this.amount -= removeAmount;
            return removeAmount;
        }
        #endregion
    }
}
