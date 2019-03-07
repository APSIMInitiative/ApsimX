// -----------------------------------------------------------------------
// <copyright file="CustomQuery.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------


using Models.Core;
using Models.Core.Attributes;
using System;

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
        /// Executes the query and stores it post simulation
        /// </summary>
        /// <param name="reader">The DataStore</param>
        public void Run(IStorageReader reader)
        {

        }
    }
}