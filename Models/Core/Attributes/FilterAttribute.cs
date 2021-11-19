using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Core
{
    /// <summary>
    /// Identifies whether a Property or Method of an IFilterable model can be used for filtering
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class FilterAttribute : System.Attribute
    {

    }
}
