// -----------------------------------------------------------------------
// <copyright file="ContextMenuAttribute.cs" company="APSIM Initiative">
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
    [AttributeUsage(AttributeTargets.Method)]
    public class ContextMenuAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the menu name
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Gets or sets the model types that this menu applies to
        /// </summary>
        public Type[] AppliesTo { get; set; }
    } 
}
