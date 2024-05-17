using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Utilities
{
    /// <summary>Encapsulates a column of data.</summary>
    public class GridTableColumn
    {
        /// <summary>Column units.</summary>
        private string units;

        /// <summary>A collection of properties that need to be kept in sync i.e. when one changes they all get changed. e.g. 'Depth'</summary>
        private IEnumerable<VariableProperty> properties { get; }

        /// <summary>Metadata about the property.</summary>
        private readonly VariableProperty metadata;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the column. Used for property name in Lists</param>
        /// <param name="property">The PropertyInfo instance</param>
        /// <param name="readOnly">Is the column readonly?</param>
        /// <param name="units">The units of the column.</param>
        /// <param name="metadata">Metadata about the property.</param>
        public GridTableColumn(string name, object property, bool readOnly = false, string units = null, VariableProperty metadata = null)
        {
            Name = name;
            this.metadata = metadata;

            //This is a merger of the old DataTables and the TabularData systems.
            //If an property array is provided, it uses the VariableProperty system
            //If a list is provided, it stores the list for use.

            if (property is VariableProperty props)
            {
                properties = new VariableProperty[] { property as VariableProperty };
            }

            IsReadOnly = readOnly;
            if (properties != null && IsReadOnly == false)
                IsReadOnly = properties.First().IsReadOnly;

            this.units = units;
            if (properties != null && this.units == null)
                this.units = properties.First().Units;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the column.</param>
        /// <param name="properties">The collection of PropertyInfo instances that are all related.</param>
        public GridTableColumn(string name, IEnumerable<VariableProperty> properties)
        {
            Name = name;
            this.properties = properties;
        }

        /// <summary>Name of column.</summary>
        public string Name { get; }

        /// <summary>Column units.</summary>
        public string Units
        {
            get => units;
            set
            {
                properties.First().Units = value;
                units = value;
            }
        }

        /// <summary>Which rows of this column are calculated?</summary>
        public List<bool> IsCalculated 
        { 
            get
            {
                var metadataValues = metadata?.Value as string[];
                if (metadataValues == null)
                    return null;

                return metadataValues.Select(m => m == "Calculated" || m == "Estimated").Prepend(false).Prepend(false).ToList();
            }
            set
            {
                if (value != null)
                    metadata.Value = value.Select(v => v ? "Calculated" : null).ToArray();
            }
        }

        /// <summary>Allowable column units.</summary>
        public IEnumerable<string> AllowableUnits => properties.First().AllowableUnits.Select(au => au.Name);

        /// <summary>Is the column readonly?</summary>
        public bool IsReadOnly { get; }

        /// <summary>Add a column to a DataTable.</summary>
        /// <param name="data">The DataTable.</param>
        /// <param name="hasUnits">Does the Table have a Units row</param>
        public void AddColumnToDataTable(DataTable data, bool hasUnits)
        {
            int startingRow = 0;
            if (hasUnits)
                startingRow = 1;

            if (properties != null)
            {
                var property = properties.First();
                var propertyValue = property.Value;
                if (propertyValue != null)
                {
                    if (propertyValue is Array)
                    {
                        string[] values = null;
                        if (property.DataType == typeof(string[]))
                            values = ((string[])propertyValue).Select(v => v.ToString()).ToArray();
                        else if (property.DataType == typeof(double[]))
                            values = ((double[])propertyValue).Select(v => double.IsNaN(v) ? string.Empty : v.ToString("F3")).ToArray();
                        else if (property.DataType == typeof(bool[]))
                            values = ((bool[])propertyValue).Select(v => v.ToString()).ToArray();
                        else if (property.DataType == typeof(int[]))
                            values = ((int[])propertyValue).Select(v => v.ToString()).ToArray();
                        else if (property.DataType == typeof(DateTime[]))
                            values = ((DateTime[])propertyValue).Select(v => v.ToString("yyyy/MM/dd")).ToArray();

                        DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Length);
                    }
                    else if (propertyValue is IEnumerable)
                    {
                        List<string> values = new List<string>();

                        PropertyInfo propInfo = null;
                        FieldInfo fieldInfo = null;
                        foreach (var obj in (propertyValue as IEnumerable))
                        {
                            object val = null;
                            if (VariableIsPrimitive(obj))
                            {
                                val = obj;
                            }
                            else
                            {
                                if (propInfo == null)
                                    propInfo = obj.GetType().GetProperty(Name);
                                if (fieldInfo == null)
                                    fieldInfo = obj.GetType().GetField(Name);

                                if (propInfo != null)
                                    val = propInfo.GetValue(obj);
                                else if (fieldInfo != null)
                                    val = fieldInfo.GetValue(obj);
                            }

                            if (val is double && double.IsNaN((double)val))
                                values.Add(string.Empty);
                            else
                                values.Add(val.ToString());
                        }
                        DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Count);
                    }
                }
            }
        }

        /// <summary>Setting model data. Called by GUI.</summary>
        /// <param name="data"></param>'
        /// <param name="hasUnits"></param>'
        public void Set(DataTable data, bool hasUnits)
        {
            if (properties != null)
            {
                Type propertyType = properties.First().DataType;
                if (propertyType == typeof(string[]) || propertyType == typeof(double[]) || propertyType == typeof(DateTime[]))
                {
                    int numRows = data.Rows.Count;
                    foreach (var property in properties)
                    {
                        if (!property.IsReadOnly)
                        {
                            int startRow = 0;
                            if (hasUnits)
                                startRow = 1;
                            if (property.DataType == typeof(string[]))
                                property.Value = DataTableUtilities.GetColumnAsStrings(data, Name, numRows, startRow, CultureInfo.CurrentCulture);
                            else if (property.DataType == typeof(double[]))
                            {
                                double[] modified = DataTableUtilities.GetColumnAsDoubles(data, Name, numRows, startRow, CultureInfo.CurrentCulture);
                                if (metadata != null)
                                {
                                    double[] original = property.Value as double[];                                
                                    string[] originalMetadata = metadata.Value as string[];
                                    string[] modifiedMetadata = SoilUtilities.DetermineMetadata(original, originalMetadata, modified, null);
                                    metadata.Value = modifiedMetadata;
                                }
                                property.Value = modified;
                            }
                            else if (property.DataType == typeof(DateTime[]))
                                property.Value = DataTableUtilities.GetColumnAsDates(data, Name, numRows-startRow, startRow);
                        }
                    }
                }
                else if (propertyType.FullName.Contains("System.Collections.Generic.List"))
                {
                    Model model = properties.First().Object as Model;
                    PropertyInfo fieldInfo = model.GetType().GetProperty(properties.First().Name);
                    IEnumerable<object> list = fieldInfo.GetValue(model) as IEnumerable<object>;

                    //make a new list
                    if (list != null)
                    {
                        Type elementType = list.GetType().GetGenericArguments()[0];
                        Type listType = typeof(List<>).MakeGenericType(new[] { elementType });
                        IList newList = (IList)Activator.CreateInstance(listType);

                        if (TypeIsPrimitive(elementType))
                        {
                            for (int i = 0; i < data.Rows.Count; i++)
                            {
                                TypeConverter typeConverter = TypeDescriptor.GetConverter(elementType);
                                object propValue = typeConverter.ConvertFromString(data.Rows[i][Name].ToString());
                                newList.Add(propValue);
                            }

                            //Set the Model to use our modified list
                            fieldInfo.SetValue(model, newList);
                        }
                        else
                        {
                            //each column send it's own update event
                            //so we need to use the existing object's value to avoid overwriting them
                            //but we only add as many rows as required by the new table
                            foreach (object obj in list)
                                if (newList.Count < data.Rows.Count)
                                    newList.Add(obj);
                            //on the first column that runs, it must add additional entries for any new lines
                            for (int i = 0; i < data.Rows.Count - list.Count(); i++)
                                newList.Add(Activator.CreateInstance(elementType));
                            //once we have a list of the right length, we then write in our cells one at a time.
                            for (int i = 0; i < data.Rows.Count; i++)
                            {
                                string value = data.Rows[i][Name].ToString();
                                ApplyChangesToListData(newList[i], Name, value);
                            }
                            //Set the Model to use our modified list
                            fieldInfo.SetValue(model, newList);
                        }
                    }
                }
            }
        }

        private static void ApplyChangesToListData(object obj, string name, string value)
        {
            PropertyInfo propInfo = obj.GetType().GetProperty(name);
            FieldInfo fieldInfo = obj.GetType().GetField(name);

            if (propInfo != null)
            {
                if (propInfo.PropertyType == typeof(double))
                {
                    double valueAsDouble = double.NaN;
                    if (!String.IsNullOrEmpty(value))
                        valueAsDouble = Convert.ToDouble(value);

                    propInfo.SetValue(obj, valueAsDouble);
                }
                else if (propInfo.PropertyType == typeof(bool))
                {
                    bool? valueAsBoolen = null;
                    if (!String.IsNullOrEmpty(value))
                        valueAsBoolen = Convert.ToBoolean(value);
                    propInfo.SetValue(obj, valueAsBoolen);
                }
                else
                {
                    propInfo.SetValue(obj, value);
                }
            }

            else if (fieldInfo != null)
            {
                if (fieldInfo.FieldType == typeof(double))
                {
                    double valueAsDouble = double.NaN;
                    if (!String.IsNullOrEmpty(value))
                        valueAsDouble = Convert.ToDouble(value);

                    fieldInfo.SetValue(obj, valueAsDouble);
                }
                else
                {
                    fieldInfo.SetValue(obj, value);
                }
            }

        }

        private bool VariableIsPrimitive(object obj)
        {
            return TypeIsPrimitive(obj.GetType());
        }

        private bool TypeIsPrimitive(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(System.String))
                return true;
            else
                return false;
        }
    }
}
