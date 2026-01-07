using System;
using System.Drawing;
using Gtk;
using Utility;

namespace UserInterface.Views
{

    /// <summary>
    /// View for observed input that has multiple tabs for each type of validation information
    /// </summary>
    public class ObservationsView : ViewBase
    {

        private Notebook notebook = null;

        private Label instructions;

        public PropertyView PropertyView = null;
        private Label propertyLabel = null;

        public ContainerView GridViewColumns = null;
        private Label columnsLabel = null;

        public ContainerView GridViewDerived = null;
        private Label derivedLabel = null;

        public ContainerView GridViewSimulation = null;
        private Label simulationLabel = null;

        public ContainerView GridViewMerge = null;
        private Label mergeLabel = null;

        public ContainerView GridViewZero = null;
        private Label zeroLabel = null;

        /// <summary>
        /// Invoked when the selected tab is changed.
        /// </summary>
        public event EventHandler TabChanged;

        /// <summary>Constructor</summary>
        public ObservationsView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.ObservationsView.glade");

            instructions = (Label)builder.GetObject("label1");
            instructions.Wrap = true;
            notebook = (Notebook)builder.GetObject("notebook1");

            PropertyView = new PropertyView(owner);
            propertyLabel = new Label("Properties");
            notebook.AppendPage(PropertyView.MainWidget, propertyLabel);

            GridViewColumns = new ContainerView(owner);
            columnsLabel = new Label("Columns");
            notebook.AppendPage(GridViewColumns.MainWidget, columnsLabel);

            GridViewDerived = new ContainerView(owner);
            derivedLabel = new Label("Derived");
            notebook.AppendPage(GridViewDerived.MainWidget, derivedLabel);

            GridViewSimulation = new ContainerView(owner);
            simulationLabel = new Label("Simulations");
            notebook.AppendPage(GridViewSimulation.MainWidget, simulationLabel);

            GridViewMerge = new ContainerView(owner);
            mergeLabel = new Label("Row Merge");
            notebook.AppendPage(GridViewMerge.MainWidget, mergeLabel);

            GridViewZero = new ContainerView(owner);
            zeroLabel = new Label("Zeros");
            notebook.AppendPage(GridViewZero.MainWidget, zeroLabel);

            Rectangle bounds = GtkUtilities.GetBorderOfRightHandView(owner);
            Paned paned = (Paned)builder.GetObject("vpaned1");
            paned = (Paned)builder.GetObject("vpaned1");
            paned.Position = (int)Math.Round(bounds.Width * 0.6);

            mainWidget = (Widget)builder.GetObject("viewport1");
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

                PropertyView.Dispose();
                propertyLabel.Dispose();
                GridViewColumns.Dispose();
                derivedLabel.Dispose();
                GridViewSimulation.Dispose();
                simulationLabel.Dispose();
                GridViewMerge.Dispose();
                mergeLabel.Dispose();
                GridViewZero.Dispose();
                zeroLabel.Dispose();

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

        public void SetInstructions(string text)
        {
            this.instructions.Text = text;
        }
    }

}

