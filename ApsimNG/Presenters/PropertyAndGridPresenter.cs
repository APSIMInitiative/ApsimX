using UserInterface.EventArguments;
using Models.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{

    /// <summary>Presenter that has a PropertyPresenter and a GridPresenter.</summary>
    class PropertyAndGridPresenter : IPresenter
    {
        /// <summary>The underlying model</summary>
        private IntellisensePresenter intellisense;
        private IPropertyAndGridView view;
        private ExplorerPresenter explorerPresenter;
        private IPresenter propertyPresenter;
        private GridPresenter gridPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            explorerPresenter = parentPresenter;
            view = v as IPropertyAndGridView;
            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            propertyPresenter = new PropertyPresenter();
            explorerPresenter.ApsimXFile.Links.Resolve(propertyPresenter);
            propertyPresenter.Attach(model, view.PropertiesView, parentPresenter);
            gridPresenter = new GridPresenter();
            gridPresenter.Attach((model as IGridTable).Tables[0], view.Grid, parentPresenter);
            gridPresenter.CellChanged += OnCellChanged;
            //view.Grid2.ContextItemsNeeded += OnContextItemsNeeded;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            gridPresenter.CellChanged -= OnCellChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            //gridPresenter.ContextItemsNeeded -= OnContextItemsNeeded;
            propertyPresenter.Detach();
            gridPresenter.Detach();
        }

        /// <summary>Invoked when a grid cell has changed.</summary>
        /// <param name="dataProvider">The provider that contains the data.</param>
        /// <param name="colIndex">The index of the column of the cell that was changed.</param>
        /// <param name="rowIndex">The index of the row of the cell that was changed.</param>
        private void OnCellChanged(ISheetDataProvider dataProvider, int colIndex, int rowIndex)
        {

        }

        /// <summary>
        /// Called when an intellisense item is selected.
        /// Inserts the item into view.Grid2 (the lower gridview).
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs e)
        {
            //view.Grid2.InsertText(e.ItemSelected);
        }

        /// <summary>
        /// Called when the view is asking for completion items.
        /// Shows the intellisense popup.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            //if (intellisense.GenerateGridCompletions(e.Code, e.Offset, tableModel as IModel, true, false, false, false, false))
            //    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
        }
    }
}
