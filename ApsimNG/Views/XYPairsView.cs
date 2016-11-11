// -----------------------------------------------------------------------
// <copyright file="InitialWaterView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Gtk;
    using System;
    //using System.Windows.Forms;
    using Interfaces;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class XYPairsView : ViewBase
    {
        private VPaned vpaned;

        private GridView gridView;

        /// <summary>
        /// Initial water graph
        /// </summary>
        private GraphView graphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialWaterView" /> class.
        /// </summary>
        public XYPairsView(ViewBase owner) : base(owner)
        {
            vpaned = new VPaned();
            _mainWidget = vpaned;
            gridView = new GridView(this);
            graphView = new GraphView(this);
            vpaned.Pack1(gridView.MainWidget, true, false);
            vpaned.Pack2(graphView.MainWidget, true, false);
        }

        /// <summary>
        /// Gets the initial water graph.
        /// </summary>
        public Views.GraphView Graph
        {
            get
            {
                return graphView;
            }
        }
        /// <summary>
        /// Gets the initial water graph.
        /// </summary>
        public Views.GridView VariablesGrid
        {
            get
            {
                return gridView;
            }
        }

    }
}
