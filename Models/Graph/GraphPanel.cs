using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Graph
{
    /// <summary>
    /// Represents a panel of graphs which has more flexibility than the
    /// page of graphs shown by a folder.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GraphPanelView")]
    [PresenterName("UserInterface.Presenters.GraphPanelPresenter")]
    [ValidParent(ParentType = typeof(ISimulationDescriptionGenerator))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Zone))]
    public class GraphPanel : Model, IPostSimulationTool
    {
        /// <summary>
        /// Called when the model is deserialised.
        /// </summary>
        public override void OnCreated()
        {
            if (Apsim.Child(this, typeof(Manager)) == null)
            {
                Manager script = new Manager();
                script.Name = "Config";
                script.Code = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.GraphPanelScriptTemplate.cs");
                Children.Insert(0, script);
            }

            base.OnCreated();
        }

        /// <summary>
        /// Clears the cache after simulations are run.
        /// </summary>
        public void Run()
        {
            Cache.Clear();
        }

        /// <summary>
        /// Use same axes scales for all graphs?
        /// </summary>
        [Description("Use same axes scales in all tabs?")]
        public bool SameAxes { get; set; }

        /// <summary>
        /// Number of columns in page of graphs.
        /// </summary>
        [Description("Number of columns of graphs per tab")]
        public int NumCols { get; set; } = 2;

        /// <summary>
        /// Script which controls tab generation.
        /// </summary>
        public IGraphPanelScript Script
        {
            get
            {
                Manager manager = Apsim.Child(this, typeof(Manager)) as Manager;
                return manager?.Children?.FirstOrDefault() as IGraphPanelScript;
            }
        }

        /// <summary>
        /// Cached graph data.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, Dictionary<int, List<SeriesDefinition>>> Cache { get; set; } = new Dictionary<string, Dictionary<int, List<SeriesDefinition>>>();
    }
}
