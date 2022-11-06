namespace Models.Core
{
    using System;
    using System.Reflection;
    using Models.Core;
    using APSIM.Shared.Utilities;


    /// <summary>
    /// This class encapsulates a single property of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    [Serializable]
    public class VariableObject : IVariable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public VariableObject(object model)
        {
            Object = model;
        }

        /// <summary>
        /// Gets or sets the object this variable is relative to
        /// </summary>
        public override object Object { get; set; }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public override string Name
        {
            get
            {
                return ReflectionUtilities.GetValueOfFieldOrProperty("Name", Object) as string;
            }
        }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public override object Value
        {
            get
            {
                return Object;
            }
            set
            {
                Object = value;
            }
        }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public override Type DataType { get { return Value?.GetType(); } }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public override string Description
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the text to use as a label for the property.
        /// </summary>
        public override string Caption
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public override string Units
        {
            get
            {
                return null;
            }

            set
            {

            }
        }

        /// <summary>
        /// Gets the units of the property as formmatted for display (in parentheses) or null if not found.
        /// </summary>
        public override string UnitsLabel
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the associated display type for the related property.
        /// </summary>
        public override DisplayAttribute Display
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets or sets the value of the specified property with arrays converted to comma separated strings.
        /// </summary>
        public override object ValueWithArrayHandling
        {
            get
            {
                return Value;
            }
        }

        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public override bool Writable { get { return true; } }

        /// <summary>
        /// Return an attribute
        /// </summary>
        /// <param name="attributeType">Type of attribute to find</param>
        /// <returns>The attribute or null if not found</returns>
        public override Attribute GetAttribute(Type attributeType) { return null; }

        /// <summary>Return the summary comments from the source code.</summary>
        public override string Summary { get { return null; } }

        /// <summary>Return the remarks comments from the source code.</summary>
        public override string Remarks { get { return null; } }
    }

    /// <summary>
    /// This class encapsulates a single field of a model. Has properties for getting the value
    /// of the property, the value in the base model and the default value as definned in the 
    /// source code.
    /// </summary>
    [Serializable]
    public class VariableField : IVariable
    {
        private FieldInfo FieldInfo;

        /// <summary>
        /// Constructor
        /// </summary>
        public VariableField(object model, FieldInfo fieldInfo)
        {
            if (model == null || fieldInfo == null)
                throw new Exception("Cannot create an instance of class VariableField with a null model or fieldInfo");
            Object = model;
            FieldInfo = fieldInfo;
        }

        /// <summary>
        /// Gets or sets the object this variable is relative to
        /// </summary>
        public override object Object { get; set; }

        /// <summary>
        /// Return the name of the property.
        /// </summary>
        public override string Name 
        { 
            get 
            {
                if (FieldInfo.Name.Contains("BackingField"))
                {
                    string st = FieldInfo.Name;
                    return "[" + StringUtilities.SplitOffBracketedValue(ref st, '<', '>') + "]";
                }
                return FieldInfo.Name; 
            } 
        }

        /// <summary>
        /// Returns the value of the property.
        /// </summary>
        public override object Value 
        { 
            get 
            { 
                return FieldInfo.GetValue(Object); 
            }
            set
            {
                FieldInfo.SetValue(Object, value);
            }
        }

        /// <summary>
        /// Gets or sets the value of the specified property with arrays converted to comma separated strings.
        /// </summary>
        public override object ValueWithArrayHandling
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

                        Array arr2d = arr.GetValue(j) as Array;
                        if (arr2d == null)
                            stringValue += VariableProperty.AsString(arr.GetValue(j));
                        else
                        {
                            for (int k = 0; k < arr2d.Length; k++)
                            {
                                if (k > 0)
                                {
                                    stringValue += " \r\n ";
                                }
                                stringValue += VariableProperty.AsString(arr2d.GetValue(k));
                            }
                        }
                    }

                    value = stringValue;
                }

                return value;
            }
        }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public override Type DataType
        {
            get
            {
                return FieldInfo.FieldType;
            }
        }

        /// <summary>
        /// Returns a description of the property or null if not found.
        /// </summary>
        public override string Description
        {
            get
            {
                DescriptionAttribute descriptionAttribute = ReflectionUtilities.GetAttribute(FieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descriptionAttribute != null && descriptionAttribute.ToString() != "")
                    return descriptionAttribute.ToString();
                return null;
            }
        }

        /// <summary>
        /// Gets the text to use as a label for the property.
        /// </summary>
        public override string Caption
        {
            get
            {
                CaptionAttribute labelAttribute = ReflectionUtilities.GetAttribute(FieldInfo, typeof(CaptionAttribute), false) as CaptionAttribute;
                if (labelAttribute == null)
                {
                    return Description;
                }
                else
                {
                    return labelAttribute.ToString();
                }
            }
        }

        /// <summary>
        /// Returns the units of the property (in brackets) or null if not found.
        /// </summary>
        public override string Units
        {
            get
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(FieldInfo, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return unitsAttribute.ToString();
                return null;
            }

            set
            {
            }
        }

        /// <summary>
        /// Gets the units of the property as formmatted for display (in parentheses) or null if not found.
        /// </summary>
        public override string UnitsLabel
        {
            get
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(FieldInfo, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return "(" + unitsAttribute.ToString() + ")";
                return null;
            }
        }

        /// <summary>
        /// Gets the associated display type for the related property.
        /// </summary>
        public override DisplayAttribute Display
        {
            get
            {
                return ReflectionUtilities.GetAttribute(FieldInfo, typeof(DisplayAttribute), false) as DisplayAttribute;
            }
        }

        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public override bool Writable { get { return true; } }

        /// <summary>
        /// Return an attribute
        /// </summary>
        /// <param name="attributeType">Type of attribute to find</param>
        /// <returns>The attribute or null if not found</returns>
        public override Attribute GetAttribute(Type attributeType)
        {
            return ReflectionUtilities.GetAttribute(FieldInfo, attributeType, false);
        }

        /// <summary>Return the summary comments from the source code.</summary>
        public override string Summary { get { return null; } }

        /// <summary>Return the remarks comments from the source code.</summary>
        public override string Remarks { get { return null; } }
    }
}
