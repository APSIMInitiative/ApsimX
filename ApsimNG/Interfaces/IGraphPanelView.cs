using System.Collections.Generic;
using UserInterface.Views;

namespace UserInterface.Interfaces
{
    public interface IGraphPanelView
    {
        /// <summary>
        /// Grid which displays the model's properties.
        /// </summary>
        IGridView PropertiesGrid { get; }
        
        /// <summary>
        /// Index of the currently selected tab.
        /// </summary>
        int CurrentTab { get; set; }

        /// <summary>
        /// Adds a new tab containing a page of graphs.
        /// </summary>
        /// <param name="graphs">Graphs to add to the new tab.</param>
        /// <param name="numCols">Number of columns into which graphs will be divided.</param>
        /// <param name="tabName">Tab label text.</param>
        void AddTab(List<GraphView> graphView, int numCols, string tabName);

        /// <summary>
        /// Removes all graph tabs from the view.
        /// </summary>
        void RemoveGraphTabs();
    }
}
