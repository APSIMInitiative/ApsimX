namespace UserInterface.Views
{
    using System;
    using Extensions;
    using Gtk;
    using Interfaces;

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
        private GridView profileGrid;
        private GridView propertyGrid;
        private GraphView graph;
        private VPaned vpaned1 = null;
        private VPaned vpaned2 = null;
        private VBox vbox1 = null;

        public ProfileView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ProfileView.glade");
            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            vpaned2 = (VPaned)builder.GetObject("vpaned2");
            vbox1 = (VBox)builder.GetObject("vbox1");
            mainWidget = vpaned1;
            propertyGrid = new GridView(this);
            vbox1.PackStart(propertyGrid.MainWidget, true, true, 0);
            //vpaned1.Pack1(PropertyGrid.MainWidget, true, true);
            profileGrid = new GridView(this);
            profileGrid.NumericFormat = "N3";
            vpaned2.Pack1(profileGrid.MainWidget, true, true);
            graph = new GraphView(this);
            vpaned2.Pack2(graph.MainWidget, true, false);
            graph.MainWidget.Realized += GraphWidget_Realized;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            try
            {
                profileGrid.MainWidget.Cleanup();
                profileGrid = null;
                propertyGrid.MainWidget.Cleanup();
                propertyGrid = null;
                graph.MainWidget.Cleanup();
                graph = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// A bit of a hack to ensure that the splitter between grid and chart
        /// starts off at the middle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphWidget_Realized(object sender, EventArgs e)
        {
            try
            {
                vpaned2.PositionSet = true;
                vpaned2.Position = vpaned1.Parent.Allocation.Height / 2;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Allow direct access to the property grid.
        /// </summary>
        public IGridView PropertyGrid
        {
            get { return propertyGrid; }
        }

        /// <summary>
        /// Allow direct access to the profile grid.
        /// </summary>
        public IGridView ProfileGrid
        {
            get { return profileGrid; }
        }

        /// <summary>
        /// Allow direct access to the graph.
        /// </summary>
        public IGraphView Graph
        {
            get { return graph; }
        }

        /// <summary>
        /// Show the property grid if Show = true;
        /// </summary>
        public void ShowPropertyGrid(bool show)
        {
            vbox1.Visible = show;
        }

        /// <summary>
        /// Show the graph if Show = true;
        /// </summary>
        public void ShowGraph(bool show)
        {
            graph.MainWidget.Visible = show;
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
