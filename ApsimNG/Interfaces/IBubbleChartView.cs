using System;
using Models.Management;
using System.Collections.Generic;
using UserInterface.EventArguments.DirectedGraph;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// This interface defines the API for talking to an bubble chart view.
    /// </summary>
    public interface IBubbleChartView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler<EventArguments.GraphObjectSelectedArgs> GraphObjectSelected;

        /// <summary>Invoked when the user changes a property</summary>
        public event EventHandler<GraphChangedEventArgs> GraphChanged;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<AddNodeEventArgs> AddNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<DelNodeEventArgs> DelNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<AddArcEventArgs> AddArcEnd;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<DelArcEventArgs> DelArc;
        /// <summary>
        /// Editor for inputting rules.
        /// </summary>
        IEditorView RuleList { get; }

        /// <summary>
        /// Editor for inputting actions.
        /// </summary>
        /// <value></value>        
        IEditorView ActionList { get; }

        /// <summary>
        /// 
        /// </summary>
        List<StateNode> Nodes { get; }

        /// <summary>
        /// 
        /// </summary>
        List<RuleAction> Arcs { get; }

        /// <summary>
        /// Properties editor.
        /// </summary>
        IPropertyView PropertiesView { get; }

        /// <summary>
        /// Node Properties editor.
        /// </summary>
        public IPropertyView NodePropertiesView { get; }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        void SetGraph(List<StateNode> nodes, List<RuleAction> arcs);

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectID">ID of the object to be selected.</param>
        void Select(int objectID);
    }
} 