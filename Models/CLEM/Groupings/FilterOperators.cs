using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Groupings
{
    /// <summary>
    /// Filter operators
    /// </summary>
    public enum FilterOperators
    {
        /// <summary>
        /// Equals
        /// </summary>
        Equal,
        /// <summary>
        /// Not equal to
        /// </summary>
        NotEqual,
        /// <summary>
        /// Less than
        /// </summary>
        LessThan,
        /// <summary>
        /// Less than or equal to
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// Greater than
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Greater than or equal to
        /// </summary>
        GreaterThanOrEqual
    }

    /// <summary>
    /// Extension methods for RuminantFilterOperators enum
    /// </summary>
    public static class FilterOperatorsExtensions
    {
        /// <summary>
        /// Method to return the symbol value of the enum
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static string ToSymbol(this FilterOperators me)
        {
            switch (me)
            {
                case FilterOperators.Equal:
                    return "=";
                case FilterOperators.NotEqual:
                    return "<>";
                case FilterOperators.LessThan:
                    return "<";
                case FilterOperators.LessThanOrEqual:
                    return "<=";
                case FilterOperators.GreaterThan:
                    return ">";
                case FilterOperators.GreaterThanOrEqual:
                    return ">=";
                default:
                    return "?";
            }
        }
    }
}
