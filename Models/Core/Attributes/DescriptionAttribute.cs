// -----------------------------------------------------------------------
// <copyright file="DescriptionAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class DescriptionAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the view class
        /// </summary>
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptionAttribute" /> class.
        /// </summary>
        /// <param name="description">Description text</param>
        public DescriptionAttribute(string description)
        {
            this.description = description;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        /// <returns>The description</returns>
        public string ToString()
        {
            return this.description;
        }
    } 
}
