using System.Reflection;
using System.Text;

namespace APSIM.Core;

/// <summary>
/// This class encapsulates a list of IVariables that are evaluated when
/// the Value property is called.
/// source code.
/// </summary>
[Serializable]
public class VariableComposite
{
    /// <summary>
    /// The list of variables to be evaluated
    /// </summary>
    private List<IVariable> Variables = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableComposite" /> class.
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="variables">The list of variables to be evaluated</param>
    public VariableComposite(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the object this variable is relative to
    /// </summary>
    public object Object { get; set; }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    public object Value
    {
        get
        {
            object relativeTo = null;
            foreach (IVariable variable in this.Variables)
            {
                if (relativeTo != null)
                {
                    variable.Object = relativeTo;
                }

                relativeTo = variable.Value;
                if (relativeTo == null)
                {
                    return null;
                }
            }

            return relativeTo;
        }

        set
        {
            object relativeTo = null;
            int i;
            for (i = 0; i < this.Variables.Count - 1; i++)
            {
                if (relativeTo != null)
                {
                    Variables[i].Object = relativeTo;
                }

                relativeTo = Variables[i].Value;
                if (relativeTo == null)
                {
                    return;
                }
            }

            Variables[i].Object = relativeTo;
            Variables[i].Value = value;
        }
    }
    /*
        /// <summary>
        /// Gets the units of the property or null if not found.
        /// </summary>
        public VariableProperty Property => Variables.Last() as VariableProperty;
    */

    /// <summary>
    /// Gets the data type of the property
    /// </summary>
    public Type DataType => Variables.Last().DataType;

    /// <summary>
    /// Gets the data type of the property
    /// </summary>
    public INodeModel FirstModel => Variables.FirstOrDefault(v => v is VariableObject obj && obj.Value is INodeModel)?.Value as INodeModel;

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable { get { return Variables.Last().Writable; } }

    public bool Any() => Variables.Any();

    public void Clear() => Variables.Clear();

    public PropertyInfo Property => Variables.Last() is VariableProperty p ? p.PropertyInfo : null;

    public void AddInstance(object instance)
    {
        Variables.Add(new VariableObject(instance));
    }

    public void AddProperty(object instance, PropertyInfo property, string arraySpecifier = null)
    {
        Variables.Add(new VariableProperty(instance, property, arraySpecifier));
    }

    public void AddProperty(object instance, string elementName)
    {
        Variables.Add(new VariableProperty(instance, elementName));
    }

    public void AddMethod(object instance, MethodInfo method, object[] arguments = null)
    {
        Variables.Add(new VariableMethod(instance, method, arguments));
    }

    public void AddExpression(object instance, string path)
    {
        Variables.Add(new VariableExpression(path, instance));
    }

    /// <summary>Calculate a full path for an IVariable.</summary>
    /// <param name="variableOrModel">The variable or model instance.</param>
    /// <returns>Full path or throws if cannot calculate path.</returns>
    public string FullPath()
    {
        StringBuilder st = new();
        foreach (var v in Variables)
        {
            if (st.Length > 0)
                st.Append('.');

            if (v is VariableObject && v.Object is INodeModel model)
                st.Append(model.FullPath);
            else if (v is VariableProperty property)
                st.Append(property.FullName);
            else
                st.Append(v.Name);
        }

        return st.ToString();
    }

}
