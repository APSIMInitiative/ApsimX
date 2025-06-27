using System.Reflection;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// This class encapsulates a single field of a model. Has properties for getting the value
/// of the property, the value in the base model and the default value as definned in the
/// source code.
/// </summary>
[Serializable]
internal class VariableField : IVariable
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
    public object Object { get; set; }

    /// <summary>
    /// Return the name of the property.
    /// </summary>
    public string Name
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
    public object Value
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
    /// Gets the data type of the property
    /// </summary>
    public Type DataType
    {
        get
        {
            return FieldInfo.FieldType;
        }
    }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable { get { return true; } }
}