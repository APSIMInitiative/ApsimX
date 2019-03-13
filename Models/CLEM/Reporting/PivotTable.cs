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
    public class PivotTable : Model
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
        /// The active ledger
        /// </summary>
        public int Ledger { get; set; } = 0;

        /// <summary>
        /// The active expression
        /// </summary>
        public int Expression { get; set; } = 0;

        /// <summary>
        /// The active value
        /// </summary>
        public int Value { get; set; } = 0;

        /// <summary>
        /// The active row
        /// </summary>
        public int Row { get; set; } = 5;

        /// <summary>
        /// The active column
        /// </summary>
        public int Column { get; set; } = 7;

        /// <summary>
        /// The active pivot
        /// </summary>
        public int Pivot { get; set; } = 4;

        /// <summary>
        /// The active time
        /// </summary>
        public int Time { get; set; } = 0;

        /// <summary>
        /// Returns the current pivot
        /// </summary>
        public string GetPivot()
        {
            if (Pivots.Count > ID) return Pivots[ID];
            else throw new IndexOutOfRangeException(ID.ToString());
        }

    }
}