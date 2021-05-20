using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// 
        /// </summary>
        FilterOperators Operator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string ParameterName { get; }

        /// <summary>
        /// 
        /// </summary>
        string Value { get; set; }
    }
}
