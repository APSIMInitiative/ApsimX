using APSIM.Numerics;
using BruTile;
using Models.CLEM.Interfaces;
using NetTopologySuite.Mathematics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A food pool of given age
    /// </summary>
    [Serializable]
    public class GrazeFoodStorePool : IFeed
    {
        /// <inheritdoc/>
        public FeedType TypeOfFeed { get; set; } = FeedType.PastureTropical;

        /// <inheritdoc/>
        public double GrossEnergyContent { get; set; }

        /// <inheritdoc/>
        public double MetabolisableEnergyContent { get; set; }

        /// <inheritdoc/>
        public double FatPercent { get; set; }

        private double nitrogenPercent = 0;

        /// <inheritdoc/>
        public double NitrogenPercent
        {
            get
            {
                return nitrogenPercent;
            }
            set
            {
                nitrogenPercent = value;
                CrudeProteinPercent = nitrogenPercent * 6.25;
            }
        }

        /// <inheritdoc/>
        public double CrudeProteinPercent { get; set; }

        /// <summary>
        /// Style of providing the dry matter digestibility of pasture
        /// </summary>
        public DryMatterDigestibilityStyle DMDStyle { get; set; }

        /// <inheritdoc/>
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        private double rumenDegradableProteinPercent;

        /// <inheritdoc/>
        public double RumenDegradableProteinPercent
        {
            get
            {
                return rumenDegradableProteinPercent;
            }
            set
            {
                rumenDegradableProteinPercent = value;
                AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(rumenDegradableProteinPercent, TypeOfFeed);
            }
        }

        /// <inheritdoc/>
        public double AcidDetergentInsolubleProtein { get; set; }

        /// <inheritdoc/>
        public double GutFill { get; set; }

        /// <summary>
        /// Age of pool in months
        /// </summary>
        [JsonIgnore]
        public int Age { get; set; }

        /// <summary>
        /// Date the growth was added to pools
        /// </summary>
        [JsonIgnore]
        public DateTime GrowthDate { get; set; } = new DateTime();

        /// <summary>
        /// Amount to set at start (kg)
        /// </summary>
        public double StartingAmount { get; set; }

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
        public double Growth => (Age == 0) ? Amount : 0; // { get; set; }

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
        public ResourcePricing Price(PurchaseOrSalePricingStyleType priceStyle)
        {
            return null;
        }

        /// <inheritdoc/>
        public double? Value
        {
            get { return null; }
        }

        private double amount = 0;

        /// <summary>
        /// Amount of biomass in this pool (kg)
        /// </summary>
        public double Amount => amount;

        /// <summary>
        /// Amount of biomass in this pool (kg)
        /// </summary>
        public double AmountAvailable => amount - AmountPending;

        /// <summary>
        /// Amount of biomass in this pool that is currently a pending take by an activity
        /// </summary>
        public double AmountPending { get; set; } = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startingAmount">Initial amount of biomass in the pool (kg)</param>
        /// <param name="age">Age of pool (in months) when created</param>
        public GrazeFoodStorePool(double startingAmount, int age = 0)
        {
            amount = startingAmount;
            Age = age;
        }

        /// <summary>
        /// Reset timestep stores
        /// </summary>
        public void Reset()
        {
            Detached = 0;
            Consumed = 0;
            AmountPending = 0;
        }

        /// <inheritdoc/>
        public void Initialise()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add another pool arranging quality mixing This style is used when a pool needs to be added to the current
        /// pool This occurs when no detachment and decay (values of zero) are included in the GrazeFoodStore parameters
        /// </summary>
        /// <param name="pool">GrazeFoodStorePool to add to this pool</param>
        public void Add(GrazeFoodStorePool pool)
        {
            if (pool.Amount <= 0) return;

            // adjust DMD and N% based on incoming if needed
            if (DryMatterDigestibility != pool.DryMatterDigestibility || NitrogenPercent != pool.NitrogenPercent)
            {
                //TODO: run calculation passed others.
                DryMatterDigestibility = ((DryMatterDigestibility * Amount) + (pool.DryMatterDigestibility * pool.Amount)) / (Amount + pool.Amount);
                NitrogenPercent = ((NitrogenPercent * Amount) + (pool.NitrogenPercent * pool.Amount)) / (Amount + pool.Amount);
            }
            amount += pool.Amount;
        }

        /// <summary>
        /// Remove an amout from the pool
        /// </summary>
        /// <param name="removeAmount">Amount taken</param>
        public void Remove(double removeAmount)
        {
            // TODO: do when need to consider burning separate to grazing in reporting or is it all consumed
            removeAmount = Math.Min(removeAmount, Amount);
            Consumed += removeAmount;
            amount -= removeAmount;
        }

        /// <summary>
        /// Reduce the pending amount in the pool
        /// </summary>
        /// <param name="amountReturned">Amount to reduce from pending (total for time step)</param>
        public void ReducePending(double amountReturned)
        {
            AmountPending -= Math.Min(AmountPending, amountReturned);
        }

        /// <summary>
        /// Detatch a proportion of the pool
        /// </summary>
        /// <param name="proportion">Proportion of the pool to detach</param>
        /// <returns>
        /// The amount detached from the pool (kg)
        /// </returns>
        public double Detach(double proportion)
        {
            double removeAmount = AmountAvailable * proportion;
            AmountPending *= proportion;
            Detached += removeAmount;
            amount -= removeAmount;
            return removeAmount;
        }

        /// <summary>
        /// Consume a specified amount of the pool (cattle, fire, cut and carry) removing from the pool and adjusting
        /// pending if required
        /// </summary>
        /// <param name="amount">Amount of the pool consumed</param>
        /// <param name="reducePending">Reduce pending</param>
        public void Consume(double amount, bool reducePending = true)
        {
            double removeAmount = Math.Min(amount, Amount);
            if (reducePending)
            {
                AmountPending = Math.Max(0, MathUtilities.RoundToZero(AmountPending - removeAmount, 1e-5));
            }
            Consumed += removeAmount;
            this.amount -= removeAmount;
        }

        /// <summary>
        /// Consume the pending amount
        /// </summary>
        public void ConsumePending()
        {
            Consumed += AmountPending;
            this.amount -= AmountPending;
            AmountPending = 0;
        }

        /// <summary>
        /// Used to set the amount in the pool at the start of simulation
        /// </summary>
        /// <param name="amount">Amount in the pool</param>
        public void InitialBiomassSet(double amount)
        {
            this.amount = amount;

        }
    }
}