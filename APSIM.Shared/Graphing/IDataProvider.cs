using System;
using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// An interface for a class which can retrieve data.
    /// </summary>
    public interface IDataProvider<T>
    {
        /// <summary>
        /// Get all data.
        /// </summary>
        IEnumerable<T> GetData();
    }
}
