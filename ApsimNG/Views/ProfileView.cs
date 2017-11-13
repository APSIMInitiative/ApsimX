using System;
using Gtk;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    interface IProfileView
    {
        /// <summary>
        /// Allow direct access to the property grid.
        /// </summary>
        IGridView PropertyGrid { get; }

        /// <summary>
        /// Allow direct access to the profile grid.
        /// </summary>
        IGridView ProfileGrid { get; }

        /// <summary>
        /// Allow direct access to the graph.
        /// </summary>
        IGraphView Graph { get; }

        /// <summary>
        /// Show the property grid if Show = true;
        /// </summary>
        void ShowPropertyGrid(bool show);

        /// <summary>
        /// Show the graph if Show = true;
        /// </summary>
        void ShowGraph(bool show);

        /// <summary>
        /// Show or hide the entire view
        /// </summary>
        void ShowView(bool show);
    }

    public class ProfileView : ViewBase, IProfileView
    {
        private GridView ProfileGrid;
        private GridView PropertyGrid;
        private GraphView Graph;
        private VPaned vpaned1 = null;
        private VPaned vpaned2 = null;
        private VBox vbox1 = null;

        public ProfileView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ProfileView.glade");
            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            vpaned2 = (VPaned)builder.GetObject("vpaned2");
            vbox1 = (VBox)builder.GetObject("vbox1");
            _mainWidget = vpaned1;
            PropertyGrid = new GridView(this);
            vbox1.PackStart(PropertyGrid.MainWidget, true, true, 0);
            //vpaned1.Pack1(PropertyGrid.MainWidget, true, true);
            ProfileGrid = new GridView(this);
            ProfileGrid.NumericFormat = "N3";
            vpaned2.Pack1(ProfileGrid.MainWidget, true, true);
            Graph = new GraphView(this);
            vpaned2.Pack2(Graph.MainWidget, true, false);
            Graph.MainWidget.Realized += GraphWidget_Realized;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            ProfileGrid.MainWidget.Destroy();
            ProfileGrid = null;
            PropertyGrid.MainWidget.Destroy();
            PropertyGrid = null;
            Graph.MainWidget.Destroy();
            Graph = null;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>
        /// A bit of a hack to ensure that the splitter between grid and chart
        /// starts off at the middle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphWidget_Realized(object sender, EventArgs e)
        {
            vpaned2.PositionSet = true;
            vpaned2.Position = vpaned1.Parent.Allocation.Height / 2;
        }

        /// <summary>
        /// Allow direct access to the property grid.
        /// </summary>
        IGridView IProfileView.PropertyGrid
        {
            get { return PropertyGrid; }
        }

        /// <summary>
        /// Allow direct access to the profile grid.
        /// </summary>
        IGridView IProfileView.ProfileGrid
        {
            get { return ProfileGrid; }
        }

        /// <summary>
        /// Allow direct access to the graph.
        /// </summary>
        IGraphView IProfileView.Graph
        {
            get { return Graph; }
        }

        /// <summary>
        /// Show the property grid if Show = true;
        /// </summary>
        public void ShowPropertyGrid(bool Show)
        {
            vbox1.Visible = Show;
        }

        /// <summary>
        /// Show the graph if Show = true;
        /// </summary>
        public void ShowGraph(bool Show)
        {
            Graph.MainWidget.Visible = Show;
        }

        /// <summary>
        /// Show or hide the entire view
        /// </summary>
        public void ShowView(bool show)
        {
            MainWidget.Visible = show;
        }

    }
}
