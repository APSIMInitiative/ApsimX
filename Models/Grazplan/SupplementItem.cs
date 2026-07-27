// -----------------------------------------------------------------------
// The GrazPlan Supplement objects
// -----------------------------------------------------------------------
using System;
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// A record to allow us to hold amount and cost information along
    /// with the FoodSupplement information
    /// In FoodSupplementItem, the "amount" should be read as kg of supplement fresh
    /// weight. and the cost should be per kg fresh weight.
    /// </summary>
    [Serializable]
    public class SupplementItem : FoodSupplement
    {
        /// <summary>
        /// SupplementItem constructor
        /// </summary>
        public SupplementItem() : base()
        {
        }

        /// <summary>
        /// Constructor
        /// Note that it makes a copy of the FoodSupplement
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="amt">The amt.</param>
        /// <param name="cst">The CST.</param>
        public SupplementItem(FoodSupplement src, double amt = 0.0, double cst = 0.0) : base(src)
        {
            Amount = amt;
            Cost = cst;
        }

        /// <summary>
        /// Gets or sets the amount in kg.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the cost.
        /// </summary>
        /// <value>
        /// The cost.
        /// </value>
        [Units("-")]
        public double Cost { get; set; }

        /// <summary>
        /// Assigns the specified source supp.
        /// </summary>
        /// <param name="srcSupp">The source supp.</param>
        public void Assign(SupplementItem srcSupp)
        {
            if (srcSupp != null)
            {
                base.Assign(srcSupp);
                Amount = srcSupp.Amount;
                Cost = srcSupp.Cost;
            }
        }
    }
}
