// -----------------------------------------------------------------------
// <copyright file="VariableProperty.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Models.Soils;

    /// <summary>
    /// Encapsulates a discovered property of a model. Provides properties for
    /// returning information about the property. 
    /// </summary>
    [Serializable]
    public class VariableProperty : IVariable
    {
        /// <summary>
        /// Gets or sets the PropertyInfo for this property.
        /// </summary>
        private PropertyInfo property;

        /// <summary>
        /// An optional lower bound array specifier.
        /// </summary>
        private int lowerArraySpecifier;

        /// <summary>
        /// An optional upper bound array specifier.
        /// </summary>
        private int upperArraySpecifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableProperty" /> class.
        /// </summary>
        /// <param name="model">The underlying model for the property</param>
        /// <param name="property">The PropertyInfo for this property</param>
        /// <param name="arraySpecifier">An optional array specifier e.g. 1:3</param>
        public VariableProperty(object model, PropertyInfo property, string arraySpecifier = null)
        {
            if (model == null || property == null)
            {
                throw new ApsimXException(null, "Cannot create an instance of class VariableProperty with a null model or propertyInfo");
            }
            
            this.Object = model;
            this.property = property;
            if (arraySpecifier != null)
            {
                // Can be either a number or a range e.g. 1:3
                int posColon = arraySpecifier.IndexOf(':');
                if (posColon == -1)
                {
                    lowerArraySpecifier = Convert.ToInt32(arraySpecifier);
                    upperArraySpecifier = lowerArraySpecifier;
                }
                else
                {
                    lowerArraySpecifier = Convert.ToInt32(arraySpecifier.Substring(0, posColon));
                    upperArraySpecifier = Convert.ToInt32(arraySpecifier.Substring(posColon + 1));
                }
            }
            else
            {
                lowerArraySpecifier = 0;
                upperArraySpecifier = 0;
            }
        }

        /// <summary>
        /// Gets or sets the underlying model that this property belongs to.
        /// </summary>
        public override object Object { get; set; }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public override string Name 
        { 
            get 
            {
                return this.property.Name; 
            } 
        }

        /// <summary>
        /// Gets the description of the property
        /// </summary>
        public override string Description
        {
            get
            {
                DescriptionAttribute descriptionAttribute = Utility.Reflection.GetAttribute(this.property, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descriptionAttribute == null)
                {
                    return null;
                }

                if (this.Object is SoilCrop)
                {
                    return (this.Object as SoilCrop).Name + " " + descriptionAttribute.ToString();
                }

                return descriptionAttribute.ToString();
            }
        }

        /// <summary>
        /// Gets the units of the property
        /// </summary>
        public override string Units
        {
            get
            {
                // Get units from property
                string unitString = null;
                UnitsAttribute unitsAttribute = Utility.Reflection.GetAttribute(this.property, typeof(UnitsAttribute), false) as UnitsAttribute;
                PropertyInfo unitsInfo = this.Object.GetType().GetProperty(this.property.Name + "Units");
                MethodInfo unitsToStringInfo = this.Object.GetType().GetMethod(this.property.Name + "UnitsToString");
                if (unitsAttribute != null)
                {
                    unitString = unitsAttribute.ToString();
                }
                else if (unitsToStringInfo != null)
                {
                    unitString = (string)unitsToStringInfo.Invoke(this.Object, new object[] { null });
                }

                return unitString;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the property is readonly.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                if (!this.property.CanWrite)
                {
                    return true;
                }

                if (this.Metadata.Contains("Estimated") || this.Metadata.Contains("Calculated"))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the metadata for each layer. Returns new string[0] if none available.
        /// </summary>
        public string[] Metadata
        {
            get
            {
                PropertyInfo metadataInfo = this.Object.GetType().GetProperty(this.property.Name + "Metadata");
                if (metadataInfo != null)
                {
                    string[] metadata = metadataInfo.GetValue(this.Object, null) as string[];
                    if (metadata != null)
                    {
                        return metadata;
                    }
                }

                return new string[0];
            }
        }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public Type DataType
        {
            get
            {
                return this.property.PropertyType;
            }
        }

        /// <summary>
        /// Gets the values of the property
        /// </summary>
        public override object Value
        {
            get
            {
                object obj = this.property.GetValue(this.Object, null);

                if (obj != null && obj.GetType().IsArray && lowerArraySpecifier != 0)
                {
                    Array array = obj as Array;
                    if (array.Length == 0)
                    {
                        return null;
                    }

                    int numElements = upperArraySpecifier - lowerArraySpecifier + 1;
                    Array values = Array.CreateInstance(this.property.PropertyType.GetElementType(), numElements);
                    for (int i = lowerArraySpecifier; i <= upperArraySpecifier; i++)
                    {
                        int index = i - lowerArraySpecifier;
                        if (i < 1 || i > array.Length)
                        {
                            throw new Exception("Array index out of bounds while getting variable: " + Name);
                        }

                        values.SetValue(array.GetValue(i - 1), index);
                    }
                    if (values.Length == 1)
                    {
                    //    return values.GetValue(0);
                    }

                    return values;
                }

                return obj;
            }

            set
            {
                if (value is string)
                {
                    SetFromString(value as string);
                }
                else
                {
                    this.property.SetValue(this.Object, value, null);
                }
            }
        }

        /// <summary>
        /// Gets the value of the specified property with arrays converted to comma separated strings.
        /// </summary>
        public object ValueWithArrayHandling
        {
            get
            {
                object value = this.Value;
                if (value == null)
                {
                    return string.Empty;
                }

                if (this.DataType.IsArray)
                {
                    string stringValue = string.Empty;
                    Array arr = value as Array;
                    if (arr == null)
                    {
                        return stringValue;
                    }

                    for (int j = 0; j < arr.Length; j++)
                    {
                        if (j > 0)
                        {
                            stringValue += ",";
                        }

                        stringValue += arr.GetValue(j).ToString();
                    }

                    value = stringValue;
                }

                return value;
            }

            set
            {
                if (value is string)
                {
                    SetFromString(value as string);
                }
                else
                {
                    this.Value = value;   
                }
            }
        }

        /// <summary>
        /// Set the value of this object via a string.
        /// </summary>
        /// <param name="value">The string value to set this property to</param>
        private void SetFromString(string value)
        {
            if (DataType.IsArray)
            {
                string[] stringValues = value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (DataType == typeof(double[]))
                {
                    Value = Utility.Math.StringsToDoubles(stringValues);
                }
                else if (DataType == typeof(int[]))
                {
                    Value = Utility.Math.StringsToDoubles(stringValues);
                }
                else if (DataType == typeof(string[]))
                {
                    Value = stringValues;
                }
                else
                {
                    throw new ApsimXException(null, "Invalid property type: " + DataType.ToString());
                }
            }
            else
            {
                if (DataType == typeof(double))
                {
                    Value = Convert.ToDouble(value);
                }
                else if (DataType == typeof(int))
                {
                    Value = Convert.ToInt32(value);
                }
                else if (DataType == typeof(string))
                {
                    this.property.SetValue(this.Object, value, null);
                }
                else
                {
                    Value = value;
                }
            }
        }

        /// <summary>
        /// Gets the display format for this property e.g. 'N3'. Can return null if not present.
        /// </summary>
        public string Format
        {
            get
            {
                DisplayAttribute displayFormatAttribute = Utility.Reflection.GetAttribute(this.property, typeof(DisplayAttribute), false) as DisplayAttribute;
                if (displayFormatAttribute != null && displayFormatAttribute.Format != null)
                {
                    return displayFormatAttribute.Format;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the crop name of the property or null if this property isn't a crop one.
        /// </summary>
        public string CropName
        {
            get
            {
                if (this.Object is SoilCrop)
                {
                    return (this.Object as SoilCrop).Name;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the sum of all values in this array property if the property has been 
        /// marked as [DisplayTotal]. Otherwise return double.Nan
        /// </summary>
        public double Total
        {
            get
            {
                DisplayAttribute displayFormatAttribute = Utility.Reflection.GetAttribute(this.property, typeof(DisplayAttribute), false) as DisplayAttribute;
                bool hasDisplayTotal = displayFormatAttribute != null && displayFormatAttribute.ShowTotal;
                if (hasDisplayTotal && this.Value != null)
                {
                    double sum = 0.0;
                    foreach (double doubleValue in this.Value as IEnumerable<double>)
                    {
                        if (doubleValue != Utility.Math.MissingValue)
                        {
                            sum += doubleValue;
                        }
                    }

                    return sum;
                }

                return double.NaN;
            }
        }

        /// <summary>
        /// Gets the associated display type for the related property.
        /// </summary>
        public DisplayAttribute.DisplayTypeEnum DisplayType
        {
            get
            {
                DisplayAttribute displayAttribute = Utility.Reflection.GetAttribute(this.property, typeof(DisplayAttribute), false) as DisplayAttribute;
                if (displayAttribute != null)
                {
                    return displayAttribute.DisplayType;
                }
                else
                {
                    return DisplayAttribute.DisplayTypeEnum.None;
                }
            }
        }
    }
}
