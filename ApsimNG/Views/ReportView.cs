using System;
using System.Drawing;
using GLib;
using Gtk;
using UserInterface.Interfaces;
using Utility;

namespace UserInterface.Views
{

    /// <summary>
    /// View for a report component that includes new report variable and report frequency UI sections.
    /// </summary>
    public class ReportView : ViewBase, IReportView
    {

        private Notebook notebook1 = null;
        private Paned reportVariablesVPaned = null;
        private Paned reportFrequencyVPaned = null;
        private Box variablesBox = null;
        private Box frequencyBox = null;
        private Box commonVariablesBox = null;
        private Box commonFrequencyBox = null;
        private Box dataBox = null;

        private IEditorView variableEditor;
        private IEditorView frequencyEditor;
        private IListView commonReportVariableList;
        private IListView commonReportFrequencyVariableList;
        private ViewBase dataStoreView1;
        private Paned panel;
        private EditView groupByEdit;
        private Button submitButton;

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        public event EventHandler TabChanged;

        /// <summary>Constructor</summary>
        public ReportView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ReportView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            reportVariablesVPaned = (Paned)builder.GetObject("reportVariablesBox");
            reportFrequencyVPaned = (Paned)builder.GetObject("reportFrequencyBox");
            variablesBox = (Box)builder.GetObject("variablesBox");
            frequencyBox = (Box)builder.GetObject("frequencyBox");
            commonVariablesBox = (Box)builder.GetObject("commonVariablesBox");
            commonFrequencyBox = (Box)builder.GetObject("commonFrequencyBox");
            dataBox = (Box)builder.GetObject("dataBox");
            submitButton = (Button)builder.GetObject("submitBtn");


            panel = (Paned)builder.GetObject("vpaned1");
            panel.Events |= Gdk.EventMask.PropertyChangeMask;

            groupByEdit = new EditView(owner,
                                      (Entry)builder.GetObject("groupByEdit"));

            mainWidget = notebook1;
            notebook1.SwitchPage += OnSwitchPage;

            reportVariablesVPaned.AddNotification(OnVariablesPanePropertyNotified);
            reportFrequencyVPaned.AddNotification(OnFrequencyPanePropertyNotified);

            variableEditor = new EditorView(this);
            variableEditor.StyleChanged += OnStyleChanged;
            variablesBox.PackStart((variableEditor as ViewBase).MainWidget, true, true, 0);

            frequencyEditor = new EditorView(this);
            frequencyEditor.StyleChanged += OnStyleChanged;
            frequencyBox.PackStart((frequencyEditor as ViewBase).MainWidget, true, true, 0);

            commonReportVariableList = new ListView(this, new Gtk.TreeView(), new Gtk.Menu(), (EditorView)variableEditor);
            commonReportVariableList.DoubleClicked += OnCommonReportVariableListDoubleClicked;
            commonVariablesBox.PackStart((commonReportVariableList as ViewBase).MainWidget, true, true, 0);

            commonReportFrequencyVariableList = new ListView(this, new Gtk.TreeView(), new Gtk.Menu(), (EditorView)frequencyEditor, submitButton);
            commonReportFrequencyVariableList.DoubleClicked += OnCommonReportFrequencyVariableListDoubleClicked;
            commonFrequencyBox.PackStart((commonReportFrequencyVariableList as ViewBase).MainWidget, true, true, 0);

            Rectangle bounds = GtkUtilities.GetBorderOfRightHandView(owner);
            double? horizontalSplitter = Configuration.Settings.ReportSplitterPosition / 100.0;
            int horizontalPos = (int)Math.Round(bounds.Width * 0.7);
            if (horizontalSplitter != null)
                if (horizontalSplitter > 0.1 && horizontalSplitter < 0.9)
                    horizontalPos = (int)(bounds.Width * horizontalSplitter);
            reportVariablesVPaned.Position = horizontalPos;
            reportFrequencyVPaned.Position = horizontalPos;

            double? verticalSplitter = Configuration.Settings.ReportSplitterVerticalPosition / 100.0;
            int verticalPos = (int)Math.Round(bounds.Height * 0.7);
            if (verticalSplitter != null)
                if (verticalSplitter > 0.1 && verticalSplitter < 0.9)
                    verticalPos = (int)(bounds.Height * verticalSplitter);
            panel.Position = verticalPos;

            dataStoreView1 = new ViewBase(this, "ApsimNG.Resources.Glade.DataStoreView.glade");
            dataBox.Add(dataStoreView1.MainWidget);
            mainWidget.Destroyed += _mainWidget_Destroyed;

        }

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        /// <remarks>
        /// Note that there is no [ConnectBefore] attribute,
        /// so at the time this is called, this.TabIndex
        /// will return the correct (updated) value.
        /// </remarks>
        private void OnSwitchPage(object sender, SwitchPageArgs args)
        {
            try
            {
                TabChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary> Updates The position of either common variable listView.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnFrequencyPanePropertyNotified(object sender, NotifyArgs args)
        {
            this.reportVariablesVPaned.Position = reportFrequencyVPaned.Position;
            if (args.Property == "position")
            {
                Rectangle bounds = GtkUtilities.GetBorderOfRightHandView(owner);
                double percentage = (double)reportVariablesVPaned.Position / (double)bounds.Width;
                Configuration.Settings.ReportSplitterPosition = (int)(percentage * 100);
                Configuration.Settings.Save();
            }

        }

        /// <summary> Updates The position of either common variable listView.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnVariablesPanePropertyNotified(object sender, NotifyArgs args)
        {
            this.reportFrequencyVPaned.Position = reportVariablesVPaned.Position;
            if (args.Property == "position")
            {
                Rectangle bounds = GtkUtilities.GetBorderOfRightHandView(owner);
                double percentage = (double)reportFrequencyVPaned.Position / (double)bounds.Width;
                Configuration.Settings.ReportSplitterPosition = (int)(percentage * 100);
                Configuration.Settings.Save();
            }
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
                notebook1.SwitchPage -= OnSwitchPage;
                frequencyEditor.StyleChanged -= OnStyleChanged;
                groupByEdit.Dispose();
                (variableEditor as ViewBase).Dispose();
                variableEditor = null;
                (frequencyEditor as ViewBase).Dispose();
                frequencyEditor = null;
                dataStoreView1.Dispose();
                dataStoreView1 = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnCommonReportVariableListDoubleClicked(object sender, EventArgs e)
        {

        }

        private void OnCommonReportFrequencyVariableListDoubleClicked(object sender, EventArgs e)
        {

        }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView VariableList { get { return variableEditor; } }

        /// <summary>Provides access to the variable list.</summary>
        public IEditorView EventList { get { return frequencyEditor; } }

        /// <summary>Provides access to the group by edit.</summary>
        public IEditView GroupByEdit { get { return groupByEdit; } }

        public IListView CommonReportVariablesList { get { return commonReportVariableList; } set { commonReportVariableList = value; } }

        public IListView CommonReportFrequencyVariablesList { get { return commonReportFrequencyVariableList; } set { commonReportFrequencyVariableList = value; } }

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

