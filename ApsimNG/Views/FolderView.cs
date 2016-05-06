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
        void SetContols(List<GraphView> controls);
    }

    /// <summary>
    /// A view for showing 1 or more user controls.
    /// </summary>
    public class FolderView : ViewBase, IFolderView
    {
        private DrawingArea drawingArea = new DrawingArea();
        public FolderView(ViewBase owner) : base(owner)
        {
            _mainWidget = drawingArea;
        }

        /// <summary>Sets the controls to show.</summary>
        public void SetContols(List<GraphView> controls)
        {
            /* TBI
            foreach (UserControl control in controls)
                control.Parent = this;

            PositionAndRefreshControls();
            */
        }

        /// <summary>Positions and refreshes all controls.</summary>
        private void PositionAndRefreshControls()
        {
            /* TBI
            this.Resize -= OnResize;
            int numControls = Controls.Count;
            if (numControls > 0)
            {
                int numCols = 2;
                int numRows;
                if (numControls == 1)
                {
                    numCols = 1;
                    numRows = 1;
                }
                else
                {
                    numCols = 2;
                    numRows = (int)Math.Ceiling((double)numControls / numCols);
                }

                int width = (Size.Width - 50) / numCols;
                int height = Size.Height / numRows - 1;
                if (height < Size.Height / 2)
                {
                    height = Size.Height / 2;
                    AutoScroll = true;
                    VScroll = true;
                }
                int controlNumber = 0;
                int col = 0;
                int row = 0;
                foreach (Control control in Controls)
                {
                    GraphView graphView = control as GraphView;
                    if (graphView != null)
                    {
                        graphView.FontSize = 10;
                        graphView.Refresh();
                        graphView.SingleClick += OnGraphClick;
                        graphView.IsLegendVisible = false;
                    }

                    control.Location = new Point(col * width, row * height);
                    control.Width = width;
                    control.Height = height;
                    controlNumber++;
                    col++;
                    if (col >= numCols)
                    {
                        col = 0;
                        row++;
                    }
                }
            }
            this.Resize += OnResize;
            */
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

        /// <summary>Called when user resized view.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnResize(object sender, EventArgs e)
        {
            PositionAndRefreshControls();
        }

    }
}
