using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Models.Core;
using Models.Utilities;

namespace UserInterface.Views;

/// <summary>
/// 
/// </summary>
public class ModelToSheetDataProvider
{

    /// <summary>
    /// Convert a model to an ISheetDataProvider so that it can be represented in a grid control.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>An ISheetDataProvider instance.</returns>
    public static ISheetDataProvider ToSheetDataProvider(IModel model)
    {
        // Discover all public properties and create a column for each.
        List<GridTableColumn> columns = new();
        List<string> columnUnits = new();
        Dictionary<string, List<SheetDataProviderCellState>> columnCellMetadata = new();
        DiscoverProperties(model, columns, columnUnits, columnCellMetadata);
        if (columnUnits.Count == 0)
            columnUnits = null;

        // Get the DataTable. This will NOT have column names or units in it.
        var gridTable = new GridTable(model.Name, columns, model);
        DataTable data = gridTable.GetData();

        // Get cell states for entire DataTable.
        int numCellStates = data.Rows.Count + 1; // Add one for the column name.
        if (columnUnits != null)
            numCellStates++;                     // Add one for the units row.

        List<List<SheetDataProviderCellState>> columnCellStates = new();
        foreach (var column in columns)
        {
            List<SheetDataProviderCellState> states = new()
            {
                SheetDataProviderCellState.ReadOnly                // this is for the column name
            };
            if (columnUnits != null)
                states.Add(SheetDataProviderCellState.ReadOnly);   // this is for the column units

            if (columnCellMetadata.TryGetValue(column.Name, out var metadata))
                states.AddRange(metadata);
            else if (column.IsReadOnly)
                states.AddRange(Enumerable.Repeat(SheetDataProviderCellState.ReadOnly, numCellStates).ToList());

            // normalise the size of the array.
            while (states.Count < numCellStates)
                states.Add(SheetDataProviderCellState.Normal);
            columnCellStates.Add(states);
        }

        return new DataTableProvider(gridTable.GetData(), columnUnits, columnCellStates);
    }

    private static void DiscoverProperties(IModel model, List<GridTableColumn> columns, List<string> columnUnits, Dictionary<string, List<SheetDataProviderCellState>> columnCellMetadata)
    {
        foreach (var property in model.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (property.Name.EndsWith("Metadata"))
            {
                var metadataValues = property.GetValue(model) as IEnumerable<string>;
                columnCellMetadata.Add(property.Name.Replace("Metadata", ""),
                                       metadataValues.Select(m => m == "Calculated" || m == "Estimated" ? SheetDataProviderCellState.Calculated : SheetDataProviderCellState.Normal).ToList());
            }
            else
            {
                DisplayAttribute display = property.GetCustomAttribute<DisplayAttribute>();
                UnitsAttribute units = property.GetCustomAttribute<UnitsAttribute>();
                if (display != null)
                {
                    string columnName = property.Name;
                    if (!string.IsNullOrEmpty(display.DisplayName))
                        columnName = display.DisplayName;
                    var column = new GridTableColumn(columnName, new VariableProperty[] { new VariableProperty(model, property) });
                    columns.Add(column);
                    if (units != null)
                        columnUnits.Add(units.ToString());
                }
            }
        }
    }
}