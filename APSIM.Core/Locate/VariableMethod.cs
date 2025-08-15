using System.Reflection;

namespace APSIM.Core;

/// <summary>
/// Encapsulates a discovered method of a model.
/// </summary>
internal class VariableMethod : IVariable
{
    /// <summary>
    /// Gets or sets the PropertyInfo for this property.
    /// </summary>
    private MethodInfo method;

    /// <summary>
    /// A list of arguments to pass to the method
    /// </summary>
    private object[] arguments = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableMethod" /> class.
    /// </summary>
    /// <param name="model">The underlying model for the property</param>
    /// <param name="method">The PropertyInfo for this property</param>
    /// <param name="arguments">An array of arguments to pass to the method</param>
    public VariableMethod(object model, MethodInfo method, object[] arguments)
    {
        if (model == null || method == null)
            throw new Exception("Cannot create an instance of class VariableMethod with a null model or methodInfo");

        this.Object = model;
        this.method = method;
        this.arguments = arguments;
    }


    /// <summary>
    /// Gets or sets the underlying model that this property belongs to.
    /// </summary>
    public object Object { get; set; }

    /// <summary>
    /// Return the name of the method.
    /// </summary>
    public string Name => method.Name;

    /// <summary>
    /// Gets a list of allowable units
    /// </summary>
    public string[] AllowableUnits => null;

    /// <summary>
    /// Gets a value indicating whether the method is readonly.
    /// </summary>
    public bool IsReadOnly => true;

    /// <summary>
    /// Gets the metadata for each layer. Returns new string[0] if none available.
    /// </summary>
    public string[] Metadata => null;

    /// <summary>
    /// Gets the data type of the method
    /// </summary>
    public Type DataType => method.ReturnType;

    /// <summary>
    /// Gets the values of the method
    /// </summary>
    public object Value
    {
        get
        {
            if (arguments != null)
            {
                return method.Invoke(Object, arguments);
            }
            else
            {
                return method.Invoke(Object, new object[] { -1 });
            }
        }

        set
        {
        }
    }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable => false;
}
