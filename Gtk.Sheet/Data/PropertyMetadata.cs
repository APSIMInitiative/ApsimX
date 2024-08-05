using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace Gtk.Sheet;

/// <summary>
/// An internal class representing a property that has been discovered.
/// </summary>
public class PropertyMetadata
{
    private readonly PropertyInfo metadataProperty;
    private List<string> values;
    private string format;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="obj">The object instance</param>
    /// <param name="property">The property info</param>
    /// <param name="metadataProperty">The property info</param>
    /// <param name="alias">The column name to use in the grid.</param>
    /// <param name="units">The units of the property.</param>
    /// <param name="validUnits">The valid units of the property.</param>
    /// <param name="format">The format to use when displaying values in the grid.</param>
    /// <param name="metadata">The metadata for each value.</param>
    public PropertyMetadata(object obj, PropertyInfo property, PropertyInfo metadataProperty, string alias, string units, string[] validUnits, string format)
    {
        Obj = obj;
        Property = property;
        this.metadataProperty = metadataProperty;
        ValidUnits = validUnits;
        Alias = alias;
        if (Alias == null)
            Alias = property.Name;
        Units = units;
        this.format = format;
        if (metadataProperty == null)
            Metadata = new();
        else
        {
            var metadataValue = metadataProperty.GetValue(obj, null);
            if (metadataValue is string s)
                Metadata = new() { s == "Calculated" || s == "Estimated" ? SheetCellState.Calculated : SheetCellState.Normal };
            else
            {
                var metadata = metadataValue as string[];
                Metadata = metadata?.Select(m => m == "Calculated" || m == "Estimated" ? SheetCellState.Calculated : SheetCellState.Normal).ToList();
            }
        }
        GetValues();

        if (Values != null && (!property.CanWrite || property.SetMethod.IsPrivate))
            Metadata.AddRange(Enumerable.Repeat(SheetCellState.ReadOnly, Values.Count));
    }

    /// <summary>The alias of the property.</summary>
    public string Alias { get; set; }
    
    /// <summary>The units of the property.</summary>
    public string Units { get; set; }

    /// <summary>The valid units of the property.</summary>
    public string[] ValidUnits { get; }

    /// <summary>The object instance.</summary>
    public object Obj { get; }
    
    /// <summary>The metadata of the property.</summary>
    public List<SheetCellState> Metadata { get; set;}

    public PropertyInfo Property { get; }

    /// <summary>The values of the property.</summary>
    public ReadOnlyCollection<string> Values => values?.AsReadOnly();

    /// <summary>
    /// Set the array values of the property.
    /// </summary>
    /// <param name="valueIndices">The indicies of the rows to change</param>
    /// <param name="values">The new values.</param>
    /// <exception cref="NotImplementedException"></exception>
    public void SetValues(int[] valueIndices, string[] values)
    {
        if (valueIndices.Length != values.Length)
            throw new Exception("The number of row indicies is not equal to the number of values while setting model properties from a grid.");
        bool dataWasChanged = false;
        for (int i = 0; i < valueIndices.Length; i++)
        {
            if (Metadata == null || i >= Metadata.Count || Metadata[i] != SheetCellState.ReadOnly)
            {
                // Ensure we have enough space in values and metadata lists.
                if (this.values == null)
                    this.values = new();
                while (valueIndices[i] > this.values.Count-1)
                    this.values.Add(null);

                if (Metadata == null)
                    Metadata = new();                    
                while (valueIndices[i] > Metadata.Count-1)
                    Metadata.Add(SheetCellState.Normal);

                this.values[valueIndices[i]] = values[i];
                Metadata[valueIndices[i]] = SheetCellState.Normal;
                dataWasChanged = true;
            }
        }

        if (dataWasChanged)
        {
            SetValues();
            SetMetadata();
        }
    }

    /// <summary>
    /// Delete specific elements of values.
    /// </summary>
    /// <param name="valueIndices">The indicies of the rows to delete</param>
    public void DeleteValues(int[] valueIndices)
    {
        bool dataWasChanged = false;
        foreach (var i in valueIndices.Reverse())
        {
            if (Metadata == null || i >= Metadata.Count || Metadata[i] != SheetCellState.ReadOnly)
            {
                if (values != null && i < values.Count)
                {
                    values.RemoveAt(i);
                    dataWasChanged = true;
                }

                if (Metadata != null && i < Metadata.Count)
                    Metadata.RemoveAt(i);
            }
        }

        if (dataWasChanged)
        {
            SetValues();
            SetMetadata();
        }
    }    

