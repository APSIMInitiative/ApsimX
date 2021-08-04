using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for all attribute models
    /// </summary>
    public interface ISetAttribute
    {
        /// <summary>
        /// Property to return a random assignment of the attribute
        /// </summary>
        Resources.IndividualAttribute GetRandomSetAttribute();

        /// <summary>
        /// Name to apply to the attribute
        /// </summary>
        string AttributeName { get; set; }

    }
}
