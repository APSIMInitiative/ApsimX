// -----------------------------------------------------------------------
// <copyright file="DisplayAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies various user interface display properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the display format (e.g. 'N3') that the user interface should
        /// use when showing values in the related property.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user interface should display
        /// a total at the top of the column in a ProfileGrid.
        /// </summary>
        public bool ShowTotal { get; set; }

        public enum DisplayTypeEnum { None, TableName };

        /// <summary>
        /// Gets or sets the display type. 
        /// </summary>
        public DisplayTypeEnum DisplayType { get; set; }
    }
}
