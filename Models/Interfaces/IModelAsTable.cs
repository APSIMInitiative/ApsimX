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
        /// <summary>Return a table of user editable values</summary>
        DataTable GetTable();

        /// <summary>User has edited the values - set the table back in the model</summary>
        /// <param name="table">The values the user has edited.</param>
        void SetTable(DataTable table);

    }
}
