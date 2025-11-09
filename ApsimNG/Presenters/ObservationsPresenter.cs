using Gtk.Sheet;
using Models.Core;
using System.Data;
using UserInterface.Views;

namespace UserInterface.Presenters
{

    /// <summary>Presenter that has a PropertyPresenter and a GridPresenter.</summary>
    class ObservationsPresenter : IPresenter
    {
        /// <summary>The underlying model</summary>
        private ObservationsView view;
        private ExplorerPresenter explorerPresenter;
        private IPresenter propertyPresenter;
        private GridPresenter columnsGridPresenter;
        private GridPresenter derivedGridPresenter;
        private GridPresenter simulationGridPresenter;
        private GridPresenter mergeGridPresenter;
        private GridPresenter zeroGridPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            explorerPresenter = parentPresenter;
            view = v as ObservationsView;

            propertyPresenter = new PropertyPresenter();
            explorerPresenter.ApsimXFile.Links.Resolve(propertyPresenter);
            propertyPresenter.Attach(model, view.PropertyView, parentPresenter);

            columnsGridPresenter = new GridPresenter();
            CreateGridTab("ColumnTable", model as IModel, columnsGridPresenter, view.GridViewColumns);

            derivedGridPresenter = new GridPresenter();
            CreateGridTab("DerivedTable", model as IModel, derivedGridPresenter, view.GridViewDerived);

            simulationGridPresenter = new GridPresenter();
            CreateGridTab("SimulationTable", model as IModel, simulationGridPresenter, view.GridViewSimulation);

            mergeGridPresenter = new GridPresenter();
            CreateGridTab("MergeTable", model as IModel, mergeGridPresenter, view.GridViewMerge);

            zeroGridPresenter = new GridPresenter();
            CreateGridTab("ZeroTable", model as IModel, zeroGridPresenter, view.GridViewZero);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            propertyPresenter.Detach();
            columnsGridPresenter.Detach();
            derivedGridPresenter.Detach();
            mergeGridPresenter.Detach();
        }

        /// <summary>
        /// Create a datatable tab on the view
        /// </summary>
        public void CreateGridTab(string property, IModel model, GridPresenter gridPresenter, ContainerView viewContainer)
        {
            gridPresenter.Attach(new DataTableProvider(new DataTable()), viewContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            IDataProvider provider = DataProviderFactory.CreateUsingDataTableName(model, property, (properties) => {});
            gridPresenter.PopulateWithDataProvider(provider);
        }
    }
}
