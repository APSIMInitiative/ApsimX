using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils
{
    /// <summary>
    /// A structure for holding runoff event information
    /// </summary>
    [Serializable]
    public class RunoffEventType
    {
        /// <summary>The runoff</summary>
        public double runoff;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Runoff">The runoff.</param>
    public delegate void RunoffEventDelegate(RunoffEventType Runoff);
}
