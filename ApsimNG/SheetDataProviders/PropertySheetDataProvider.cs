using System.Collections.Generic;
using System.Linq;

namespace UserInterface.Views;

/// <summary>
/// A SheetDataProvider that wraps a list of model property instances.
/// </summary>
class PropertySheetDataProvider : ISheetDataProvider
{
    private readonly List<PropertyMetadata> properties;
    private int numHeadingRows;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <param name="model">The model.</param>
    public PropertySheetDataProvider(List<PropertyMetadata> properties)
    {
        this.properties = properties;

        if (properties.Any())
        {
            // Determine number of heading rows.
            numHeadingRows = properties.Any(p => !string.IsNullOrEmpty(p.Units)) ? 2 : 1;

            // Determine the number of rows the grid should have. 
            RowCount = properties.Max(p => p.Values == null ? 0 : p.Values.Count) + numHeadingRows;
        }
    }

    /// <summary>An event invoked when a cell changes.</summary>
    public event ISheetDataProvider.CellChangedDelegate CellChanged;

    /// <summary>Gets the number of columns of data.</summary>
    public int ColumnCount => properties.Count;

    /// <summary>Gets the number of rows of data.</summary>
    public int RowCount { get; private set; }

    /// <summary>Get the cell state.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    public SheetDataProviderCellState GetCellState(int colIndex, int rowIndex)
    {
        if (rowIndex < numHeadingRows)
            return SheetDataProviderCellState.ReadOnly;

        int valuesIndex = rowIndex - numHeadingRows;

        if (properties[colIndex].Metadata == null || valuesIndex >= properties[colIndex].Metadata.Count)
            return SheetDataProviderCellState.Normal;
        else
            return properties[colIndex].Metadata[valuesIndex];
    }

    /// <summary>Set the cell state.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    /// <param name="state">The cell state</param>
    public void SetCellState(int colIndex, int rowIndex)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>Get the Units assigned to this column</summary>
    /// <param name="colIndex">Column index of cell.</param>
    public string GetColumnUnits(int colIndex)
    {
        return properties[colIndex].Units;
    }

    /// <summary>Get the contents of a cell.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    public string GetCellContents(int colIndex, int rowIndex)
    {
        if (rowIndex == 0)
            return properties[colIndex].Alias;
        if (rowIndex == 1 && numHeadingRows == 2)
            return properties[colIndex].Units;
        
        int valuesIndex = rowIndex - numHeadingRows;

        if (properties[colIndex].Values == null || valuesIndex >= properties[colIndex].Values.Count)
            return null;
        else
            return properties[colIndex].Values[valuesIndex];
    }

    /// <summary>Set the contents of a cell.</summary>
    /// <param name="colIndices">Column index of cell.</param>
    /// <param name="rowIndices">Row index of cell.</param>
    /// <param name="values">The value.</param>
    public void SetCellContents(int[] colIndices, int[] rowIndices, string[] values)
    {
        var valueIndicies = new List<int>();
        for (int i = 0; i < rowIndices.Length; i++)
        {
            if (rowIndices[i] >= numHeadingRows)
                valueIndicies.Add(rowIndices[i] - numHeadingRows);
        }

        if (valueIndicies.Count > 0)
        {
            // Set property values.
            foreach (var colIndex in colIndices)
                properties[colIndex].SetValues(valueIndicies.ToArray(), values);

            // Update the number of rows.
            RowCount = properties.Max(p => p.Values.Count) + numHeadingRows;

            CellChanged?.Invoke(this, colIndices, rowIndices, values);
        }
    }
}