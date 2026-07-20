using APSIM.Numerics;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Defines the interface for any feed pool being fed to animals via grazing
    /// </summary>
    public interface IGrazeIntakePool: IFeed
    {
        /// <summary>
        /// Age of pool in months
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Amount detached in this time step (kg)
        /// </summary>
        public double Detached { get; set; }

        /// <summary>
        /// Amount consumed in this time step (kg)
        /// </summary>
        public double Consumed { get; set; }

        /// <summary>
        /// Amount of growth in this time step (kg)
        /// </summary>
        public double Growth { get; }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <summary>
        /// Amount of biomass in this pool (kg)
        /// </summary>
        public double Amount { get; }

        /// <summary>
        /// Amount of biomass in this pool (kg)
        /// </summary>
        public double AmountAvailable { get; }

        /// <summary>
        /// Amount of biomass in this pool that is currently a pending take by an activity
        /// </summary>
        public double AmountPending { get; set; }

        /// <summary>
        /// Reset timestep stores
        /// </summary>
        public void Reset();

        /// <summary>
        /// Add another pool arranging quality mixing This style is used when a pool needs to be added to the current
        /// pool This occurs when no detachment and decay (values of zero) are included in the GrazeFoodStore parameters
        /// </summary>
        /// <param name="pool">GrazeFoodStorePool to add to this pool</param>
        public void Add(GrazeFoodStorePool pool);

        /// <summary>
        /// Remove an amout from the pool
        /// </summary>
        /// <param name="removeAmount">Amount taken</param>
        public void Remove(double removeAmount);

        /// <summary>
        /// Reduce the pending amount in the pool
        /// </summary>
        /// <param name="amountReturned">Amount to reduce from pending (total for time step)</param>
        public void ReducePending(double amountReturned);

        /// <summary>
        /// Detatch a proportion of the pool
        /// </summary>
        /// <param name="proportion">Proportion of the pool to detach</param>
        /// <returns>
        /// The amount detached from the pool (kg)
        /// </returns>
        public double Detach(double proportion);

        /// <summary>
        /// Consume a specified amount of the pool (cattle, fire, cut and carry) removing from the pool and adjusting
        /// pending if required
        /// </summary>
        /// <param name="amount">Amount of the pool consumed</param>
        /// <param name="reducePending">Reduce pending</param>
        public void Consume(double amount, bool reducePending = true);

        /// <summary>
        /// Consume the pending amount
        /// </summary>
        public void ConsumePending();
    }
}
