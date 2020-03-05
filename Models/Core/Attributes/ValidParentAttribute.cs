namespace Models.Core
{
    using System;

    /// <summary>Specifies the models that this class can sit under in the user interface./// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class ValidParentAttribute : System.Attribute
    {
        /// <summary>Allowable parent type.</summary>
        public Type ParentType { get;  set; }

        /// <summary>Allow the model to be dropped anywhere?</summary>
        public bool DropAnywhere { get; set; }
    }
}
