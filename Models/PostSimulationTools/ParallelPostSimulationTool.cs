using System.Threading.Tasks;
using APSIM.Core;
using DocumentFormat.OpenXml.Office.CustomXsn;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

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
        private IScope scope;

        /// <summary>Scope supplied by APSIM.core.</summary>
        public void SetScope(IScope scope) => this.scope = scope;

        /// <summary>
        /// Run the post-simulation tool.
        /// </summary>
        public void Run()
        {
            Parallel.ForEach(FindAllChildren<IPostSimulationTool>(), tool =>
            {
                new Links(new object[1] { scope.Find<IDataStore>() }).Resolve(tool);
                tool.Run();
            });
        }
    }
}
