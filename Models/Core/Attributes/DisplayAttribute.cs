// -----------------------------------------------------------------------
// <copyright file="DisplayAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;

    /// <summary>
    /// An enumeration for display types.
    /// </summary>
    public enum DisplayType
    {
        /// <summary>
        /// No specific display editor.
        /// </summary>
        None,

        /// <summary>
        /// Use the table name editor.
        /// </summary>
        TableName,

        /// <summary>
        /// A cultivar name editor.
        /// </summary>
        CultivarName,

        /// <summary>
        /// A file name editor.
        /// </summary>
        FileName,

        /// <summary>
        /// A field name editor.
        /// </summary>
        FieldName,

        /// <summary>
        /// Use a list of known residue types
        /// </summary>
        ResidueName,

        /// <summary>
        /// A model drop down.
        /// </summary>
        Model
    }

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

        /// <summary>
        /// Gets or sets a value denoting the type of model to show in the model drop down.
        /// </summary>
        public Type ModelType { get; set; }

        /// <summary>
        /// Gets or sets the display type. 
        /// </summary>
        public DisplayType Type { get; set; }
    }
}
