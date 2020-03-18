using System;
using System.Collections.Generic;
using System.Data;
using UserInterface.Views;

namespace UserInterface.Interfaces
{
    /// <summary>An interface for a list view</summary>
    public interface IListView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Get or sets the datasource for the view.</summary>
        DataTable DataSource { get; set; }

        /// <summary>Gets or sets the selected row in the data source.</summary>
        int[] SelectedRows { get; set; }

        /// <summary>Sets the render details for particular cells.</summary>
        List<CellRendererDescription> CellRenderDetails { get; set; }
    }
}
