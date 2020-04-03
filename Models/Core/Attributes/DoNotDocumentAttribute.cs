namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related field/property/link should not be documented.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DoNotDocumentAttribute : System.Attribute
    {
    } 
}
