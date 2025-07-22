using System.Collections;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// Encapsulates a discovered property of a model. Provides properties for
/// returning information about the property.
/// </summary>
internal class VariableProperty : IVariable, IDataProvider
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
        this.property = property ?? throw new Exception("Cannot create an instance of class VariableProperty with a null model or propertyInfo");
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
    public object Object { get; set; }

    /// <summary>
    /// Return the name of the property.
    /// </summary>
    public string Name
    {
        get
        {
            return this.property.Name;
        }
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
    /// Gets the data type of the property
    /// </summary>
    public Type DataType { get; }

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
    public object Value
    {
        get => DataAccessor.Get(this, arrayFilter);
        set => DataAccessor.Set(this, value, arrayFilter);
    }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable { get { return property.CanRead && property.CanWrite && property.GetSetMethod() != null; } }

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