// -----------------------------------------------------------------------
// <copyright file="ValidParentAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies the models that this class can sit under in the user interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ValidParentAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidParentAttribute" /> class.
        /// </summary>
        public ValidParentAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidParentAttribute" /> class.
        /// </summary>
        /// <param name="model">The model that this class must sit under.</param>
        public ValidParentAttribute(Type model)
        {
            this.ParentModels = new Type[] { model };
        }

        /// <summary>
        /// Gets or sets the list of allowable parent models
        /// </summary>
        public Type[] ParentModels { get; set; }
    }
}
