using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// An individual labour specification item with monthly values
    /// </summary>
    public interface ILabourSpecificationItem : IModel
    {
        /// <summary>
        /// Provide availability for the specified month 
        /// </summary>
        double GetAvailability(int month);
    }
}
