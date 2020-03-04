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
        ViewBase DataStoreView { get; }

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

        private EditorView variableEditor;
        private EditorView frequencyEditor;
        private ViewBase dataStoreView1;

        /// <summary>Constructor</summary>
        public ReportView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ReportView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            vbox1 = (VBox)builder.GetObject("vbox1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            alignment1 = (Alignment)builder.GetObject("alignment1");
            mainWidget = notebook1;

            variableEditor = new EditorView(this);
            variableEditor.StyleChanged += OnStyleChanged;
            vbox1.PackStart(variableEditor.MainWidget, true, true, 0);

            frequencyEditor = new EditorView(this);
            frequencyEditor.StyleChanged += OnStyleChanged;
            vbox2.PackStart(frequencyEditor.MainWidget, true, true, 0);

            dataStoreView1 = new ViewBase(this, "ApsimNG.Resources.Glade.DataStoreView.glade");
            alignment1.Add(dataStoreView1.MainWidget);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// Invoked when the user changes the colour scheme (style) of one of
        /// the text editors. Refreshes both text editors, so that the new
        /// both use the new style.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnStyleChanged(object sender, EventArgs e)
        {
            try
            {
                variableEditor?.Refresh();
                frequencyEditor?.Refresh();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            try
            {
                variableEditor.StyleChanged -= OnStyleChanged;
                frequencyEditor.StyleChanged -= OnStyleChanged;
                variableEditor.MainWidget.Destroy();
                variableEditor = null;
                frequencyEditor.MainWidget.Destroy();
                frequencyEditor = null;
                dataStoreView1.MainWidget.Destroy();
                dataStoreView1 = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView VariableList { get { return variableEditor; } }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView EventList { get { return frequencyEditor; } }

        /// <summary>Provides access to the DataGrid.</summary>
        public ViewBase DataStoreView { get { return dataStoreView1; } }

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