    /// <summary>
    /// Get formatted values for the property.
    /// </summary>
    private void GetValues()
    {
        object propertyValue = Property.GetValue(Obj, null);

        if (propertyValue != null)
        {
            if (propertyValue is string st)
                values = new List<string>() { st };
            else if (propertyValue is double doub)
                values = new List<string>() { double.IsNaN(doub) ? string.Empty : doub.ToString(format) };
            else if (propertyValue is int integer)
                values = new List<string>() { integer.ToString() };
            else if (propertyValue is DateTime datet)
                values = new List<string>() { datet.ToString("yyyy/MM/dd") };
            else if (propertyValue is string[] s)
                values = s.Select(v => v?.ToString()).ToList();
            else if (propertyValue is double[] d)
                values = d.Select(v => double.IsNaN(v) ? string.Empty : v.ToString(format)).ToList();
            else if (propertyValue is int[] i)
                values = i.Select(v => v.ToString()).ToList();
            else if (propertyValue is bool[] b)
                values = b.Select(v => v ? "X" : string.Empty).ToList();
            else if (propertyValue is DateTime[] dt)
                values = dt.Select(v => v.ToString("yyyy/MM/dd")).ToList();
            else if (propertyValue is List<string> ls)
                values = ls.Select(v => v?.ToString()).ToList();
            else if (propertyValue is List<double> ld)
                values = ld.Select(v => double.IsNaN(v) ? string.Empty : v.ToString(format)).ToList();
            else if (propertyValue is List<int> li)
                values = li.Select(v => v.ToString()).ToList();
            else if (propertyValue is bool[] lb)
                values = lb.Select(v => v ? "X" : string.Empty).ToList();
            else if (propertyValue is List<DateTime> ldt)
                values = ldt.Select(v => v.ToString("yyyy/MM/dd")).ToList();
        }
    }

    /// <summary>
    /// Set values back to the property.
    /// </summary>
    private void SetValues()
    {
        object newValues = null;

        if (Property.PropertyType == typeof(string))
            newValues = values.First();
        else if (Property.PropertyType == typeof(double))
            newValues = Double.TryParse(values.First(), out double d) ? d : double.NaN;
        else if (Property.PropertyType == typeof(int))
            newValues = Int32.TryParse(values.First(), out int i) ? i : Int32.MaxValue;
        else if (Property.PropertyType == typeof(DateTime))
            newValues = DateTime.ParseExact(values.First(), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        else if (Property.PropertyType == typeof(string[]))
            newValues = values.ToArray();
        else if (Property.PropertyType == typeof(double[]))
        {
            newValues = values.Select(v => 
            {
                if (Double.TryParse(v, out double d))
                    return d;
                return double.NaN;
            }).ToArray();
        }
        else if (Property.PropertyType == typeof(int[]))
            newValues = values.Select(v => 
            {
                if (Int32.TryParse(v, out int i))
                    return i;
                return Int32.MaxValue;

            }).ToArray();
        else if (Property.PropertyType == typeof(bool[]))
            newValues = values.Select(v => v == "X" || v == "x").ToList();
        else if (Property.PropertyType == typeof(DateTime[]))
            newValues = values.Select(v => DateTime.ParseExact(v, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToArray();
        else if (Property.PropertyType == typeof(List<string>))
            newValues = values.ToList();
        else if (Property.PropertyType == typeof(List<double>))
        {
            newValues = values.Select(v => 
            {
                if (Double.TryParse(v, out double d))
                    return d;
                return double.NaN;
            }).ToList();
        }
        else if (Property.PropertyType == typeof(List<int>))
            newValues = values.Select(v => 
            {
                if (Int32.TryParse(v, out int i))
                    return i;
                return Int32.MaxValue;

            }).ToList();
        else if (Property.PropertyType == typeof(List<bool>))
            newValues = values.Select(v => v == "X" || v == "x").ToList();
        else if (Property.PropertyType == typeof(DateTime[]) || Property.PropertyType == typeof(List<DateTime>))
            newValues = values.Select(v => DateTime.ParseExact(v, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToList();
        else
            throw new Exception($"Unknown property data type found while trying to set values from grid. Data type: {Property.PropertyType}");

        if (Property.CanWrite)
            Property.SetValue(Obj, newValues);
    }

    private void SetMetadata()
    {
        if (Metadata != null && metadataProperty != null)
        {
            if (metadataProperty.PropertyType.IsArray)
                metadataProperty.SetValue(Obj, Metadata.Select(m => m == SheetCellState.Calculated ? "Calculated" : null).ToArray());    
            else
                metadataProperty.SetValue(Obj, Metadata.First() == SheetCellState.Calculated ? "Calculated" : null);
        }
    }
}