using ApsimNG.EventArguments;
using Gtk;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UserInterface.Interfaces;
using UserInterface.Presenters;

namespace UserInterface.Views
{
    public class GraphPanelView : ViewBase, IGraphPanelView
    {
        private GridView propertiesGrid;
        private Notebook notebook;

        public GraphPanelView(ViewBase owner) : base(owner)
        {
            notebook = new Notebook();
            notebook.Scrollable = true;

            propertiesGrid = new GridView(this);
            notebook.AppendPage(propertiesGrid.MainWidget, new Label("Properties"));

            mainWidget = notebook;
        }

        /// <summary>
        /// Grid which displays the model's properties.
        /// </summary>
        public IGridView PropertiesGrid { get { return propertiesGrid; } }

        public event EventHandler<CustomDataEventArgs<IGraphView>> GraphViewCreated;

        /// <summary>
        /// Adds a new tab containing a page of graphs.
        /// </summary>
        /// <param name="tab">List of graphs and cached data.</param>
        /// <param name="numCols">Number of columns into which graphs will be divided.</param>
        public void AddTab(GraphPanelPresenter.GraphTab tab, int numCols)
        {
            // This code may be called from a background thread but
            // must be run on the main UI thread.
            Application.Invoke(delegate
            {
                try
                {
                    int numGraphs = tab.Graphs.Count;
                    int numRows = numGraphs / numCols;
                    if (numGraphs % numCols > 0)
                        numRows++;

#if NETFRAMEWORK
                    Table panel = new Table((uint)numRows, (uint)numCols, true);
#else
                    Grid panel = new Grid();
#endif
                    for (int n = 0; n < numGraphs; n++)
                    {
                        GraphPresenter presenter = new GraphPresenter();
                        presenter.SimulationFilter = new List<string>() { tab.SimulationName };
                        tab.Presenter.ApsimXFile.Links.Resolve(presenter);

                        GraphView view = new GraphView();
                        presenter.Attach(tab.Graphs[n].Graph, view, tab.Presenter, tab.Graphs[n].Cache);
                        GraphViewCreated?.Invoke(this, new CustomDataEventArgs<IGraphView>(view));

                        tab.Graphs[n].Presenter = presenter;
                        tab.Graphs[n].View = view;

#if NETFRAMEWORK
                        uint i = (uint)(n / numCols);
                        uint j = (uint)(n % numCols);
#else
                        int i = n / numCols;
                        int j = n % numCols;
#endif
                        panel.Attach(view.MainWidget, j, j + 1, i, i + 1);
                    }

                    Label tabLabel = new Label(tab.SimulationName);
                    tabLabel.UseUnderline = false;

                    notebook.AppendPage(panel, tabLabel);
                    notebook.ShowAll();
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            });
            //while (GLib.MainContext.Iteration()) ;
        }

        /// <summary>
        /// Removes all graph tabs from the view.
        /// </summary>
        public void RemoveGraphTabs()
        {
            // This code may be called from a background thread but
            // must be run on the main UI thread.
            Application.Invoke(delegate
            {
                try
                {
                    while (notebook.NPages > 1)
                        notebook.RemovePage(notebook.NPages - 1);
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            });
            //while (GLib.MainContext.Iteration()) ;
        }
    }
}
