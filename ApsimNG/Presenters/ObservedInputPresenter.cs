using Gtk.Sheet;
using Models.Core;
using System.Data;
using UserInterface.Views;

namespace UserInterface.Presenters
{

    /// <summary>Presenter that has a PropertyPresenter and a GridPresenter.</summary>
    class ObservedInputPresenter : IPresenter
    {
        /// <summary>The underlying model</summary>
        private ObservedInputView view;
        private ExplorerPresenter explorerPresenter;
        private IPresenter propertyPresenter;
        private GridPresenter columnsGridPresenter;
        private GridPresenter derivedGridPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            explorerPresenter = parentPresenter;
            view = v as ObservedInputView;
            
            propertyPresenter = new PropertyPresenter();
            explorerPresenter.ApsimXFile.Links.Resolve(propertyPresenter);
            propertyPresenter.Attach(model, view.PropertyView, parentPresenter);
            
            columnsGridPresenter = new GridPresenter();
            columnsGridPresenter.Attach(new DataTableProvider(new DataTable()), view.GridViewColumns, parentPresenter);
            columnsGridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            IDataProvider columnProvider = DataProviderFactory.CreateUsingDataTableName(model, "ColumnData", (properties) => {});
            columnsGridPresenter.PopulateWithDataProvider(columnProvider);

            derivedGridPresenter = new GridPresenter();
            derivedGridPresenter.Attach(new DataTableProvider(new DataTable()), view.GridViewAdded, parentPresenter);
            derivedGridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            IDataProvider derivedProvider = DataProviderFactory.CreateUsingDataTableName(model, "CalculatedVariables", (properties) => {});
            derivedGridPresenter.PopulateWithDataProvider(derivedProvider);
            
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            propertyPresenter.Detach();
            columnsGridPresenter.Detach();
            derivedGridPresenter.Detach();
        }
    }
}
