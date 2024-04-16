using System;
using System.Collections.Generic;
using Gtk;

namespace UserInterface.Views
{

    /// <summary>
    /// A view for showing 1 or more user controls.
    /// </summary>
    public class FolderView : ViewBase, IFolderView
    {

        private Grid table = new Grid();

        private ScrolledWindow scroller;

        public FolderView(ViewBase owner) : base(owner)
        {
            scroller = new ScrolledWindow();
            scroller.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            table.RowHomogeneous = true;
            table.ColumnHomogeneous = true;

            Viewport vport = new Viewport();
            vport.Add(table);
            vport.ShadowType = ShadowType.None;
            scroller.Add(vport);
            mainWidget = scroller;
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

        /// <summary>Sets the controls to show.</summary>
        /// <remarks>This should be reworked once we ditch gtk2 support.</remarks>
        public void SetContols(List<GraphView> controls)
        {
            int numControls = controls.Count;
            if (numControls > 0)
            {
                uint numCols = 2;
                uint numRows;
                if (numControls == 1)
                {
                    numCols = 1;
                    numRows = 1;
                }
                else
                {
                    numCols = 2;
                    numRows = (uint)Math.Ceiling((double)numControls / numCols);
                }

                // GtkGrid automatically resizes I think. Need to test this
                int col = 0;
                int row = 0;

                foreach (GraphView gview in controls)
                {
                    if (gview != null)
                    {
                        gview.ShowControls(false);
                        gview.Refresh();
                        gview.SingleClick += OnGraphClick;
                        gview.ShowControls(false);
                        gview.MainWidget.SetSizeRequest(400, 400);

                        table.Attach(gview.MainWidget, col, row, 1, 1);

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

        /// <summary>User has double clicked a graph.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGraphClick(object sender, EventArgs e)
        {
            try
            {
                GraphView graphView = sender as GraphView;
                if (graphView != null)
                {
                    graphView.IsLegendVisible = !graphView.IsLegendVisible;
                    graphView.Refresh();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    /// <summary>
    /// Describes an interface for a folder view.
    /// </summary>
    interface IFolderView
    {
        /// <summary>Sets the user controls to show.</summary>
        void SetContols(List<GraphView> controls);
    }
}
