// -----------------------------------------------------------------------
// <copyright file="IRotBubbleChartView.cs" company="UQ">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;
    using Views;
    using Models.Management;
    using System.Collections.Generic;
    using EventArguments.DirectedGraph;

    /// <summary>
    /// This interface defines the API for talking to an bubble chart view.
    /// </summary>
    public interface IBubbleChartView
    {
        /// <summary>
        /// 
        /// </summary>
        event EventHandler<GraphChangedEventArgs> OnGraphChanged;

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
        event EventHandler<AddArcEventArgs> AddArc;

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
        /// Set the graph in the view.
        /// </summary>
        /// <param name="nodes">Nodes of the graph.</param>
        /// <param name="arcs">Arcs of the graph.</param>
        void SetGraph(List<StateNode> nodes, List<RuleAction> arcs);
    }
} 