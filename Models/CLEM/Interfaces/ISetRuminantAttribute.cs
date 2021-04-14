using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    public interface ISetRuminantAttribute
    {
        /// <summary>
        /// Property to return a random assignment of the attribute
        /// </summary>
        RuminantAttribute GetRandomSetAttribute { get; }

        /// <summary>
        /// Name to apply to the attribute
        /// </summary>
        string AttributeName { get; set; }

    }
}
