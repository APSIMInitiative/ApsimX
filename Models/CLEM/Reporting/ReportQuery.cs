using Models.Core;
using Models.Core.Attributes;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
        /// Tracks the active selection in the value box
        /// </summary>
        [Description("Save the results to the Datastore post simulation")]
        public bool Save { get; set; }

        /// <summary>
        /// The query
        /// </summary>
        public string SQL { get; set; }

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Runs the query
        /// </summary>
        /// <returns></returns>
        public DataTable RunQuery() => FindInScope<DataStore>().Reader.GetDataUsingSql(SQL);

        /// <summary>
        /// 
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            if (Save)
            {
                dataStore.AddView(this.Name, SQL);
            }
        }
    }
}