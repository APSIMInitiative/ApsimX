using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// This is a post simulation tool that lets the user filter the rows of a source data table.
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class Filter : Model, IPostSimulationTool
    {
        /// <summary>Link to datastore</summary>
        [Link]
        private IDataStore dataStore = null;

        /// <summary>The name of the source table name.</summary>
        [Description("Name of source table")]
        [Display(Type = DisplayType.TableName)]
        public string SourceTableName { get; set; }

        /// <summary>The row filter.</summary>
        [Description("Row filter")]
        [Display]
        public string FilterString { get; set; }

        /// <summary>The row filter.</summary>
        [Description("List columns to include (one per line). Leave empty for all columns")]
        [Display(Type = DisplayType.MultiLineText)]
        public string[] ColumnFilter { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (string.IsNullOrEmpty(FilterString))
                throw new Exception($"Empty filter found in {Name}");

            var sourceData = dataStore.Reader.GetData(SourceTableName);
            if (sourceData != null)
            {
                var view = new DataView(sourceData);
                view.RowFilter = FilterString;

                // Strip out unwanted columns.
                var table = view.ToTable();

                if (ColumnFilter != null && ColumnFilter.Length > 0)
                {
                    var columnsToKeep = new List<string>(ColumnFilter);
                    columnsToKeep.Add("SimulationName");

                    // Trim spaces from all column names.
                    for (int i = 0; i < columnsToKeep.Count; i++)
                        columnsToKeep[i] = new string(columnsToKeep[i].Trim().Where(c => c != '[' && c != ']').ToArray());

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        if (!columnsToKeep.Contains(table.Columns[i].ColumnName))
                        {
                            table.Columns.Remove(table.Columns[i]);
                            i--;
                        }
                    }
                }

                // Give the new data table to the data store.
                table.TableName = Name;
                dataStore.Writer.WriteTable(table);
            }
        }
    }
}
