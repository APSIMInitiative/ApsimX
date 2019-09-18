using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public class GraphPanelView : ViewBase, IGraphPanelView
    {
        private GridView propertiesGrid;
        private Notebook notebook;
        private ManagerView scriptEditor;

        public GraphPanelView(ViewBase owner) : base(owner)
        {
            notebook = new Notebook();
            notebook.Scrollable = true;

            propertiesGrid = new GridView(this);
            notebook.AppendPage(propertiesGrid.MainWidget, new Label("Properties"));

            scriptEditor = new ManagerView(this);
            notebook.AppendPage(scriptEditor.MainWidget, new Label("Script"));

            mainWidget = notebook;
        }

        /// <summary>
        /// Grid which displays the model's properties.
        /// </summary>
        public IGridView PropertiesGrid { get { return propertiesGrid; } }

        /// <summary>
        /// View which displays the manager script and its properties tab.
        /// </summary>
        public IManagerView ScriptEditor { get { return scriptEditor; } }

        /// <summary>
        /// Index of the currently selected tab.
        /// </summary>
        public int CurrentTab
        {
            get
            {
                return notebook.CurrentPage;
            }
            set
            {
                notebook.CurrentPage = value;
            }
        }

        /// <summary>
        /// Adds a new tab containing a page of graphs.
        /// </summary>
        /// <param name="graphs">Graphs to add to the new tab.</param>
        /// <param name="numCols">Number of columns into which graphs will be divided.</param>
        /// <param name="tabName">Tab label text.</param>
        public void AddTab(List<GraphView> graphs, int numCols, string tabName)
        {
            int numRows = graphs.Count / numCols;
            if (graphs.Count % numCols > 0)
                numRows++;

            Table panel = new Table((uint)numRows, (uint)numCols, true);
            for (int n = 0; n < graphs.Count; n++)
            {
                uint i = (uint)(n / numCols);
                uint j = (uint)(n % numCols);

                panel.Attach(graphs[n].MainWidget, j, j + 1, i, i + 1);
            }

            Label tabLabel = new Label(tabName);
            tabLabel.UseUnderline = false;

            notebook.AppendPage(panel, tabLabel);
            notebook.ShowAll();
        }

        /// <summary>
        /// Removes all graph tabs from the view.
        /// </summary>
        public void RemoveGraphTabs()
        {
            while (notebook.NPages > 2)
                notebook.RemovePage(notebook.NPages - 1);
        }
    }
}
