// -----------------------------------------------------------------------
// <copyright file="FolderView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Gtk;

    /// <summary>
    /// Describes an interface for a folder view.
    /// </summary>
    interface IFolderView
    {
        /// <summary>Sets the user controls to show.</summary>
        void SetControls(List<GraphView> controls);

        /// <summary>Number of columns in graph panel.</summary>
        int NumCols { get; set; }
    }

    /// <summary>
    /// A view for showing 1 or more user controls.
    /// </summary>
    public class FolderView : ViewBase, IFolderView
    {
        private Table table;
        private ScrolledWindow scroller;
        private int numCols = 2;
        private List<GraphView> graphs;

        public FolderView(ViewBase owner) : base(owner)
        {
            scroller = new ScrolledWindow();
            scroller.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            table = new Table(1, 1, false);
            Viewport vport = new Viewport();
            vport.Add(table);
            vport.ShadowType = ShadowType.None;
            scroller.Add(vport);
            mainWidget = scroller;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        public int NumCols
        {
            get
            {
                return (int)table.NColumns;
            }
            set
            {
                numCols = value;
                if (graphs != null && graphs.Count > 0)
                    SetControls(graphs);
            }
        }

        /// <summary>Sets the controls to show.</summary>
        public void SetControls(List<GraphView> controls)
        {
            RemoveOldGraphs();

            graphs = controls;
            int numControls = controls.Count;
            if (numControls > 0)
            {
                uint numRows;
                if (numControls == 1)
                {
                    numCols = 1;
                    numRows = 1;
                }
                else
                    numRows = (uint)Math.Ceiling((double)numControls / numCols);
                table.Resize(numRows, (uint)numCols);
                uint col = 0;
                uint row = 0;
                foreach (GraphView gview in controls)
                {
                    if (gview != null)
                    {
                        gview.ShowControls(false);
                        gview.Refresh();
                        gview.SingleClick += OnGraphClick;
                        gview.IsLegendVisible = false;
                        gview.MainWidget.SetSizeRequest(400, 400);
                        gview.ShowControls(false);
                        table.Attach(gview.MainWidget, col, col + 1, row, row + 1);
                        gview.MainWidget.ShowAll();
                    }

                    col++;
                    if (col >= numCols)
                    {
                        col = 0;
                        row++;
                    }
                }
            }
        }

        private void RemoveOldGraphs()
        {
            if (table == null || table.Children == null)
                return;

            while (table.Children.Length > 0)
                table.Remove(table.Children[0]);
        }

        /// <summary>User has double clicked a graph.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGraphClick(object sender, EventArgs e)
        {
            GraphView graphView = sender as GraphView;
            if (graphView != null)
            {
                graphView.IsLegendVisible = !graphView.IsLegendVisible;
                graphView.Refresh();
            }
        }
    }
}
