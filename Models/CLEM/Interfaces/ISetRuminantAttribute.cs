using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for all attribute models
    /// </summary>
    public interface ISetAttribute
    {
        /// <summary>
        /// Property to return a random assignment of the attribute
        /// </summary>
        Resources.CLEMAttribute GetRandomSetAttribute();

        /// <summary>
        /// Name to apply to the attribute
        /// </summary>
        string AttributeName { get; set; }

    }
}
