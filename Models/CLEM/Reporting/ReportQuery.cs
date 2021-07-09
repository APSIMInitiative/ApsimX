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
                if(SaveView(storage))
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
            if(!SaveView(dataStore))
            {
                throw new ApsimXException(this, $"Invalid SQL: Unable to create query report [{this.Name}] using SQL provided\r\nIf your SQL contains links to other ReportQueries you may need to run this Report after the others have been created.");
            }
        }

        private bool SaveView(IDataStore store)
        {
            if (SQL != null && SQL != "")
            {
                if ((store.Reader as DataStoreReader).TestSql(SQL))
                {
                    store.AddView(Name, SQL);
                    return true;
                }
                return false;
            }
            return true;
        }
    }
}