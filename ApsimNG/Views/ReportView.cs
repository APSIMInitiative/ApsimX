// -----------------------------------------------------------------------
// <copyright file="ReportView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Views
{
    using System;
    using Gtk;
    using EventArguments;

    interface IReportView
    {
        /// <summary>Provides access to the variable list.</summary>
        IEditorView VariableList { get; }

        /// <summary>Provides access to the variable list.</summary>
        IEditorView EventList { get; }

        /// <summary>Provides access to the DataGrid.</summary>
        IDataStoreView DataStoreView { get; }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get; set; }
    }

    public class ReportView : ViewBase, IReportView
    {
        private Notebook notebook1 = null;
        private VBox vbox1 = null;
        private VBox vbox2 = null;
        private Alignment alignment1 = null;

        private EditorView VariableEditor;
        private EditorView FrequencyEditor;
        private DataStoreView dataStoreView1;

        /// <summary>Constructor</summary>
        public ReportView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ReportView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            vbox1 = (VBox)builder.GetObject("vbox1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            alignment1 = (Alignment)builder.GetObject("alignment1");
            _mainWidget = notebook1;

            VariableEditor = new EditorView(this);
            vbox1.PackStart(VariableEditor.MainWidget, true, true, 0);

            FrequencyEditor = new EditorView(this);
            vbox2.PackStart(FrequencyEditor.MainWidget, true, true, 0);

            dataStoreView1 = new DataStoreView(this);
            alignment1.Add(dataStoreView1.MainWidget);
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            VariableEditor.MainWidget.Destroy();
            VariableEditor = null;
            FrequencyEditor.MainWidget.Destroy();
            FrequencyEditor = null;
            dataStoreView1.MainWidget.Destroy();
            dataStoreView1 = null;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView VariableList { get { return VariableEditor; } }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView EventList { get { return FrequencyEditor; } }

        /// <summary>Provides access to the DataGrid.</summary>
        public IDataStoreView DataStoreView { get { return dataStoreView1; } }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        public int TabIndex
        {
            get { return notebook1.CurrentPage; }
            set { notebook1.CurrentPage = value; }
        }

    }
}
