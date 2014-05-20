using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Models.Core
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public interface IPostSimulationTool
    {
        void Run(DataStore store);
    }
}
