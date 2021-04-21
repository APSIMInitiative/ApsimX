using Models.Core;
using Models.Core.Attributes;
using Models.Storage;
using System;
using System.Data;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Provides utility to quickly summarise data from a report
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.CLEMView")]
    [PresenterName("UserInterface.Presenters.ReportQueryPresenter")]
    [ValidParent(ParentType = typeof(Report))]
    [Description("Queries a report")]
    [Version(1, 0, 0, "")]
    public class ReportQuery : Model, ICLEMUI
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>
        /// The line by line query, separated for display purposes
        /// </summary>
        [Description("SQL to run on parent report")]
        [Display(Type = DisplayType.MultiLineText)]
        public string[] Lines { get; set; }
        
        /// <summary>
        /// The complete query
        /// </summary>
        public string SQL => string.Join(" ", Lines);

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Runs the query
        /// </summary>
        public DataTable RunQuery()
        {
            var storage = FindInScope<IDataStore>();
            storage.AddView(Name, SQL);
            return storage.Reader.GetDataUsingSql(SQL);
        }

        /// <summary>
        /// Saves the view post-simulation
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e) => dataStore.AddView(Name, SQL);
    }
}