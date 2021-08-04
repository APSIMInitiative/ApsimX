using Models.CLEM.Resources;
using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Describes an individual with an attribute list
    /// </summary>
    public interface IAttributable
    {
        /// <summary>
        /// A list of attributes added to this individual
        /// </summary>
        IndividualAttributeList Attributes { get; set; }
    }
}
