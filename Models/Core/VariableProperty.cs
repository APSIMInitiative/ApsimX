using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;
using Models.Soils;
using APSIM.Numerics;
using APSIM.Core;

namespace Models.Core
{

    /// <summary>
    /// Encapsulates a discovered property of a model. Provides properties for
    /// returning information about the property.
    /// </summary>
    [Serializable]
    public class VariableProperty : IVariable, IDataProvider
    {
        /// <summary>
        /// Gets or sets the PropertyInfo for this property.
        /// </summary>
        private PropertyInfo property;

        /// <summary>The name of the property to call on each array element.</summary>
        private string elementPropertyName;

        private DataArrayFilter arrayFilter;

        private string fullName;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableProperty" /> class.
        /// </summary>
        /// <param name="model">The underlying model for the property</param>
        /// <param name="property">The PropertyInfo for this property</param>
        /// <param name="arraySpecifier">An optional array specification e.g. 1:3</param>
        public VariableProperty(object model, PropertyInfo property, string arraySpecifier = null)
        {
            Object = model;
            this.property = property ?? throw new ApsimXException(null, "Cannot create an instance of class VariableProperty with a null model or propertyInfo");
            if (!string.IsNullOrEmpty(arraySpecifier))
                arrayFilter = new DataArrayFilter(arraySpecifier);

            DataType = property.PropertyType;
            fullName = $"{Name}[{arraySpecifier}]";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableProperty" /> class.
        /// </summary>
        /// <param name="model">The underlying model for the property</param>
        /// <param name="elementPropertyName">The name of the property to call on each array element.</param>
        /// <param name="arraySpecifier">An optional array specification e.g. 1:3</param>
        public VariableProperty(object model, string elementPropertyName, string arraySpecifier = null)
        {
            property = DataAccessor.GetElementTypeOfIList(model.GetType())
                                   .GetProperty(elementPropertyName)
                                   ?? throw new Exception($"Cannot get property {elementPropertyName} from class {model.GetType().Name}");

            Object = model;
            this.elementPropertyName = elementPropertyName;
            if (!string.IsNullOrEmpty(arraySpecifier))
                arrayFilter = new DataArrayFilter(arraySpecifier);
            var tempArray = Array.CreateInstance(property.PropertyType, 0);
            DataType = tempArray.GetType();
        }

        /// <summary>Get the PropertyInfo instance.</summary>
        public PropertyInfo PropertyInfo => property;

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
        /// Looks for a description string associated with an enumerated value
        /// Adapted from http://blog.spontaneouspublicity.com/associating-strings-with-enums-in-c
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
            (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].ToString();
            else
                return value.ToString();
        }

        /// <summary>
        /// Simple structure to hold both a name and an associated label
        /// </summary>
        public struct NameLabelPair
        {
            /// <summary>
            /// Name of an object
            /// </summary>
            public string Name;
            /// <summary>
            /// Display label for the object
            /// </summary>
            public string Label;
            /// <summary>
            /// Constructs a NameLabelPair
            /// </summary>
            /// <param name="name">Name of the object</param>
            /// <param name="label">Display label for the object</param>
            public NameLabelPair(string name, string label = null)
            {
                Name = name;
                if (String.IsNullOrEmpty(label))
                    Label = name;
                else
                    Label = label;
            }
        }

