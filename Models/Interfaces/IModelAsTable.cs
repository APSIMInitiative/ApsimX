// -----------------------------------------------------------------------
// <copyright file="IModelAsTable.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System;
    using System.Data;

    /// <summary>This interface describes the way a grid presenter talks to a model via a data table.</summary>
    public interface IModelAsTable
    {
        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        DataTable Table { get; set; }
    }
}
