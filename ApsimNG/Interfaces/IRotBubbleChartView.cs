// -----------------------------------------------------------------------
// <copyright file="IRotBubbleChartView.cs" company="UQ">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// This interface defines the API for talking to an initial water view.
    /// </summary>
    public interface IRotBubbleChartView
    {
        /// <summary>
        /// Invoked when the user changes the initial state list box.
        /// </summary>
        event EventHandler OnInitialStateChanged;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.AddNodeEventArgs> AddNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.DupNodeEventArgs> DupNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.DelNodeEventArgs> DelNode;
        
        /// <summary>
        /// Gets the directed graph.
        /// </summary>
        Views.DirectedGraphView Graph { get; }
    }
}