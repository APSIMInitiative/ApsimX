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
            if (SQL != "")
            {
                var storage = FindInScope<IDataStore>();
                AddView(storage, Name, SQL);
                return storage.Reader.GetDataUsingSql(SQL);
            }
            else
            {
                return new DataTable();
            }
        }

        private void AddView(IDataStore data, string name, string sql)
        {
            try
            {
                data.AddView(Name, SQL);
            }
            catch (Exception ex)
            {
                throw new ApsimXException(this, $"Error trying to execute SQL query for [{this.Name}]: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the view post-simulation
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            if (SQL != "")
            {
                AddView(dataStore, Name, SQL);
            }
        }
    }
}