        /// <summary>
        /// Gets a list of allowable units
        /// The list contains both the actual name and a display name for each entry
        /// </summary>
        public NameLabelPair[] AllowableUnits
        {
            get
            {
                PropertyInfo unitsInfo = this.Object.GetType().GetProperty(this.property.Name + "Units");
                if (unitsInfo != null && unitsInfo.PropertyType.BaseType == typeof(Enum))
                {
                    Array enumValArray = Enum.GetValues(unitsInfo.PropertyType);
                    List<NameLabelPair> enumValList = new List<NameLabelPair>(enumValArray.Length);

                    foreach (int val in enumValArray)
                    {
                        object parsedEnum = Enum.Parse(unitsInfo.PropertyType, val.ToString());
                        enumValList.Add(new NameLabelPair(parsedEnum.ToString(), GetEnumDescription((Enum)parsedEnum)));
                    }
                    return enumValList.ToArray();
                }
                else
                    return new NameLabelPair[0];
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
        public override Type DataType { get; }

        /// <summary>Data object</summary>
        public object Data
        {
            get
            {
                if (elementPropertyName == null)
                    return property.GetValue(Object);
                else if (Object is IList source)
                {
                    var returnArray = Array.CreateInstance(property.PropertyType, source.Count);
                    for (int i = 0; i < source.Count; i++)
                        returnArray.SetValue(property.GetValue(source[i]), i);
                    return returnArray;
                }
                else
                    return null;
            }
            set
            {
                if (elementPropertyName == null)
                {
                    if (property.CanWrite)
                        property.SetValue(Object, value);
                    else
                        throw new Exception($"{this.property.Name} is read only and cannot be written to.");
                }
                else
                    throw new Exception($"Cannot set value of a property of an array of model instances");
            }
        }

        /// <summary>Type of data object</summary>
        public Type Type => property.PropertyType;

        /// <summary>
        /// Gets the values of the property
        /// </summary>
        public override object Value
        {
            get => DataAccessor.Get(this, arrayFilter);
            set => DataAccessor.Set(this, value, arrayFilter);
        }

        /// <summary>
        /// Special case where trying to get a property of an array(IList). In this case
        /// we want to return the property value for all items in the array.
        /// e.g. Maize.Root.Zones.WaterUptake
        /// Zones is a List of ZoneState objects.
        /// </summary>
        private object ProcessPropertyOfArrayElement()
        {
            IList list = Object as IList;

            // Get the type of the items in the array.
            Type elementType;
            if (list.GetType().HasElementType)
                elementType = list.GetType().GetElementType();
            else
            {
                Type[] genericArguments = list.GetType().GetGenericArguments();
                if (genericArguments.Length > 0)
                    elementType = genericArguments[0];
                else
                    throw new Exception("Unknown type of array in Locater");
            }

            PropertyInfo propertyInfo = elementType.GetProperty(elementPropertyName);
            if (propertyInfo == null)
                throw new Exception(elementPropertyName + " is not a property of type " + elementType.Name);

            // Create a return array.
            Array values = Array.CreateInstance(propertyInfo.PropertyType, list.Count);
            for (int i = 0; i < list.Count; i++)
                values.SetValue(propertyInfo.GetValue(list[i]), i);

            return values;
        }

        /// <summary>
        /// Returns the string representation of a scalar value.
        /// Uses InvariantCulture when converting doubles
        /// to ensure a consistent representation of Nan and Inf
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string AsString(object value)
        {
            if (value == null)
                return string.Empty;
            Type type = value.GetType();
            if (type == typeof(double))
                return ((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            else if (value is Enum)
                return GetEnumDescription(value as Enum);
            else if (value is DateTime)
                return ((DateTime)value).ToShortDateString();
            else
                return value.ToString();
        }

        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public override bool Writable { get { return property.CanRead && property.CanWrite && property.GetSetMethod() != null; } }

        /// <summary>
        /// Gets the display format for this property e.g. 'N3'. Can return null if not present.
        /// </summary>
        public string Format
        {
            get
            {
                DisplayAttribute displayFormatAttribute = ReflectionUtilities.GetAttribute(this.property, typeof(DisplayAttribute), false) as DisplayAttribute;
                if (displayFormatAttribute != null && displayFormatAttribute.Format != null)
                {
                    return displayFormatAttribute.Format;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Return an attribute
        /// </summary>
        /// <param name="attributeType">Type of attribute to find</param>
        /// <returns>The attribute or null if not found</returns>
        public Attribute GetAttribute(Type attributeType)
        {
            return ReflectionUtilities.GetAttribute(this.property, attributeType, false);
        }

        /// <summary>Get the full name of the property.</summary>
        public string FullName => fullName;
    }
}