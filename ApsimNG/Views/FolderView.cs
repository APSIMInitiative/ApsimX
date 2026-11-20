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

        private Box navigation;
        private Label pageLabel;
        private Button previousPage;
        private Button nextPage;
        private List<GraphView> graphViews = new List<GraphView>();

        /// <summary>Request the preceding page of graphs.</summary>
        public event EventHandler PreviousPage;

        /// <summary>Request the next page of graphs.</summary>
        public event EventHandler NextPage;

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
            navigation = new Box(Orientation.Horizontal, 8);
            pageLabel = new Label() { Xalign = 0 };
            previousPage = new Button("Previous");
            nextPage = new Button("Next");
            previousPage.Clicked += OnPreviousPage;
            nextPage.Clicked += OnNextPage;
            navigation.PackStart(pageLabel, true, true, 8);
            navigation.PackStart(previousPage, false, false, 0);
            navigation.PackStart(nextPage, false, false, 8);
            navigation.ShowAll();
            navigation.NoShowAll = true;
            navigation.Hide();
            var contents = new Box(Orientation.Vertical, 8);
            contents.PackStart(navigation, false, false, 0);
            contents.PackStart(scroller, true, true, 0);
            mainWidget = contents;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                previousPage.Clicked -= OnPreviousPage;
                nextPage.Clicked -= OnNextPage;
                ClearControls();
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
            ClearControls();
            graphViews.AddRange(controls);
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

        /// <summary>Update page information and prevent navigation beyond either end.</summary>
        public void SetPage(int firstGraph, int count, int totalGraphs)
        {
            pageLabel.Text = $"Graphs {firstGraph + 1}-{firstGraph + count} of {totalGraphs}";
            previousPage.Sensitive = firstGraph > 0;
            nextPage.Sensitive = firstGraph + count < totalGraphs;
            navigation.Visible = totalGraphs > 0;
            scroller.Vadjustment.Value = 0;
        }

        private void OnPreviousPage(object sender, EventArgs e) => PreviousPage?.Invoke(this, e);

        private void OnNextPage(object sender, EventArgs e) => NextPage?.Invoke(this, e);

        /// <summary>Release widgets and click handlers from the preceding page.</summary>
        private void ClearControls()
        {
            foreach (GraphView graphView in graphViews)
            {
                graphView.SingleClick -= OnGraphClick;
                table.Remove(graphView.MainWidget);
                graphView.Dispose();
            }
            graphViews.Clear();
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

        /// <summary>Request the preceding page.</summary>
        event EventHandler PreviousPage;

        /// <summary>Request the next page.</summary>
        event EventHandler NextPage;

        /// <summary>Update the page controls.</summary>
        void SetPage(int firstGraph, int count, int totalGraphs);
    }
}
