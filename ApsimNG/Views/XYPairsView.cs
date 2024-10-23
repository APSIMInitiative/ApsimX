using Gtk;
using System;
using UserInterface.Presenters;
using Utility;

namespace UserInterface.Views
{
    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class XYPairsView : ViewBase
    {
        private Paned hpaned;

        private ContainerView containerView;

        /// <summary>
        /// Initial water graph
        /// </summary>
        private GraphView graphView;

        /// <summary>
        /// Constructor
        /// </summary>
        public XYPairsView(ViewBase owner) : base(owner)
        {
            hpaned = new Paned(Orientation.Horizontal);
            hpaned.Position = GtkUtilities.GetBorderOfRightHandView(owner).Width / 6;
            mainWidget = hpaned;
            containerView = new ContainerView(this);
            graphView = new GraphView(this);
            hpaned.Pack1(containerView.MainWidget, true, false);
            hpaned.Pack2(graphView.MainWidget, true, false);
            graphView.Height = 200;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
        public ContainerView VariablesGrid
        {
            get
            {
                return containerView;
            }
        }
    }
}
