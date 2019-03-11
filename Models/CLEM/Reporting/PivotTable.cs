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
        public int ID { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int Ledger { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int Expression { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int Value { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int Row { get; set; } = 5;

        /// <summary>
        /// 
        /// </summary>
        public int Column { get; set; } = 7;

        /// <summary>
        /// 
        /// </summary>
        public int Pivot { get; set; } = 4;

        /// <summary>
        /// Returns the current pivot
        /// </summary>
        public string GetPivot()
        {
            if (Pivots.Count > ID) return Pivots[ID];
            else throw new IndexOutOfRangeException(ID.ToString());
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