using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models.CLEM
{
    /// <summary>
    /// Interface of a Resource Type.
    /// </summary>
    public interface IResourceType
    {
        /// <summary>
        /// Add this Amount to the existing Amount.
        /// </summary>
        /// <param name="ResourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="ActivityName">Name of activity requesting resource</param>
        /// <param name="Reason">Name of individual requesting resource</param>
        void Add(object ResourceAmount, string ActivityName, string Reason);

        /// <summary>
        /// Remove this Amount from the existing Amount
        /// </summary>
        /// <param name="Request">The resource request object that hold information</param>
        void Remove(ResourceRequest Request);

        /// <summary>
        /// Set the amount to this new value.
        /// </summary>
        void Set(double NewAmount);

        /// <summary>
        /// Initialise the variables that store the current state of the resource.
        /// </summary>
        void Initialise();

        /// <summary>
        /// Get the current amount of this resource available.
        /// </summary>
        double Amount { get; }
    }
}
