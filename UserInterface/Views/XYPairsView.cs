// -----------------------------------------------------------------------
// <copyright file="InitialWaterView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;
    using Interfaces;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class XYPairsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitialWaterView" /> class.
        /// </summary>
        public XYPairsView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the initial water graph.
        /// </summary>
        public Views.GraphView Graph
        {
            get
            {
                return this.graphView;
            }
        }
        /// <summary>
        /// Gets the initial water graph.
        /// </summary>
        public Views.GridView VariablesGrid
        {
            get
            {
                return this.gridView;
            }
        }

    }
}
