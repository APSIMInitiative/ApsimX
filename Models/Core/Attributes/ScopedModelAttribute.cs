namespace Models.Core
{
    using System;

    /// <summary>
    /// When applied to a class, denotes an instance of the class and all
    /// child instances make up a scoped unit. e.g. events published in
    /// a child model will propagate to all models within the scoped unit
    /// before going up to parent models.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScopedModelAttribute : Attribute
    {
    }
}
