using System;
using Gtk;

namespace UserInterface.Views
{

    /// <summary>
    /// View for observed input that has multiple tabs for each type of validation information
    /// </summary>
    public class ObservationsView : ViewBase
    {

        private Notebook notebook = null;

        public PropertyView PropertyView = null;

        public ContainerView GridViewColumns = null;

        public ContainerView GridViewDerived = null;

        public ContainerView GridViewSimulation = null;

        public ContainerView GridViewMerge = null;

        public ContainerView GridViewZero = null;

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        public event EventHandler TabChanged;

        /// <summary>Constructor</summary>
        public ObservationsView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ObservationsView.glade");
            notebook = (Notebook)builder.GetObject("notebook1");

            PropertyView = new PropertyView(owner);
            notebook.AppendPage(PropertyView.MainWidget, new Label("Properties"));

            GridViewColumns = new ContainerView(owner);
            notebook.AppendPage(GridViewColumns.MainWidget, new Label("Columns"));
            
            GridViewDerived = new ContainerView(owner);
            notebook.AppendPage(GridViewDerived.MainWidget, new Label("Derived"));

            GridViewSimulation = new ContainerView(owner);
            notebook.AppendPage(GridViewSimulation.MainWidget, new Label("Simulations"));

            GridViewMerge = new ContainerView(owner);
            notebook.AppendPage(GridViewMerge.MainWidget, new Label("Row Merge"));
            
            GridViewZero = new ContainerView(owner);
            notebook.AppendPage(GridViewZero.MainWidget, new Label("Zeros"));

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

