namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
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
        private Table table;
        private ScrolledWindow scroller;

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
                table.Resize(numRows, numCols);
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
}
