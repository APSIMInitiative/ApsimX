using System;

namespace Models.Core
{

    /// <summary>
    /// Specifies that the associated property is a solute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SoluteAttribute : System.Attribute
    {
    }
}
