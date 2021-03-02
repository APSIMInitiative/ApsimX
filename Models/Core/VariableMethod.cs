namespace Models.Core
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Encapsulates a discovered method of a model. 
    /// </summary>
    [Serializable]
    public class VariableMethod : IVariable
    {
        /// <summary>
        /// Gets or sets the PropertyInfo for this property.
        /// </summary>
        private MethodInfo method;

        /// <summary>
        /// A lit of arguments to pass to the method
        /// </summary>
        private object[] arguments = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableMethod" /> class.
        /// </summary>
        /// <param name="model">The underlying model for the property</param>
        /// <param name="method">The PropertyInfo for this property</param>
        public VariableMethod(object model, MethodInfo method)
        {
            if (model == null || method == null)
            {
                throw new ApsimXException(null, "Cannot create an instance of class VariableMethod with a null model or methodInfo");
            }

            this.Object = model;
            this.method = method;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableMethod" /> class.
        /// </summary>
        /// <param name="model">The underlying model for the property</param>
        /// <param name="method">The PropertyInfo for this property</param>
        /// <param name="arguments">An array of arguments to pass to the method</param>
        public VariableMethod(object model, MethodInfo method, object[] arguments)
        {
            if (model == null || method == null)
            {
                throw new ApsimXException(null, "Cannot create an instance of class VariableMethod with a null model or methodInfo");
            }

            this.Object = model;
            this.method = method;
            this.arguments = arguments;
        }


        /// <summary>
        /// Gets or sets the underlying model that this property belongs to.
        /// </summary>
        public override object Object { get; set; }

        /// <summary>
        /// Return the name of the method.
        /// </summary>
        public override string Name 
        { 
            get 
            {
                return this.method.Name; 
            } 
        }

        /// <summary>
        /// Gets the description of the method
        /// </summary>
        public override string Description { get { return string.Empty; } }

        /// <summary>
        /// Gets the text to use as a label for the property.
        /// </summary>
        public override string Caption { get { return string.Empty; } }
        /// <summary>
        /// Gets the units of the method
        /// </summary>
        public override string Units { get { return string.Empty; } set { } }

        /// <summary>
        /// Gets the units of the property as formmatted for display (in parentheses) or null if not found.
        /// </summary>
        public override string UnitsLabel { get { return string.Empty; } }
        
        /// <summary>
        /// Gets a list of allowable units
        /// </summary>
        public string[] AllowableUnits { get { return null; } }

        /// <summary>
        /// Gets a value indicating whether the method is readonly.
        /// </summary>
        public bool IsReadOnly { get { return true; } }

        /// <summary>
        /// Gets the metadata for each layer. Returns new string[0] if none available.
        /// </summary>
        public string[] Metadata { get { return null; } }

        /// <summary>
        /// Gets the data type of the method
        /// </summary>
        public override Type DataType { get { return this.method.ReturnType; } }

        /// <summary>
        /// Gets the values of the method
        /// </summary>
        public override object Value
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
        /// Gets the associated display type for the related method.
        /// </summary>
        public override DisplayAttribute Display { get { return null; } }

        /// <summary>
        /// Gets or sets the value of the specified property with arrays converted to comma separated strings.
        /// </summary>
        public override object ValueWithArrayHandling
        {
            get
            {
                return Object;
            }
        }

        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public override bool Writable { get { return false; } }

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
}
