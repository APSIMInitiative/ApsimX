using APSIM.Core;
using System;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{
    /// <summary>
    /// This is a post-simulation tool which will run all child post-simulation
    /// tools serially.
    /// </summary>
    [ValidParent(typeof(IDataStore))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(typeof(SerialPostSimulationTool))]
    public class SerialPostSimulationTool : Model, IPostSimulationTool, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }



        /// <summary>
        /// Run the post-simulation tool.
        /// </summary>
        public void Run()
        {
            IDataStore storage = Structure.Find<IDataStore>();
            Links links = new Links(new object[1] { storage });
            foreach (IPostSimulationTool tool in FindAllChildren<IPostSimulationTool>())
            {
                links.Resolve(tool);
                tool.Run();
                storage.Reader.Refresh();
            }
        }
    }
}
