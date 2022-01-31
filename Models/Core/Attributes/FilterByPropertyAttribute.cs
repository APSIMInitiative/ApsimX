using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM
{
    /// <summary>
    /// Identifies whether a Property or Method of an IFilterable model can be used for filtering
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class FilterByPropertyAttribute : System.Attribute
    {

    }
}
