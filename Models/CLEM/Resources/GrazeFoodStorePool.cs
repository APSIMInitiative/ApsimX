using APSIM.Numerics;
using BruTile;
using Models.CLEM.Interfaces;
using Models.PMF.Phen;
using NetTopologySuite.Mathematics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A pasture pool of given age as used by CLEM pasture
    /// </summary>
    [Serializable]
    public class GrazeFoodStorePool : IGrazeIntakePool
    {
        private IGrazeFoodStoreType grazeStore;

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

        private double dmd = 0;

        /// <inheritdoc/>
        public double DryMatterDigestibility 
        { 
            get
            {
                return dmd;
            }
            set
            {
                dmd = value;
                GutFill = grazeStore?.CalculateGutFill(dmd)??0.08; // ToDo: determine default gut fill if grazestore not provided.
            }
        }

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
        /// Age of pool in days
        /// </summary>
        [JsonIgnore]
        public int AgeInDays { get; set; }

        /// <summary>
        /// Age of pool in months
        /// </summary>
        [JsonIgnore]
        public int AgeInMonths { get; set; }

        /// <summary>
        /// Date the growth was added to pool
        /// </summary>
        [JsonIgnore]
        public DateTime GrowthDate { get; private set; } = new DateTime();

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
        /// Determines if this is a growth time step
        /// </summary>
        public bool IsGrowthThisTimeStep { get; private set; }

        /// <summary>
        /// Amount of growth in this time step (kg)
        /// </summary>
        public double Growth => (IsGrowthThisTimeStep) ? Amount : 0;

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

        /// <inheritdoc/>
        public double Amount => amount;

        /// <inheritdoc/>
        public double AmountAvailable => amount - AmountPending;
        /// <inheritdoc/>
        public double AmountPending { get; private set; } = 0;
        /// <inheritdoc/>
        public double AmountInitialPending { get; private set; }

        /// <summary>
        /// Constructor for working with temporary pools (e.g. for grazing) where no store reference or age is required
        /// </summary>
        /// <param name="startingAmount">Initial amount of biomass in the pool (kg)</param>
        /// <param name="store">Reference to the GrazeFoodStoreType that owns this pool</param>
        public GrazeFoodStorePool(double startingAmount, GrazeFoodStoreType store = null)
        {
            if (store is not null)
                grazeStore = store;
            amount = startingAmount;
        }

        /// <summary>
        /// Constructor for working with pools that are part of a GrazeFoodStoreType where a reference to the store and
        /// age tracking is required
        /// </summary>
        /// <param name="startingAmount">Initial amount of biomass in the pool (kg)</param>
        /// <param name="store">Reference to the GrazeFoodStoreType that owns this pool</param>
        /// <param name="growthDate">Date the growth was added to pool</param>
        /// <param name="currentDate">Current date of the simulation</param>
        public GrazeFoodStorePool(double startingAmount, GrazeFoodStoreType store, DateTime growthDate, DateTime currentDate)
        {
            if (growthDate > currentDate)
                throw new ArgumentException($"Growth date {growthDate.ToShortDateString()} cannot be after current date {currentDate.ToShortDateString()} during initialisation pools in [{store.NameWithParent}]");

            amount = startingAmount;
            grazeStore = store;

            UpdateAge(growthDate, currentDate);
        }

        /// <summary>
        /// Method to update the age of the pool based on the growth date and current date. This method calculates the
        /// age in days and months, and determines if this is a growth time step.
        /// </summary>
        /// <param name="growthDate">
        /// Date the growth was added to the pool (will be first day of time step in update pasture)
        /// </param>
        /// <param name="currentDate">Current date of the simulation</param>
        public void UpdateAge(DateTime growthDate, DateTime currentDate)
        {
            GrowthDate = growthDate;
            IsGrowthThisTimeStep = growthDate == currentDate;

            AgeInDays = (int)(currentDate - GrowthDate).TotalDays;
            AgeInMonths = CalculateMonthsDifference(GrowthDate, currentDate);
        }

        private static int CalculateMonthsDifference(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentOutOfRangeException(nameof(startDate), "The start date must be before the end date.");
            }

            endDate = endDate.AddDays(1);
            int months = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;

            if (endDate.Day < startDate.Day)
            {
                months--;
            }

            return months;
        }

        /// <summary>
        /// Reset timestep stores
        /// </summary>
        public void Reset()
        {
            Detached = 0;
            Consumed = 0;
            AmountPending = 0;
            AmountInitialPending = 0;
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
                //amount weighted average
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
            // TODO: deoes pending also detach? AmountPending *= proportion;
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
            AmountInitialPending = 0;
        }

        /// <inheritdoc/>
        public void SetPending(double amountPending)
        {
            AmountPending = amountPending;
            AmountInitialPending = amountPending;
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