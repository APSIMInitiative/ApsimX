using System;
using GLib;
using Gtk;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

    /// <summary>
    /// View for a report component that includes new report variable and report frequency UI sections.
    /// </summary>
    public class NewReportView : ViewBase, IReportView
    {

        private Notebook notebook1 = null;
        private Paned reportVariablesBox = null;
        private Paned reportFrequencyBox = null;
        private Box variablesBox = null;
        private Box commonVariablesBox = null;
        private Box frequencyBox = null;
        private Box commonFrequencyBox = null;
        private Alignment alignment1 = null;

        private IEditorView variableEditor;
        private IEditorView frequencyEditor;
        private IListView commonReportVariableList;
        private IListView commonReportFrequencyVariableList;
        private ViewBase dataStoreView1;
        private VPaned panel;
        private EditView groupByEdit;

        /// <summary>
        /// Invoked when the user moves the vertical splitter
        /// between the two text editors.
        /// </summary>
        public event EventHandler SplitterChanged;

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        public event EventHandler TabChanged;

        /// <summary>Constructor</summary>
        public NewReportView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.NewReportView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            reportVariablesBox = (Paned)builder.GetObject("reportVariablesBox");
            variablesBox = (Box)builder.GetObject("variablesBox");
            commonVariablesBox = (Box)builder.GetObject("commonVariablesBox");
            reportFrequencyBox = (Paned)builder.GetObject("reportFrequencyBox");
            frequencyBox = (Box)builder.GetObject("frequencyBox");
            commonFrequencyBox = (Box)builder.GetObject("commonFrequencyBox");
            alignment1 = (Alignment)builder.GetObject("alignment1");

            panel = (VPaned)builder.GetObject("vpaned1");
            panel.Events |= Gdk.EventMask.PropertyChangeMask;
            panel.AddNotification(OnPropertyNotified);

            groupByEdit = new EditView(owner,
                                      (Entry)builder.GetObject("groupByEdit"));

            mainWidget = notebook1;
            notebook1.SwitchPage += OnSwitchPage;

            reportVariablesBox.AddNotification(OnVariablesPanePropertyNotified);
            reportFrequencyBox.AddNotification(OnFrequencyPanePropertyNotified);

            variableEditor = new EditorView(this);
            variableEditor.StyleChanged += OnStyleChanged;
            variablesBox.PackStart((variableEditor as ViewBase).MainWidget, true, true, 0);

            frequencyEditor = new EditorView(this);
            frequencyEditor.StyleChanged += OnStyleChanged;
            frequencyBox.PackStart((frequencyEditor as ViewBase).MainWidget, true, true, 0);

            commonReportVariableList = new ListView(this, new Gtk.TreeView(), new Gtk.Menu(), (EditorView)variableEditor);
            commonReportVariableList.DoubleClicked += OnCommonReportVariableListDoubleClicked;
            commonVariablesBox.PackStart((commonReportVariableList as ViewBase).MainWidget, true, true, 0);

            commonReportFrequencyVariableList = new ListView(this, new Gtk.TreeView(), new Gtk.Menu(), (EditorView)frequencyEditor);
            commonReportFrequencyVariableList.DoubleClicked += OnCommonReportFrequencyVariableListDoubleClicked;
            commonFrequencyBox.PackStart((commonReportFrequencyVariableList as ViewBase).MainWidget, true, true, 0);

            // Set the position of the divider to 80% of the width of the Paned gtk object.
            reportVariablesBox.Position = (int)Math.Round(this.owner.MainWidget.AllocatedWidth * 0.7);
            reportFrequencyBox.Position = (int)Math.Round(this.owner.MainWidget.AllocatedWidth * 0.7);

            dataStoreView1 = new ViewBase(this, "ApsimNG.Resources.Glade.DataStoreView.glade");
            alignment1.Add(dataStoreView1.MainWidget);
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
            this.reportVariablesBox.Position = reportFrequencyBox.Position;
        }

        /// <summary> Updates The position of either common variable listView.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnVariablesPanePropertyNotified(object sender, NotifyArgs args)
        {
            this.reportFrequencyBox.Position = reportVariablesBox.Position;
        }

        /// <summary>
        /// Called whenever a property of vpaned1 is modified.
        /// We use this to trap when the user moves the handle
        /// which separates the two text editors. Unfortunately,
        /// this is called many times per second as long as the
        /// user is dragging the handle. I couldn't find a better
        /// event to trap - the MoveHandle event only fires when
        /// the handle is moved via keypresses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPropertyNotified(object sender, NotifyArgs args)
        {
            try
            {
                if (args.Property == "position")
                    SplitterChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
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
                panel.RemoveNotification(OnPropertyNotified);
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

        /// <summary>
        /// Position of the splitter between the variable and
        /// frequency text editors. Larger number means further
        /// down.
        /// </summary>
        public int SplitterPosition
        {
            get
            {
                return panel.Position;
            }
            set
            {
                panel.Position = value;
            }
        }

    }

}

