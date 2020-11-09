using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A class for performing basic pivot operations on a resource ledger
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Views.CLEM.PivotTableView")]
    [PresenterName("ApsimNG.Presenters.CLEM.PivotTablePresenter")]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(Folder))]
    [Description("Generates a Pivot Table from the DataStore")]
    [Version(1, 0, 1, "")]
    public class PivotTable : Model
    {
        /// <summary>
        /// Returns the current filter
        /// </summary>
        public string Filter { get; set; } = "";

        /// <summary>
        /// Tracks the active selection in the ledger box
        /// </summary>
        public int LedgerViewBox { get; set; } = 0;

        /// <summary>
        /// Tracks the active selection in the expression box
        /// </summary>
        public int ExpressionViewBox { get; set; } = 0;

        /// <summary>
        /// Tracks the active selection in the value box
        /// </summary>
        public int ValueViewBox { get; set; } = 0;

        /// <summary>
        /// Tracks the active selection in the row box
        /// </summary>
        public int RowViewBox { get; set; } = 5;

        /// <summary>
        /// Tracks the active selection in the column box
        /// </summary>
        public int ColumnViewBox { get; set; } = 7;

        /// <summary>
        /// Tracks the active selection in the pivot box
        /// </summary>
        public int FilterViewBox { get; set; } = 4;

        /// <summary>
        /// Tracks the active selection in the time box
        /// </summary>
        public int TimeViewBox { get; set; } = 0;

    }
}