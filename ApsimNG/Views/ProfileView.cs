using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using Glade;
using Gtk;
///using System.Windows.Forms;
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
    }

    public class ProfileView : ViewBase, IProfileView
    {
		private GridView ProfileGrid;
		private GridView PropertyGrid;
		private GraphView Graph;
        [Widget]
        private VPaned vpaned1;
        [Widget]
        private VPaned vpaned2;
        [Widget]
        private VBox vbox1;

		public ProfileView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.ProfileView.glade", "vpaned1");
            gxml.Autoconnect(this);
            _mainWidget = vpaned1;
            PropertyGrid = new GridView(this);
            vbox1.PackStart(PropertyGrid.MainWidget, true, true, 0);
            //vpaned1.Pack1(PropertyGrid.MainWidget, true, true);
            ProfileGrid = new GridView(this);
            vpaned2.Pack1(ProfileGrid.MainWidget, true, true);
            Graph = new GraphView(this);
            vpaned2.Pack2(Graph.MainWidget, true, true);
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
    }
}
