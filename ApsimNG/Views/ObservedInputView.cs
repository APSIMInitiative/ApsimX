using System;
using Gtk;

namespace UserInterface.Views
{

    /// <summary>
    /// View for observed input that has multiple tabs for each type of validation information
    /// </summary>
    public class ObservedInputView : ViewBase
    {

        private Notebook notebook = null;

        public PropertyView PropertyView = null;

        public ContainerView GridViewColumns = null;

        public ContainerView GridViewAdded = null;

        public ContainerView GridViewDataTypeError = null;

        public ContainerView GridViewDataError = null;

        public ContainerView GridViewZeroValue = null;

        public ContainerView GridViewCalculatedErrors = null;

        public ContainerView GridViewSimulationNameErrors = null;

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        public event EventHandler TabChanged;

        /// <summary>Constructor</summary>
        public ObservedInputView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ObservedInputView.glade");
            notebook = (Notebook)builder.GetObject("notebook1");

            PropertyView = new PropertyView(owner);
            notebook.AppendPage(PropertyView.MainWidget, new Label("Properties"));

            GridViewColumns = new ContainerView(owner);
            notebook.AppendPage(GridViewColumns.MainWidget, new Label("Columns"));

            GridViewAdded = new ContainerView(owner);
            notebook.AppendPage(GridViewAdded.MainWidget, new Label("Derived"));

            GridViewDataTypeError = new ContainerView(owner);
            notebook.AppendPage(GridViewDataTypeError.MainWidget, new Label("Data Type Error"));

            GridViewDataError = new ContainerView(owner);
            notebook.AppendPage(GridViewDataError.MainWidget, new Label("Data Error"));

            GridViewZeroValue = new ContainerView(owner);
            notebook.AppendPage(GridViewZeroValue.MainWidget, new Label("Zero Value"));

            GridViewCalculatedErrors = new ContainerView(owner);
            notebook.AppendPage(GridViewCalculatedErrors.MainWidget, new Label("Calculated Error"));

            GridViewSimulationNameErrors = new ContainerView(owner);
            notebook.AppendPage(GridViewSimulationNameErrors.MainWidget, new Label("Sim Name Error"));

            mainWidget = notebook;
            notebook.SwitchPage += OnSwitchPage;
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

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            try
            {
                notebook.SwitchPage -= OnSwitchPage;
                notebook.Dispose();

                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        public int TabIndex
        {
            get { return notebook.CurrentPage; }
            set { notebook.CurrentPage = value; }
        }
    }

}

