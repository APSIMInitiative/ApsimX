using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A food pool of given age
    /// </summary>
    [Serializable]
    public class GrazeFoodStorePool : IFeedType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// Dry Matter (%)
        /// </summary>
        [Description("Dry Matter (%)")]
        [Required, Percentage]
        public double DryMatter { get; set; }

        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        [Required, Percentage]
        public double DMD { get; set; }

        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        [Required, Percentage]
        public double Nitrogen { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount { get { return amount; } }
        private double amount = 0;

        /// <summary>
        /// Age of pool in months
        /// </summary>
        [XmlIgnore]
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
        /// Amount detached in this time step (kg)
        /// </summary>
        public double Growth { get; set; }

        /// <summary>
        /// pricing
        /// </summary>
        public ResourcePricing Price(PurchaseOrSalePricingStyleType priceStyle)
        {
            return null;
        }

        /// <summary>
        /// Reset timestep stores
        /// </summary>
        public void Reset()
        {
            Detached = 0;
            Consumed = 0;
            Growth = 0;
        }

        /// <summary>
        /// Add to Resource method.
        /// This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="reason">Name of individual adding resource</param>
        public void Add(object resourceAmount, CLEMModel activity, string reason)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="removeAmount"></param>
        /// <param name="activity"></param>
        /// <param name="reason"></param>
        public double Remove(double removeAmount, CLEMModel activity, string reason)
        {
            removeAmount = Math.Min(this.amount, removeAmount);
            this.Consumed += removeAmount;
            this.amount -= removeAmount;

            return removeAmount;
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public void Remove(ResourceRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newAmount"></param>
        public void Set(double newAmount)
        {
            this.amount = Math.Max(0,newAmount);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Initialise()
        {
            throw new NotImplementedException();
        }
    }
}