using APSIM.Core;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.Core.Run;
using Models.Storage;
using System;
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
    public class ReportQuery : Model, ICLEMUI, IPostSimulationTool, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

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

        /// <inheritdoc/>
        public void Run() => SaveView(dataStore);

        /// <summary>
        /// Runs the query
        /// </summary>
        public DataTable RunQuery()
        {
            var storage = Structure.Find<IDataStore>() ?? dataStore;
            string viewSQL = storage.GetViewSQL(Name);
            if (viewSQL != "")
                return storage.Reader.GetData(Name);
            return new DataTable();
        }

        private void SaveView(IDataStore store)
        {
            if (Name.Any(c => c == ' '))
                throw new Exception($"Invalid name: [{Name}]\n[ReportQuery] names cannot contain spaces as they are used to name the database tables");

            if (SQL != null && SQL != "")
            {
                // Find the data
                var storage = Structure.Find<IDataStore>() ?? dataStore;

                string viewSQL = storage.GetViewSQL(Name);
                if (!viewSQL.EndsWith(SQL.TrimEnd(new char[] { '\r', '\n' })))
                {
                    // We assume any sql errors are thrown when data is retreived
                    storage.AddView($"{Name}", SQL);
                }
            }
        }
    }
}