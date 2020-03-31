using ApsimNG.EventArguments;
using Models;
using System;
using System.Collections.Generic;
using UserInterface.Presenters;
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
        /// Adds a new tab containing a page of graphs.
        /// </summary>
        /// <param name="tab">List of graphs and cached data.</param>
        /// <param name="numCols">Number of columns into which graphs will be divided.</param>
        void AddTab(GraphPanelPresenter.GraphTab tab, int numCols);

        /// <summary>
        /// Removes all graph tabs from the view.
        /// </summary>
        void RemoveGraphTabs();

        event EventHandler<CustomDataEventArgs<IGraphView>> GraphViewCreated;
    }
}
