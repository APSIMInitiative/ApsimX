using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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

    public partial class ProfileView : UserControl, IProfileView
    {
        public ProfileView()
        {
            InitializeComponent();
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
            PropertyGrid.Visible = Show;
            splitter1.Visible = Show;
        }

        /// <summary>
        /// Show the graph if Show = true;
        /// </summary>
        public void ShowGraph(bool Show)
        {
            splitContainer1.Panel2Collapsed = !Show;
        }

    }
}
