using System;

namespace Models.Core
{

    /// <summary>Specifies the models that this class can sit under in the user interface./// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class ValidParentAttribute : Attribute
    {
        /// <summary>Allowable parent type.</summary>
        public Type ParentType { get; set; }

        /// <summary>Allow the model to be dropped anywhere?</summary>
        public bool DropAnywhere { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ValidParentAttribute()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type"></param>
        public ValidParentAttribute(Type type)
        {
            ParentType = type;
        }
    }
}
