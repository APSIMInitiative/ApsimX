using Newtonsoft.Json;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A food pool of given age
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="amount">Amount of food</param>
    /// <param name="age">Age of food</param>
    [Serializable]
    public class HumanFoodStorePool(double amount, int age)
    {
        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
        public double Amount { get { return amount; } }
        private double amount = amount;

        /// <summary>
        /// Age of pool in months
        /// </summary>
        [JsonIgnore]
        public int Age { get; set; } = age;

        #region transactions

        /// <summary>
        /// Add to Resource method.
        /// This style is used when a pool needs to be added to the current pool
        /// </summary>
        /// <param name="pool">HumanFoodStorePool to add to this pool</param>
        public void Add(HumanFoodStorePool pool)
        {
            if (pool.Amount > 0)
            {
                amount += pool.Amount;
            }
        }

        /// <summary>
        /// Add to Resource method.
        /// This style is used when a pool needs to be added to the current pool
        /// </summary>
        /// <param name="amount">Amount to add to this pool</param>
        public void Add(double amount)
        {
            if (amount > 0)
            {
                this.amount += amount;
            }
        }

        /// <summary>
        /// Remove from Resource method applied to an amount from a HumanFoodStorePool.
        /// </summary>
        /// <param name="removeAmount">Amount to remove from this pool</param>
        public double Remove(double removeAmount)
        {
            removeAmount = Math.Min(amount, removeAmount);
            amount -= removeAmount;
            return removeAmount;
        }
        #endregion
    }
}
