// -----------------------------------------------------------------------
// <copyright file="IRotBubbleChartView.cs" company="UQ">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// This interface defines the API for talking to an bubble chart view.
    /// </summary>
    public interface IRotBubbleChartView
    {
        /// <summary>
        /// Invoked when the user changes the initial state list box.
        /// </summary>
        event EventHandler<EventArguments.InitialStateEventArgs> OnInitialStateChanged;

        /// <summary> </summary>
        event EventHandler<EventArguments.GraphChangedEventArgs> OnGraphChanged;
        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.AddNodeEventArgs> AddNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.DelNodeEventArgs> DelNode;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.AddArcEventArgs> AddArc;

        /// <summary>
        /// Invoked when the user adds a node to the chart
        /// </summary>
        event EventHandler<EventArguments.DelArcEventArgs> DelArc;

        /// <summary>
        /// Gets / sets the directed graph.
        /// </summary>
        Models.RotBubbleChart.RBGraph Graph { get; set; }
    }
}