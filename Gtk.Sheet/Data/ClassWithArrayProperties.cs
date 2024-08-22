namespace Gtk.Sheet;

/// <summary>
/// A SheetDataProvider that wraps a list of model property instances.
/// </summary>
class ClassWithArrayProperties : IDataProvider
{
    private readonly List<PropertyMetadata> properties;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="properties">The properties.</param>
    public ClassWithArrayProperties(List<PropertyMetadata> properties)
    {
       this.properties = properties;

        if (properties.Any())
        {
            // Determine the number of rows the grid should have. 
            RowCount = this.properties.Max(p => p.Values == null ? 0 : p.Values.Count);
        }
    }

    /// <summary>An event invoked when a cell changes.</summary>
    public event IDataProvider.CellChangedDelegate CellChanged;

    /// <summary>Gets the number of columns of data.</summary>
    public int ColumnCount => properties.Count;

    /// <summary>Gets the number of rows of data.</summary>
    public int RowCount { get; private set; }

    /// <summary>Get the name of a column.</summary>
    /// <param name="columnIndex">Column index.</param>
    public string GetColumnName(int columnIndex)
    {
        if (columnIndex < ColumnCount)
            return properties[columnIndex].Alias;
        throw new Exception($"Invalid column index: {columnIndex}");
    }

    /// <summary>Get the units of a column.</summary>
    /// <param name="columnIndex">Column index.</param>
    public string GetColumnUnits(int columnIndex)
    {
        if (columnIndex < ColumnCount)
            return properties[columnIndex].Units;
        throw new Exception($"Invalid column index: {columnIndex}");
    }

    /// <summary>Get the allowable units of a column.</summary>
    /// <param name="columnIndex">Column index.</param>
    public IReadOnlyList<string> GetColumnValidUnits(int columnIndex)
    {
        if (columnIndex < ColumnCount)
            return properties[columnIndex].ValidUnits;
        throw new Exception($"Invalid column index: {columnIndex}");            
    }        

    /// <summary>Get the cell state.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    public SheetCellState GetCellState(int colIndex, int rowIndex)
    {
        if (properties[colIndex].Metadata != null && 
            properties[colIndex].Metadata.Count > 0 && 
            properties[colIndex].Metadata.All(m => m == SheetCellState.ReadOnly))
            return SheetCellState.ReadOnly;
        if (properties[colIndex].Metadata == null || rowIndex >= properties[colIndex].Metadata.Count)
            return SheetCellState.Normal;
        else
            return properties[colIndex].Metadata[rowIndex];
    }

    /// <summary>Set the cell state.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    /// <param name="state">The cell state</param>
    public void SetCellState(int colIndex, int rowIndex)
    {
        throw new System.NotImplementedException("Cannot change the state of a cell in a properties grid.");
    }

    /// <summary>Get the contents of a cell.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    public string GetCellContents(int colIndex, int rowIndex)
    {
        if (properties[colIndex].Values == null || rowIndex >= properties[colIndex].Values.Count)
            return null;
        else
            return properties[colIndex].Values[rowIndex];
    }

    /// <summary>Set the contents of a cell.</summary>
    /// <param name="colIndices">Column index of cell.</param>
    /// <param name="rowIndices">Row index of cell.</param>
    /// <param name="values">The value.</param>
    public void SetCellContents(int[] colIndices, int[] rowIndices, string[] values)
    {
        // Set property values.
        foreach (var colIndex in colIndices)
            properties[colIndex].SetValues(rowIndices, values);

        // Update the number of rows.
        RowCount = properties.Max(p => p.Values == null ? 0 : p.Values.Count);

        CellChanged?.Invoke(this, colIndices, rowIndices, values);
    }

    /// <summary>Delete the specified rows.</summary>
    /// <param name="rowIndices">Row indexes of cell.</param>
    public void DeleteRows(int[] rowIndices)
    {
        if (rowIndices.Length > 0)
        {
            // Delete row in each property.
            foreach (var property in properties)
                property.DeleteValues(rowIndices);

            // Determine the number of rows the grid should have. 
            RowCount = properties.Max(p => p.Values == null ? 0 : p.Values.Count);

            CellChanged?.Invoke(this, null, rowIndices, null);
        }
    }
}