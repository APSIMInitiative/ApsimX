// -----------------------------------------------------------------------
// <copyright file="CustomQuery.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------


using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A class for custom SQL queries
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Views.CLEM.PivotTableView")]
    [PresenterName("ApsimNG.Presenters.CLEM.PivotTablePresenter")]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [Description("Generates a Pivot Table from the DataStore")]
    [Version(1, 0, 1, "")]
    public class PivotTable : Model, IPostSimulationTool
    {
        /// <summary>
        /// The list of all pivots
        /// </summary>
        public List<string> Pivots { get; set; } = null;

        /// <summary>
        /// The id of the current pivot
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// Returns the current pivot
        /// </summary>
        public string GetPivot()
        {
            if (Pivots.Count > Id) return Pivots[Id];
            else throw new IndexOutOfRangeException(Id.ToString());
        }

        /// <summary>
        /// Executes the query and stores it post simulation
        /// </summary>
        /// <param name="reader">The DataStore</param>
        public void Run(IStorageReader reader)
        {

        }
    }
}