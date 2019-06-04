namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Graph;
    using Models.Core.Interfaces;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;

    /// <summary>
    /// The rotation manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.RotBubbleChartView")]
    [PresenterName("UserInterface.Presenters.RotBubbleChartPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class RotBubbleChart : Model
    {
        /// <summary>Rules and actions required for a transition</summary>
        [Serializable]
        public class RuleActionArc : Arc
        {
            /// <summary>Test conditions that need to be satisfied for this transition</summary>
            public List<string> testCondition { get; set; }
            /// <summary>Actions undertaken when making this transition</summary>
            public List<string> action { get; set; }

        }
        /// <summary>A state in the DG</summary>
        [Serializable]
        public class StateNode : Node
        {
        }

        [Description("Node list")]
        private List<StateNode> Nodes { get; set; }
        [Description("Arc list")]
        private List<RuleActionArc> Arcs { get; set; }

        /// <summary>The description (nodes &amp; arcs) of the directed graph.</summary>
        [NonSerialized]
        private DirectedGraph stateGraph = null;

        /// <summary>Get directed graph from model</summary>
        public DirectedGraph DirectedGraphInfo
        {
            get
            {
                if (stateGraph == null) { buildGraph(); }
                return stateGraph;
            }
            set
            {
                stateGraph = value;
            }
        }


        /// <summary>At simulation commencing time, rebuild the script assembly if required.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            buildGraph();
            RebuildScriptModel();
        }

        /// <summary>Rebuild the script model and return error message if script cannot be compiled.</summary>
        public void RebuildScriptModel()
        {
        }

        /// <summary>Add a node to the graph.</summary>
        public void AddNode(object sender, string Name, Color Fill, Color Outline)
        {
            stateGraph.AddNode(Name, Fill, Outline);
        }

        /// <summary>Add a node to the graph.</summary>
        public void DupNode(object sender, string nodeName)
        {
            stateGraph.DupNode(nodeName);
        }

        /// <summary>Add a node to the graph.</summary>
        public void DelNode(object sender, string nodeName)
        {
            stateGraph.DelNode(nodeName);
        }


        private void buildGraph()
        {
            stateGraph = new DirectedGraph();
            Nodes?.ForEach(n => stateGraph.AddNode(n.Name, n.Colour, n.OutlineColour));
            Arcs?.ForEach(a => stateGraph.AddArc(a.Text, a.SourceName, a.DestinationName, a.Colour));
        }


    }
}
