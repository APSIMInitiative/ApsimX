﻿using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A food pool of given age
    /// </summary>
    [Serializable]
    public class GrazeFoodStorePool : IFeedType, IResourceType
    {
        /// <inheritdoc/>
        public double EnergyGross { get; set; }

        /// <inheritdoc/>
        public double FatContent { get; set; }

        /// <inheritdoc/>
        public double Nitrogen { get; set; }

        /// <inheritdoc/>
        public double CPDegradability { get; set; }

        /// <inheritdoc/>
        public double DMD { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
        public double Amount { get { return amount; } }
        private double amount = 0;

        /// <summary>
        /// Age of pool in months
        /// </summary>
        [JsonIgnore]
        public int Age { get; set; }

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
        public double Growth { get; set; }

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
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Get the amount of the last gain in this resource 
        /// </summary>
        [JsonIgnore]
        public double LastGain { get; set; }

        /// <summary>
        /// Reset timestep stores
        /// </summary>
        public void Reset()
        {
            Detached = 0;
            Consumed = 0;
            Growth = 0;
        }

        /// <inheritdoc/>
        public void Initialise()
        {
            throw new NotImplementedException();
        }

        #region transactions

        /// <inheritdoc/>
        public void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add to Resource method.
        /// This style is used when a pool needs to be added to the current pool
        /// This occurs when no detachment and decay (values of zero) are included in the GrazeFoodStore parameters
        /// </summary>
        /// <param name="pool">GrazeFoodStorePool to add to this pool</param>
        public void Add(GrazeFoodStorePool pool)
        {
            if (pool.Amount > 0)
            {
                // adjust DMD and N% based on incoming if needed
                if (DMD != pool.DMD || Nitrogen != pool.Nitrogen)
                {
                    //TODO: run calculation passed others.
                    DMD = ((DMD * Amount) + (pool.DMD * pool.Amount)) / (Amount + pool.Amount);
                    Nitrogen = ((Nitrogen * Amount) + (pool.Nitrogen * pool.Amount)) / (Amount + pool.Amount);
                }
                amount += pool.Amount;
                Growth += pool.Growth;
            }
        }

        /// <inheritdoc/>
        public double Remove(double removeAmount, CLEMModel activity, string reason)
        {
            removeAmount = Math.Min(this.amount, removeAmount);
            this.Consumed += removeAmount;
            this.amount -= removeAmount;

            return removeAmount;
        }

        /// <inheritdoc/>
        public void Remove(ResourceRequest request)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Set(double newAmount)
        {
            this.amount = Math.Max(0, newAmount);
        } 
        #endregion

    }
}