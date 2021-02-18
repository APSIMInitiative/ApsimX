namespace Models.Core
{
    using System;

    /// <summary>
    /// When applied to a field, the infrastructure will locate an object in scope of the 
    /// related field and store a reference to it in the field. If no matching
    /// model is found (and IsOptional is not specified or is false), then an 
    /// exception will be thrown. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SummaryAttribute : Attribute
    {
    }
}
