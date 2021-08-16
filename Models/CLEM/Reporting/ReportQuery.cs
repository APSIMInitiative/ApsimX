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
    [Description("Allows an SQL statement to be applied to the database as a view for analysis and graphing")]
    [Version(1, 0, 0, "")]
    public class ReportQuery : Model, ICLEMUI
    {
        [Link]
        private IDataStore dataStore = null;
        //[Link]
        //private Summary summary = null;

        /// <summary>
        /// The line by line SQL query, separated for display purposes
        /// </summary>
        [Description("SQL statement")]
        [Display(Type = DisplayType.MultiLineText)]
        [System.ComponentModel.DataAnnotations.Required]
        public string SQL { get; set; }

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Runs the query
        /// </summary>
        public DataTable RunQuery()
        {
            var storage = FindInScope<IDataStore>();
            if (storage != null)
            {
                if (SaveView(storage, out _))
                {
                    return storage.Reader.GetDataUsingSql(SQL);
                }
            }
            return new DataTable();
        }

        /// <summary>
        /// Saves the view post-simulation
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            string errorMsg;
            if(!SaveView(dataStore, out errorMsg))
            {
                throw new ApsimXException(this, $"Invalid SQL: Unable to create query report [{this.Name}] using SQL provided\r\nError: {errorMsg}\r\nIf your SQL contains links to other ReportQueries you may need to run this Report after the others have been created by disabling it in the first run and then enabling again.");
                //summary.WriteWarning(this, $"Invalid SQL: Unable to create query report [{this.Name}] using SQL provided\r\nIf your SQL contains links to other ReportQueries you may need to run this Report after the others have been created.");
            }
        }

        private bool SaveView(IDataStore store, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                if (SQL != null && SQL != "")
                {
                    (store.Reader as DataStoreReader).ExecuteSql(SQL);
                    store.AddView(Name, SQL);
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            return false;
        }
    }
}