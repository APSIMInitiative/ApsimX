using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// Compares a base simulation with 1 or more other simulations creating a new table
    /// with side-by-side columns for variables ready for easy graphing
    /// 
    /// For example this table:
    /// 
    /// SimulationName Year TotalC TotalN
    /// Sim1           1970     10     11
    /// Sim1           1971     12     13
    /// Sim2           1970     14     15
    /// Sim2           1971     16     17
    /// 
    /// becomes
    /// 
    /// Year Sim1.TotalC Sim2.TotalC Sim1.TotalN Sim2.TotalN
    /// 1970          10          14          11          15
    /// 1971          12          16          13          17
    /// 
    /// Where the Year column is used as the matching colulmn.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    public class SimulationComparison : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>Gets or sets the name of the predicted table.</summary>
        [Description("Predicted table")]
        [Display(Type = DisplayType.TableName)]
        public string TableName { get; set; }

        /// <summary>Gets or sets the field name used for match.</summary>
        [Description("Field name to use for matching predicted with observed data")]
        [Display(Type = DisplayType.FieldName)]
        public string[] FieldNamesUsedForMatch { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            try
            {
                if (TableName == null)
                    return;

                IEnumerable<Tuple<string, Type>> columns = dataStore.Reader.GetColumns(TableName);
                IEnumerable<string> simulationNames = dataStore.Reader.SimulationNames;

                if (columns == null || !columns.Any())
                    throw new ApsimXException(this, $"Could not find data in table: {TableName}");

                if (FieldNamesUsedForMatch == null || FieldNamesUsedForMatch.Length == 0)
                    throw new ApsimXException(this, $"No field names for matching have been specified");

                var columnsToExclude = FieldNamesUsedForMatch.Concat(new string[] { "SimulationID", "CheckpointID" });

                // Determine which columns to return in the return data.
                var columnsToReturn = columns.Where(c => c.Item2 == typeof(double) || c.Item2 == typeof(int))
                                             .Where(c => !columnsToExclude.Contains(c.Item1))
                                             .ToList();

                // Create a new data table with columns.
                DataTable newData = new DataTable(Name);
                foreach (string columnName in FieldNamesUsedForMatch)
                {
                    var col = columns.First(c => c.Item1 == columnName);
                    newData.Columns.Add(columnName, col.Item2);
                }
                foreach (var column in columnsToReturn)
                    foreach (string simulationName in simulationNames)
                        newData.Columns.Add($"{simulationName}.{column.Item1}", column.Item2);

                DataTable table = dataStore.Reader.GetData(TableName);
                DataView view = new DataView(table);

                // Determine distinct values of match values.
                List<List<string>> distinctMatchValues = new List<List<string>>();
                foreach (var matchColumnName in FieldNamesUsedForMatch)
                {
                    IEnumerable<string> values;
                    if (table.Columns[matchColumnName].DataType == typeof(DateTime))
                        values = DataTableUtilities.GetColumnAsDates(view, matchColumnName)
                                                   .Select(d => d.ToString("#yyyy-MM-dd#"));
                    else
                        values = DataTableUtilities.GetColumnAsStrings(view, matchColumnName);

                    distinctMatchValues.Add(values.Distinct().ToList());
                }

                foreach (var combination in MathUtilities.AllCombinationsOf(distinctMatchValues.ToArray()))
                {
                    StringBuilder filter = new StringBuilder();
                    for (int i = 0; i < combination.Count; i++)
                        filter.AppendLine($"{FieldNamesUsedForMatch[i]}={combination[i]}");

                    DataRow newRow = newData.NewRow();

                    view.RowFilter = filter.ToString().Trim();
                    for (int i = 0; i < combination.Count; i++)
                        newRow[FieldNamesUsedForMatch[i]] = combination[i];

                    foreach (DataRowView row in view)
                    {
                        var simulationName = (string)row["SimulationName"];
                        foreach (var column in columnsToReturn)
                        {
                            object value = row[column.Item1];

                            if (column.Item2 != value.GetType())
                                value = ReflectionUtilities.StringToObject(column.Item2, value.ToString());

                            newRow[$"{simulationName}.{column.Item1}"] = value;
                        }
                    }
                    newData.Rows.Add(newRow);
                }

                // Write table to datastore.
                dataStore.Writer.WriteTable(newData);
            }
            catch (Exception err)
            {
                throw new Exception($"Error in PredictedObserved tool {Name}", err);
            }
        }
    }
}
