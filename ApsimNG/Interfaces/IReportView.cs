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
        /// Invoked when the user moves the vertical splitter
        /// between the two text editors.
        /// </summary>
        event EventHandler SplitterChanged;

        /// <summary>
        /// Invoked when the user moves the horizontal splitter
        /// between the Report ListViews and TextEditors.
        /// </summary>
        event EventHandler VerticalSplitterChanged;

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        event EventHandler TabChanged;

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get; set; }

        /// <summary>
        /// Position of the splitter between the variable and
        /// frequency text editors. Larger number means further
        /// down.
        /// </summary>
        int SplitterPosition { get; set; }

        /// <summary>
        /// Position of the splitter between both the common variable/event list and report variable/event text editors.
        /// Larger number means further to the start (left).
        /// </summary>
        int VerticalSplitterPosition { get; set; }

    }
}
