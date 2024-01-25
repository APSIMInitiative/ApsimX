using System;

namespace Models.Core
{

    /// <summary>
    /// Specifies that the related field/property/link should not be documented.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DoNotDocumentAttribute : System.Attribute
    {
    }
}
