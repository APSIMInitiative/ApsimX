using System.Threading.Tasks;
using APSIM.Core;
using Models.Core;
using Models.Core.Run;
using Models.Storage;
using System;

namespace Models.PostSimulationTools
{
    /// <summary>
    /// This is a post-simulation tool which will run all child post-simulation
    /// tools in parallel.
    /// </summary>
    [ValidParent(typeof(IDataStore))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(typeof(SerialPostSimulationTool))]
    public class ParallelPostSimulationTool : Model, IPostSimulationTool, IScopeDependency
    {
        /// <summary>Scope supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IScope Scope { private get; set; }

        /// <summary>
        /// Run the post-simulation tool.
        /// </summary>
        public void Run()
        {
            Parallel.ForEach(FindAllChildren<IPostSimulationTool>(), tool =>
            {
                new Links(new object[1] { Scope.Find<IDataStore>() }).Resolve(tool);
                tool.Run();
            });
        }
    }
}
