﻿using Models.CLEM.Resources;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface of a Resource Type.
    /// </summary>
    public interface IResourceType
    {
        /// <summary>
        /// Add this Amount to the existing Amount.
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity requesting resource</param>
        /// <param name="relatesToResource">The resource the transaction relates to, not uses</param>
        /// <param name="category">Name of individual requesting resource</param>
        void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category);

        /// <summary>
        /// Remove this Amount from the existing Amount
        /// </summary>
        /// <param name="request">The resource request object that hold information</param>
        void Remove(ResourceRequest request);

        /// <summary>
        /// Set the amount to this new value.
        /// </summary>
        void Set(double newAmount);

        /// <summary>
        /// Get the current amount of this resource available.
        /// </summary>
        double Amount { get; }

        /// <summary>
        /// Get the amount of the last gain in this resource 
        /// </summary>
        double LastGain { get; set; }

        /// <summary>
        /// Get the current price of this resource.
        /// </summary>
        ResourcePricing Price(PurchaseOrSalePricingStyleType priceType);

        /// <summary>
        /// Value of the resource
        /// </summary>
        double? Value { get; }

        /// <summary>
        /// Get the units of measure this resource.
        /// </summary>
        string Units { get; }

        /// <summary>
        /// Name of model
        /// </summary>
        string Name { get; set; }
    }
}
