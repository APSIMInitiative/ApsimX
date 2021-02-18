namespace Models.Core
{
    using System;
    
    /// <summary>
    /// Specifies that the associated property is a solute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SoluteAttribute : System.Attribute
    {
    }
}
