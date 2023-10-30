using System;
using UserInterface.Views;

namespace UserInterface.Interfaces
{
    interface IReportView
    {
        /// <summary>Provides access to the variable list.</summary>
        IEditorView VariableList { get; }

        /// <summary>Provides access to the variable list.</summary>
        IEditorView EventList { get; }

        /// <summary>Provides access to the DataGrid.</summary>
        ViewBase DataStoreView { get; }

        /// <summary>Provides access to the group by edit.</summary>
        IEditView GroupByEdit { get; }

        /// <summary> Provides access to common reporting variable list.</summary>
        IListView CommonReportVariablesList { get; set; }

        /// <summary> Provides access to common reporting frequency variable list. </summary>
        IListView CommonReportFrequencyVariablesList { get; set; }

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        event EventHandler TabChanged;

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get; set; }
    }
}
