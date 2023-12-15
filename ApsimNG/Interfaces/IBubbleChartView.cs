using System;
using Models.Management;
using System.Collections.Generic;
using ApsimNG.EventArguments.DirectedGraph;
using APSIM.Shared.Graphing;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// This interface defines the API for talking to an bubble chart view.
    /// </summary>
    public interface IBubbleChartView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler<GraphObjectsArgs> GraphObjectSelected;

        /// <summary>Invoked when the user changes a property</summary>
        public event EventHandler<GraphChangedEventArgs> GraphChanged;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<AddNodeEventArgs> AddNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<GraphObjectsArgs> DelNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<AddArcEventArgs> AddArcEnd;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<GraphObjectsArgs> DelArc;

        /// <summary>
        /// 
        /// </summary>
        List<Node> Nodes { get; }

        /// <summary>
        /// 
        /// </summary>
        List<Arc> Arcs { get; }

        /// <summary>
        /// Properties editor.
        /// </summary>
        IPropertyView PropertiesView { get; }

        /// <summary>
        /// Node Properties editor.
        /// </summary>
        public IPropertyView ObjectPropertiesView { get; }

        /// <summary>
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        void SetGraph(List<Node> nodes, List<Arc> arcs);

        /// <summary>
        /// A graph object has been selected. Make the (middle part of) UI relevant to it
        /// </summary>
        /// <param name="objectID">ID of the object to be selected.</param>
        void Select(int objectID);
    }
} 